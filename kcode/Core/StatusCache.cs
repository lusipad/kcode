using Kcode.Transport;

namespace Kcode.Core;

public class StatusCache : IAsyncDisposable
{
    private readonly IControlTransport _transport;
    private readonly CancellationTokenSource _cts = new();
    private Task? _worker;
    private readonly object _lock = new();
    private MachineStatus _latest = new() { State = "IDLE" };

    public StatusCache(IControlTransport transport)
    {
        _transport = transport;
    }

    public MachineStatus Latest
    {
        get
        {
            lock (_lock) return _latest;
        }
    }

    public Task StartAsync()
    {
        _worker = Task.Run(async () =>
        {
            await foreach (var status in _transport.StreamStatusAsync(_cts.Token))
            {
                lock (_lock)
                {
                    _latest = status;
                }

                if (_cts.IsCancellationRequested) break;
            }
        }, _cts.Token);

        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        if (_worker != null)
        {
            try { await _worker; } catch { /* ignore */ }
        }
        _cts.Dispose();
    }
}
