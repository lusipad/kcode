using System.Runtime.CompilerServices;
using Grpc.Net.Client;
using Kcode.Core;
using Kcode.Protos;

namespace Kcode.Transport;

public class GrpcTransport : IControlTransport
{
    private readonly string _endpoint;
    private readonly int _timeoutMs;
    private readonly GrpcChannel _channel;
    private readonly ControlService.ControlServiceClient _client;

    public GrpcTransport(string endpoint, int timeoutMs = 2000)
    {
        _endpoint = endpoint;
        _timeoutMs = timeoutMs;
        _channel = GrpcChannel.ForAddress(endpoint);
        _client = new ControlService.ControlServiceClient(_channel);
    }

    public Task ConnectAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task DisconnectAsync(CancellationToken ct = default) => Task.CompletedTask;

    public async Task<CommandResult> ExecuteAsync(string command, CancellationToken ct = default)
    {
        var reply = await _client.ExecuteAsync(new CommandRequest { Text = command }, deadline: GetDeadline(), cancellationToken: ct);
        return new CommandResult(reply.Success, reply.Message);
    }

    public async Task<CommandResult> SetParameterAsync(string key, double value, CancellationToken ct = default)
    {
        var reply = await _client.SetParameterAsync(new SetParamRequest { Key = key, Value = value }, deadline: GetDeadline(), cancellationToken: ct);
        return new CommandResult(reply.Success, reply.Message);
    }

    public async Task<CommandResult> ResetAsync(CancellationToken ct = default)
    {
        var reply = await _client.ResetAsync(new Empty(), deadline: GetDeadline(), cancellationToken: ct);
        return new CommandResult(reply.Success, reply.Message);
    }

    public async Task<CommandResult> TriggerEStopAsync(CancellationToken ct = default)
    {
        var reply = await _client.EStopAsync(new Empty(), deadline: GetDeadline(), cancellationToken: ct);
        return new CommandResult(reply.Success, reply.Message);
    }

    public async Task<CommandResult> ToggleFeedHoldAsync(CancellationToken ct = default)
    {
        var reply = await _client.ToggleFeedHoldAsync(new Empty(), deadline: GetDeadline(), cancellationToken: ct);
        return new CommandResult(reply.Success, reply.Message);
    }

    public async IAsyncEnumerable<MachineStatus> StreamStatusAsync([EnumeratorCancellation] CancellationToken ct = default)
    {
        using var call = _client.StreamStatus(new StatusRequest(), cancellationToken: ct);
        while (await call.ResponseStream.MoveNext(ct))
        {
            var s = call.ResponseStream.Current;
            yield return new MachineStatus
            {
                X = s.X,
                Y = s.Y,
                Z = s.Z,
                Feed = s.Feed,
                Speed = s.Speed,
                Temp = s.Temp,
                State = s.State,
                Alarm = s.Alarm
            };
        }
    }

    public async Task<ParamsReply> GetParamsReplyAsync(CancellationToken ct = default)
    {
        return await _client.GetParametersAsync(new Empty(), deadline: GetDeadline(), cancellationToken: ct);
    }

    public async Task<ToolsReply> GetToolsReplyAsync(CancellationToken ct = default)
    {
        return await _client.GetToolsAsync(new Empty(), deadline: GetDeadline(), cancellationToken: ct);
    }

    public IReadOnlyDictionary<string, double> GetParameters()
    {
        try
        {
            var reply = GetParamsReplyAsync().GetAwaiter().GetResult();
            return reply.Parameters.ToDictionary(kv => kv.Key, kv => kv.Value);
        }
        catch
        {
            return new Dictionary<string, double>();
        }
    }

    public IReadOnlyList<Core.Tool> GetTools()
    {
        try
        {
            var reply = GetToolsReplyAsync().GetAwaiter().GetResult();
            return reply.Tools.Select(t => new Core.Tool
            {
                Id = t.Id,
                Diameter = t.Diameter,
                Length = t.Length,
                Description = t.Description
            }).ToList();
        }
        catch
        {
            return Array.Empty<Core.Tool>();
        }
    }

    private DateTime? GetDeadline()
    {
        return DateTime.UtcNow.AddMilliseconds(_timeoutMs);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.ShutdownAsync();
    }
}
