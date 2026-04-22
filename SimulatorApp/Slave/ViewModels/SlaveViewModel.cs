using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using SimulatorApp.Shared.Helpers;
using SimulatorApp.Shared.Logging;
using SimulatorApp.Shared.Models;
using SimulatorApp.Shared.Services;
using SimulatorApp.Shared.Views;
using SimulatorApp.Slave.Services;
using SimulatorApp.Slave.Views.Panels;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Net.NetworkInformation;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

// Row type alias for protocol import tuples
using ProtocolRow = (string ChineseName, string EnglishName, int Address, string ReadWrite, string Range, string Unit, string Note);

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
    public ObservableCollection<ImportedDeviceViewModel> ImportedDevices { get; } = new();
    public bool HasImportedDevices => ImportedDevices.Count > 0;
    public ObservableCollection<DeviceViewModelBase> BuiltinDevices { get; } = new();
    public IReadOnlyList<DeviceViewModelBase> InspectorList { get; private set; } = [];

    // ── DB 持久化 ────────────────────────────────────────────────────
    [ObservableProperty] private string _slaveDbConnectionString =
        "Server=10.184.4.153,1433;Database=ModBusT;User Id=sa;Password=000000;Encrypt=True;TrustServerCertificate=True;Connect Timeout=10;";
    [ObservableProperty] private bool   _isSlaveDbConnected = false;
    [ObservableProperty] private string _slaveDbStatusText  = "未连接数据库";
    private ISlaveProtocolDbService? _slaveDbService;

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
        InspectorList = [inspectorVm];

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
        if (vm is not RegisterInspectorViewModel)
            BuiltinDevices.Add(vm);
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
            ThemedMessageBox.Show($"监听启动失败：\n{ex.Message}", "错误",
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
    // 命令：协议文档格式导入（粘贴 / 文件）
    // ----------------------------------------------------------------

    [RelayCommand]
    public async Task ConnectSlaveDbAsync()
    {
        if (string.IsNullOrWhiteSpace(SlaveDbConnectionString))
        {
            SlaveDbStatusText = "请输入连接字符串";
            return;
        }
        try
        {
            SlaveDbStatusText = "连接中…";
            var svc = new SlaveProtocolDbService(SlaveDbConnectionString);
            await svc.InitializeAsync();
            _slaveDbService   = svc;
            IsSlaveDbConnected = true;
            SlaveDbStatusText  = "数据库已连接";
            AppLogger.Info("从站协议 DB 连接成功");
            await LoadProtocolDevicesFromDbAsync();
        }
        catch (Exception ex)
        {
            IsSlaveDbConnected = false;
            SlaveDbStatusText  = $"连接失败：{ex.Message}";
            AppLogger.Error($"从站协议 DB 连接失败：{ex.Message}", ex);
        }
    }

    private async Task LoadProtocolDevicesFromDbAsync()
    {
        if (_slaveDbService == null) return;
        try
        {
            var devices = await _slaveDbService.GetAllDevicesAsync();
            foreach (var (name, rows) in devices)
                AddProtocolDevice(name, rows, saveToDb: false);
            if (devices.Count > 0)
                AppLogger.Info($"已从数据库加载 {devices.Count} 个协议设备");
        }
        catch (Exception ex)
        {
            AppLogger.Error($"加载协议设备失败：{ex.Message}", ex);
        }
    }

    /// <summary>创建协议文档导入设备 ViewModel，追加到列表并自动选中</summary>
    private void AddProtocolDevice(string deviceName,
        IEnumerable<ProtocolRow> rows,
        bool saveToDb = true)
    {
        var rowList = rows.ToList();
        var bank    = _services.GetRequiredService<RegisterBank>();
        var mapSvc  = _services.GetRequiredService<RegisterMapService>();
        var vm      = new ImportedDeviceViewModel(bank, mapSvc, deviceName, rowList);
        _panelCache[vm] = new ImportedDevicePanel { DataContext = vm };
        DeviceList.Add(vm);
        ImportedDevices.Add(vm);
        OnPropertyChanged(nameof(HasImportedDevices));
        SelectedDevice = vm;

        if (saveToDb && _slaveDbService != null)
            _ = _slaveDbService.UpsertDeviceAsync(vm.DeviceName, rowList);
    }

    [RelayCommand]
    public void PasteImportProtocol()
    {
        var text = Clipboard.GetText();
        if (string.IsNullOrWhiteSpace(text))
        {
            ThemedMessageBox.Show(
                "剪贴板为空。\n请先在 Excel 协议文档中选中寄存器行（含标题行）并复制，再点此按钮。",
                "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            var rows = ExcelHelper.ParseProtocolRowsFromClipboard(text);
            if (rows.Count == 0)
            {
                ThemedMessageBox.Show(
                    "未解析到有效数据行。\n请确认复制了含「Addr」标题行的协议表格（格式：Addr | 中文名 | 英文名 | R/W | 范围 | 单位 | 备注）。",
                    "解析失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            AddProtocolDevice("协议导入", rows);
            AppLogger.Info($"已从剪贴板导入协议格式（{rows.Count} 行）");
        }
        catch (Exception ex)
        {
            AppLogger.Error($"粘贴协议导入失败：{ex.Message}", ex);
            ThemedMessageBox.Show($"粘贴协议导入失败：\n{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public void ImportProtocolExcel()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Excel 文件|*.xlsx;*.xls",
            Title  = "选择协议文档 Excel（如 MPPT_Modbus_V1.0.xlsx）"
        };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var (deviceName, rows) = ExcelHelper.ParseProtocolRowsFromFile(dlg.FileName);
            if (rows.Count == 0)
            {
                ThemedMessageBox.Show(
                    "文件中未找到有效协议数据行。\n请确认文件中存在「Addr | 中文名 | 英文名 | R/W | 范围 | 单位 | 备注」格式的寄存器表。",
                    "导入失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            AddProtocolDevice(deviceName, rows);
            AppLogger.Info($"已导入协议文档 {deviceName}（{rows.Count} 行）← {dlg.FileName}");
        }
        catch (Exception ex)
        {
            AppLogger.Error($"协议 Excel 导入失败：{ex.Message}", ex);
            ThemedMessageBox.Show($"协议 Excel 导入失败：\n{ex.Message}", "错误",
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
        AppLogger.ModbusRequest(fc, addr, qty, 0, source);
    }
}
