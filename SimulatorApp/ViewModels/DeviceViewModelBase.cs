using CommunityToolkit.Mvvm.ComponentModel;
using SimulatorApp.Services;

namespace SimulatorApp.ViewModels;

/// <summary>所有设备 ViewModel 的基类。</summary>
public abstract partial class DeviceViewModelBase : ObservableObject
{
    protected readonly RegisterBank      _bank;
    protected readonly IRegisterMapService _map;

    [ObservableProperty]
    private bool _isExpanded = false;

    protected DeviceViewModelBase(RegisterBank bank, IRegisterMapService map)
    {
        _bank = bank;
        _map  = map;
    }

    /// <summary>把当前属性值刷入 RegisterBank（由属性 Changed 回调触发）。</summary>
    protected abstract void FlushToRegisters();
}
