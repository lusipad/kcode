using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Kcode.Core.Config;

namespace Kcode.Core.Transport;

/// <summary>
/// RESTful API 传输层实现
/// </summary>
public class RestTransport : ITransport
{
    private readonly HttpClient _httpClient;
    private readonly TransportConfig _config;
    private bool _isConnected;

    public bool IsConnected => _isConnected;
    public string TransportType => "rest";

    public RestTransport(TransportConfig config)
    {
        _config = config;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(config.BaseUrl ?? "http://localhost"),
            Timeout = TimeSpan.FromMilliseconds(config.TimeoutMs)
        };

        // 设置请求头 (跳过内容头)
        var contentHeaders = new[] { "content-type", "content-length", "content-encoding" };
        foreach (var header in config.Headers)
        {
            // 跳过内容头，这些应该设置在 HttpContent 上
            if (!contentHeaders.Contains(header.Key.ToLower()))
            {
                _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        // 设置认证
        if (config.Auth != null)
        {
            ConfigureAuthentication(config.Auth);
        }
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

    public async Task<TransportResponse> InvokeAsync(
        string endpoint,
        Dictionary<string, object>? request = null,
        CancellationToken ct = default)
    {
        if (_config.Endpoints == null || !_config.Endpoints.TryGetValue(endpoint, out var endpointConfig))
        {
            return TransportResponse.CreateFailure($"Endpoint not found: {endpoint}");
        }

        try
        {
            // 构建请求
            var path = BuildPath(endpointConfig.Path, request);
            var httpRequest = CreateHttpRequest(endpointConfig, path, request);

            // 发送请求
            var response = await _httpClient.SendAsync(httpRequest, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            // 解析响应
            if (response.IsSuccessStatusCode)
            {
                var data = ParseResponse(responseBody, endpointConfig.Response);
                return TransportResponse.CreateSuccess(data);
            }
            else
            {
                return TransportResponse.CreateFailure(
                    $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            return TransportResponse.CreateFailure($"Request failed: {ex.Message}");
        }
    }

    public async IAsyncEnumerable<TransportResponse> SubscribeAsync(
        string endpoint,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        if (_config.Endpoints == null || !_config.Endpoints.TryGetValue(endpoint, out var endpointConfig))
        {
            yield return TransportResponse.CreateFailure($"Endpoint not found: {endpoint}");
            yield break;
        }

        // 如果配置了轮询，则使用轮询模式
        if (endpointConfig.Polling?.Enabled == true)
        {
            var intervalMs = endpointConfig.Polling.IntervalMs;

            while (!ct.IsCancellationRequested)
            {
                var response = await InvokeAsync(endpoint, null, ct);
                yield return response;

                await Task.Delay(intervalMs, ct);
            }
        }
        else
        {
            yield return TransportResponse.CreateFailure(
                $"Endpoint '{endpoint}' does not support streaming. Enable polling in config.");
        }
    }

    /// <summary>
    /// 配置认证
    /// </summary>
    private void ConfigureAuthentication(AuthConfig auth)
    {
        switch (auth.Type.ToLower())
        {
            case "bearer":
                if (!string.IsNullOrEmpty(auth.Token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", auth.Token);
                }
                break;

            case "basic":
                if (!string.IsNullOrEmpty(auth.Username) && !string.IsNullOrEmpty(auth.Password))
                {
                    var credentials = Convert.ToBase64String(
                        Encoding.UTF8.GetBytes($"{auth.Username}:{auth.Password}"));
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Basic", credentials);
                }
                break;

            case "api_key":
                if (!string.IsNullOrEmpty(auth.Token))
                {
                    _httpClient.DefaultRequestHeaders.Add("X-API-Key", auth.Token);
                }
                break;
        }
    }

    /// <summary>
    /// 构建路径 (替换路径参数)
    /// </summary>
    private string BuildPath(string pathTemplate, Dictionary<string, object>? request)
    {
        if (request == null) return pathTemplate;

        // 替换路径参数 {key}
        return Regex.Replace(pathTemplate, @"\{(\w+)\}", match =>
        {
            var key = match.Groups[1].Value;
            if (request.TryGetValue(key, out var value))
            {
                return value?.ToString() ?? "";
            }
            return match.Value;
        });
    }

    /// <summary>
    /// 创建 HTTP 请求
    /// </summary>
    private HttpRequestMessage CreateHttpRequest(
        RestEndpointConfig config,
        string path,
        Dictionary<string, object>? request)
    {
        var method = config.Method.ToUpper() switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "DELETE" => HttpMethod.Delete,
            _ => HttpMethod.Get
        };

        var httpRequest = new HttpRequestMessage(method, path);

        // 添加请求体
        if (request != null && config.Request?.Body != null && method != HttpMethod.Get)
        {
            var body = BuildRequestBody(config.Request.Body, request);
            var json = JsonSerializer.Serialize(body);
            httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return httpRequest;
    }

    /// <summary>
    /// 构建请求体
    /// </summary>
    private Dictionary<string, object> BuildRequestBody(
        Dictionary<string, string> bodyTemplate,
        Dictionary<string, object> request)
    {
        var body = new Dictionary<string, object>();

        foreach (var kvp in bodyTemplate)
        {
            var fieldName = kvp.Key;
            var fieldType = kvp.Value;

            if (request.TryGetValue(fieldName, out var value))
            {
                body[fieldName] = ConvertValue(value, fieldType);
            }
        }

        return body;
    }

    /// <summary>
    /// 解析响应 (简化版 JSONPath)
    /// </summary>
    private Dictionary<string, object?> ParseResponse(
        string responseBody,
        Dictionary<string, string> responseMapping)
    {
        var result = new Dictionary<string, object?>();

        try
        {
            var jsonDoc = JsonDocument.Parse(responseBody);
            var root = jsonDoc.RootElement;

            foreach (var kvp in responseMapping)
            {
                var fieldName = kvp.Key;
                var jsonPath = kvp.Value;

                // 简化版 JSONPath: 只支持 $.field.subfield 格式
                var value = ExtractJsonValue(root, jsonPath);
                result[fieldName] = value;
            }
        }
        catch (Exception)
        {
            // 解析失败，返回空字典
        }

        return result;
    }

    /// <summary>
    /// 提取 JSON 值 (简化版 JSONPath)
    /// </summary>
    private object? ExtractJsonValue(JsonElement root, string jsonPath)
    {
        // 移除 $. 前缀
        var path = jsonPath.StartsWith("$.") ? jsonPath[2..] : jsonPath;
        var parts = path.Split('.');

        var current = root;

        foreach (var part in parts)
        {
            if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(part, out var property))
            {
                current = property;
            }
            else
            {
                return null;
            }
        }

        // 转换为 .NET 类型
        return current.ValueKind switch
        {
            JsonValueKind.String => current.GetString(),
            JsonValueKind.Number => current.TryGetInt32(out var intValue) ? intValue : current.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => current.EnumerateArray().Select(e => ExtractJsonValue(e, "$")).ToList(),
            JsonValueKind.Object => JsonSerializer.Deserialize<Dictionary<string, object>>(current.GetRawText()),
            _ => null
        };
    }

    /// <summary>
    /// 类型转换
    /// </summary>
    private object ConvertValue(object value, string targetType)
    {
        return targetType.ToLower() switch
        {
            "string" => value.ToString() ?? "",
            "double" => Convert.ToDouble(value),
            "int" => Convert.ToInt32(value),
            "bool" => Convert.ToBoolean(value),
            _ => value
        };
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _httpClient.Dispose();
    }
}
