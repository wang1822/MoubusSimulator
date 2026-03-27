using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using SimulatorApp.Models.Pcs;
using SimulatorApp.Services;

namespace SimulatorApp.ViewModels;

/// <summary>
/// PCS 储能变流器 ViewModel。
/// 用户在面板中输入物理量（V / A / kW / Hz / ℃），FlushToRegisters 按协议比例系数转换后写入 RegisterBank。
/// 注意：DcCurrent/DcPower/TotalActPower/TotalReactPower 使用 ×(-0.1)/×(-0.001) 字序，正值=放电。
/// </summary>
public partial class PcsViewModel : DeviceViewModelBase
{
    public string Title => "PCS 储能变流器";
    private readonly PcsModel _model = new();

    // ── 遥测 ──
    [ObservableProperty] private double _dcVoltage       = 750.0;  // V，×0.1
    partial void OnDcVoltageChanged(double v)       => FlushToRegisters();

    [ObservableProperty] private double _dcCurrent       = 0.0;    // A，正=放电，×(-0.1)
    partial void OnDcCurrentChanged(double v)       => FlushToRegisters();

    [ObservableProperty] private double _dcPower         = 0.0;    // kW，正=放电，×(-0.1)
    partial void OnDcPowerChanged(double v)         => FlushToRegisters();

    [ObservableProperty] private double _phaseAVolt      = 220.0;  // V，×0.1
    partial void OnPhaseAVoltChanged(double v)      => FlushToRegisters();

    [ObservableProperty] private double _phaseBVolt      = 220.0;  // V，×0.1
    partial void OnPhaseBVoltChanged(double v)      => FlushToRegisters();

    [ObservableProperty] private double _phaseCVolt      = 220.0;  // V，×0.1
    partial void OnPhaseCVoltChanged(double v)      => FlushToRegisters();

    [ObservableProperty] private double _gridPhaseACurr  = 0.0;    // A，×0.1
    partial void OnGridPhaseACurrChanged(double v)  => FlushToRegisters();

    [ObservableProperty] private double _totalActPower   = 0.0;    // kW，正=放电，×(-0.001)
    partial void OnTotalActPowerChanged(double v)   => FlushToRegisters();

    [ObservableProperty] private double _totalReactPower = 0.0;    // kvar，×(-0.001)
    partial void OnTotalReactPowerChanged(double v) => FlushToRegisters();

    [ObservableProperty] private double _gridFrequency   = 50.0;   // Hz，×0.01
    partial void OnGridFrequencyChanged(double v)   => FlushToRegisters();

    [ObservableProperty] private double _temp1           = 25.0;   // ℃，×0.1
    partial void OnTemp1Changed(double v)           => FlushToRegisters();

    [ObservableProperty] private double _dailyCharged    = 0.0;    // kWh，×0.1
    partial void OnDailyChargedChanged(double v)    => FlushToRegisters();

    [ObservableProperty] private double _dailyDischarged = 0.0;    // kWh，×0.1
    partial void OnDailyDischargedChanged(double v) => FlushToRegisters();

    [ObservableProperty] private double _cumCharged      = 0.0;    // kWh，×0.1
    partial void OnCumChargedChanged(double v)      => FlushToRegisters();

    [ObservableProperty] private double _cumDischarged   = 0.0;    // kWh，×0.1
    partial void OnCumDischargedChanged(double v)   => FlushToRegisters();

    // ── 运行状态（ComboBox SelectedIndex）──
    /// <summary>0=待机 1=自检 2=运行 3=告警 4=故障</summary>
    [ObservableProperty] private int _operatingState = 2;
    partial void OnOperatingStateChanged(int v) => FlushToRegisters();

    /// <summary>0=静置 1=充电 2=放电</summary>
    [ObservableProperty] private int _chargingState = 0;
    partial void OnChargingStateChanged(int v) => FlushToRegisters();

    /// <summary>0=并网 1=离网 2=切换中</summary>
    [ObservableProperty] private int _operatingMode = 0;
    partial void OnOperatingModeChanged(int v) => FlushToRegisters();

    // ── 故障/告警注入（bitmask CheckBox 列表）──
    public ObservableCollection<AlarmItem> Fault1Items { get; } = new()
    {
        new AlarmItem("直流极性反接", 1 << 0),
        new AlarmItem("从MCU故障",   1 << 1),
        new AlarmItem("STS断开",     1 << 2),
        new AlarmItem("DC侧过压",    1 << 3),
        new AlarmItem("DC侧欠压",    1 << 4),
        new AlarmItem("AC侧过压",    1 << 5),
        new AlarmItem("AC侧欠压",    1 << 6),
        new AlarmItem("过温保护",    1 << 7),
    };

