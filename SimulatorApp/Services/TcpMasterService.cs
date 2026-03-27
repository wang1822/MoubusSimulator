using System.Net.Sockets;
using Modbus.Device;
using SimulatorApp.Logging;
using ModelProto = SimulatorApp.Models.ProtocolType;

namespace SimulatorApp.Services;

/// <summary>Modbus TCP 主站服务，支持定时轮询。</summary>
public class TcpMasterService : IMasterService
{
    private readonly RegisterBank _bank;
    private readonly AppLogger    _log;

    private TcpClient?      _client;
    private ModbusIpMaster? _master;
    private CancellationTokenSource? _cts;
    private Task? _pollTask;

    public bool       IsRunning { get; private set; }
    public ModelProto Protocol  => ModelProto.Tcp;
    public string   Host         { get; set; } = "127.0.0.1";
    public int      Port         { get; set; } = 502;
    public string   ComPort      { get; set; } = "COM1";
    public int      BaudRate     { get; set; } = 9600;
    public byte     SlaveId      { get; set; } = 1;
    public int      StartAddress { get; set; } = 0;
    public int      RegisterCount{ get; set; } = 100;
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(1);

    public event Action<ushort[]>? DataReceived;

    public TcpMasterService(RegisterBank bank, AppLogger log)
    {
        _bank = bank;
        _log  = log;
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (IsRunning) return;
        _cts    = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _client = new TcpClient();
        await _client.ConnectAsync(Host, Port, _cts.Token);
        _master = ModbusIpMaster.CreateIp(_client);
        IsRunning = true;
        _log.Info($"[主站TCP] 已连接 {Host}:{Port}，SlaveId={SlaveId}");
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (IsRunning) return;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        _client = new TcpClient();
        await _client.ConnectAsync(Host, Port, _cts.Token);
        _master = ModbusIpMaster.CreateIp(_client);

        IsRunning = true;
        _log.Info($"[主站TCP] 已连接 {Host}:{Port}，SlaveId={SlaveId}，轮询间隔={PollInterval.TotalMilliseconds}ms");

        _pollTask = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(PollInterval);
            while (await timer.WaitForNextTickAsync(_cts.Token).ConfigureAwait(false))
            {
                try { await PollOnceAsync(_cts.Token); }
                catch (OperationCanceledException) { break; }
                catch (Exception ex) { _log.Error("[主站TCP] 轮询异常", ex); }
            }
            IsRunning = false;
        }, _cts.Token);
    }

    public async Task PollOnceAsync(CancellationToken ct = default)
        => await PollBlockAsync((ushort)StartAddress, (ushort)RegisterCount, ct);

    public async Task PollBlockAsync(ushort startAddress, ushort count, CancellationToken ct = default)
    {
        if (_master == null) return;
        var regs = await Task.Run(() =>
            _master.ReadHoldingRegisters(SlaveId, startAddress, count), ct);

        for (int i = 0; i < regs.Length; i++)
            _bank.Write(startAddress + i, regs[i]);

        DataReceived?.Invoke(regs);
        _log.Debug($"[主站TCP] FC03 addr={startAddress} qty={count}");
    }

    public async Task WriteRegistersAsync(ushort startAddress, ushort[] values, CancellationToken ct = default)
    {
        if (_master == null) return;
        await Task.Run(() =>
            _master.WriteMultipleRegisters(SlaveId, startAddress, values), ct);
        _log.Info($"[主站TCP] FC16 addr={startAddress} qty={values.Length} values=[{string.Join(",", values)}]");
    }

    public async Task StopAsync()
    {
        if (!IsRunning) return;
        _cts?.Cancel();
        if (_pollTask != null) await _pollTask.ConfigureAwait(false);
        _master?.Dispose();
        _client?.Close();
        IsRunning = false;
        _log.Info("[主站TCP] 已停止");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cts?.Dispose();
        _client?.Dispose();
    }
}
