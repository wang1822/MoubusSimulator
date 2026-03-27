using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using SimulatorApp.Models.Dehumidifier;
using SimulatorApp.Services;

namespace SimulatorApp.ViewModels;

/// <summary>
/// 除湿机 ViewModel（FX除湿机通讯协议 V1.1 全量实现）。
/// 覆盖：24个状态寄存器 + 8个参数寄存器 + 命令寄存器，共 48 个 EMS 侧寄存器。
/// </summary>
public partial class DehumidifierViewModel : DeviceViewModelBase
{
    public string Title => "除湿机";

    private readonly DehumidifierModel _model = new();

    // ════ 版本/设备信息（FX 0x1001-0x1008）════

    /// <summary>硬件版本，FX: 0x1001，U16</summary>
    [ObservableProperty] private int _hwVersion = 0x0101;
    partial void OnHwVersionChanged(int v) => FlushToRegisters();

    /// <summary>软件版本，FX: 0x1002，U16</summary>
    [ObservableProperty] private int _swVersion = 0x0101;
    partial void OnSwVersionChanged(int v) => FlushToRegisters();

    /// <summary>设备编号，FX: 0x1003-0x1004，U32</summary>
    [ObservableProperty] private uint _deviceId = 0;
    partial void OnDeviceIdChanged(uint v) => FlushToRegisters();

    /// <summary>软件日期，FX: 0x1005-0x1006，U32（0x1005=年, 0x1006高字节=月低字节=日）</summary>
    [ObservableProperty] private uint _swDate = 0x07E80105;  // 2024-01-05
    partial void OnSwDateChanged(uint v) => FlushToRegisters();

    /// <summary>心跳值，FX: 0x1007-0x1008，U32，每秒+1</summary>
    [ObservableProperty] private uint _heartbeat = 0;
    partial void OnHeartbeatChanged(uint v) => FlushToRegisters();

    // ════ 遥测（FX 0x100D-0x1013）════

    /// <summary>温度 [℃]，FX: 0x100D，S16, ×0.1</summary>
    [ObservableProperty] private double _temperature = 25.0;
    partial void OnTemperatureChanged(double v) => FlushToRegisters();

    /// <summary>湿度 [%RH]，FX: 0x100E，U16, ×0.1</summary>
    [ObservableProperty] private double _humidity = 70.0;
    partial void OnHumidityChanged(double v) => FlushToRegisters();

    /// <summary>NTC1 温度 [℃]，FX: 0x100F，S16, ×0.1</summary>
    [ObservableProperty] private double _ntc1Temp = 25.0;
    partial void OnNtc1TempChanged(double v) => FlushToRegisters();

    /// <summary>NTC2 温度 [℃]，FX: 0x1010，S16, ×0.1</summary>
    [ObservableProperty] private double _ntc2Temp = 25.0;
    partial void OnNtc2TempChanged(double v) => FlushToRegisters();

    /// <summary>输入电压 [V]，FX: 0x1011，U16, ×0.01V</summary>
    [ObservableProperty] private double _inputVoltage = 12.0;
    partial void OnInputVoltageChanged(double v) => FlushToRegisters();

    /// <summary>输出电压 [mV]，FX: 0x1012，U16, ×1mV</summary>
    [ObservableProperty] private double _outputVoltage = 0.0;
    partial void OnOutputVoltageChanged(double v) => FlushToRegisters();

    /// <summary>TEC 电流 [mA]，FX: 0x1013，U16, ×1mA</summary>
    [ObservableProperty] private double _tecCurrent = 0.0;
    partial void OnTecCurrentChanged(double v) => FlushToRegisters();

    // ════ 工作时长（FX 0x1014-0x101F）════

    /// <summary>主板工作时长 [h]，FX: 0x1014-0x1015，U32</summary>
    [ObservableProperty] private uint _mainBoardRuntime = 0;
    partial void OnMainBoardRuntimeChanged(uint v) => FlushToRegisters();

    /// <summary>风机1 工作时长 [h]，FX: 0x1016-0x1017，U32</summary>
    [ObservableProperty] private uint _fan1Runtime = 0;
    partial void OnFan1RuntimeChanged(uint v) => FlushToRegisters();

    /// <summary>风机2 工作时长 [h]，FX: 0x1018-0x1019，U32</summary>
    [ObservableProperty] private uint _fan2Runtime = 0;
    partial void OnFan2RuntimeChanged(uint v) => FlushToRegisters();

    /// <summary>风机3 工作时长 [h]，FX: 0x101A-0x101B，U32</summary>
    [ObservableProperty] private uint _fan3Runtime = 0;
    partial void OnFan3RuntimeChanged(uint v) => FlushToRegisters();

    /// <summary>风机4 工作时长 [h]，FX: 0x101C-0x101D，U32</summary>
    [ObservableProperty] private uint _fan4Runtime = 0;
    partial void OnFan4RuntimeChanged(uint v) => FlushToRegisters();

