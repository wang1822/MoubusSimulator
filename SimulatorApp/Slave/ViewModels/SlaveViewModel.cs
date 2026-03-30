using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SimulatorApp.Shared.Helpers;
using SimulatorApp.Shared.Logging;
using SimulatorApp.Shared.Models;
using SimulatorApp.Slave.Services;
using SimulatorApp.Slave.Views.Panels;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;

namespace SimulatorApp.Slave.ViewModels;

/// <summary>
/// 从站主 ViewModel：连接配置、启停、持有所有设备 ViewModel、设备面板路由。
/// </summary>
public partial class SlaveViewModel : ObservableObject
{
    private readonly IServiceProvider _services;
    private ISlaveService?            _currentService;

    // ----------------------------------------------------------------
    // 连接参数
    // ----------------------------------------------------------------

    [ObservableProperty] private ProtocolType _protocol     = ProtocolType.Tcp;
    [ObservableProperty] private string  _listenAddress     = "0.0.0.0";
    [ObservableProperty] private int     _port              = 502;
    [ObservableProperty] private byte    _slaveId           = 1;
    [ObservableProperty] private string  _comPort           = "COM3";
    [ObservableProperty] private int     _baudRate          = 9600;

    // 协议辅助属性（XAML 显示/隐藏 TCP/RTU 配置区域）
    public bool IsTcpMode => Protocol == ProtocolType.Tcp;
    public bool IsRtuMode => Protocol == ProtocolType.Rtu;

    // SelectedIndex 绑定：避免 int→ProtocolType 类型转换错误
    public IReadOnlyList<string> ProtocolNames { get; } = new[] { "TCP", "RTU" };
    public int ProtocolIndex
    {
        get => (int)Protocol;
        set => Protocol = (ProtocolType)value;
    }

    partial void OnProtocolChanged(ProtocolType value)
    {
        OnPropertyChanged(nameof(IsTcpMode));
        OnPropertyChanged(nameof(IsRtuMode));
        OnPropertyChanged(nameof(ProtocolIndex));
        // 切换协议时自动刷新对应的地址/端口列表
        if (value == ProtocolType.Tcp)
            RefreshTcpAddresses();
        else
            RefreshComPorts();
    }

    // ----------------------------------------------------------------
    // 运行状态
    // ----------------------------------------------------------------

    [ObservableProperty] private bool   _isRunning        = false;
    [ObservableProperty] private long   _requestCount     = 0;
    [ObservableProperty] private string _statusText       = "未启动";

    // ----------------------------------------------------------------
    // 设备列表（左侧选择器）
    // ----------------------------------------------------------------

    public ObservableCollection<DeviceViewModelBase> DeviceList { get; } = new();

    [ObservableProperty] private DeviceViewModelBase? _selectedDevice;

    /// <summary>根据选中设备返回对应的 UserControl 面板（ContentControl 内容）</summary>
    public UserControl? SelectedDevicePanel => SelectedDevice == null ? null
        : _panelCache.GetValueOrDefault(SelectedDevice.GetType());

    partial void OnSelectedDeviceChanged(DeviceViewModelBase? value)
    {
        OnPropertyChanged(nameof(SelectedDevicePanel));
    }

    // 面板缓存（类型 → UserControl 实例）
    private readonly Dictionary<Type, UserControl> _panelCache = new();

    // 快速访问各设备 ViewModel
    public PcsViewModel             PcsVm       { get; }
    public BmsViewModel             BmsVm       { get; }
    public MpptViewModel            MpptVm      { get; }
    public AirConditionerViewModel  AirVm       { get; }
    public DehumidifierViewModel    DehumVm     { get; }
    public ExternalMeterViewModel   ExtMeterVm  { get; }
    public StorageMeterViewModel    StorMeterVm { get; }
    public StsInstrumentViewModel   StsInstVm   { get; }
    public StsControlViewModel      StsCtrlVm   { get; }
    public DIDOControllerViewModel  DiDoVm      { get; }
    public DieselGeneratorViewModel DieselVm    { get; }
    public GasDetectorViewModel     GasVm       { get; }

    // TCP 本地 IP 列表
    public ObservableCollection<string> AvailableTcpAddresses { get; } = new();

    // 串口列表 / 波特率
    public ObservableCollection<string> AvailableComPorts { get; } = new();
    public IReadOnlyList<int> BaudRateOptions { get; } = new[] { 4800, 9600, 19200, 38400, 115200 };

    // ----------------------------------------------------------------
    // 日志
    // ----------------------------------------------------------------

    public ObservableCollection<LogEntry> LogEntries { get; } = new();

    // ----------------------------------------------------------------
    // 构造
    // ----------------------------------------------------------------

