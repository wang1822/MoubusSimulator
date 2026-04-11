using Modbus.Device;
using SimulatorApp.Master.Models;
using SimulatorApp.Shared.Logging;
using System.Net.Sockets;

namespace SimulatorApp.Master.Services;

/// <summary>
/// Modbus TCP 主站服务（NModbus4 2.1.0）。
/// 建立连接后，由外部（MasterViewModel）驱动轮询。
/// </summary>
public class TcpMasterService : IMasterService
{
    private TcpClient?      _client;
    private IModbusMaster?  _master;
    private SlaveEndpoint?  _endpoint;
    // 轮询和写入共享同一条 TCP 连接，必须串行访问，否则并发帧会导致 SlaveException
    private readonly SemaphoreSlim _lock = new(1, 1);

    public bool IsConnected => _client?.Connected == true;

    public async Task ConnectAsync(SlaveEndpoint endpoint, CancellationToken ct = default)
    {
        _endpoint = endpoint;
        _client   = new TcpClient();
        await _client.ConnectAsync(endpoint.Host, endpoint.Port, ct);

        _master = ModbusIpMaster.CreateIp(_client);
        _master.Transport.ReadTimeout  = 3000;
        _master.Transport.WriteTimeout = 3000;
        _master.Transport.Retries      = 2;

        AppLogger.Info($"TCP 主站已连接 → {endpoint.Host}:{endpoint.Port}  SlaveId={endpoint.SlaveId}");
    }

    public async Task<ushort[]> ReadRegistersAsync(int startAddress, int quantity)
    {
        if (_master == null) throw new InvalidOperationException("尚未连接");
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            return await Task.Run(() =>
                _master.ReadHoldingRegisters(_endpoint!.SlaveId, (ushort)startAddress, (ushort)quantity))
                .ConfigureAwait(false);
        }
        finally { _lock.Release(); }
    }

    public async Task WriteSingleRegisterAsync(int address, ushort value)
    {
        if (_master == null) throw new InvalidOperationException("尚未连接");
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            await Task.Run(() =>
                _master.WriteSingleRegister(_endpoint!.SlaveId, (ushort)address, value))
                .ConfigureAwait(false);
            AppLogger.Info($"TCP FC06 写寄存器  addr={address}  value=0x{value:X4}");
        }
        finally { _lock.Release(); }
    }

    public async Task WriteMultipleRegistersAsync(int address, ushort[] values)
    {
        if (_master == null) throw new InvalidOperationException("尚未连接");
        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            await Task.Run(() =>
                _master.WriteMultipleRegisters(_endpoint!.SlaveId, (ushort)address, values))
                .ConfigureAwait(false);
            AppLogger.Info($"TCP FC16 写多寄存器  addr={address}  count={values.Length}" +
                           $"  [{string.Join(" ", values.Select(v => $"0x{v:X4}"))}]");
        }
        finally { _lock.Release(); }
    }

    public Task DisconnectAsync()
    {
        _master?.Dispose();
        _client?.Close();
        _client?.Dispose();
        _master = null;
        _client = null;
        AppLogger.Info("TCP 主站已断开");
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync();
}