    /// <summary>除湿模块工作时长 [h]，FX: 0x101E-0x101F，U32</summary>
    [ObservableProperty] private uint _dehumiModuleRuntime = 0;
    partial void OnDehumiModuleRuntimeChanged(uint v) => FlushToRegisters();

    // ════ 风机电流（FX 0x1020-0x1023）════

    /// <summary>风机1 电流 [mA]，FX: 0x1020，U16, ×1mA</summary>
    [ObservableProperty] private double _fan1Current = 0.0;
    partial void OnFan1CurrentChanged(double v) => FlushToRegisters();

    /// <summary>风机2 电流 [mA]，FX: 0x1021，U16, ×1mA</summary>
    [ObservableProperty] private double _fan2Current = 0.0;
    partial void OnFan2CurrentChanged(double v) => FlushToRegisters();

    /// <summary>风机3 电流 [mA]，FX: 0x1022，U16, ×1mA</summary>
    [ObservableProperty] private double _fan3Current = 0.0;
    partial void OnFan3CurrentChanged(double v) => FlushToRegisters();

    /// <summary>风机4 电流 [mA]，FX: 0x1023，U16, ×1mA</summary>
    [ObservableProperty] private double _fan4Current = 0.0;
    partial void OnFan4CurrentChanged(double v) => FlushToRegisters();

    // ════ 参数寄存器（FX 0x7031-0x7035）════

    /// <summary>除湿设定点 [%RH]，FX: 0x7031，U16, ×0.1，范围 35-90%RH</summary>
    [ObservableProperty] private double _dehumiSetPoint = 60.0;
    partial void OnDehumiSetPointChanged(double v) => FlushToRegisters();

    /// <summary>除湿回差 [%RH]，FX: 0x7032，U16, ×0.1，范围 0-55%RH</summary>
    [ObservableProperty] private double _dehumiReturnDiff = 5.0;
    partial void OnDehumiReturnDiffChanged(double v) => FlushToRegisters();

    /// <summary>过电压值 [V]，FX: 0x7033，U16, ×0.1V</summary>
    [ObservableProperty] private double _overVoltageSet = 28.0;
    partial void OnOverVoltageSetChanged(double v) => FlushToRegisters();

    /// <summary>欠电压值 [V]，FX: 0x7034，U16, ×0.1V</summary>
    [ObservableProperty] private double _underVoltageSet = 18.0;
    partial void OnUnderVoltageSetChanged(double v) => FlushToRegisters();

    /// <summary>电压保护回差 [V]，FX: 0x7035，U16, ×0.1V</summary>
    [ObservableProperty] private double _voltageHysteresis = 2.0;
    partial void OnVoltageHysteresisChanged(double v) => FlushToRegisters();

    // ════ 运行指令（FX 0x6011/0x6012）════

    /// <summary>运行指令：0=停止, 1=自动除湿, 2=强制除湿</summary>
    [ObservableProperty] private int _runCmd = 0;
    partial void OnRunCmdChanged(int v) => FlushToRegisters();

    // ════ 状态字 bitmask（FX 0x1009-0x100A）════

    public ObservableCollection<AlarmItem> StatusItems { get; } = new()
    {
        new AlarmItem("待机模式",     (int)DehumidifierStatusBits.StandbyMode),
        new AlarmItem("除湿模式",     (int)DehumidifierStatusBits.DehumiMode),
        new AlarmItem("强制模式",     (int)DehumidifierStatusBits.ForcedMode),
        new AlarmItem("风机1 运行",   (int)DehumidifierStatusBits.Fan1Running),
        new AlarmItem("风机2 运行",   (int)DehumidifierStatusBits.Fan2Running),
        new AlarmItem("风机3 运行",   (int)DehumidifierStatusBits.Fan3Running),
        new AlarmItem("风机4 运行",   (int)DehumidifierStatusBits.Fan4Running),
        new AlarmItem("除湿模块运行", (int)DehumidifierStatusBits.DehumiModuleRunning),
        new AlarmItem("工程模式",     (int)DehumidifierStatusBits.EngineeringMode),
    };

    // ════ 故障字 bitmask（FX 0x100B-0x100C）════

    public ObservableCollection<AlarmItem> FaultItems { get; } = new()
    {
        new AlarmItem("MODBUS通讯中断", (int)DehumidifierFaultBits.ModbusInterrupt),
        new AlarmItem("过电压",         (int)DehumidifierFaultBits.OverVoltage),
        new AlarmItem("欠电压",         (int)DehumidifierFaultBits.UnderVoltage),
        new AlarmItem("温度传感器失效", (int)DehumidifierFaultBits.TempSensorFail),
        new AlarmItem("湿度传感器失效", (int)DehumidifierFaultBits.HumiSensorFail),
        new AlarmItem("NTC1传感器失效", (int)DehumidifierFaultBits.Ntc1SensorFail),
        new AlarmItem("NTC2传感器失效", (int)DehumidifierFaultBits.Ntc2SensorFail),
        new AlarmItem("除湿模块故障",   (int)DehumidifierFaultBits.DehumiModuleFault),
        new AlarmItem("风机故障1",      (int)DehumidifierFaultBits.FanFault1),
        new AlarmItem("风机故障2",      (int)DehumidifierFaultBits.FanFault2),
        new AlarmItem("风机故障3",      (int)DehumidifierFaultBits.FanFault3),
        new AlarmItem("风机故障4",      (int)DehumidifierFaultBits.FanFault4),
        new AlarmItem("湿度过大",       (int)DehumidifierFaultBits.HighHumidity),
    };

