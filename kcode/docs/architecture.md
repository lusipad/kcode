# KCode v2 架构设计

## 核心理念

**KCode 本身不包含任何业务逻辑**，它是一个：
- **多协议客户端壳** - 支持 Virtual/REST/gRPC（规划中）
- **配置驱动的 UI 渲染器** - 布局、颜色、数据绑定全部来自配置
- **命令路由器** - 将用户输入映射到后端 API 调用

```
┌─────────────────────────────────────────────────────────────┐
│                        config.yaml                          │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────────────┐ │
│  │  app    │  │transport│  │ commands│  │     layout      │ │
│  │ (配置)  │  │ (传输)  │  │ (命令)  │  │   (UI布局)      │ │
│  └────┬────┘  └────┬────┘  └────┬────┘  └────────┬────────┘ │
└───────┼────────────┼────────────┼─────────────────┼──────────┘
        │            │            │                 │
        ▼            ▼            ▼                 ▼
┌─────────────────────────────────────────────────────────────┐
│                      KCode 壳程序                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐   │
│  │  传输层抽象  │  │  命令路由器  │  │    布局引擎      │   │
│  │ ITransport   │  │  (配置解析)  │  │  (Spectre.Console)│   │
│  └──────┬───────┘  └──────────────┘  └──────────────────┘   │
└─────────┼───────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────┐
│                      后端服务 (可选)                          │
│  ┌─────────────────┐              ┌─────────────────┐       │
│  │   Virtual       │      或      │   REST API      │       │
│  │  (本地测试)     │              │  (HTTP/HTTPS)   │       │
│  └─────────────────┘              └─────────────────┘       │
│         CNC 控制器 / 3D 打印机 / IoT 设备 / 任意服务         │
└─────────────────────────────────────────────────────────────┘
```

---

## 一、传输层 (Transport Layer)

### 1.1 接口定义

```csharp
public interface ITransport : IAsyncDisposable
{
    Task ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);

    // 一元调用 (Request-Response)
    Task<TransportResponse> InvokeAsync(
        string endpoint,
        Dictionary<string, object>? request = null,
        CancellationToken ct = default);

    // 流式订阅 (Server Stream / Polling)
    IAsyncEnumerable<TransportResponse> SubscribeAsync(
        string endpoint,
        CancellationToken ct = default);

    bool IsConnected { get; }
    string TransportType { get; }
}
```

### 1.2 实现状态

| 传输类型 | 状态 | 说明 |
|---------|------|------|
| Virtual | ✅ 已实现 | 本地模拟，用于测试和演示 |
| REST | ✅ 已实现 | HTTP/HTTPS API，支持 GET/POST/PUT/DELETE |
| gRPC | ⏳ 规划中 | 高性能双向流式通信 |
| WebSocket | ⏳ 规划中 | 实时数据推送 |

### 1.3 Virtual 传输层

**文件：** `Core/Transport/VirtualTransport.cs`

本地模拟传输层，无需真实设备：

```csharp
public class VirtualTransport : ITransport
{
    // 模拟执行命令
    public Task<TransportResponse> InvokeAsync(...)
    {
        return endpoint switch
        {
            "execute" => SimulateExecuteCommand(request),
            "get_status" => SimulateGetStatus(),
            "get_parameters" => SimulateGetParameters(),
            _ => CreateFailure($"Unknown endpoint: {endpoint}")
        };
    }

    // 模拟状态流
    public async IAsyncEnumerable<TransportResponse> SubscribeAsync(...)
    {
        while (!ct.IsCancellationRequested)
        {
            yield return SimulateGetStatus();
            await Task.Delay(100, ct);
        }
    }
}
```

**配置示例：**

```yaml
transport:
  type: "virtual"
```

### 1.4 REST 传输层

**文件：** `Core/Transport/RestTransport.cs`

支持标准 HTTP/HTTPS API 调用：

**特性：**
- ✅ 支持 GET/POST/PUT/DELETE 方法
- ✅ 自动请求/响应序列化
- ✅ 支持多种认证方式（Bearer/Basic/API Key）
- ✅ 自定义请求头
- ✅ 超时控制
- ✅ 轮询模拟流式数据

**配置示例：**

