using Kcode.Core.Config;
using Kcode.Core.Transport;

namespace Kcode.Core.UI;

/// <summary>
/// 数据绑定引擎
/// 负责从传输层订阅数据并更新 DataContext
/// </summary>
public class BindingEngine : IAsyncDisposable
{
    private readonly ITransport _transport;
    private readonly DataContext _dataContext;
    private readonly RootConfig _config;
    private readonly CancellationTokenSource _cts = new();
    private Task? _statusTask;

    public event EventHandler? DataUpdated;

    public BindingEngine(ITransport transport, DataContext dataContext, RootConfig config)
    {
        _transport = transport;
        _dataContext = dataContext;
        _config = config;
    }

    /// <summary>
    /// 启动数据绑定
    /// </summary>
    public Task StartAsync()
    {
        // 初始化元数据
        InitializeMetaData();

        // 启动状态数据订阅
        if (_config.Bindings.Status != null)
        {
            _statusTask = SubscribeStatusAsync();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// 停止数据绑定
    /// </summary>
    public async Task StopAsync()
    {
        _cts.Cancel();

        if (_statusTask != null)
        {
            try
            {
                await _statusTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }
    }

    /// <summary>
    /// 初始化元数据
    /// </summary>
    private void InitializeMetaData()
    {
        if (_config.Bindings.Meta != null)
        {
            var metaData = new Dictionary<string, object?>();

            foreach (var field in _config.Bindings.Meta.Fields)
            {
                var path = field.Value.Path;
                var value = _dataContext.GetByPath(path);
                metaData[field.Key] = value;
            }

            _dataContext.Set("meta", metaData);
        }
    }

    /// <summary>
    /// 订阅状态数据
    /// </summary>
    private async Task SubscribeStatusAsync()
    {
        if (_config.Bindings.Status == null)
        {
            return;
        }

        var source = _config.Bindings.Status.Source;

        // 解析数据源: stream:endpoint_name
        if (source.StartsWith("stream:"))
        {
            var endpoint = source["stream:".Length..];

            try
            {
                await foreach (var response in _transport.SubscribeAsync(endpoint, _cts.Token))
                {
                    if (response.Success)
                    {
                        UpdateStatusData(response.Data);
                        OnDataUpdated();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when stopping
            }
            catch (Exception)
            {
                // 静默处理连接错误
            }
        }
        // 轮询模式
        else if (_config.Bindings.Status.RefreshMs.HasValue)
        {
            var intervalMs = _config.Bindings.Status.RefreshMs.Value;

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    // 从 API 获取状态
                    var endpoint = source.Replace("api:", "");
                    var response = await _transport.InvokeAsync(endpoint, null, _cts.Token);

                    if (response.Success)
                    {
                        UpdateStatusData(response.Data);
                        OnDataUpdated();
                    }

                    await Task.Delay(intervalMs, _cts.Token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception)
                {
                    // 静默处理错误，继续轮询
                    await Task.Delay(intervalMs, _cts.Token);
                }
            }
        }
    }

    /// <summary>
    /// 更新状态数据
    /// </summary>
    private void UpdateStatusData(Dictionary<string, object?> rawData)
    {
        if (_config.Bindings.Status == null)
        {
            return;
        }

        var statusData = new Dictionary<string, object?>();

        foreach (var field in _config.Bindings.Status.Fields)
        {
            var fieldName = field.Key;
            var fieldConfig = field.Value;

            // 从原始数据中提取字段
            var value = rawData.GetValueOrDefault(fieldConfig.Path);

            // 应用格式化
            if (!string.IsNullOrEmpty(fieldConfig.Format) && value != null)
            {
                value = FormatValue(value, fieldConfig.Format);
            }

            // 应用转换
            if (fieldConfig.Transform != null && value != null)
            {
                var stringValue = value.ToString() ?? "";
                value = fieldConfig.Transform.GetValueOrDefault(stringValue,
                    fieldConfig.Transform.GetValueOrDefault("_default", stringValue));
            }

            statusData[fieldName] = value;
        }

        _dataContext.Set("status", statusData);
    }

    /// <summary>
    /// 格式化值
    /// </summary>
    private object FormatValue(object value, string format)
    {
        if (value is IFormattable formattable)
        {
            return formattable.ToString(format, null);
        }

        return value;
    }

    /// <summary>
    /// 触发数据更新事件
    /// </summary>
    private void OnDataUpdated()
    {
        DataUpdated?.Invoke(this, EventArgs.Empty);
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cts.Dispose();
    }
}
