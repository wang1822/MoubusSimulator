using SimulatorApp.Services;

namespace SimulatorApp.ViewModels;

/// <summary>STS 转换开关（仪表） ViewModel（字段待补充）。</summary>
public partial class StsInstrumentViewModel : DeviceViewModelBase
{
    public string Title => "STS 转换开关（仪表）";

    // TODO: 根据字段文档添加 [ObservableProperty] 字段

    public StsInstrumentViewModel(RegisterBank bank, IRegisterMapService map)
        : base(bank, map) { }

    protected override void FlushToRegisters()
    {
        // TODO: 根据字段文档实现
    }
}
