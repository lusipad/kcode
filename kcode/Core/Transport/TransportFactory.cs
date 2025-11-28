using Kcode.Core.Config;

namespace Kcode.Core.Transport;

/// <summary>
/// 传输层工厂
/// 根据配置创建对应的传输层实例
/// </summary>
public class TransportFactory
{
    /// <summary>
    /// 创建传输层实例
    /// </summary>
    public static ITransport Create(TransportConfig config)
    {
        return config.Type.ToLower() switch
        {
            "rest" => new RestTransport(config),
            "grpc" => throw new NotImplementedException("gRPC transport will be implemented in next phase"),
            "virtual" => new VirtualTransport(config),
            _ => throw new ArgumentException($"Unknown transport type: {config.Type}")
        };
    }
}

/// <summary>
/// 虚拟传输层 (用于本地测试)
/// </summary>
public class VirtualTransport : ITransport
{
    private readonly TransportConfig _config;
    private bool _isConnected;

    public bool IsConnected => _isConnected;
    public string TransportType => "virtual";

    public VirtualTransport(TransportConfig config)
    {
        _config = config;
    }

    public Task ConnectAsync(CancellationToken ct = default)
    {
        _isConnected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        _isConnected = false;
        return Task.CompletedTask;
    }

    public Task<TransportResponse> InvokeAsync(
        string endpoint,
        Dictionary<string, object>? request = null,
        CancellationToken ct = default)
    {
        // 模拟响应
        var response = endpoint switch
        {
            "execute" => SimulateExecuteCommand(request),
            "get_status" => SimulateGetStatus(),
            "get_parameters" => SimulateGetParameters(),
            "set_parameter" => SimulateSetParameter(request),
            "estop" => SimulateEmergencyStop(),
            "feed_hold" => SimulateFeedHold(),
            _ => TransportResponse.CreateFailure($"Unknown endpoint: {endpoint}")
        };

        return Task.FromResult(response);
    }

    public async IAsyncEnumerable<TransportResponse> SubscribeAsync(
        string endpoint,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        // 模拟轮询状态
        while (!ct.IsCancellationRequested)
        {
            yield return SimulateGetStatus();
            await Task.Delay(100, ct);
        }
    }

    private TransportResponse SimulateExecuteCommand(Dictionary<string, object>? request)
    {
        var command = request?.GetValueOrDefault("text")?.ToString() ?? "";
        return TransportResponse.CreateSuccess(new Dictionary<string, object?>
        {
            ["success"] = true,
            ["message"] = $"Virtual: Executed command '{command}'"
        });
    }

    private TransportResponse SimulateGetStatus()
    {
        var random = new Random();
        return TransportResponse.CreateSuccess(new Dictionary<string, object?>
        {
            ["x"] = random.NextDouble() * 300,
            ["y"] = random.NextDouble() * 180,
            ["z"] = random.NextDouble() * 50,
            ["feed"] = random.Next(0, 2000),
            ["speed"] = random.Next(0, 12000),
            ["state"] = "IDLE",
            ["alarm"] = "",
            ["temp"] = 25.0 + random.NextDouble() * 10
        });
    }

    private TransportResponse SimulateGetParameters()
    {
        return TransportResponse.CreateSuccess(new Dictionary<string, object?>
        {
            ["parameters"] = new Dictionary<string, double>
            {
                ["max_velocity_x"] = 2000,
                ["max_velocity_y"] = 2000,
                ["max_velocity_z"] = 800,
                ["acceleration"] = 500
            }
        });
    }

    private TransportResponse SimulateSetParameter(Dictionary<string, object>? request)
    {
        var key = request?.GetValueOrDefault("key")?.ToString() ?? "";
        var value = request?.GetValueOrDefault("value") ?? 0.0;

        return TransportResponse.CreateSuccess(new Dictionary<string, object?>
        {
            ["success"] = true,
            ["message"] = $"Parameter '{key}' set to {value}"
        });
    }

    private TransportResponse SimulateEmergencyStop()
    {
        return TransportResponse.CreateSuccess(new Dictionary<string, object?>
        {
            ["success"] = true,
            ["message"] = "Emergency stop triggered"
        });
    }

    private TransportResponse SimulateFeedHold()
    {
        return TransportResponse.CreateSuccess(new Dictionary<string, object?>
        {
            ["success"] = true,
            ["message"] = "Feed hold toggled"
        });
    }

    public ValueTask DisposeAsync()
    {
        _isConnected = false;
        return ValueTask.CompletedTask;
    }
}
