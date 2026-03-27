using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using ModelProto = SimulatorApp.Models.ProtocolType;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SimulatorApp.Logging;
using SimulatorApp.Models;
using SimulatorApp.Services;

namespace SimulatorApp.ViewModels;

public partial class SlaveViewModel : ObservableObject
{
    private readonly ISlaveService       _tcpSlave;
    private readonly ISlaveService       _rtuSlave;
    private readonly IRegisterMapService _map;
    private readonly AppLogger           _log;

    // ===== 协议选择 =====
    [ObservableProperty] private ModelProto _protocol = ModelProto.Tcp;
    [ObservableProperty] private bool _isTcp = true;
    [ObservableProperty] private bool _isRtu = false;

    partial void OnProtocolChanged(ModelProto value)
    {
        IsTcp = value == ModelProto.Tcp;
        IsRtu = value == ModelProto.Rtu;
    }

    partial void OnIsTcpChanged(bool value) { if (value) Protocol = ModelProto.Tcp; }
    partial void OnIsRtuChanged(bool value)
    {
        if (value)
        {
            Protocol = ModelProto.Rtu;
            RefreshPorts(); // 切换到 RTU 时自动刷新串口列表
        }
    }

    // ===== TCP 参数 =====
    [ObservableProperty] private string _tcpBindAddress = "0.0.0.0";
    [ObservableProperty] private int    _tcpPort        = 502;
    [ObservableProperty] private byte   _tcpSlaveId     = 1;

    /// <summary>本机可用 IP 地址列表（含 0.0.0.0 = 所有接口）。</summary>
    public ObservableCollection<string> AvailableAddresses { get; } = new();

    // ===== RTU 参数 =====
    [ObservableProperty] private string _comPort    = "";
    [ObservableProperty] private int    _baudRate   = 9600;
    [ObservableProperty] private byte   _rtuSlaveId = 1;

    /// <summary>系统可用 COM 口列表。</summary>
    public ObservableCollection<string> AvailablePorts { get; } = new();

    // ===== 状态 =====
    [ObservableProperty] private bool   _isRunning;
    [ObservableProperty] private string _statusText = "未启动";

    // ===== 各设备 ViewModel =====
    public PcsViewModel            Pcs            { get; }
    public BmsViewModel            Bms            { get; }
    public MpptViewModel           Mppt           { get; }
    public StsInstrumentViewModel  StsInstrument  { get; }
    public StsControlViewModel     StsControl     { get; }
    public AirConditionerViewModel AirConditioner { get; }
    public DehumidifierViewModel   Dehumidifier   { get; }
    public DieselGeneratorViewModel DieselGenerator { get; }
    public GasDetectorViewModel    GasDetector    { get; }
    public ExternalMeterViewModel  ExternalMeter  { get; }
    public StorageMeterViewModel   StorageMeter   { get; }
    public DIDOControllerViewModel DIDOController { get; }

    public ISlaveService ActiveService => Protocol == ModelProto.Tcp ? _tcpSlave : _rtuSlave;

    public SlaveViewModel(
        TcpSlaveService tcpSlave, RtuSlaveService rtuSlave,
        IRegisterMapService map, AppLogger log,
        PcsViewModel pcs, BmsViewModel bms, MpptViewModel mppt,
        StsInstrumentViewModel stsInstrument, StsControlViewModel stsControl,
        AirConditionerViewModel airConditioner, DehumidifierViewModel dehumidifier,
        DieselGeneratorViewModel dieselGenerator, GasDetectorViewModel gasDetector,
        ExternalMeterViewModel externalMeter, StorageMeterViewModel storageMeter,
        DIDOControllerViewModel didoController)
    {
        _tcpSlave = tcpSlave;
        _rtuSlave = rtuSlave;
        _map      = map;
        _log      = log;

        Pcs            = pcs;
        Bms            = bms;
        Mppt           = mppt;
        StsInstrument  = stsInstrument;
        StsControl     = stsControl;
        AirConditioner = airConditioner;
        Dehumidifier   = dehumidifier;
        DieselGenerator= dieselGenerator;
        GasDetector    = gasDetector;
        ExternalMeter  = externalMeter;
        StorageMeter   = storageMeter;
        DIDOController = didoController;

        LoadAvailableAddresses();
        RefreshPorts();
    }

    // ===== 本机 IP 枚举 =====
    private void LoadAvailableAddresses()
    {
        AvailableAddresses.Clear();
        AvailableAddresses.Add("0.0.0.0");  // 监听所有接口

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            foreach (var ua in ni.GetIPProperties().UnicastAddresses)
            {
                if (ua.Address.AddressFamily == AddressFamily.InterNetwork)
                    AvailableAddresses.Add(ua.Address.ToString());
            }
        }

        // 确保选中项在列表里
        if (!AvailableAddresses.Contains(TcpBindAddress))
            TcpBindAddress = "0.0.0.0";
    }

    // ===== COM 口枚举 =====
    [RelayCommand]
    private void RefreshPorts()
    {
        var ports  = SerialPort.GetPortNames().OrderBy(p => p).ToArray();
        var current = ComPort;

        AvailablePorts.Clear();
        foreach (var p in ports) AvailablePorts.Add(p);

        // 保持选中；若原来的口不再存在则选第一个
        ComPort = ports.Contains(current) ? current
                : ports.Length > 0        ? ports[0]
                : "";
    }

    // ===== 启停 =====
    [RelayCommand]
    private async Task ToggleAsync()
    {
        if (!IsRunning) await StartAsync();
        else            await StopAsync();
    }

    private async Task StartAsync()
    {
        try
        {
            if (Protocol == ModelProto.Tcp)
            {
                _tcpSlave.BindAddress = TcpBindAddress;
                _tcpSlave.Port        = TcpPort;
                _tcpSlave.SlaveId     = TcpSlaveId;
            }
            else
            {
                _rtuSlave.ComPort  = ComPort;
                _rtuSlave.BaudRate = BaudRate;
                _rtuSlave.SlaveId  = RtuSlaveId;
            }

            _map.FlushAll();
            await ActiveService.StartAsync();
            IsRunning  = true;
            StatusText = Protocol == ModelProto.Tcp
                ? $"运行中 [TCP {TcpBindAddress}:{TcpPort}]"
                : $"运行中 [{ComPort} {BaudRate}bps]";
            _log.Info($"[从站] 已启动 Protocol={Protocol}");
        }
        catch (Exception ex)
        {
            _log.Error("[从站] 启动失败", ex);
            StatusText = $"启动失败: {ex.Message}";
        }
    }

    private async Task StopAsync()
    {
        try
        {
            await ActiveService.StopAsync();
            IsRunning  = false;
            StatusText = "已停止";
            _log.Info("[从站] 已停止");
        }
        catch (Exception ex) { _log.Error("[从站] 停止异常", ex); }
    }

    public async Task ForceStopAsync()
    {
        if (IsRunning) await StopAsync();
    }
}