```yaml
transport:
  type: "rest"
  base_url: "http://localhost:8080"
  timeout_ms: 5000

  auth:
    type: "bearer"  # none | bearer | basic | api_key
    token: "your-token-here"

  headers:
    User-Agent: "KCode/2.0"
    Accept: "application/json"

  endpoints:
    execute:
      method: "POST"
      path: "/api/execute"
      request_body:
        command: "{text}"

    get_status:
      method: "GET"
      path: "/api/status"
      poll_interval_ms: 100  # 轮询间隔（用于流式订阅）
```

### 1.5 gRPC 传输层（规划中）

**状态：** ⏳ 未实现

**计划特性：**
- 基于 Protobuf 的高性能通信
- 支持四种调用模式：
  - Unary (一元调用)
  - Server Streaming (服务端流)
  - Client Streaming (客户端流)
  - Bidirectional Streaming (双向流)
- TLS/SSL 支持
- 自动重连

**配置示例（规划）：**

```yaml
transport:
  type: "grpc"
  endpoint: "localhost:50051"
  timeout_ms: 5000

  tls:
    enabled: true
    cert_path: "certs/server.crt"

  services:
    control:
      package: "control"
      methods:
        execute:
          type: "unary"
          request: { text: "string" }
          response: { success: "bool", message: "string" }

        stream_status:
          type: "server_stream"
          response:
            x: "double"
            y: "double"
            state: "string"
```

---

## 二、命令系统 (Command System)

### 2.1 命令类型

| 类型 | 说明 | 示例 |
|------|------|------|
| **System** | 内置系统命令 | `help`, `exit`, `status`, `clear` |
| **API** | 调用传输层 API | G-Code 命令、参数设置 |
| **Macros** | 多步骤命令序列 | `home`, `zero_work`, `auto_probe` |
| **Aliases** | 命令别名 | `mv` → `G0`, `rapid` → `G0` |

### 2.2 命令解析流程

```
用户输入 → CommandParser → CommandRegistry → CommandExecutor
                              ↓
                        匹配命令类型
                              ↓
                    ┌─────────┼─────────┐
                    ▼         ▼         ▼
                System      API      Macro
                    │         │         │
                    ▼         ▼         ▼
                执行内置  调用Transport  执行序列
```

### 2.3 System 命令

**文件：** `Core/Commands/CommandExecutor.cs`

内置命令，直接在客户端执行：

```yaml
commands:
  system:
    help:
      aliases: ["?", "h"]
      description: "显示帮助信息"
      action: "builtin:help"

    exit:
      aliases: ["quit", "q"]
      description: "退出程序"
      action: "builtin:exit"

    status:
      aliases: ["st"]
      description: "显示机器状态"
      action: "builtin:status_panel"

    clear:
      aliases: ["cls"]
      description: "清屏"
      action: "builtin:clear"
```

### 2.4 API 命令

**文件：** `Core/Commands/CommandParser.cs`

通过正则模式匹配，调用传输层 API：

```yaml
commands:
  api:
    # G-Code 命令
    gcode:
      pattern: "^[GMgm]\\d+.*"
      endpoint: "execute"
      request_mapping:
        text: "$input"  # 整个输入
      response_template: |
        {{if .success}}
        [green]✓[/] {{.message}}
        {{else}}
        [red]✗[/] {{.message}}
        {{end}}

    # 参数设置
    set:
      pattern: "^/set\\s+(\\w+)\\s+([\\d.]+)$"
      description: "设置参数 /set <键> <值>"
      endpoint: "set_parameter"
      request_mapping:
        key: "$1"      # 第一个捕获组
        value: "$2"    # 第二个捕获组
      response_template: |
        {{if .success}}
        [green]✅ 参数已更新[/]: {{.message}}
        {{else}}
        [red]❌ 设置失败[/]: {{.message}}
        {{end}}
```

**参数提取规则：**
- `$input` - 完整输入
- `$1`, `$2`, ... - 正则捕获组

### 2.5 Macro 命令

**文件：** `Core/Commands/CommandExecutor.cs`

多步骤命令序列：

