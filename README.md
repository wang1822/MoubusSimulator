# GS215 EMS Modbus 模拟器 · 主站应用

> 面向 GS215 EMS 系统联调测试的 Modbus 主站客户端，无需真实硬件即可完成遥测读取、遥控写入与 API 比对验证。

---

## 功能概览

| 功能 | 说明 |
|------|------|
| **双协议连接** | 支持 Modbus TCP（以太网）与 Modbus RTU（串口）两种接入方式 |
| **多站点管理** | 创建任意数量的从站配置，选中后自动回填参数并加载寄存器表 |
| **持续轮询** | 按地址段分组发送 FC03 请求，轮询间隔可自定义（最小 1 ms） |
| **遥测显示** | 实时展示原始寄存器值与按比例系数换算后的物理值，支持三种解析模式 |
| **遥控写入** | 表格内联编辑，失焦自动触发 FC16 写入，写入值持久化到数据库 |
| **按需读取** | 遥控行支持单独触发 FC03，即时读回设备当前状态 |
| **API 比对验证** | 调用指定 HTTP 接口获取预期值，与实测物理值比较并打绿点标记 |
| **Excel 批量导入** | 从 Excel 文件或剪贴板（TSV）批量导入寄存器配置 |
| **数据库持久化** | 站点配置、寄存器表、写入历史、验证状态均存储于 SQL Server |
| **实时日志** | 面板内嵌日志窗口，所有操作与异常实时记录并支持导出 |
| **搜索定位** | 支持按中文名 / 英文名搜索寄存器，以及快速跳转下一个未通过验证的行 |

---

## 技术栈

| 层 | 技术 |
|---|---|
| UI 框架 | WPF .NET 8 |
| MVVM | CommunityToolkit.Mvvm 8.x（Source Generator） |
| Modbus 协议 | NModbus4 2.1.0（TCP / RTU） |
| 数据库 ORM | Dapper + SQL Server（Microsoft.Data.SqlClient） |
| Excel 解析 | ClosedXML |
| 日志 | NLog + NLog.Extensions.Logging |
| 测试 | xUnit |

---

## 架构总览

```
Views (.xaml)
  └── MasterPanel · SaveStationDialog · PasswordDialog
        │  DataContext / Command Binding
        ▼
ViewModel (MasterViewModel)
  ├── 站点管理（Stations / SelectedStation）
  ├── 连接参数（Protocol / Host / Port / ComPort / SlaveId）
  ├── 轮询循环（PollLoopAsync → PollGroup × FC03）
  ├── 显示数据（TelemeterRows / ControlRows）
  └── 验证状态（IsVerified / VerifyFailCount）
        │
        ├── IMasterService ──→ TcpMasterService（NModbus4 TCP）
        │                └──→ RtuMasterService（NModbus4 RTU）
        │
        ├── IMasterDbService ─→ MasterDbService（SQL Server）
        │
        ├── MasterExcelHelper（ClosedXML 导入）
        │
        └── ApiVerifyService（HTTP 比对）
```

---

## 快速开始

### 环境要求

- Windows 10 / 11
- .NET 8 SDK
- SQL Server 2016+（含 LocalDB / Express）
- Visual Studio 2022 或 Rider

### 构建运行

```bash
git clone https://github.com/wang1822/MoubusSimulator.git
cd MoubusSimulator
dotnet build SimulatorApp/SimulatorApp.csproj
dotnet run --project SimulatorApp/SimulatorApp.csproj
```

### 首次配置

1. **填写数据库连接字符串**，点击「连接数据库」（首次连接自动建表）
2. **点击「新建站点」**，填写从站 IP / 端口（TCP）或串口 / 波特率（RTU）及 Slave ID
3. **在编辑对话框中导入 Excel**，或手动填写寄存器配置表后保存
4. **在左侧列表选中站点**，点击「▶ 开始轮询」，遥测 Tab 立即刷新

---

## 主要功能说明

### 站点管理

- 每个站点独立存储连接参数与寄存器配置
- 新建 / 编辑通过「站点编辑对话框」完成，支持在线修改名称（实时写库）
- 删除操作需输入密码（默认 `000000`）防止误操作

### 寄存器配置

每条寄存器配置包含以下关键字段：

| 字段 | 说明 |
|------|------|
| 起始地址 | Modbus 保持寄存器地址（十进制） |
| 数量 | 占用寄存器个数（1 / 2） |
| 数据类型 | `uint16` / `int16` / `uint32` / `int32` / `float` |
| 比例系数 / 偏移量 | 物理值 = (原始值 × 比例系数) + 偏移量 |
| 读写 | `R`（遥测）或 `R/W`（遥控） |
| 分类 | 遥测行显示于「遥测 Tab」，遥控行显示于「遥控 Tab」 |

