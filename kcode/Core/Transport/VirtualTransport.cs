using System.Runtime.CompilerServices;
using Kcode.Core;

namespace Kcode.Transport;

public class VirtualTransport : IControlTransport
{
    private readonly VirtualCncController _controller;
    private bool _connected;

    public VirtualTransport(VirtualCncController controller)
    {
        _controller = controller;
    }

    public Task ConnectAsync(CancellationToken ct = default)
    {
        _connected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        _connected = false;
        return Task.CompletedTask;
    }

    public async Task<CommandResult> ExecuteAsync(string command, CancellationToken ct = default)
    {
        if (!_connected) await ConnectAsync(ct);

        await _controller.ExecuteCommandAsync(command);
        if (_controller.State == "ALARM")
        {
            var reason = string.IsNullOrWhiteSpace(_controller.AlarmReason)
                ? "Alarm triggered"
                : _controller.AlarmReason;
            return new CommandResult(false, reason);
        }

        return new CommandResult(true, "OK");
    }

    public Task<CommandResult> SetParameterAsync(string key, double value, CancellationToken ct = default)
    {
        if (_controller.Params.ContainsKey(key))
        {
            _controller.Params[key] = value;
            return Task.FromResult(new CommandResult(true, $"Set {key} to {value}"));
        }

        return Task.FromResult(new CommandResult(false, $"Parameter {key} not found"));
    }

    public Task<CommandResult> ResetAsync(CancellationToken ct = default)
    {
        _controller.ClearAlarm();
        return Task.FromResult(new CommandResult(true, "Alarm cleared"));
    }

    public Task<CommandResult> TriggerEStopAsync(CancellationToken ct = default)
    {
        _controller.EStop();
        return Task.FromResult(new CommandResult(false, "Emergency stop triggered"));
    }

    public Task<CommandResult> ToggleFeedHoldAsync(CancellationToken ct = default)
    {
        _controller.FeedHold();
        return Task.FromResult(new CommandResult(true, $"State: {_controller.State}"));
    }

    public async IAsyncEnumerable<MachineStatus> StreamStatusAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        while (!ct.IsCancellationRequested)
        {
            yield return _controller.GetStatus();
            await Task.Delay(50, ct);
        }
    }

    public IReadOnlyDictionary<string, double> GetParameters() => _controller.Params;

    public IReadOnlyList<Tool> GetTools() => _controller.Tools;

    public ValueTask DisposeAsync()
    {
        _connected = false;
        return ValueTask.CompletedTask;
    }
}
