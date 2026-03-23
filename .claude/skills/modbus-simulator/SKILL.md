---
name: modbus-simulator
description: |
  用于开发 GS215 EMS 设备故障模拟器的编码技能，基于 C# WPF (.NET 8) + CommunityToolkit.Mvvm + NModbus4 技术栈。
  当用户提到"编写Modbus代码"、"修改模拟器"、"添加设备面板"、"寄存器映射"、"故障注入"、"告警注入"、
  "ViewModel"、"RegisterBank"、"NModbus4"、"Slave启停"、"配置导入导出"等关键词时，必须使用此技能。
  即使用户只说"帮我写PCS的ViewModel"或"怎么实现float32写寄存器"也要触发此技能。
  输出物包含完整可编译的 C# 代码文件 + 说明书（MARKDOWN格式）。
---

# GS215 EMS 设备故障模拟器 编码技能

## 项目概览

- **目标**：Modbus Slave 模拟器，供 GS215 EMS 通过 Modbus TCP/RTU 轮询，无需真实硬件即可联调测试
- **设计文档**：`E:/MoubusSimulator/设备故障模拟器_设计文档.md`（完整字段定义、寄存器地址、UI设计，编码前必读）
- **项目根目录**：`E:/MoubusSimulator/`

## 技术栈

| 层 | 技术 | 版本 |
|---|---|---|
| UI | WPF .NET 8 | - |
| MVVM | CommunityToolkit.Mvvm | 8.3.2（使用 Source Generator） |
| Modbus | NModbus4 | 2.1.0 |
| 序列化 | System.Text.Json | 8.0.5 |
| 日志 | NLog + NLog.Extensions.Logging | 5.x |

## 项目目录结构

```
SimulatorApp/
├── App.xaml / App.xaml.cs          # DI 容器注册（Microsoft.Extensions.DI）
├── Models/                          # 纯数据模型，无UI依赖
│   ├── DeviceModelBase.cs           # 抽象基类：DeviceName/BaseAddress/SlaveId/ToRegisters()/FromRegisters()
│   └── {Device}/
│       ├── {Device}Model.cs         # 遥测字段 CLR 属性
│       └── {Device}FaultBits.cs     # [Flags] 枚举（故障/告警位定义）
├── ViewModels/
│   ├── MainViewModel.cs             # 连接参数/启停/日志/导入导出/10个设备VM实例
│   ├── DeviceViewModelBase.cs       # IsExpanded / FlushToRegisters()
│   └── {Device}ViewModel.cs        # [ObservableProperty] 字段 + AlarmItem列表
├── Views/
│   ├── MainWindow.xaml
│   ├── Controls/
│   │   ├── AlarmCheckList.xaml      # WrapPanel+CheckBox 多选告警控件
│   │   ├── FieldRow.xaml            # 标签+输入框+单位 通用行（140+100+50px）
│   │   └── DeviceExpander.xaml
│   └── Panels/                      # 各设备字段面板 XAML
├── Services/
│   ├── RegisterBank.cs              # 65536 holding regs，lock 保护
│   ├── IModbusSlaveService.cs / ModbusSlaveService.cs  # TCP/RTU Slave
│   └── IRegisterMapService.cs / RegisterMapService.cs  # Model→寄存器刷新 + JSON快照
├── Converters/
│   ├── BoolToColorConverter.cs
│   └── FlagsToListConverter.cs
├── Helpers/
│   └── FloatRegisterHelper.cs       # float32 ↔ 两个 uint16（AB CD 字序）
└── Logging/
    ├── AppLogger.cs
    └── nlog.config
```

## 编码规范与核心模式

### 1. ViewModel 字段声明（CommunityToolkit.Mvvm Source Generator）

```csharp
// 继承 DeviceViewModelBase，使用 [ObservableProperty] 自动生成属性
public partial class PcsViewModel : DeviceViewModelBase
{
    [ObservableProperty]
    private double _dcVoltage = 750.0;

    // 属性变更时自动刷新寄存器
    partial void OnDcVoltageChanged(double value) => FlushToRegisters();

    // 枚举状态下拉
    [ObservableProperty]
    private int _operatingState = 0;  // 0=待机/1=自检/2=运行/3=告警/4=故障
    partial void OnOperatingStateChanged(int value) => FlushToRegisters();

    // 告警多选（bitmask）
    public ObservableCollection<AlarmItem> Fault1Items { get; } = new()
    {
        new AlarmItem("直流极性反接", 1 << 0),
        new AlarmItem("从MCU故障",   1 << 1),
        // ...
    };
}
```

### 2. float32 寄存器写入（AB CD 字序）

```csharp
// FloatRegisterHelper.cs
public static (ushort high, ushort low) ToRegisters(float value)
{
    byte[] bytes = BitConverter.GetBytes(value); // Little-Endian [D C B A]
    ushort high = (ushort)((bytes[3] << 8) | bytes[2]); // AB → offset+0
    ushort low  = (ushort)((bytes[1] << 8) | bytes[0]); // CD → offset+1
    return (high, low);
}

public static float FromRegisters(ushort high, ushort low)
{
    byte[] bytes = {
        (byte)(low  & 0xFF), (byte)(low  >> 8),   // D C
        (byte)(high & 0xFF), (byte)(high >> 8)    // B A
    };
    return BitConverter.ToSingle(bytes, 0);
}
```

