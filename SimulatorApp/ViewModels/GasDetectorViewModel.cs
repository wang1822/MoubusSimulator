using SimulatorApp.Services;

namespace SimulatorApp.ViewModels;

/// <summary>气体检测 ViewModel（字段待补充）。</summary>
public partial class GasDetectorViewModel : DeviceViewModelBase
{
    public string Title => "气体检测";

    // TODO: 根据字段文档添加 [ObservableProperty] 字段

    public GasDetectorViewModel(RegisterBank bank, IRegisterMapService map)
        : base(bank, map) { }

    protected override void FlushToRegisters()
    {
        // TODO: 根据字段文档实现
    }
}
