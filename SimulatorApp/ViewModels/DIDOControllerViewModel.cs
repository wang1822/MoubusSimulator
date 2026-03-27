using SimulatorApp.Services;

namespace SimulatorApp.ViewModels;

/// <summary>DI/DO 动环控制器 ViewModel（字段待补充）。</summary>
public partial class DIDOControllerViewModel : DeviceViewModelBase
{
    public string Title => "DI/DO 动环控制器";

    // TODO: 根据字段文档添加 [ObservableProperty] 字段

    public DIDOControllerViewModel(RegisterBank bank, IRegisterMapService map)
        : base(bank, map) { }

    protected override void FlushToRegisters()
    {
        // TODO: 根据字段文档实现
    }
}
