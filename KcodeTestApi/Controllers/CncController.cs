using Microsoft.AspNetCore.Mvc;

namespace KcodeTestApi.Controllers;

/// <summary>
/// CNC 设备模拟 API
/// </summary>
[ApiController]
[Route("api/v1/cnc")]
public class CncController : ControllerBase
{
    private static readonly Random _random = new();
    private static readonly Dictionary<string, object> _parameters = new()
    {
        ["max_velocity"] = 2000,
        ["acceleration"] = 500,
        ["jerk"] = 100
    };

    private static CncStatus _status = new()
    {
        X = 0.0,
        Y = 0.0,
        Z = 0.0,
        Feed = 0.0,
        Speed = 0.0,
        State = "IDLE",
        Alarm = "",
        Temp = 25.0
    };

    private readonly ILogger<CncController> _logger;

    public CncController(ILogger<CncController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 执行 G 代码命令
    /// </summary>
    [HttpPost("execute")]
    public IActionResult Execute([FromBody] ExecuteRequest request)
    {
        _logger.LogInformation("执行命令: {Command}", request.Text);

        // 模拟执行
        var command = request.Text?.Trim() ?? "";

        if (command.StartsWith("G0") || command.StartsWith("G1"))
        {
            // 模拟移动
            _status.X = _random.Next(0, 300);
            _status.Y = _random.Next(0, 200);
            _status.Z = _random.Next(0, 100);
            _status.State = "RUN";

            return Ok(new
            {
                success = true,
                message = $"执行成功: {command}",
                position = new { _status.X, _status.Y, _status.Z }
            });
        }
        else if (command.StartsWith("G28"))
        {
            // 回零
            _status.X = 0;
            _status.Y = 0;
            _status.Z = 0;
            _status.State = "IDLE";

            return Ok(new
            {
                success = true,
                message = "回零完成",
                position = new { _status.X, _status.Y, _status.Z }
            });
        }
        else if (command.StartsWith("M"))
        {
            // M 代码
            return Ok(new
            {
                success = true,
                message = $"M 代码执行: {command}"
            });
        }
        else
        {
            return BadRequest(new
            {
                success = false,
                message = $"未知命令: {command}"
            });
        }
    }

    /// <summary>
    /// 获取机器状态
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        // 模拟温度波动
        _status.Temp = 25.0 + _random.NextDouble() * 10.0;

        // 如果在运行状态，逐渐停止
        if (_status.State == "RUN")
        {
            _status.Feed = Math.Max(0, _status.Feed - 100);
            if (_status.Feed <= 0)
            {
                _status.State = "IDLE";
            }
        }

        return Ok(new
        {
            success = true,
            data = _status
        });
    }

    /// <summary>
    /// 设置参数
    /// </summary>
    [HttpPost("set_param")]
    public IActionResult SetParameter([FromBody] SetParamRequest request)
    {
        _logger.LogInformation("设置参数: {Key} = {Value}", request.Key, request.Value);

        _parameters[request.Key ?? ""] = request.Value ?? 0;

        return Ok(new
        {
            success = true,
            message = $"参数已更新: {request.Key} = {request.Value}",
            parameter = new
            {
                key = request.Key,
                value = request.Value
            }
        });
    }

    /// <summary>
    /// 获取所有参数
    /// </summary>
    [HttpGet("params")]
    public IActionResult GetParameters()
    {
        return Ok(new
        {
            success = true,
            data = _parameters
        });
    }

    /// <summary>
    /// 紧急停止
    /// </summary>
    [HttpPost("emergency_stop")]
    public IActionResult EmergencyStop()
    {
        _logger.LogWarning("紧急停止");

        _status.State = "ALARM";
        _status.Alarm = "EMERGENCY_STOP";
        _status.Feed = 0;
        _status.Speed = 0;

        return Ok(new
        {
            success = true,
            message = "紧急停止已触发"
        });
    }

    /// <summary>
    /// 复位报警
    /// </summary>
    [HttpPost("reset")]
    public IActionResult Reset()
    {
        _logger.LogInformation("复位报警");

        _status.State = "IDLE";
        _status.Alarm = "";

        return Ok(new
        {
            success = true,
            message = "报警已复位"
        });
    }
}

/// <summary>
/// 执行命令请求
/// </summary>
public class ExecuteRequest
{
    public string? Text { get; set; }
}

/// <summary>
/// 设置参数请求
/// </summary>
public class SetParamRequest
{
    public string? Key { get; set; }
    public object? Value { get; set; }
}

/// <summary>
/// CNC 状态
/// </summary>
public class CncStatus
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public double Feed { get; set; }
    public double Speed { get; set; }
    public string State { get; set; } = "IDLE";
    public string Alarm { get; set; } = "";
    public double Temp { get; set; }
}
