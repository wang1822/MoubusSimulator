using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimulatorApp.Logging;
using SimulatorApp.Models;

namespace SimulatorApp.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly AppLogger      _log;
    private readonly SlaveViewModel _slaveVm;
    private readonly MasterViewModel _masterVm;

    [ObservableProperty] private ModeType _currentMode = ModeType.Slave;
    [ObservableProperty] private bool     _isSlaveMode = true;
    [ObservableProperty] private bool     _isMasterMode;

    public SlaveViewModel  SlaveVm  => _slaveVm;
    public MasterViewModel MasterVm => _masterVm;

    public AppLogger Log => _log;

    public MainViewModel(SlaveViewModel slaveVm, MasterViewModel masterVm, AppLogger log)
    {
        _slaveVm  = slaveVm;
        _masterVm = masterVm;
        _log      = log;
    }

    partial void OnCurrentModeChanged(ModeType value)
    {
        IsSlaveMode  = value == ModeType.Slave;
        IsMasterMode = value == ModeType.Master;
    }

    [RelayCommand]
    private async Task SwitchToSlaveAsync()
    {
        if (CurrentMode == ModeType.Slave) return;
        await _masterVm.ForceStopAsync();
        CurrentMode = ModeType.Slave;
        _log.Info("已切换到 [从站模式]");
    }

    [RelayCommand]
    private async Task SwitchToMasterAsync()
    {
        if (CurrentMode == ModeType.Master) return;
        await _slaveVm.ForceStopAsync();
        CurrentMode = ModeType.Master;
        _log.Info("已切换到 [主站模式]");
    }
}
