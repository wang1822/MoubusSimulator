using SimulatorApp.Services;

namespace SimulatorApp.ViewModels;

/// <summary>空调 ViewModel（字段待补充）。</summary>
public partial class AirConditionerViewModel : DeviceViewModelBase
{
    public string Title => "空调";

    // TODO: 根据字段文档添加 [ObservableProperty] 字段

    public AirConditionerViewModel(RegisterBank bank, IRegisterMapService map)
        : base(bank, map) { }

    protected override void FlushToRegisters()
    {
        // TODO: 根据字段文档实现
    }
}
