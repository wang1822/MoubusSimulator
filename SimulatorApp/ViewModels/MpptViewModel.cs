using SimulatorApp.Services;

namespace SimulatorApp.ViewModels;

/// <summary>MPPT 光伏控制器 ViewModel（字段待补充）。</summary>
public partial class MpptViewModel : DeviceViewModelBase
{
    public string Title => "MPPT 光伏控制器";

    // TODO: 根据字段文档添加 [ObservableProperty] 字段

    public MpptViewModel(RegisterBank bank, IRegisterMapService map)
        : base(bank, map) { }

    protected override void FlushToRegisters()
    {
        // TODO: 根据字段文档实现
    }
}