    public SlaveViewModel(
        IServiceProvider         services,
        PcsViewModel             pcsVm,
        BmsViewModel             bmsVm,
        MpptViewModel            mpptVm,
        AirConditionerViewModel  airVm,
        DehumidifierViewModel    dehumVm,
        ExternalMeterViewModel   extMeterVm,
        StorageMeterViewModel    storMeterVm,
        StsInstrumentViewModel   stsInstVm,
        StsControlViewModel      stsCtrlVm,
        DIDOControllerViewModel  diDoVm,
        DieselGeneratorViewModel dieselVm,
        GasDetectorViewModel     gasVm,
        RegisterInspectorViewModel inspectorVm)
    {
        _services   = services;
        PcsVm       = pcsVm;
        BmsVm       = bmsVm;
        MpptVm      = mpptVm;
        AirVm       = airVm;
        DehumVm     = dehumVm;
        ExtMeterVm  = extMeterVm;
        StorMeterVm = storMeterVm;
        StsInstVm   = stsInstVm;
        StsCtrlVm   = stsCtrlVm;
        DiDoVm      = diDoVm;
        DieselVm    = dieselVm;
        GasVm       = gasVm;

        // 注册设备列表及对应面板
        RegisterDevice(pcsVm,       () => new PcsPanel       { DataContext = pcsVm       });
        RegisterDevice(bmsVm,       () => new BmsPanel       { DataContext = bmsVm       });
        RegisterDevice(mpptVm,      () => new MpptPanel      { DataContext = mpptVm      });
        RegisterDevice(airVm,       () => new AirConditionerPanel { DataContext = airVm  });
        RegisterDevice(dehumVm,     () => new DehumidifierPanel   { DataContext = dehumVm });
        RegisterDevice(extMeterVm,  () => new ExternalMeterPanel  { DataContext = extMeterVm  });
        RegisterDevice(storMeterVm, () => new StorageMeterPanel   { DataContext = storMeterVm });
        RegisterDevice(stsInstVm,   () => new StsInstrumentPanel  { DataContext = stsInstVm  });
        RegisterDevice(stsCtrlVm,   () => new StsControlPanel     { DataContext = stsCtrlVm  });
        RegisterDevice(diDoVm,      () => new DIDOControllerPanel  { DataContext = diDoVm    });
        RegisterDevice(dieselVm,    () => new DieselGeneratorPanel { DataContext = dieselVm  });
        RegisterDevice(gasVm,       () => new GasDetectorPanel       { DataContext = gasVm       });
        RegisterDevice(inspectorVm, () => new RegisterInspectorPanel { DataContext = inspectorVm });

        SelectedDevice = DeviceList.FirstOrDefault();
        RefreshTcpAddresses();   // 初始化时检测本地 IP（默认 TCP 模式）
        RefreshComPorts();

        // 订阅 AppLogger 日志事件 → 追加到 LogEntries（UI 线程安全）
        AppLogger.OnUiLog += (level, message) =>
        {
            var logLevel = level switch
            {
                "WARN"  => LogLevel.Warn,
                "ERROR" => LogLevel.Error,
                _       => LogLevel.Info
            };
            var entry = LogEntry.Create(logLevel, message);
            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                if (LogEntries.Count >= 500)
                    LogEntries.RemoveAt(0);
                LogEntries.Add(entry);
            });
        };
    }

    private void RegisterDevice(DeviceViewModelBase vm, Func<UserControl> panelFactory)
    {
        DeviceList.Add(vm);
        try
        {
            _panelCache[vm.GetType()] = panelFactory();
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[RegisterDevice] 设备面板创建失败：{vm.DeviceName} — {ex.Message}", ex);
        }
    }

    // ----------------------------------------------------------------
    // 命令：启停（Toggle）
    // ----------------------------------------------------------------

    [RelayCommand]
    public async Task ToggleSlaveAsync()
    {
        if (IsRunning) await StopSlaveAsync();
        else           await StartSlaveAsync();
    }

    [RelayCommand]
    public async Task StartSlaveAsync()
    {
        if (IsRunning) return;
        try
        {
            if (Protocol == ProtocolType.Tcp)
            {
                var svc = _services.GetRequiredService<TcpSlaveService>();
                svc.ListenAddress = ListenAddress;
                svc.Port          = Port;
                svc.OnRequest    += OnRequest;
                _currentService   = svc;
            }
            else
            {
                var svc = _services.GetRequiredService<RtuSlaveService>();
                svc.PortName  = ComPort;
                svc.BaudRate  = BaudRate;
                svc.OnRequest += OnRequest;
                _currentService = svc;
            }

            await _currentService.StartAsync(SlaveId);
            IsRunning  = true;
            StatusText = Protocol == ProtocolType.Tcp
                ? $"监听中  {ListenAddress}:{Port}"
                : $"监听中  {ComPort}@{BaudRate}";
            AppLogger.Info($"从站已启动（{Protocol}）");
        }
        catch (Exception ex)
        {
            StatusText = $"启动失败：{ex.Message}";
            AppLogger.Error("从站启动失败", ex);
            MessageBox.Show($"从站启动失败：\n{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public async Task StopSlaveAsync()
    {
        if (!IsRunning || _currentService == null) return;
        await _currentService.StopAsync();
        _currentService = null;
        IsRunning  = false;
        StatusText = "已停止";
        AppLogger.Info("从站已停止");
    }

    // ----------------------------------------------------------------
    // 命令：刷新串口列表
    // ----------------------------------------------------------------

    [RelayCommand]
    public void RefreshComPorts()
    {
        AvailableComPorts.Clear();
        foreach (var port in SerialPort.GetPortNames())
            AvailableComPorts.Add(port);
        if (AvailableComPorts.Count > 0 && !AvailableComPorts.Contains(ComPort))
            ComPort = AvailableComPorts[0];
    }

    [RelayCommand]
    public void RefreshTcpAddresses()
    {
        AvailableTcpAddresses.Clear();
        // "0.0.0.0" 表示监听所有网卡，始终放第一位；127.0.0.1 用于本机自测
        AvailableTcpAddresses.Add("0.0.0.0");
        AvailableTcpAddresses.Add("127.0.0.1");
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                foreach (var addr in nic.GetIPProperties().UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        AvailableTcpAddresses.Add(addr.Address.ToString());
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.Warn($"枚举本地 IP 失败：{ex.Message}");
        }
        // 若当前地址不在列表中，重置为 0.0.0.0
        if (!AvailableTcpAddresses.Contains(ListenAddress))
            ListenAddress = "0.0.0.0";
    }

    // ----------------------------------------------------------------
    // 命令：导出当前设备 Excel
    // ----------------------------------------------------------------

    [RelayCommand]
    public void ImportDeviceExcel()
    {
        if (SelectedDevice == null)
        {
            MessageBox.Show("请先在左侧选择一个设备", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var dlg = new OpenFileDialog
        {
            Filter = "Excel 文件|*.xlsx;*.xls",
            Title  = $"选择要导入的 Excel 文件（{SelectedDevice.DeviceName}）"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            ExcelHelper.ImportDeviceViewModel(dlg.FileName, SelectedDevice);
            AppLogger.Info($"{SelectedDevice.DeviceName} 设备 Excel 已导入 ← {dlg.FileName}");
        }
        catch (Exception ex)
        {
            AppLogger.Error($"导入失败：{ex.Message}", ex);
            MessageBox.Show($"导入失败：\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public void ExportDeviceExcel()
    {
        if (SelectedDevice == null) return;
        var dlg = new SaveFileDialog
        {
            Filter   = "Excel 文件|*.xlsx",
            FileName = $"{SelectedDevice.DeviceName}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            ExcelHelper.ExportDeviceViewModel(dlg.FileName, SelectedDevice);
            AppLogger.Info($"设备 Excel 已导出 → {dlg.FileName}");
        }
        catch (Exception ex)
        {
            AppLogger.Error($"导出失败：{ex.Message}", ex);
        }
    }

    // ----------------------------------------------------------------
    // 命令：导出快照
    // ----------------------------------------------------------------

    [RelayCommand]
    public void ExportSnapshot()
    {
        var dlg = new SaveFileDialog
        {
            Filter   = "JSON 快照|*.json",
            FileName = $"Snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var mapSvc = _services.GetRequiredService<RegisterMapService>();
            mapSvc.SaveSnapshot(dlg.FileName, DeviceList);
            AppLogger.Info($"快照已导出 → {dlg.FileName}");
        }
        catch (Exception ex)
        {
            AppLogger.Error($"快照导出失败：{ex.Message}", ex);
        }
    }

    // ----------------------------------------------------------------
    // 命令：导入快照
    // ----------------------------------------------------------------

    [RelayCommand]
    public void ImportSnapshot()
    {
        var dlg = new OpenFileDialog { Filter = "JSON 快照|*.json" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var mapSvc = _services.GetRequiredService<RegisterMapService>();
            mapSvc.LoadSnapshot(dlg.FileName, DeviceList);
            AppLogger.Info($"快照已导入 ← {dlg.FileName}");
        }
        catch (Exception ex)
        {
            AppLogger.Error($"快照导入失败：{ex.Message}", ex);
        }
    }

    // ----------------------------------------------------------------
    // 命令：导入自定义设备 Excel
    // ----------------------------------------------------------------

    [RelayCommand]
    public void ImportCustomDevice()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Excel 文件|*.xlsx;*.xls",
            Title  = "选择自定义设备 Excel（地址/字段/类型/比例系数）"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var customVm = ExcelHelper.ImportCustomDevice(dlg.FileName,
                _services.GetRequiredService<Shared.Services.RegisterBank>());
            var panel = new CustomDevicePanel { DataContext = customVm };
            DeviceList.Add(customVm);
            _panelCache[customVm.GetType()] = panel;
            SelectedDevice = customVm;
            AppLogger.Info($"已导入自定义设备：{customVm.DeviceName}（{dlg.FileName}）");
        }
        catch (Exception ex)
        {
            AppLogger.Error($"自定义设备导入失败：{ex.Message}", ex);
            MessageBox.Show($"导入失败：\n{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ----------------------------------------------------------------
    // 命令：清空日志
    // ----------------------------------------------------------------

    [RelayCommand]
    public void ClearLog() => LogEntries.Clear();

    // ----------------------------------------------------------------
    // 请求计数回调
    // ----------------------------------------------------------------

    private void OnRequest(byte fc, int addr, int qty, string source)
    {
        RequestCount++;
        AppLogger.ModbusRequest(fc, addr, qty, SlaveId, source);
    }
}
