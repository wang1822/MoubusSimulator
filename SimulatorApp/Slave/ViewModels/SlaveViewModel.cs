using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SimulatorApp.Shared.Helpers;
using SimulatorApp.Shared.Logging;
using SimulatorApp.Shared.Models;
using SimulatorApp.Shared.Services;
using SimulatorApp.Slave.Services;
using SimulatorApp.Slave.Views.Panels;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace SimulatorApp.Slave.ViewModels;

/// <summary>
/// 从站主 ViewModel：管理多条监听配置、所有设备 ViewModel、设备面板路由。
/// </summary>
public partial class SlaveViewModel : ObservableObject
{
    private readonly IServiceProvider _services;
    private readonly RegisterBank     _bank;
    private DispatcherTimer?          _simTimer;
    private int                       _runningCount;

    // ----------------------------------------------------------------
    // 监听配置集合
    // ----------------------------------------------------------------

    /// <summary>所有监听端点配置，支持多条同时运行</summary>
    public ObservableCollection<SlaveListenerConfig> Listeners { get; } = new();

    /// <summary>任意一条监听处于运行状态即为 true</summary>
    public bool IsRunning => _runningCount > 0;

    /// <summary>请求总计数（所有监听合计）</summary>
    [ObservableProperty] private long _requestCount = 0;

    // ----------------------------------------------------------------
    // 设备列表
    // ----------------------------------------------------------------

    public ObservableCollection<DeviceViewModelBase> DeviceList { get; } = new();

    [ObservableProperty] private DeviceViewModelBase? _selectedDevice;

    public UserControl? SelectedDevicePanel => SelectedDevice == null ? null
        : _panelCache.GetValueOrDefault(SelectedDevice);

    partial void OnSelectedDeviceChanged(DeviceViewModelBase? value)
        => OnPropertyChanged(nameof(SelectedDevicePanel));

    // 以实例为键，支持同类型多设备
    private readonly Dictionary<DeviceViewModelBase, UserControl> _panelCache = new();
    // 面板工厂，导入时用于为新实例创建面板（key=VM类型）
    private readonly Dictionary<Type, Func<DeviceViewModelBase, UserControl>> _panelFactories = new();

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

    // ----------------------------------------------------------------
    // 可用地址 / 串口（DataTemplate 通过 RelativeSource 绑定）
    // ----------------------------------------------------------------

    public ObservableCollection<string> AvailableTcpAddresses { get; } = new();
    public ObservableCollection<string> AvailableComPorts     { get; } = new();
    public IReadOnlyList<int>           BaudRateOptions       { get; } = [4800, 9600, 19200, 38400, 115200];

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
        _services = services;
        _bank     = services.GetRequiredService<RegisterBank>();

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

        // 注册设备及面板（lambda 接收 vm 参数，面板工厂可复用于动态导入）
        RegisterDevice(pcsVm,       vm => new PcsPanel              { DataContext = vm });
        RegisterDevice(bmsVm,       vm => new BmsPanel              { DataContext = vm });
        RegisterDevice(mpptVm,      vm => new MpptPanel             { DataContext = vm });
        RegisterDevice(airVm,       vm => new AirConditionerPanel   { DataContext = vm });
        RegisterDevice(dehumVm,     vm => new DehumidifierPanel     { DataContext = vm });
        RegisterDevice(extMeterVm,  vm => new ExternalMeterPanel    { DataContext = vm });
        RegisterDevice(storMeterVm, vm => new StorageMeterPanel     { DataContext = vm });
        RegisterDevice(stsInstVm,   vm => new StsInstrumentPanel    { DataContext = vm });
        RegisterDevice(stsCtrlVm,   vm => new StsControlPanel       { DataContext = vm });
        RegisterDevice(diDoVm,      vm => new DIDOControllerPanel   { DataContext = vm });
        RegisterDevice(dieselVm,    vm => new DieselGeneratorPanel  { DataContext = vm });
        RegisterDevice(gasVm,       vm => new GasDetectorPanel      { DataContext = vm });
        RegisterDevice(inspectorVm, vm => new RegisterInspectorPanel{ DataContext = vm });

        SelectedDevice = DeviceList.FirstOrDefault();

        // 默认添加一条 TCP 监听配置
        Listeners.Add(new SlaveListenerConfig());

        RefreshTcpAddresses();
        RefreshComPorts();

