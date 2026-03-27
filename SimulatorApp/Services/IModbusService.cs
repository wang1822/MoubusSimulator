using SimulatorApp.Models;

namespace SimulatorApp.Services;

public interface IModbusService : IAsyncDisposable
{
    bool         IsRunning { get; }
    ProtocolType Protocol  { get; }

    Task StartAsync(CancellationToken ct = default);
    Task StopAsync();
}
