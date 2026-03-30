using SimulatorApp.Master.Models;

namespace SimulatorApp.Master.Services;

/// <summary>
/// 主站服务接口 — 连接从站并持续轮询 Holding Register
/// </summary>
public interface IMasterService : IAsyncDisposable
{
    /// <summary>当前是否已连接</summary>
    bool IsConnected { get; }

    /// <summary>每次轮询完成后触发，传出最新一批寄存器原始值（地址→值）</summary>
    event Action<IReadOnlyDictionary<int, ushort>> OnPollCompleted;

    /// <summary>连接异常时触发</summary>
    event Action<Exception> OnError;

    /// <summary>连接并开始轮询</summary>
    Task ConnectAndStartPollingAsync(SlaveEndpoint endpoint, CancellationToken ct = default);

    /// <summary>停止轮询并断开连接</summary>
    Task DisconnectAsync();

    /// <summary>
    /// 单次写单个寄存器（FC06）
    /// </summary>
    Task WriteSingleRegisterAsync(int address, ushort value);

    /// <summary>
    /// 批量读 Holding Register（FC03），返回原始 ushort 数组
    /// </summary>
    Task<ushort[]> ReadRegistersAsync(int startAddress, int quantity);
}
