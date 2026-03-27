using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using SimulatorApp.Models.Bms;
using SimulatorApp.Services;

namespace SimulatorApp.ViewModels;

/// <summary>
/// BMS 电池管理系统 ViewModel。
/// 用户输入物理量，FlushToRegisters 按协议比例系数转换后写入 RegisterBank。
/// </summary>
public partial class BmsViewModel : DeviceViewModelBase
{
    public string Title => "BMS 电池管理系统";
    private readonly BmsModel _model = new();

    // ── 遥测 ──
    [ObservableProperty] private double _totalVolt          = 600.0;  // V，×0.1
    partial void OnTotalVoltChanged(double v)          => FlushToRegisters();

    [ObservableProperty] private double _current            = 0.0;    // A，正=充电，×0.1
    partial void OnCurrentChanged(double v)            => FlushToRegisters();

    [ObservableProperty] private double _soc                = 80.0;   // %，×0.1
    partial void OnSocChanged(double v)                => FlushToRegisters();

    [ObservableProperty] private double _soh                = 98.0;   // %，×0.1
    partial void OnSohChanged(double v)                => FlushToRegisters();

    [ObservableProperty] private double _maxCellVolt        = 3.650;  // V，×0.001
    partial void OnMaxCellVoltChanged(double v)        => FlushToRegisters();

    [ObservableProperty] private double _minCellVolt        = 3.600;  // V，×0.001
    partial void OnMinCellVoltChanged(double v)        => FlushToRegisters();

    [ObservableProperty] private double _voltDiff           = 0.050;  // V，×0.001
    partial void OnVoltDiffChanged(double v)           => FlushToRegisters();

    [ObservableProperty] private int    _maxCellTemp        = 30;     // ℃，int32×1
    partial void OnMaxCellTempChanged(int v)           => FlushToRegisters();

    [ObservableProperty] private int    _minCellTemp        = 25;     // ℃，int32×1
    partial void OnMinCellTempChanged(int v)           => FlushToRegisters();

    [ObservableProperty] private double _allowChargeCurr    = 200.0;  // A，×0.1
    partial void OnAllowChargeCurrChanged(double v)    => FlushToRegisters();

    [ObservableProperty] private double _allowDischargeCurr = 200.0;  // A，×0.1
    partial void OnAllowDischargeCurrChanged(double v) => FlushToRegisters();

    // ── 运行状态（ComboBox SelectedIndex）──
    /// <summary>0=静置 1=充电 2=放电</summary>
    [ObservableProperty] private int _systemState = 0;
    partial void OnSystemStateChanged(int v) => FlushToRegisters();

    // ── 故障/告警注入（bitmask CheckBox 列表）──
    public ObservableCollection<AlarmItem> Fault1Items { get; } = new()
    {
        new AlarmItem("单体过压",  1 << 0),
        new AlarmItem("单体欠压",  1 << 1),
        new AlarmItem("总压过压",  1 << 2),
        new AlarmItem("总压欠压",  1 << 3),
        new AlarmItem("过温保护",  1 << 4),
        new AlarmItem("过流保护",  1 << 5),
        new AlarmItem("短路保护",  1 << 6),
        new AlarmItem("通信故障",  1 << 7),
    };

    public ObservableCollection<AlarmItem> Alarm1Items { get; } = new()
    {
        new AlarmItem("SOC低告警",    1 << 0),
        new AlarmItem("高温告警",     1 << 1),
        new AlarmItem("低温告警",     1 << 2),
        new AlarmItem("过流告警",     1 << 3),
        new AlarmItem("电压压差大",   1 << 4),
        new AlarmItem("绝缘下降告警", 1 << 5),
    };

    public BmsViewModel(RegisterBank bank, IRegisterMapService map)
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
        _model.TimeoutFlag = 0;

        _model.TotalVolt          = (short)Math.Round(Math.Clamp(TotalVolt,           0,   3276.7) *   10);
        _model.Current            = (short)Math.Round(Math.Clamp(Current,         -3276.8, 3276.7) *   10);
        _model.Soc                = (short)Math.Round(Math.Clamp(Soc,                 0,    100)   *   10);
        _model.AmpereHourSoc      = _model.Soc;
        _model.Soh                = (short)Math.Round(Math.Clamp(Soh,                 0,    100)   *   10);
        _model.AllowChargeCurr    = (short)Math.Round(Math.Clamp(AllowChargeCurr,     0,   3276.7) *   10);
        _model.AllowDischargeCurr = (short)Math.Round(Math.Clamp(AllowDischargeCurr,  0,   3276.7) *   10);
        _model.MaxCellVolt        = (short)Math.Round(Math.Clamp(MaxCellVolt,         0,    32.767) * 1000);
        _model.MinCellVolt        = (short)Math.Round(Math.Clamp(MinCellVolt,         0,    32.767) * 1000);
        _model.VoltDiff           = (short)Math.Round(Math.Clamp(VoltDiff,            0,    32.767) * 1000);
        _model.MaxCellTemp        = Math.Clamp(MaxCellTemp, -100, 100);
        _model.MinCellTemp        = Math.Clamp(MinCellTemp, -100, 100);
        _model.SystemState        = (byte)Math.Clamp(SystemState, 0, 2);
        _model.SystemSubstate     = 0;
        _model.FaultState         = 0;

        ushort fault1 = 0;
        foreach (var item in Fault1Items) if (item.IsChecked) fault1 |= (ushort)item.BitMask;
        _model.Fault1 = fault1;
        _model.Fault2 = 0; _model.Fault3 = 0; _model.Fault4 = 0;
        _model.Fault5 = 0; _model.Fault6 = 0; _model.Fault7 = 0;

        ushort alarm1 = 0;
        foreach (var item in Alarm1Items) if (item.IsChecked) alarm1 |= (ushort)item.BitMask;
        _model.Alarm1 = alarm1;
        _model.Alarm2 = 0; _model.Alarm3 = 0; _model.Alarm4 = 0;

        _model.ToRegisters(_bank);
    }
}
