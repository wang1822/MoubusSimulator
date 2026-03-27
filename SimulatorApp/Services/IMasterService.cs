namespace SimulatorApp.Services;

public interface IMasterService : IModbusService
{
    string   Host         { get; set; }
    int      Port         { get; set; }
    string   ComPort      { get; set; }
    int      BaudRate     { get; set; }
    byte     SlaveId      { get; set; }
    int      StartAddress { get; set; }
    int      RegisterCount{ get; set; }
    TimeSpan PollInterval { get; set; }

    event Action<ushort[]>? DataReceived;

    /// <summary>只建立连接，不启动自动轮询。</summary>
    Task ConnectAsync(CancellationToken ct = default);

    /// <summary>使用当前 StartAddress/RegisterCount 读一次。</summary>
    Task PollOnceAsync(CancellationToken ct = default);

    /// <summary>读取指定块，直接指定地址和数量，线程安全地写入 RegisterBank。</summary>
    Task PollBlockAsync(ushort startAddress, ushort count, CancellationToken ct = default);

    /// <summary>FC16 写多个寄存器（遥调遥控）。</summary>
    Task WriteRegistersAsync(ushort startAddress, ushort[] values, CancellationToken ct = default);
}
