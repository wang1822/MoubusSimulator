using SimulatorApp.Services;

namespace SimulatorApp.ViewModels;

/// <summary>外部电表 ViewModel（字段待补充）。</summary>
public partial class ExternalMeterViewModel : DeviceViewModelBase
{
    public string Title => "外部电表";

    // TODO: 根据字段文档添加 [ObservableProperty] 字段

    public ExternalMeterViewModel(RegisterBank bank, IRegisterMapService map)
        : base(bank, map) { }

    protected override void FlushToRegisters()
    {
        // TODO: 根据字段文档实现
    }
}
