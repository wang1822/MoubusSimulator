using SimulatorApp.Services;

namespace SimulatorApp.ViewModels;

/// <summary>STS 控制IO卡 ViewModel（字段待补充）。</summary>
public partial class StsControlViewModel : DeviceViewModelBase
{
    public string Title => "STS 控制IO卡";

    // TODO: 根据字段文档添加 [ObservableProperty] 字段

    public StsControlViewModel(RegisterBank bank, IRegisterMapService map)
        : base(bank, map) { }

    protected override void FlushToRegisters()
    {
        // TODO: 根据字段文档实现
    }
}
