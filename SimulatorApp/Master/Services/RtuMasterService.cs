using Modbus.Device;
using SimulatorApp.Master.Models;
using SimulatorApp.Shared.Logging;
using System.IO.Ports;

namespace SimulatorApp.Master.Services;

/// <summary>
/// Modbus RTU 主站服务（NModbus4 2.1.0）。
/// 通过串口连接从站并周期轮询 Holding Register（FC03）。
/// </summary>
public class RtuMasterService : IMasterService
{
    private SerialPort?     _port;
    private IModbusMaster?  _master;
    private CancellationTokenSource? _cts;
    private SlaveEndpoint?  _endpoint;
    private Task?           _pollTask;

    public bool IsConnected => _port?.IsOpen == true;

    public event Action<IReadOnlyDictionary<int, ushort>>? OnPollCompleted;
    public event Action<Exception>? OnError;

    public async Task ConnectAndStartPollingAsync(SlaveEndpoint endpoint, CancellationToken ct = default)
    {
        _endpoint = endpoint;

        _port = new SerialPort(endpoint.PortName, endpoint.BaudRate, Parity.None, 8, StopBits.One)
        {
            ReadTimeout  = 3000,
            WriteTimeout = 3000
        };
        _port.Open();

        // NModbus4 2.1.0 RTU 主站 API
        _master = ModbusSerialMaster.CreateRtu(_port);
        _master.Transport.ReadTimeout  = 3000;
        _master.Transport.WriteTimeout = 3000;
        _master.Transport.Retries      = 2;

        AppLogger.Info($"RTU 主站已连接 → {endpoint.PortName}@{endpoint.BaudRate}  SlaveId={endpoint.SlaveId}");

        _cts      = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _pollTask = PollLoopAsync(_cts.Token);

        await Task.CompletedTask;
    }

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
                    _endpoint.SlaveId, _endpoint.PortName);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                AppLogger.Error("RTU 轮询失败", ex);
                break;
            }

            await Task.Delay(_endpoint!.PollIntervalMs, ct).ConfigureAwait(false);
        }
    }

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
        AppLogger.Info($"RTU FC06 写寄存器  addr={address}  value={value}");
    }

    public async Task DisconnectAsync()
    {
        _cts?.Cancel();
        if (_pollTask != null)
            await _pollTask.ConfigureAwait(false);

        _master?.Dispose();
        _port?.Close();
        _port?.Dispose();
        _master = null;
        _port   = null;
        AppLogger.Info("RTU 主站已断开");
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync();
}
