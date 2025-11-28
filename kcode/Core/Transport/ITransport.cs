using System.Text.Json;

namespace Kcode.Core.Transport;

/// <summary>
/// 统一传输层接口 (协议无关)
/// 支持 gRPC、REST、WebSocket 等多种传输协议
/// </summary>
public interface ITransport : IAsyncDisposable
{
    /// <summary>
    /// 连接到后端
    /// </summary>
    Task ConnectAsync(CancellationToken ct = default);

    /// <summary>
    /// 断开连接
    /// </summary>
    Task DisconnectAsync(CancellationToken ct = default);

    /// <summary>
    /// 调用端点 (一元调用)
    /// </summary>
    /// <param name="endpoint">端点名称 (协议无关)</param>
    /// <param name="request">请求数据</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>响应数据</returns>
    Task<TransportResponse> InvokeAsync(
        string endpoint,
        Dictionary<string, object>? request = null,
        CancellationToken ct = default);

    /// <summary>
    /// 订阅流式数据 (服务端流/WebSocket/轮询)
    /// </summary>
    /// <param name="endpoint">端点名称</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>异步数据流</returns>
    IAsyncEnumerable<TransportResponse> SubscribeAsync(
        string endpoint,
        CancellationToken ct = default);

    /// <summary>
    /// 连接状态
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// 传输类型
    /// </summary>
    string TransportType { get; }
}

/// <summary>
/// 传输层响应
/// </summary>
public class TransportResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 响应数据 (字典格式)
    /// </summary>
    public Dictionary<string, object?> Data { get; set; } = new();

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 原始响应 (用于调试)
    /// </summary>
    public string? RawResponse { get; set; }

    /// <summary>
    /// 创建成功响应
    /// </summary>
    public static TransportResponse CreateSuccess(Dictionary<string, object?> data)
    {
        return new TransportResponse
        {
            Success = true,
            Data = data
        };
    }

    /// <summary>
    /// 创建失败响应
    /// </summary>
    public static TransportResponse CreateFailure(string errorMessage)
    {
        return new TransportResponse
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }

    /// <summary>
    /// 获取字段值
    /// </summary>
    public T? GetField<T>(string fieldName, T? defaultValue = default)
    {
        if (Data.TryGetValue(fieldName, out var value))
        {
            try
            {
                // 处理类型转换
                if (value == null)
                {
                    return defaultValue;
                }

                if (value is T typedValue)
                {
                    return typedValue;
                }

                // 尝试使用 Convert
                if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }

                // 尝试使用 JSON 序列化/反序列化
                var json = JsonSerializer.Serialize(value);
                return JsonSerializer.Deserialize<T>(json);
            }
            catch
            {
                return defaultValue;
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// 获取字符串字段
    /// </summary>
    public string GetString(string fieldName, string defaultValue = "")
    {
        return GetField(fieldName, defaultValue) ?? defaultValue;
    }

    /// <summary>
    /// 获取布尔字段
    /// </summary>
    public bool GetBool(string fieldName, bool defaultValue = false)
    {
        return GetField(fieldName, defaultValue);
    }

    /// <summary>
    /// 获取双精度字段
    /// </summary>
    public double GetDouble(string fieldName, double defaultValue = 0.0)
    {
        return GetField(fieldName, defaultValue);
    }

    /// <summary>
    /// 获取整数字段
    /// </summary>
    public int GetInt(string fieldName, int defaultValue = 0)
    {
        return GetField(fieldName, defaultValue);
    }
}

/// <summary>
/// 传输异常
/// </summary>
public class TransportException : Exception
{
    public TransportException(string message) : base(message)
    {
    }

    public TransportException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
