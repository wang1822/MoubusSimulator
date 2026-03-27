using System.Net;
using System.Net.Sockets;
using Modbus.Data;
using Modbus.Device;
using SimulatorApp.Logging;
using ModelProto = SimulatorApp.Models.ProtocolType;

namespace SimulatorApp.Services;

/// <summary>Modbus TCP 从站服务。</summary>
public class TcpSlaveService : ISlaveService
{
    private readonly RegisterBank _bank;
    private readonly AppLogger    _log;

    private TcpListener?       _listener;
    private ModbusTcpSlave?    _slave;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;

    public bool          IsRunning   { get; private set; }
    public ModelProto    Protocol    => ModelProto.Tcp;
    public byte          SlaveId     { get; set; } = 1;
    public int           Port        { get; set; } = 502;
    public string        BindAddress { get; set; } = "0.0.0.0";
    public string        ComPort     { get; set; } = "COM1";
    public int           BaudRate    { get; set; } = 9600;

    public TcpSlaveService(RegisterBank bank, AppLogger log)
    {
        _bank = bank;
        _log  = log;
    }

    public async Task StartAsync(CancellationToken ct = default)
    {
        if (IsRunning) return;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        var bindIp = BindAddress is "0.0.0.0" or "" ? IPAddress.Any
                                                      : IPAddress.Parse(BindAddress);
        _listener = new TcpListener(bindIp, Port);
        _listener.Start();

        var dataStore = DataStoreFactory.CreateDefaultDataStore();
        SyncBankToStore(dataStore);
        _slave = ModbusTcpSlave.CreateTcp(SlaveId, _listener);
        _slave.DataStore = dataStore;

        // WriteComplete 事件：把写入的值同步回 RegisterBank
        _slave.DataStore.DataStoreWrittenTo += (_, args) =>
        {
            if (args.ModbusDataType == ModbusDataType.HoldingRegister)
            {
                lock (_bank)
                {
                    for (int i = 0; i < args.Data.B.Count; i++)
                        _bank.Write(args.StartAddress + i, args.Data.B[i]);
                }
            }
        };

        _listenTask = Task.Run(() =>
        {
            try
            {
                _log.Info($"[从站TCP] 已启动，端口={Port}，SlaveId={SlaveId}");
                _slave.Listen(); // 同步阻塞
            }
            catch (SocketException) { /* 端口关闭时正常退出 */ }
            catch (ObjectDisposedException) { }
            catch (Exception ex) { _log.Error("[从站TCP] 异常", ex); }
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
        _listener?.Stop();
        if (_listenTask != null)
            await _listenTask.ConfigureAwait(false);
        IsRunning = false;
        _log.Info("[从站TCP] 已停止");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cts?.Dispose();
    }

    /// <summary>把 RegisterBank 当前值预填充到 DataStore。</summary>
    private void SyncBankToStore(DataStore store)
    {
        // 预填充常用地址段（0-2000）
        var regs = _bank.ReadRange(0, 2000);
        for (int i = 0; i < regs.Length; i++)
            store.HoldingRegisters[i + 1] = regs[i]; // NModbus4: index 从 1 开始
    }
}