### 3. RegisterBank 线程安全写入

```csharp
public class RegisterBank
{
    private readonly ushort[] _regs = new ushort[65536];
    private readonly object _lock = new();

    public void Write(int address, ushort value)
    {
        lock (_lock) { _regs[address] = value; }
    }

    public void WriteFloat32(int address, float value)
    {
        var (hi, lo) = FloatRegisterHelper.ToRegisters(value);
        lock (_lock) { _regs[address] = hi; _regs[address + 1] = lo; }
    }

    public ushort Read(int address)
    {
        lock (_lock) { return _regs[address]; }
    }
}
```

### 4. bitmask 告警字合并

```csharp
// AlarmItem 的 IsChecked 变化时重新计算合并值
private ushort CalculateFault1()
    => (ushort)Fault1Items.Where(x => x.IsChecked).Aggregate(0, (acc, x) => acc | x.BitMask);
```

### 5. Model.ToRegisters() 写入示例（PCS，起始地址 7296）

```csharp
public override void ToRegisters(RegisterBank bank)
{
    int b = BaseAddress; // 7296
    bank.WriteFloat32(b + 0,  (float)(DcVoltage   / 0.1));   // scale 0.1
    bank.WriteFloat32(b + 2,  (float)(DcCurrent   / 0.1));
    bank.Write(b + 100, (ushort)OperatingState);
    bank.Write(b + 101, (ushort)ChargingState);
    bank.Write(b + 200, CalculateFault1());
    bank.Write(b + 201, CalculateAlarm1());
}
```

### 6. Modbus Slave 启动（TCP，NModbus4）

```csharp
public async Task StartTcpAsync(int port, byte slaveId)
{
    _tcpListener = new TcpListener(IPAddress.Any, port);
    _tcpListener.Start();
    _factory = new ModbusFactory();
    _network = _factory.CreateSlaveNetwork(_tcpListener);
    var slave = _factory.CreateSlave(slaveId, _dataStore); // _dataStore 适配 RegisterBank
    _network.AddSlave(slave);
    await _network.ListenAsync(_cts.Token);
}
```

### 7. 输入校验规则

| 字段类型 | 范围 |
|---|---|
| 电压(V) | 0～1200 |
| 电流(A) | -3000～3000 |
| SOC/SOH(%) | 0～100 |
| 温度(℃) | -50～200 |
| 频率(Hz) | 0～70 |
| 端口号 | 1～65535 |
| SlaveID | 1～247 |

超量程时**自动截断**到边界值，并在日志追加 WARN。

## 设备寄存器地址速查

| 设备 | 起始地址 | 寄存器数 |
|---|---|---|
| PCS 储能变流器 | 7296 | 345 |
| BMS 电池管理系统 | 23680 | 110 |
| MPPT 光伏 | 40064 | 157 |
| STS 转换开关（仪表） | 1408 | 161 |
| STS 控制IO卡 | 1920 | 194 |
| 空调 | 52352 | 73 |
| 除湿机 | 53248 | 32 |
| 柴发 | 53504 | 64 |
| 气体检测 | 53760 | 16 |
| 外部电表 | 384 | 178 |
| 储能电表 | 48256 | 178 |
| DI/DO 动环控制器 | 60544 | 41 |

> 完整字段定义（中文名/变量名/初始值/比例系数/故障位）见设计文档第8节和第9节。

## 日志规范

```
// 界面日志格式
"HH:mm:ss.fff  [INFO/WARN/ERROR]  消息"
// Modbus 请求日志
"14:32:01.123  [INFO]  FC03  addr=7296  qty=345  slaveId=1  来源=192.168.1.100"
// 文件日志（NLog）：logs/app-yyyy-MM-dd.log，保留30天
```

## 配置快照 JSON 结构

```json
{
  "SlaveId": 1,
  "Protocol": "TCP",
  "Port": 502,
  "Devices": {
    "PCS": { "DcVoltage": 750.0, "OperatingState": 0, "Fault1": 0 },
    "BMS": { "Soc": 80.0, "SystemState": 0, "Alarm1": 0 }
  }
}
```

## 输出要求

每次编写或修改代码时，必须同时输出：

1. **完整 C# 代码**（可直接编译，含命名空间、using、类结构）
2. **说明书（Markdown）**，包含：
   - 本次新增/修改了哪些文件
   - 关键逻辑说明（寄存器映射、比例系数、字序）
   - 如何接入已有代码（DI注册、ViewModel绑定等）
   - 注意事项（线程安全、异常处理）

> 如需编写某设备的完整代码（Model+ViewModel+XAML），先读设计文档第8节对应设备的字段定义，确保变量名、初始值、比例系数与文档一致。