    // ════ 构造 ════

    public DehumidifierViewModel(RegisterBank bank, IRegisterMapService map)
        : base(bank, map)
    {
        foreach (var item in StatusItems)
            item.PropertyChanged += (_, _) => FlushToRegisters();
        foreach (var item in FaultItems)
            item.PropertyChanged += (_, _) => FlushToRegisters();
    }

    // ════ 命令 ════

    [RelayCommand]
    private void ClearAllFaults()
    {
        foreach (var item in StatusItems) item.IsChecked = false;
        foreach (var item in FaultItems)  item.IsChecked = false;
    }

    // ════ 寄存器刷新 ════

    protected override void FlushToRegisters()
    {
        // 版本/设备信息
        _model.HwVersion = (ushort)Math.Clamp(HwVersion, 0, 0xFFFF);
        _model.SwVersion = (ushort)Math.Clamp(SwVersion, 0, 0xFFFF);
        _model.DeviceId  = DeviceId;
        _model.SwDate    = SwDate;
        _model.Heartbeat = Heartbeat;

        // 状态字 U32 bitmask
        uint statusWord = 0;
        foreach (var item in StatusItems)
            if (item.IsChecked) statusWord |= (uint)item.BitMask;
        _model.StatusWord = statusWord;

        // 故障字 U32 bitmask
        uint faultWord = 0;
        foreach (var item in FaultItems)
            if (item.IsChecked) faultWord |= (uint)item.BitMask;
        _model.FaultWord = faultWord;

        // 遥测（输入校验 + 比例转换）
        _model.Temperature   = (short) Math.Round(Math.Clamp(Temperature,    -50.0,  200.0) * 10);
        _model.Humidity      = (ushort)Math.Round(Math.Clamp(Humidity,          0.0,  100.0) * 10);
        _model.Ntc1Temp      = (short) Math.Round(Math.Clamp(Ntc1Temp,        -50.0,  200.0) * 10);
        _model.Ntc2Temp      = (short) Math.Round(Math.Clamp(Ntc2Temp,        -50.0,  200.0) * 10);
        _model.InputVoltage  = (ushort)Math.Round(Math.Clamp(InputVoltage,      0.0,  100.0) * 100);  // ×0.01V
        _model.OutputVoltage = (ushort)Math.Round(Math.Clamp(OutputVoltage,     0.0, 65535.0));        // ×1mV
        _model.TecCurrent    = (ushort)Math.Round(Math.Clamp(TecCurrent,        0.0, 65535.0));        // ×1mA

        // 工作时长
        _model.MainBoardRuntime    = MainBoardRuntime;
        _model.Fan1Runtime         = Fan1Runtime;
        _model.Fan2Runtime         = Fan2Runtime;
        _model.Fan3Runtime         = Fan3Runtime;
        _model.Fan4Runtime         = Fan4Runtime;
        _model.DehumiModuleRuntime = DehumiModuleRuntime;

        // 风机电流 ×1mA
        _model.Fan1Current = (ushort)Math.Round(Math.Clamp(Fan1Current, 0.0, 65535.0));
        _model.Fan2Current = (ushort)Math.Round(Math.Clamp(Fan2Current, 0.0, 65535.0));
        _model.Fan3Current = (ushort)Math.Round(Math.Clamp(Fan3Current, 0.0, 65535.0));
        _model.Fan4Current = (ushort)Math.Round(Math.Clamp(Fan4Current, 0.0, 65535.0));

        // 参数寄存器
        _model.DehumiSetPoint    = (ushort)Math.Round(Math.Clamp(DehumiSetPoint,    35.0,  90.0) * 10);
        _model.DehumiReturnDiff  = (ushort)Math.Round(Math.Clamp(DehumiReturnDiff,   0.0,  55.0) * 10);
        _model.OverVoltageSet    = (ushort)Math.Round(Math.Clamp(OverVoltageSet,      0.0, 999.0) * 10);
        _model.UnderVoltageSet   = (ushort)Math.Round(Math.Clamp(UnderVoltageSet,     0.0, 999.0) * 10);
        _model.VoltageHysteresis = (ushort)Math.Round(Math.Clamp(VoltageHysteresis,   0.0, 100.0) * 10);

        // 运行指令
        _model.RunCmd = (ushort)RunCmd;

        _model.ToRegisters(_bank);
    }
}