**支持从 Excel 批量导入**，格式见下表（第 1 行为表头，从第 2 行起为数据）：

```
起始地址 | 寄存器数量 | 变量名 | 中文名 | 读写 | 单位 | 数据类型 | 寄存器数据类型 | 比例系数 | 偏移量 | 取值范围 | 说明
```

### 遥测轮询

- 连续地址段自动合并为一个 FC03 请求（减少通信次数）
- 原始值列（`0x0000 0x0002`）与物理值列（`2.0 A`）并排显示
- 右键菜单可切换解析模式：无符号整数 / 有符号整数 / 原始 HEX 字符串

### 遥控写入

1. 在「遥控 Tab」的写入值单元格直接输入目标物理值
2. 单元格失焦后自动换算并通过 **FC16** 写入设备
3. 写入值与原始寄存器值持久化，下次打开自动恢复

### API 比对验证

1. 在面板右上角填写 `API URL` 与 `Authorization` Header
2. 点击「验证一次」，应用对每个遥测行请求 API 获取预期值并进行比对
3. 通过行打绿点标记，未通过行可通过「下一未通过」按钮快速定位
4. 批量清除绿点同样需要密码确认

---

## 项目结构

```
SimulatorApp/Master/
├── Models/
│   ├── MasterStation.cs            # 站点连接配置
│   ├── MasterRegisterConfig.cs     # 寄存器解析规则
│   ├── RegisterDisplayRow.cs       # UI 运行时显示行
│   └── SlaveEndpoint.cs            # 连接端点值对象
├── Services/
│   ├── IMasterService.cs           # Modbus 读写接口
│   ├── TcpMasterService.cs         # TCP 实现（NModbus4）
│   ├── RtuMasterService.cs         # RTU 实现（NModbus4）
│   ├── IMasterDbService.cs         # 数据库接口
│   ├── MasterDbService.cs          # SQL Server 实现（Dapper）
│   ├── MasterExcelHelper.cs        # Excel 导入（ClosedXML）
│   └── ApiVerifyService.cs         # HTTP API 比对
├── ViewModels/
│   ├── MasterViewModel.cs          # 主 ViewModel（~1200 行）
│   └── SaveStationDialogViewModel.cs
└── Views/
    ├── MasterPanel.xaml            # 主面板
    ├── SaveStationDialog.xaml      # 站点编辑对话框
    └── PasswordDialog.xaml         # 密码确认框
```

---

## 数据库表结构

应用首次连接数据库时自动建表，无需手动执行 SQL。

```sql
-- 站点配置
MasterStations (Id, Name, Protocol, Host, Port, PortName, BaudRate,
                SlaveId, PollIntervalMs, CreatedAt)

-- 寄存器配置
MasterRegisterConfigs (Id, StationId, StartAddress, Quantity,
                       VariableName, ChineseName, ReadWrite, Unit,
                       DataType, ScaleFactor, Offset, ValueRange,
                       Category, SortOrder, IsVerified,
                       LastRawRegisters, LastPhysicalValue)
```

---

## 故障排查

| 现象 | 可能原因 | 解决方法 |
|------|---------|---------|
| 连接后遥测无数据 | Slave ID 不匹配 | 确认站点配置中 Slave ID 与设备一致 |
| 轮询超时 / 日志显示 Timeout | 网络不通或设备未响应 | `ping` 目标 IP，检查防火墙 1502 端口 |
| FC16 写入失败 | 设备仅支持 FC06 | 检查协议文档，当前版本仅支持 FC16 |
| 数据库连接失败 | 连接字符串错误或 1433 端口未开放 | 检查 `Server=IP,Port;` 格式及网络策略 |
| 物理值异常偏大/偏小 | 比例系数配置错误 | 在编辑对话框中校正 ScaleFactor / Offset |
| RTU 无响应 | 串口占用或参数不匹配 | 确认波特率 / 校验位与设备一致，关闭其他占用程序 |

---

## 开发规范

- 分支命名：`feat/xxx` · `fix/xxx` · `refactor/xxx`
- 提交格式：`feat(master): 中文描述`（Conventional Commits）
- `main` 分支受保护，所有改动必须通过 PR 合并
- 代码合并要求：至少 1 位审批 + `dotnet build` 通过

详见 [`GIT_WORKFLOW.md`](GIT_WORKFLOW.md)

---

## License

内部项目，未对外开源。
