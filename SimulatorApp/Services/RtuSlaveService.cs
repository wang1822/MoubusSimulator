using System.IO.Ports;
using Modbus.Device;
using SimulatorApp.Logging;
using ModelProto = SimulatorApp.Models.ProtocolType;

namespace SimulatorApp.Services;

/// <summary>Modbus RTU 从站服务（串口）。</summary>
public class RtuSlaveService : ISlaveService
{
    private readonly RegisterBank _bank;
    private readonly AppLogger    _log;

    private SerialPort?          _port;
    private ModbusSerialSlave?   _slave;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;

    public bool       IsRunning   { get; private set; }
    public ModelProto Protocol    => ModelProto.Rtu;
    public byte       SlaveId     { get; set; } = 1;
    public int        Port        { get; set; } = 502;
    public string     BindAddress { get; set; } = "0.0.0.0"; // RTU 不使用，接口占位
    public string     ComPort     { get; set; } = "COM1";
    public int        BaudRate    { get; set; } = 9600;

    public RtuSlaveService(RegisterBank bank, AppLogger log)
    {
        _bank = bank;
        _log  = log;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (IsRunning) return;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        _port = new SerialPort(ComPort, BaudRate, Parity.None, 8, StopBits.One)
        {
            ReadTimeout  = 1000,
            WriteTimeout = 1000
        };
        _port.Open();

        _slave = ModbusSerialSlave.CreateRtu(SlaveId, _port);

        _listenTask = Task.Run(() =>
        {
            try
            {
                _log.Info($"[从站RTU] 已启动，{ComPort} {BaudRate}bps，SlaveId={SlaveId}");
                _slave.Listen();
            }
            catch (InvalidOperationException) { }
            catch (Exception ex) { _log.Error("[从站RTU] 异常", ex); }
            finally { IsRunning = false; }
        }, _cts.Token);

        IsRunning = true;
        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (!IsRunning) return;
        _cts?.Cancel();
        _slave?.Dispose();
        _port?.Close();
        if (_listenTask != null)
            await _listenTask.ConfigureAwait(false);
        IsRunning = false;
        _log.Info("[从站RTU] 已停止");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cts?.Dispose();
        _port?.Dispose();
    }
}