        AppLogger.OnUiLog += (level, message) =>
        {
            var logLevel = level switch { "WARN" => LogLevel.Warn, "ERROR" => LogLevel.Error, _ => LogLevel.Info };
            Application.Current?.Dispatcher.InvokeAsync(() =>
            {
                if (LogEntries.Count >= 500) LogEntries.RemoveAt(0);
                LogEntries.Add(LogEntry.Create(logLevel, message));
            });
        };
    }

    private void RegisterDevice(DeviceViewModelBase vm, Func<DeviceViewModelBase, UserControl> panelFactory)
    {
        DeviceList.Add(vm);
        _panelFactories[vm.GetType()] = panelFactory;
        try   { _panelCache[vm] = panelFactory(vm); }
        catch (Exception ex) { AppLogger.Error($"[RegisterDevice] 面板创建失败：{vm.DeviceName} — {ex.Message}", ex); }
    }

    // ----------------------------------------------------------------
    // 命令：监听配置管理
    // ----------------------------------------------------------------

    [RelayCommand]
    public void AddListener()
        => Listeners.Add(new SlaveListenerConfig { Port = 502 + Listeners.Count });

    [RelayCommand]
    public async Task RemoveListenerAsync(SlaveListenerConfig config)
    {
        if (config.IsRunning) await StopListenerCoreAsync(config);
        Listeners.Remove(config);
    }

    // ----------------------------------------------------------------
    // 命令：单条启停（Toggle）
    // ----------------------------------------------------------------

    [RelayCommand]
    public async Task ToggleListenerAsync(SlaveListenerConfig config)
    {
        if (config.IsRunning) await StopListenerCoreAsync(config);
        else                  await StartListenerCoreAsync(config);
    }

    // ----------------------------------------------------------------
    // 命令：全部启动 / 全部停止
    // ----------------------------------------------------------------

    [RelayCommand]
    public async Task StartAllListenersAsync()
    {
        foreach (var cfg in Listeners.Where(c => c.IsEnabled && !c.IsRunning).ToList())
            await StartListenerCoreAsync(cfg);
    }

    [RelayCommand]
    public async Task StopAllListenersAsync()
    {
        foreach (var cfg in Listeners.Where(c => c.IsRunning).ToList())
            await StopListenerCoreAsync(cfg);
    }

    // ----------------------------------------------------------------
    // 核心启停逻辑
    // ----------------------------------------------------------------

    private async Task StartListenerCoreAsync(SlaveListenerConfig config)
    {
        if (config.IsRunning) return;
        try
        {
            ISlaveService svc;
            if (config.Protocol == ProtocolType.Tcp)
            {
                var tcpSvc = _services.GetRequiredService<TcpSlaveService>();
                tcpSvc.ListenAddress = config.ListenAddress;
                tcpSvc.Port          = config.Port;
                tcpSvc.OnRequest    += OnRequest;
                svc = tcpSvc;
            }
            else
            {
                var rtuSvc = _services.GetRequiredService<RtuSlaveService>();
                rtuSvc.PortName  = config.ComPort;
                rtuSvc.BaudRate  = config.BaudRate;
                rtuSvc.OnRequest += OnRequest;
                svc = rtuSvc;
            }

            // 启动前清零寄存器，只刷勾选设备
            _bank.ClearAll();
            foreach (var vm in DeviceList.Where(v => v.IsSimulating))
                vm.FlushToRegisters();

            await svc.StartAsync(config.SlaveId);
            config.Service    = svc;
            config.IsRunning  = true;
            config.StatusText = config.Protocol == ProtocolType.Tcp
                ? $"监听中  {config.ListenAddress}:{config.Port}"
                : $"监听中  {config.ComPort}@{config.BaudRate}";

            _runningCount++;
            OnPropertyChanged(nameof(IsRunning));

            // 第一条监听启动时开启模拟定时器
            if (_runningCount == 1)
            {
                _simTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
                _simTimer.Tick += (_, _) =>
                {
                    foreach (var vm in DeviceList)
                        if (vm.IsSimulating) vm.GenerateData();
                };
                _simTimer.Start();
            }

            AppLogger.Info($"监听已启动（{config.Protocol}）SlaveID={config.SlaveId}");
        }
        catch (Exception ex)
        {
            config.StatusText = $"启动失败：{ex.Message}";
            AppLogger.Error("监听启动失败", ex);
            MessageBox.Show($"监听启动失败：\n{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task StopListenerCoreAsync(SlaveListenerConfig config)
    {
        if (!config.IsRunning || config.Service == null) return;
        config.Service.OnRequest -= OnRequest;
        await config.Service.StopAsync();
        config.Service    = null;
        config.IsRunning  = false;
        config.StatusText = "已停止";

        _runningCount = Math.Max(0, _runningCount - 1);
        OnPropertyChanged(nameof(IsRunning));

        // 最后一条停止时关闭模拟定时器
        if (_runningCount == 0)
        {
            _simTimer?.Stop();
            _simTimer = null;
        }

        AppLogger.Info($"监听已停止（{config.Protocol}）");
    }

    // ----------------------------------------------------------------
    // 命令：刷新串口 / IP 列表
    // ----------------------------------------------------------------

    [RelayCommand]
    public void RefreshComPorts()
    {
        AvailableComPorts.Clear();
        foreach (var p in SerialPort.GetPortNames()) AvailableComPorts.Add(p);
    }

    [RelayCommand]
    public void RefreshTcpAddresses()
    {
        AvailableTcpAddresses.Clear();
        AvailableTcpAddresses.Add("0.0.0.0");
        AvailableTcpAddresses.Add("127.0.0.1");
        try
        {
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up) continue;
                foreach (var addr in nic.GetIPProperties().UnicastAddresses)
                    if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        AvailableTcpAddresses.Add(addr.Address.ToString());
            }
        }
        catch (Exception ex) { AppLogger.Warn($"枚举本地 IP 失败：{ex.Message}"); }
    }

    // ----------------------------------------------------------------
    // 命令：导出设备 Excel（所有已勾选设备 → 多 Sheet）
    // ----------------------------------------------------------------

    [RelayCommand]
    public void ImportDeviceExcel()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Excel 文件|*.xlsx;*.xls",
            Title  = "选择要导入的设备 Excel 文件"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var (deviceName, rows) = ExcelHelper.ParseRowsFromFile(dlg.FileName);
            if (rows.Count == 0)
            {
                MessageBox.Show("文件中未找到有效数据行。\n请确认文件格式与导出格式一致（第5行起为数据行）。",
                    "导入失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            AddImportedDevice(deviceName, rows);
            AppLogger.Info($"已导入 {deviceName}（{rows.Count} 行）← {dlg.FileName}");
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
        var checkedDevices = DeviceList.Where(v => v.IsSimulating).ToList();
        if (checkedDevices.Count == 0)
        {
            MessageBox.Show("没有已勾选的设备，无法导出。\n请先勾选至少一个设备。",
                "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title = $"选择导出目录（将保存 {checkedDevices.Count} 个设备的 Excel 文件）"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var saved = ExcelHelper.ExportDeviceViewModelsToFolder(dlg.FolderName, checkedDevices);
            AppLogger.Info($"设备 Excel 已导出（{saved.Count} 个文件）→ {dlg.FolderName}");
            MessageBox.Show($"已导出 {saved.Count} 个文件至：\n{dlg.FolderName}",
                "导出完成", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AppLogger.Error($"导出失败：{ex.Message}", ex);
            MessageBox.Show($"导出失败：\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public void PasteImportDevice()
    {
        var text = Clipboard.GetText();
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show("剪贴板为空，请先在 Excel 中选中并复制数据行，再点此按钮。",
                "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            var rows = ExcelHelper.ParseRowsFromClipboard(text);
            if (rows.Count == 0)
            {
                MessageBox.Show("未解析到有效数据行。\n请确认已复制含「地址」列和「值」列的表格数据。",
                    "解析失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            AddImportedDevice("粘贴数据", rows);
            AppLogger.Info($"已从剪贴板导入（{rows.Count} 行）");
        }
        catch (Exception ex)
        {
            AppLogger.Error($"粘贴导入失败：{ex.Message}", ex);
            MessageBox.Show($"粘贴导入失败：\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>创建导入设备 ViewModel，追加到列表底部并自动选中</summary>
    private void AddImportedDevice(string deviceName,
        IEnumerable<(string ChineseName, int Address, double Value)> rows)
    {
        var bank   = _services.GetRequiredService<RegisterBank>();
        var mapSvc = _services.GetRequiredService<RegisterMapService>();
        var vm     = new ImportedDeviceViewModel(bank, mapSvc, deviceName, rows);
        _panelCache[vm] = new ImportedDevicePanel { DataContext = vm };
        DeviceList.Add(vm);
        SelectedDevice = vm;
    }

    // ----------------------------------------------------------------
    // 命令：快照
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
            var checkedDevices = DeviceList.Where(v => v.IsSimulating).ToList();
            if (checkedDevices.Count == 0)
            {
                MessageBox.Show("没有已勾选的设备，无法导出快照。\n请先勾选至少一个设备。",
                    "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var mapSvc = _services.GetRequiredService<RegisterMapService>();
            mapSvc.SaveSnapshot(dlg.FileName, checkedDevices);
            AppLogger.Info($"快照已导出（{checkedDevices.Count} 个设备）→ {dlg.FileName}");
        }
        catch (Exception ex) { AppLogger.Error($"快照导出失败：{ex.Message}", ex); }
    }

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
        catch (Exception ex) { AppLogger.Error($"快照导入失败：{ex.Message}", ex); }
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
        AppLogger.ModbusRequest(fc, addr, qty, 0, source);
    }
}
