using Modbus.Device;
using SimulatorApp.Master.Models;
using SimulatorApp.Shared.Logging;
using System.Net.Sockets;

namespace SimulatorApp.Master.Services;

/// <summary>
/// Modbus TCP 主站服务（NModbus4 2.1.0）。
/// 使用 ModbusIpMaster.CreateIp(TcpClient) 连接从站并周期轮询 Holding Register（FC03）。
/// </summary>
public class TcpMasterService : IMasterService
{
    private TcpClient?      _client;
    private IModbusMaster?  _master;
    private CancellationTokenSource? _cts;
    private SlaveEndpoint?  _endpoint;
    private Task?           _pollTask;

    public bool IsConnected => _client?.Connected == true;

    public event Action<IReadOnlyDictionary<int, ushort>>? OnPollCompleted;
    public event Action<Exception>? OnError;

    // ----------------------------------------------------------------
    // 连接 + 启动轮询
    // ----------------------------------------------------------------

    public async Task ConnectAndStartPollingAsync(SlaveEndpoint endpoint, CancellationToken ct = default)
    {
        _endpoint = endpoint;

        _client = new TcpClient();
        await _client.ConnectAsync(endpoint.Host, endpoint.Port, ct);

        // NModbus4 2.1.0 TCP 主站 API
        _master = ModbusIpMaster.CreateIp(_client);
        _master.Transport.ReadTimeout  = 3000;
        _master.Transport.WriteTimeout = 3000;
        _master.Transport.Retries      = 2;

        AppLogger.Info($"TCP 主站已连接 → {endpoint.Host}:{endpoint.Port}  SlaveId={endpoint.SlaveId}");

        _cts      = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _pollTask = PollLoopAsync(_cts.Token);
    }

    // ----------------------------------------------------------------
    // 轮询循环
    // ----------------------------------------------------------------

    private async Task PollLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var regs = await ReadRegistersAsync(_endpoint!.StartAddr, _endpoint.Quantity);
                var dict = new Dictionary<int, ushort>(regs.Length);
                for (int i = 0; i < regs.Length; i++)
                    dict[_endpoint.StartAddr + i] = regs[i];

                OnPollCompleted?.Invoke(dict);
                AppLogger.ModbusRequest(0x03, _endpoint.StartAddr, _endpoint.Quantity,
                    _endpoint.SlaveId, $"{_endpoint.Host}:{_endpoint.Port}");
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                AppLogger.Error("TCP 轮询失败", ex);
                break;
            }

            await Task.Delay(_endpoint!.PollIntervalMs, ct).ConfigureAwait(false);
        }
    }

    // ----------------------------------------------------------------
    // 读写接口
    // ----------------------------------------------------------------

    public async Task<ushort[]> ReadRegistersAsync(int startAddress, int quantity)
    {
        if (_master == null) throw new InvalidOperationException("尚未连接");
        return await Task.Run(() =>
            _master.ReadHoldingRegisters(_endpoint!.SlaveId, (ushort)startAddress, (ushort)quantity));
    }

    public async Task WriteSingleRegisterAsync(int address, ushort value)
    {
        if (_master == null) throw new InvalidOperationException("尚未连接");
        await Task.Run(() =>
            _master.WriteSingleRegister(_endpoint!.SlaveId, (ushort)address, value));
        AppLogger.Info($"FC06 写寄存器  addr={address}  value={value}");
    }

    // ----------------------------------------------------------------
    // 断开连接
    // ----------------------------------------------------------------

    public async Task DisconnectAsync()
    {
        _cts?.Cancel();
        if (_pollTask != null)
            await _pollTask.ConfigureAwait(false);

        _master?.Dispose();
        _client?.Close();
        _client?.Dispose();
        _master = null;
        _client = null;
        AppLogger.Info("TCP 主站已断开");
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync();
}