```yaml
commands:
  macros:
    home:
      aliases: ["home_all", "回零"]
      description: "所有轴回零"
      steps:
        - endpoint: "execute"
          request:
            text: "G28"
      response_template: "[green]🏠 回零完成[/]"

    auto_probe:
      aliases: ["对刀"]
      description: "自动对刀"
      steps:
        - endpoint: "execute"
          request:
            text: "G91 G38.2 Z-50 F50"  # 探针下降
        - endpoint: "execute"
          request:
            text: "G90 G10 L20 P1 Z0"   # 设置工件零点
        - endpoint: "execute"
          request:
            text: "G91 G0 Z5"            # 抬起探针
      response_template: "[green]🛠 对刀完成[/]"
```

### 2.6 Aliases

简单的字符串替换：

```yaml
commands:
  aliases:
    mv: "G0"           # mv X10 Y20 → G0 X10 Y20
    rapid: "G0"
    feed: "G1"
```

---

## 三、UI 引擎 (UI Engine)

### 3.1 组件架构

**核心文件：**
- `Core/UI/LayoutEngine.cs` - 布局解析和构建
- `Core/UI/ComponentFactory.cs` - 组件工厂
- `Core/UI/BindingEngine.cs` - 数据绑定
- `Core/UI/DataContext.cs` - 数据上下文
- `Core/UI/SpectreReplView.cs` - Spectre.Console 视图

### 3.2 布局系统

基于 Spectre.Console，支持动态布局：

```yaml
layout:
  type: "rows"  # rows | columns | grid
  regions:
    - name: "header"
      type: "panel"
      height: 3
      border: "rounded"
      content: |
        [bold cyan]{{app.name}}[/] {{app.version}}
        [dim]{{app.slogan}}[/]

    - name: "status"
      type: "grid"
      columns:
        - binding: "status.x"
          format: "X: {0:F2}"
        - binding: "status.y"
          format: "Y: {0:F2}"
        - binding: "status.state"
          format: "State: {0}"

    - name: "input"
      type: "textbox"
      placeholder: "{{app.prompt_placeholder}}"
```

### 3.3 数据绑定

**文件：** `Core/UI/BindingEngine.cs`

支持从多个数据源绑定：

```yaml
bindings:
  status:
    source: "transport.stream_status"  # 从传输层流式数据
    fields:
      x: "double"
      y: "double"
      z: "double"
      state: "string"

  config:
    source: "app"  # 从应用配置
    fields:
      name: "string"
      version: "string"
```

**实时更新机制：**
- 传输层流式数据自动更新 UI
- 配置数据静态绑定
- 支持格式化模板

---

## 四、模板引擎 (Template Engine)

**文件：** `Core/Template/TemplateEngine.cs`

### 4.1 语法

```
{{.field}}              # 变量替换
{{if .condition}}...{{end}}  # 条件渲染
{{range .items}}...{{end}}   # 循环渲染
```

### 4.2 示例

```yaml
response_template: |
  {{if .success}}
  [green]✅ 成功[/]
  {{else}}
  [red]❌ 失败[/]: {{.error}}
  {{end}}

  Position:
  {{range .positions}}
    - {{.name}}: {{.value}}
  {{end}}
```

**Spectre.Console Markup 支持：**
- `[bold]...[/]` - 粗体
- `[green]...[/]` - 绿色
- `[red]...[/]` - 红色
- `[dim]...[/]` - 暗淡

---

## 五、配置系统 (Configuration System)

### 5.1 配置加载

**文件：** `Core/Config/ConfigLoader.cs`

**特性：**
- ✅ YAML 格式
- ✅ 多文件导入 (`imports`)
- ✅ 自动探测配置路径
- ✅ 配置验证

### 5.2 配置结构

```yaml
# 应用配置
app:
  name: "kcode"
  version: "2.0.0"

# 传输层配置
transport:
  type: "virtual"  # virtual | rest | grpc

# API 端点定义
api:
  execute:
    description: "执行命令"
    request:
      text: "string"
    response:
      success: "bool"
      message: "string"

# 命令定义
commands:
  system: {...}
  api: {...}
  macros: {...}
  aliases: {...}

# 布局定义
layout:
  type: "rows"
  regions: [...]

# 主题配置
theme:
  colors: {...}

# 数据绑定
bindings:
  status:
    source: "transport.stream_status"
    fields: {...}

# 导入其他配置文件
imports:
  - "config-theme.yaml"
  - "config-commands.yaml"
```

