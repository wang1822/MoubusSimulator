using SimulatorApp.Services;

namespace SimulatorApp.ViewModels;

/// <summary>柴发 ViewModel（字段待补充）。</summary>
public partial class DieselGeneratorViewModel : DeviceViewModelBase
{
    public string Title => "柴发";

    // TODO: 根据字段文档添加 [ObservableProperty] 字段

    public DieselGeneratorViewModel(RegisterBank bank, IRegisterMapService map)
        : base(bank, map) { }

    protected override void FlushToRegisters()
    {
        // TODO: 根据字段文档实现
    }
}
