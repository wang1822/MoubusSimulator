using CommunityToolkit.Mvvm.ComponentModel;
using SimulatorApp.Shared.Services;
using SimulatorApp.Slave.Models;
using SimulatorApp.Slave.Models.Dehumidifier;
using SimulatorApp.Slave.Services;
using System.Collections.ObjectModel;

namespace SimulatorApp.Slave.ViewModels;

/// <summary>除湿机 ViewModel</summary>
public partial class DehumidifierViewModel : DeviceViewModelBase
{
    private readonly DehumidifierModel _model = new();
    public override string DeviceName => "除湿机";
    protected override DeviceModelBase Model => _model;

    public DehumidifierViewModel(RegisterBank bank, RegisterMapService mapService) : base(bank, mapService)
    { InitAlarmItems(); FlushToRegisters(); }

    [ObservableProperty] private double _temperature  = 25.0;
    [ObservableProperty] private double _humidity     = 70.0;
    [ObservableProperty] private double _ntc1Temp     = 25.0;
    [ObservableProperty] private double _ntc2Temp     = 25.0;
    [ObservableProperty] private double _inputVoltage = 12.0;
    [ObservableProperty] private uint   _fan1Runtime  = 0;
    [ObservableProperty] private uint   _fan2Runtime  = 0;
    [ObservableProperty] private double _fan1Current  = 0.0;

    partial void OnTemperatureChanged(double v)  => FlushToRegisters();
    partial void OnHumidityChanged(double v)     => FlushToRegisters();
    partial void OnNtc1TempChanged(double v)     => FlushToRegisters();
    partial void OnNtc2TempChanged(double v)     => FlushToRegisters();
    partial void OnInputVoltageChanged(double v) => FlushToRegisters();
    partial void OnFan1RuntimeChanged(uint v)    => FlushToRegisters();
    partial void OnFan2RuntimeChanged(uint v)    => FlushToRegisters();
    partial void OnFan1CurrentChanged(double v)  => FlushToRegisters();

    // 状态字（bit 对应运行状态）
    public ObservableCollection<AlarmItem> StatusItems { get; } = new();
    // 故障字（bit 对应故障类型）
    public ObservableCollection<AlarmItem> FaultItems  { get; } = new();

    private void InitAlarmItems()
    {
        void Add(ObservableCollection<AlarmItem> col, int bit, string label)
        {
            var item = new AlarmItem(label, 1 << bit);
            item.CheckedChanged += () => FlushToRegisters();
            col.Add(item);
        }
        Add(StatusItems, 0, "待机模式"); Add(StatusItems, 1, "除湿模式");
        Add(StatusItems, 2, "强制模式"); Add(StatusItems, 3, "风机1运行");
        Add(StatusItems, 4, "风机2运行"); Add(StatusItems, 5, "风机3运行");
        Add(StatusItems, 6, "风机4运行"); Add(StatusItems, 7, "除湿模块运行");
        Add(StatusItems, 8, "工程模式");

        Add(FaultItems, 0, "MODBUS通讯中断"); Add(FaultItems, 1, "过电压");
        Add(FaultItems, 2, "欠电压");         Add(FaultItems, 3, "温度传感器失效");
        Add(FaultItems, 4, "湿度传感器失效");  Add(FaultItems, 5, "NTC1传感器失效");
        Add(FaultItems, 6, "NTC2传感器失效");  Add(FaultItems, 7, "除湿模块故障");
        Add(FaultItems, 8, "风机故障1");       Add(FaultItems, 9, "风机故障2");
        Add(FaultItems, 10,"风机故障3");       Add(FaultItems, 11,"风机故障4");
        Add(FaultItems, 12,"湿度过大");
    }

    protected override void SyncToModel()
    {
        _model.Temperature  = Temperature;
        _model.Humidity     = Humidity;
        _model.Ntc1Temp     = Ntc1Temp;
        _model.Ntc2Temp     = Ntc2Temp;
        _model.InputVoltage = InputVoltage;
        _model.Fan1Runtime  = Fan1Runtime;
        _model.Fan2Runtime  = Fan2Runtime;
        _model.Fan1Current  = Fan1Current;
        _model.StatusWord   = (uint)StatusItems.Where(x => x.IsChecked).Aggregate(0, (a, x) => a | x.BitMask);
        _model.FaultWord    = (uint)FaultItems.Where(x => x.IsChecked).Aggregate(0, (a, x) => a | x.BitMask);
    }

    public override void GenerateData()
    {
        var rnd = new Random();
        Temperature  = Math.Round(20 + rnd.NextDouble() * 15, 1);
        Humidity     = Math.Round(50 + rnd.NextDouble() * 30, 1);
        Ntc1Temp     = Math.Round(22 + rnd.NextDouble() * 10, 1);
        Ntc2Temp     = Math.Round(22 + rnd.NextDouble() * 10, 1);
        InputVoltage = Math.Round(11.5 + rnd.NextDouble() * 1, 2);
        base.GenerateData();
    }

    public override void ClearAlarms()
    {
        foreach (var item in FaultItems) item.IsChecked = false;
        base.ClearAlarms();
    }
}
