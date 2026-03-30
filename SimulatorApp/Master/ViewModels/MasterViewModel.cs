using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using SimulatorApp.Master.Models;
using SimulatorApp.Master.Services;
using SimulatorApp.Shared.Helpers;
using SimulatorApp.Shared.Logging;
using SimulatorApp.Shared.Models;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Windows;
using System.Windows.Threading;

namespace SimulatorApp.Master.ViewModels;

/// <summary>
/// 主站 ViewModel — 连接配置、轮询数据展示、写寄存器操作
/// </summary>
public partial class MasterViewModel : ObservableObject
{
    // ----------------------------------------------------------------
    // 连接参数
    // ----------------------------------------------------------------

    [ObservableProperty] private ProtocolType _protocol       = ProtocolType.Tcp;
    [ObservableProperty] private string  _remoteHost          = "127.0.0.1";
    [ObservableProperty] private int     _remotePort          = 502;
    [ObservableProperty] private byte    _slaveId             = 1;
    [ObservableProperty] private string  _comPort             = "COM3";
    [ObservableProperty] private int     _baudRate            = 9600;
    [ObservableProperty] private int     _startAddress        = 7296;
    [ObservableProperty] private int     _quantity            = 100;
    [ObservableProperty] private int     _pollIntervalMs      = 1000;

    // 协议模式辅助属性（用于 XAML 显示/隐藏）
    public bool IsTcpMode => Protocol == ProtocolType.Tcp;
    public bool IsRtuMode => Protocol == ProtocolType.Rtu;

    partial void OnProtocolChanged(ProtocolType value)
    {
        OnPropertyChanged(nameof(IsTcpMode));
        OnPropertyChanged(nameof(IsRtuMode));
    }

    // ----------------------------------------------------------------
    // 运行状态
    // ----------------------------------------------------------------

    [ObservableProperty] private bool   _isConnected         = false;
    [ObservableProperty] private string _statusText          = "未连接";
    [ObservableProperty] private long   _pollCount           = 0;

    // ----------------------------------------------------------------
    // 写寄存器操作（单个）
    // ----------------------------------------------------------------

    [ObservableProperty] private int    _writeAddress        = 0;
    [ObservableProperty] private ushort _writeValue          = 0;

    // ----------------------------------------------------------------
    // 轮询数据表格
    // ----------------------------------------------------------------

    public ObservableCollection<RegisterRow> Registers { get; } = new();

    // ----------------------------------------------------------------
    // 日志
    // ----------------------------------------------------------------

    public ObservableCollection<LogEntry> LogEntries { get; } = new();

    // ----------------------------------------------------------------
    // 串口列表（RTU 模式）
    // ----------------------------------------------------------------

    public ObservableCollection<string> AvailableComPorts { get; } = new();
    public IReadOnlyList<int> BaudRateOptions { get; } = new[] { 4800, 9600, 19200, 38400, 115200 };

    // 协议下拉选项
    public IReadOnlyList<ComboItem<ProtocolType>> ProtocolItems { get; } = new List<ComboItem<ProtocolType>>
    {
        new("TCP",  ProtocolType.Tcp),
        new("RTU",  ProtocolType.Rtu),
    };

    // ----------------------------------------------------------------
    // 私有成员
    // ----------------------------------------------------------------

    private IMasterService? _service;
    private CancellationTokenSource? _cts;
    private readonly Dispatcher _dispatcher = Dispatcher.CurrentDispatcher;

    public MasterViewModel() => RefreshComPorts();

    // ----------------------------------------------------------------
    // 命令：连接 / 断开
    // ----------------------------------------------------------------