    public ObservableCollection<AlarmItem> Alarm1Items { get; } = new()
    {
        new AlarmItem("DC侧高温",    1 << 0),
        new AlarmItem("AC侧高温",    1 << 1),
        new AlarmItem("散热风扇异常", 1 << 2),
        new AlarmItem("绝缘检测告警", 1 << 3),
        new AlarmItem("DC过流告警",  1 << 4),
        new AlarmItem("AC过流告警",  1 << 5),
    };

    public PcsViewModel(RegisterBank bank, IRegisterMapService map)
        : base(bank, map)
    {
        foreach (var item in Fault1Items)  item.PropertyChanged += (_, _) => FlushToRegisters();
        foreach (var item in Alarm1Items)  item.PropertyChanged += (_, _) => FlushToRegisters();
    }

    [RelayCommand]
    private void ClearAllFaults()
    {
        foreach (var item in Fault1Items)  item.IsChecked = false;
        foreach (var item in Alarm1Items)  item.IsChecked = false;
    }

    protected override void FlushToRegisters()
    {
        _model.TimeoutFlag = 0; // 模拟器始终在线

        // 遥测：物理值 → int16 原始值
        _model.DcVoltage         = (short)Math.Round(Math.Clamp(DcVoltage,        0,    3276.7) *   10);
        // DcCurrent/DcPower 协议符号：raw×(-0.1)=physical → raw = physical/(-0.1) = -physical×10
        _model.DcCurrent         = (short)Math.Round(Math.Clamp(DcCurrent,    -3276.8, 3276.7) *  -10);
        _model.DcPower           = (short)Math.Round(Math.Clamp(DcPower,      -3276.8, 3276.7) *  -10);
        _model.PhaseAVolt        = (short)Math.Round(Math.Clamp(PhaseAVolt,      0,    3276.7) *   10);
        _model.PhaseBVolt        = (short)Math.Round(Math.Clamp(PhaseBVolt,      0,    3276.7) *   10);
        _model.PhaseCVolt        = (short)Math.Round(Math.Clamp(PhaseCVolt,      0,    3276.7) *   10);
        _model.GridPhaseAVolt    = _model.PhaseAVolt;
        _model.GridPhaseBVolt    = _model.PhaseBVolt;
        _model.GridPhaseCVolt    = _model.PhaseCVolt;
        _model.GridPhaseACurrent = (short)Math.Round(Math.Clamp(GridPhaseACurr, -3276.8, 3276.7) *  10);
        _model.GridPhaseBCurrent = _model.GridPhaseACurrent;
        _model.GridPhaseCCurrent = _model.GridPhaseACurrent;
        _model.GridFrequency     = (short)Math.Round(Math.Clamp(GridFrequency,   0,    327.67) *  100);
        _model.GridPowerFactor   = 1000;  // 固定 1.000
        // TotalActPower/TotalReactPower 协议符号：raw×(-0.001)=physical → raw = -physical×1000
        _model.TotalActPower     = (short)Math.Round(Math.Clamp(TotalActPower,  -32.768, 32.767) * -1000);
        _model.PhaseAActPower    = _model.TotalActPower;
        _model.PhaseBActPower    = _model.TotalActPower;
        _model.PhaseCActPower    = _model.TotalActPower;
        _model.TotalReactPower   = (short)Math.Round(Math.Clamp(TotalReactPower,-32.768, 32.767) * -1000);
        _model.DailyCharged      = (short)Math.Round(Math.Clamp(DailyCharged,    0,    3276.7) *   10);
        _model.DailyDischarged   = (short)Math.Round(Math.Clamp(DailyDischarged, 0,    3276.7) *   10);
        _model.CumCharged        = (short)Math.Round(Math.Clamp(CumCharged,      0,    3276.7) *   10);
        _model.CumDischarged     = (short)Math.Round(Math.Clamp(CumDischarged,   0,    3276.7) *   10);
        _model.Temp1             = (short)Math.Round(Math.Clamp(Temp1,         -100,    200)   *   10);
        _model.Temp2             = _model.Temp1;

        // 运行状态
        _model.OperatingState  = (ushort)Math.Clamp(OperatingState, 0, 4);
        _model.ChargingState   = (ushort)Math.Clamp(ChargingState,  0, 2);
        _model.OperatingMode   = (ushort)Math.Clamp(OperatingMode,  0, 2);

        // 故障/告警 bitmask
        ushort fault1 = 0;
        foreach (var item in Fault1Items) if (item.IsChecked) fault1 |= (ushort)item.BitMask;
        _model.Fault1 = fault1;
        _model.Fault2 = 0; _model.Fault3 = 0; _model.Fault4 = 0;

        ushort alarm1 = 0;
        foreach (var item in Alarm1Items) if (item.IsChecked) alarm1 |= (ushort)item.BitMask;
        _model.Alarm1 = alarm1;
        _model.Alarm2 = 0; _model.Alarm3 = 0; _model.Alarm4 = 0;

        _model.ToRegisters(_bank);
    }
}
