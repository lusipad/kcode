using Kcode.Core;

namespace Kcode.Transport;

public interface IControlTransport : IAsyncDisposable
{
    Task ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);

    Task<CommandResult> ExecuteAsync(string command, CancellationToken ct = default);
    Task<CommandResult> SetParameterAsync(string key, double value, CancellationToken ct = default);
    Task<CommandResult> ResetAsync(CancellationToken ct = default);
    Task<CommandResult> TriggerEStopAsync(CancellationToken ct = default);
    Task<CommandResult> ToggleFeedHoldAsync(CancellationToken ct = default);

    IAsyncEnumerable<MachineStatus> StreamStatusAsync(CancellationToken ct = default);

    IReadOnlyDictionary<string, double> GetParameters();
    IReadOnlyList<Tool> GetTools();
}