    [RelayCommand]
    public async Task ConnectAsync()
    {
        if (IsConnected) return;
        try
        {
            var endpoint = BuildEndpoint();
            _cts = new CancellationTokenSource();
            _service = Protocol == ProtocolType.Tcp
                ? new TcpMasterService()
                : new RtuMasterService();

            _service.OnPollCompleted += OnPollCompleted;
            _service.OnError        += OnServiceError;

            await _service.ConnectAndStartPollingAsync(endpoint, _cts.Token);
            IsConnected = true;
            StatusText  = Protocol == ProtocolType.Tcp
                ? $"已连接 {RemoteHost}:{RemotePort}"
                : $"已连接 {ComPort}@{BaudRate}";
            AddLog(LogLevel.Info, StatusText);
        }
        catch (Exception ex)
        {
            StatusText = $"连接失败：{ex.Message}";
            AddLog(LogLevel.Error, $"连接失败：{ex.Message}");
            MessageBox.Show($"主站连接失败：\n{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public async Task DisconnectAsync()
    {
        if (!IsConnected || _service == null) return;
        _cts?.Cancel();
        await _service.DisconnectAsync();
        await _service.DisposeAsync();
        _service    = null;
        IsConnected = false;
        StatusText  = "已断开";
        AddLog(LogLevel.Info, "主站已断开连接");
    }

    // ----------------------------------------------------------------
    // 命令：切换连接（XAML 绑定到 ToggleMasterCommand）
    // ----------------------------------------------------------------

    [RelayCommand]
    public async Task ToggleMasterAsync()
    {
        if (IsConnected) await DisconnectAsync();
        else             await ConnectAsync();
    }

    // ----------------------------------------------------------------
    // 命令：写单个寄存器
    // ----------------------------------------------------------------

    [RelayCommand]
    public async Task WriteRegisterAsync()
    {
        if (_service == null || !IsConnected)
        {
            MessageBox.Show("请先连接从站", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            await _service.WriteSingleRegisterAsync(WriteAddress, WriteValue);
            AddLog(LogLevel.Info, $"FC06 写寄存器  addr={WriteAddress}  value={WriteValue}");
        }
        catch (Exception ex)
        {
            AddLog(LogLevel.Error, $"写寄存器失败：{ex.Message}");
        }
    }

    // ----------------------------------------------------------------
    // 命令：刷新串口列表
    // ----------------------------------------------------------------

    [RelayCommand]
    public void RefreshComPorts()
    {
        AvailableComPorts.Clear();
        foreach (var p in SerialPort.GetPortNames())
            AvailableComPorts.Add(p);
        if (AvailableComPorts.Count > 0 && !AvailableComPorts.Contains(ComPort))
            ComPort = AvailableComPorts[0];
    }

    // ----------------------------------------------------------------
    // 命令：导入设备 Excel 模板（配置轮询地址/描述）
    // ----------------------------------------------------------------

    [RelayCommand]
    public void ImportDeviceExcel()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Excel 文件|*.xlsx;*.xls",
            Title  = "选择主站轮询数据 Excel（可导入本站之前导出的文件）"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            var rows = ExcelHelper.ImportPollData(dlg.FileName);
            Registers.Clear();
            foreach (var (addr, desc, rawVal) in rows)
                Registers.Add(new RegisterRow { Address = addr, Description = desc, RawValue = rawVal });
            AddLog(LogLevel.Info, $"导入成功，共 {Registers.Count} 行");
        }
        catch (Exception ex)
        {
            AddLog(LogLevel.Error, $"导入失败：{ex.Message}");
        }
    }

    // ----------------------------------------------------------------
    // 命令：导出当前轮询数据到 Excel
    // ----------------------------------------------------------------

    [RelayCommand]
    public void ExportPollData()
    {
        var dlg = new SaveFileDialog
        {
            Filter   = "Excel 文件|*.xlsx",
            FileName = $"主站轮询数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            ExcelHelper.ExportDeviceData(dlg.FileName, Registers.Select(r =>
                (r.Address, r.Description, r.RawValue, r.HexValue, r.PhysicalValue, r.LastUpdated)));
            AddLog(LogLevel.Info, $"导出成功 → {dlg.FileName}");
        }
        catch (Exception ex)
        {
            AddLog(LogLevel.Error, $"导出失败：{ex.Message}");
        }
    }

    // ----------------------------------------------------------------
    // 命令：清空日志
    // ----------------------------------------------------------------

    [RelayCommand]
    public void ClearLog() => LogEntries.Clear();

    // ----------------------------------------------------------------
    // 轮询数据回调
    // ----------------------------------------------------------------

    private void OnPollCompleted(IReadOnlyDictionary<int, ushort> data)
    {
        _dispatcher.InvokeAsync(() =>
        {
            PollCount++;

            // 若已有模板行，更新已有行
            if (Registers.Count > 0)
            {
                foreach (var row in Registers)
                {
                    if (data.TryGetValue(row.Address, out var val))
                        row.RawValue = val;
                }
            }
            else
            {
                // 没有模板时，动态展示所有轮询到的地址
                foreach (var (addr, val) in data)
                {
                    var existing = Registers.FirstOrDefault(r => r.Address == addr);
                    if (existing != null)
                        existing.RawValue = val;
                    else
                        Registers.Add(new RegisterRow { Address = addr, RawValue = val });
                }
            }
        });
    }

    private void OnServiceError(Exception ex)
    {
        _dispatcher.InvokeAsync(() =>
        {
            IsConnected = false;
            StatusText  = $"通信异常：{ex.Message}";
            AddLog(LogLevel.Error, $"通信异常：{ex.Message}");
        });
    }

    // ----------------------------------------------------------------
    // 辅助方法
    // ----------------------------------------------------------------

    private SlaveEndpoint BuildEndpoint() => new()
    {
        Name          = "主站目标",
        Protocol      = Protocol,
        Host          = RemoteHost,
        Port          = RemotePort,
        PortName      = ComPort,
        BaudRate      = BaudRate,
        SlaveId       = SlaveId,
        StartAddr     = StartAddress,
        Quantity      = Quantity,
        PollIntervalMs = PollIntervalMs
    };

    private void AddLog(LogLevel level, string msg)
    {
        if (LogEntries.Count >= 500)
            LogEntries.RemoveAt(0);
        LogEntries.Add(LogEntry.Create(level, msg));
    }
}

/// <summary>通用 ComboBox 数据项</summary>
public record ComboItem<T>(string Display, T Value);
