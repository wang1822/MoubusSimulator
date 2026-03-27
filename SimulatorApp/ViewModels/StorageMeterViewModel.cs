using SimulatorApp.Services;

namespace SimulatorApp.ViewModels;

/// <summary>储能电表 ViewModel（字段待补充）。</summary>
public partial class StorageMeterViewModel : DeviceViewModelBase
{
    public string Title => "储能电表";

    // TODO: 根据字段文档添加 [ObservableProperty] 字段

    public StorageMeterViewModel(RegisterBank bank, IRegisterMapService map)
        : base(bank, map) { }

    protected override void FlushToRegisters()
    {
        // TODO: 根据字段文档实现
    }
}