### 5.3 配置文件组织

```
Config/
├── config-virtual.yaml      # 虚拟模式（默认）
├── config-rest.yaml         # REST 模式
├── config-rest-test.yaml    # REST 测试模式
└── config-grpc.yaml         # gRPC 模式（规划中）
```

---

## 六、实现状态总览

### 6.1 核心模块

| 模块 | 状态 | 文件 |
|------|------|------|
| 配置加载 | ✅ 完成 | `Core/Config/ConfigLoader.cs` |
| 配置模型 | ✅ 完成 | `Core/Config/ConfigModels.cs` |
| 传输层接口 | ✅ 完成 | `Core/Transport/ITransport.cs` |
| Virtual 传输 | ✅ 完成 | `Core/Transport/VirtualTransport.cs` |
| REST 传输 | ✅ 完成 | `Core/Transport/RestTransport.cs` |
| gRPC 传输 | ⏳ 规划中 | - |
| 命令解析器 | ✅ 完成 | `Core/Commands/CommandParser.cs` |
| 命令执行器 | ✅ 完成 | `Core/Commands/CommandExecutor.cs` |
| 命令注册表 | ✅ 完成 | `Core/Commands/CommandRegistry.cs` |
| 模板引擎 | ✅ 完成 | `Core/Template/TemplateEngine.cs` |
| 布局引擎 | ✅ 完成 | `Core/UI/LayoutEngine.cs` |
| 数据绑定 | ✅ 完成 | `Core/UI/BindingEngine.cs` |
| REPL 引擎 | ✅ 完成 | `Core/ReplEngine.cs` |

### 6.2 功能清单

| 功能 | 状态 | 说明 |
|------|------|------|
| 配置驱动 | ✅ 完成 | YAML 配置，多文件导入 |
| Virtual 传输 | ✅ 完成 | 本地测试模式 |
| REST 传输 | ✅ 完成 | HTTP/HTTPS API 调用 |
| gRPC 传输 | ⏳ 规划中 | 高性能流式通信 |
| WebSocket | ⏳ 规划中 | 实时数据推送 |
| System 命令 | ✅ 完成 | help, exit, status, clear |
| API 命令 | ✅ 完成 | 正则模式匹配，参数提取 |
| Macro 命令 | ✅ 完成 | 多步骤序列 |
| 命令别名 | ✅ 完成 | 简单字符串替换 |
| 动态布局 | ✅ 完成 | Spectre.Console 布局系统 |
| 数据绑定 | ✅ 完成 | 实时数据更新 |
| 模板引擎 | ✅ 完成 | 变量/条件/循环 |
| 主题配置 | ✅ 完成 | 颜色、图标配置 |
| 插件系统 | ⏳ 规划中 | 动态加载扩展 |

---

## 七、架构原则

### 7.1 设计原则

- **配置优于代码** - 所有行为通过配置定义
- **协议无关** - 传输层抽象，支持多种协议
- **组件化** - 模块化设计，职责单一
- **可扩展** - 插件化架构，易于扩展
- **类型安全** - 强类型配置模型

### 7.2 SOLID 原则

- **S** - 单一职责：每个类专注一个功能
- **O** - 开闭原则：对扩展开放，对修改封闭
- **L** - 里氏替换：传输层可互换
- **I** - 接口隔离：最小化接口依赖
- **D** - 依赖倒置：依赖抽象而非具体实现

### 7.3 代码规范

- **DRY** - 避免代码重复
- **KISS** - 保持简单直观
- **YAGNI** - 只实现当前需要的功能

---

## 八、未来规划

### 8.1 短期计划

- [ ] gRPC 传输层实现
- [ ] WebSocket 支持
- [ ] 配置文件完整文档
- [ ] 单元测试覆盖

### 8.2 长期计划

- [ ] 插件系统
- [ ] 可视化配置编辑器
- [ ] 多语言支持
- [ ] 性能优化和监控

---

**最后更新：** 2025-11-29
**版本：** v2.0.0
