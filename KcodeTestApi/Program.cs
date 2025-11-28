using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// 添加控制器服务
builder.Services.AddControllers();

// 添加 CORS 支持
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 配置 JSON 选项
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null; // 保持原始命名
    options.JsonSerializerOptions.WriteIndented = true; // 格式化输出
});

// 配置日志
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

// 启用 CORS
app.UseCors();

// 请求日志中间件
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("请求: {Method} {Path}", context.Request.Method, context.Request.Path);
    await next();
});

// 映射控制器
app.MapControllers();

// 健康检查端点
app.MapGet("/health", () => new
{
    status = "healthy",
    timestamp = DateTime.Now
});

// 欢迎页面
app.MapGet("/", () => new
{
    name = "KCode Test API",
    version = "1.0.0",
    endpoints = new[]
    {
        "GET  /health - 健康检查",
        "POST /api/v1/cnc/execute - 执行 G 代码",
        "GET  /api/v1/cnc/status - 获取机器状态",
        "POST /api/v1/cnc/set_param - 设置参数",
        "GET  /api/v1/cnc/params - 获取所有参数",
        "POST /api/v1/cnc/emergency_stop - 紧急停止",
        "POST /api/v1/cnc/reset - 复位报警"
    }
});

// 配置监听地址
app.Urls.Add("http://localhost:5000");

Console.WriteLine("===========================================");
Console.WriteLine("  KCode Test API Server");
Console.WriteLine("  监听地址: http://localhost:5000");
Console.WriteLine("  开始时间: " + DateTime.Now);
Console.WriteLine("===========================================");

app.Run();
