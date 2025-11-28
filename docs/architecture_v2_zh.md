# KCode v2 架构设计 - 配置驱动的多协议客户端壳

## 核心理念

**KCode 本身不包含任何业务逻辑**，它是一个：
- **多协议客户端** - 支持 gRPC 和 RESTful API
- **配置驱动的 UI 渲染器** - 布局、颜色、数据绑定全部来自配置
- **命令路由器** - 将用户输入映射到后端 API 调用

```
┌─────────────────────────────────────────────────────────────┐
│                        config.yaml                          │
│  ┌─────────┐  ┌─────────┐  ┌─────────┐  ┌─────────────────┐ │
│  │ schema  │  │ commands│  │  layout │  │    bindings     │ │
│  │ (接口)  │  │ (命令)  │  │ (布局)  │  │  (数据→UI映射)  │ │
│  └────┬────┘  └────┬────┘  └────┬────┘  └────────┬────────┘ │
└───────┼────────────┼───────────┼─────────────────┼──────────┘
        │            │           │                 │
        ▼            ▼           ▼                 ▼
┌─────────────────────────────────────────────────────────────┐
│                      KCode 壳程序                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐   │
│  │  传输层抽象  │  │  命令路由器  │  │    布局引擎      │   │
│  │ gRPC / REST  │  │  (配置解析)  │  │  (Spectre.Console)│   │
│  └──────┬───────┘  └──────────────┘  └──────────────────┘   │
└─────────┼───────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────┐
│                      后端服务 (任选其一)                      │
│  ┌─────────────────┐              ┌─────────────────┐       │
│  │   gRPC Server   │      或      │   REST API      │       │
│  │  (高性能/流式)  │              │  (简单/通用)    │       │
│  └─────────────────┘              └─────────────────┘       │
│         CNC 控制器 / 3D 打印机 / IoT 设备 / 任意服务         │
└─────────────────────────────────────────────────────────────┘
```

---

## 一、传输层配置 (Transport Schema)

支持 **gRPC** 和 **RESTful** 两种协议，通过配置切换：

### 1.1 gRPC 配置

```yaml
# schema.yaml - gRPC 模式
transport:
  type: "grpc"
  endpoint: "localhost:50051"
  timeout_ms: 5000
  reconnect_interval_ms: 3000
  
  # TLS 配置 (可选)
  tls:
    enabled: false
    cert_path: ""
    
  # 服务方法定义
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
          request: {}
          response:
            x: "double"
            y: "double"
            z: "double"
            feed: "double"
            speed: "double"
            state: "string"
            alarm: "string"
            temp: "double"
        
        get_parameters:
          type: "unary"
          request: {}
          response:
            parameters: "map<string, double>"
        
        set_parameter:
          type: "unary"
          request: { key: "string", value: "double" }
          response: { success: "bool", message: "string" }
        
        estop:
          type: "unary"
          request: {}
          response: { success: "bool", message: "string" }
        
        feed_hold:
          type: "unary"
          request: {}
          response: { success: "bool", message: "string" }
```

### 1.2 RESTful API 配置

```yaml
# schema.yaml - REST 模式
transport:
  type: "rest"
  base_url: "http://localhost:8080/api/v1"
  timeout_ms: 5000
  
  # 认证配置 (可选)
  auth:
    type: "bearer"              # none / basic / bearer / api_key
    token: "${API_TOKEN}"       # 支持环境变量
  
  # 请求头 (可选)
  headers:
    Content-Type: "application/json"
    X-Client: "kcode"
  
  # API 端点定义
  endpoints:
    execute:
      method: "POST"
      path: "/command"
      request:
        body: { text: "string" }
      response:
        success: "$.success"           # JSONPath 提取
        message: "$.message"
    
    get_status:
      method: "GET"
      path: "/status"
      response:
        x: "$.position.x"
        y: "$.position.y"
        z: "$.position.z"
        feed: "$.feed_rate"
        speed: "$.spindle_speed"
        state: "$.machine_state"
        alarm: "$.alarm_code"
        temp: "$.temperature"
    
    # 轮询模式 (替代 gRPC 流)
    poll_status:
      method: "GET"
      path: "/status"
      polling:
        enabled: true
        interval_ms: 100              # 轮询间隔
      response:
        x: "$.position.x"
        y: "$.position.y"
        z: "$.position.z"
        feed: "$.feed_rate"
        speed: "$.spindle_speed"
        state: "$.machine_state"
        temp: "$.temperature"
    
    get_parameters:
      method: "GET"
      path: "/parameters"
      response:
        parameters: "$.data"          # 返回键值对数组
    
    set_parameter:
      method: "PUT"
      path: "/parameters/{key}"       # URL 参数
      request:
        path_params: { key: "string" }
        body: { value: "double" }
      response:
        success: "$.success"
        message: "$.message"
    
    estop:
      method: "POST"
      path: "/emergency-stop"
      response:
        success: "$.success"
        message: "$.message"
    
    feed_hold:
      method: "POST"
      path: "/feed-hold"
      request:
        body: { toggle: "bool" }
      response:
        success: "$.success"
        message: "$.message"

  # WebSocket 配置 (用于实时数据，替代 gRPC 流)
  websocket:
    enabled: true
    url: "ws://localhost:8080/ws/status"
    reconnect_interval_ms: 3000
    subscriptions:
      status:
        message_type: "status_update"
        fields:
          x: "$.x"
          y: "$.y"
          z: "$.z"
          feed: "$.feed"
          speed: "$.speed"
          state: "$.state"
```

### 1.3 协议对比

| 特性 | gRPC | RESTful |
|------|------|---------|
| 性能 | 高 (HTTP/2, 二进制) | 中 (HTTP/1.1, JSON) |
| 实时数据 | 原生流支持 | WebSocket / 轮询 |
| 调试 | 需要专用工具 | 浏览器/curl 即可 |
| 兼容性 | 需要 proto 定义 | 通用，任何语言 |
| 适用场景 | 高频控制、实时监控 | 简单集成、Web 服务 |

---

## 二、接口定义 (Schema 配置化)

不再硬编码接口结构，而是在配置中描述服务的接口：

```yaml
# schema.yaml - 统一接口描述 (协议无关)
api:
  # 执行命令
  execute:
    description: "执行 G 代码或命令"
    request: { text: "string" }
    response: { success: "bool", message: "string" }
  
  # 获取状态 (实时)
  stream_status:
    description: "获取机器状态流"
    stream: true                      # 标记为流式/轮询
    response:
      x: "double"
      y: "double"
      z: "double"
      feed: "double"
      speed: "double"
      state: "string"
      alarm: "string"
      temp: "double"
  
  # 获取参数
  get_parameters:
    description: "获取所有参数"
    response:
      parameters: "map<string, double>"
  
  # 设置参数
  set_parameter:
    description: "设置单个参数"
    request: { key: "string", value: "double" }
    response: { success: "bool", message: "string" }
  
  # 紧急停止
  estop:
    description: "紧急停止"
    response: { success: "bool", message: "string" }
  
  # 进给保持
  feed_hold:
    description: "进给保持/恢复"
    response: { success: "bool", message: "string" }
```

---

## 三、命令系统 (Commands 配置化)

所有命令都通过配置定义，**协议无关** - 同样的命令定义可以用于 gRPC 或 REST：

```yaml
# commands.yaml - 命令定义
commands:
  # 系统命令 (内置功能)
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

  # API 命令 (映射到后端接口，协议无关)
  api:
    # 直接执行 G 代码
    gcode:
      pattern: "^[GMgm]\\d+.*"           # 正则匹配 G/M 代码
      endpoint: "execute"                 # 调用的 API 端点 (不是 method)
      request_mapping:
        text: "$input"                    # 整个输入作为 text 字段
      response_template: |
        {{if .success}}
        [green]✓[/] {{.message}}
        {{else}}
        [red]✗[/] {{.message}}
        {{end}}
    
    # 设置参数
    set:
      pattern: "^/set\\s+(\\w+)\\s+([\\d.]+)$"
      description: "设置参数 /set <键> <值>"
      endpoint: "set_parameter"
      request_mapping:
        key: "$1"                         # 第一个捕获组
        value: "$2"                       # 第二个捕获组 (自动转 double)
      response_template: |
        {{if .success}}
        [green]📝 参数已更新[/]: {{.message}}
        {{else}}
        [red]⚠️ 设置失败[/]: {{.message}}
        {{end}}
    
    # 获取参数列表
    params:
      aliases: ["parameters", "参数"]
      description: "显示所有参数"
      endpoint: "get_parameters"
      response_render: "table"            # 使用表格渲染
      table_config:
        title: "机器参数"
        columns:
          - { header: "参数名", field: "key", color: "cyan" }
          - { header: "数值", field: "value", color: "white" }
    
    # 重置报警
    reset:
      aliases: ["rst", "复位"]
      description: "清除报警"
      endpoint: "reset"
      response_template: "[green]✓[/] 报警已清除"

  # 宏命令 (多步骤序列)
  macros:
    home:
      aliases: ["home_all", "回零"]
      description: "所有轴回零"
      steps:
        - { endpoint: "execute", request: { text: "G28" } }
      response_template: "[green]🏠 回零完成[/]"
    
    zero_work:
      aliases: ["清零"]
      description: "设置当前位置为工件零点"
      steps:
        - { endpoint: "execute", request: { text: "G10 L20 P1 X0 Y0 Z0" } }
      response_template: "[green]📍 工件零点已设置[/]"
    
    auto_probe:
      aliases: ["对刀"]
      description: "自动对刀"
      steps:
        - { endpoint: "execute", request: { text: "G91 G38.2 Z-50 F50" } }
        - { endpoint: "execute", request: { text: "G90 G10 L20 P1 Z0" } }
        - { endpoint: "execute", request: { text: "G91 G0 Z5" } }
      response_template: "[green]🔧 对刀完成[/]"

  # 别名 (简单的命令替换)
  aliases:
    mv: "G0"              # mv X10 Y20 → G0 X10 Y20
    rapid: "G0"
    feed: "G1"
    主轴开: "M3"
    主轴关: "M5"
    冷却开: "M8"
    冷却关: "M9"

# 快捷键绑定
shortcuts:
  Escape:
    action: "api:estop"                   # 协议无关的 API 调用
    feedback: "[red]🚨 紧急停止![/]"
  
  Space:
    action: "api:feed_hold"
    feedback: "[yellow]⏸️ 进给保持[/]"
  
  F1:
    action: "builtin:help"
  
  F5:
    action: "builtin:status_panel"
```

---

## 四、布局系统 (Layout 配置化)

UI 布局完全由配置定义，支持：
- 区域划分 (header, body, footer, sidebar)
- 数据绑定 (从 gRPC 流获取实时数据)
- 条件渲染

```yaml
# layout.yaml - UI 布局定义
layout:
  # 整体结构
  structure:
    type: "rows"                          # 行布局
    children:
      - { id: "header", size: 12 }        # 头部区域，12行高
      - { id: "body", ratio: 1 }          # 主体区域，自适应
      - { id: "suggestion", size: 3 }     # 建议栏
      - { id: "prompt", size: 3 }         # 输入框
      - { id: "footer", size: 4 }         # 状态栏

  # 区域定义
  regions:
    header:
      type: "panel"
      border: "rounded"
      border_color: "{theme.colors.panel_border}"
      content:
        type: "grid"
        columns: 2
        children:
          - type: "column"
            children:
              - type: "text"
                value: "{config.ui.header.welcome}"
                style: "bold {theme.colors.accent_primary}"
              - type: "ascii_art"
                lines: "{config.ui.header.logo}"
                color: "{theme.colors.header_text}"
              - type: "text_list"
                items: "{config.ui.header.context_lines}"
                color: "{theme.colors.header_text}"
          - type: "column"
            children:
              - type: "list_block"
                title: "{config.ui.header.tips.title}"
                items: "{config.ui.header.tips.items}"
              - type: "divider"
              - type: "list_block"
                title: "{config.ui.header.activity.title}"
                items: "{config.ui.header.activity.items}"

    body:
      type: "panel"
      border: "rounded"
      border_color: "grey19"
      content:
        type: "history"                   # 内置组件: 命令历史
        empty_text: "暂无消息。输入命令开始使用。"

    suggestion:
      type: "panel"
      border: "rounded"
      border_color: "{theme.colors.accent_secondary}"
      content:
        type: "text"
        value: "> {config.ui.suggestion_text}"
        color: "grey70"

    prompt:
      type: "panel"
      border: "rounded"
      border_color: "{theme.colors.prompt_border}"
      content:
        type: "input"                     # 内置组件: 输入框
        prefix: ">"
        prefix_color: "{theme.colors.accent_primary}"
        text_color: "{theme.colors.prompt_text}"
        cursor: "_"
        cursor_color: "grey35"

    footer:
      type: "panel"
      border: "none"
      padding: [1, 0]
      content:
        type: "rows"
        children:
          - type: "status_bar"            # 内置组件: 状态栏
            sections: "{config.ui.footer.sections}"
            badges: "{config.ui.footer.badges}"
          - type: "text"
            value: "{config.ui.footer.notice}"
            color: "{theme.colors.footer_notice}"
            bindings:
              permissions: "{meta.permissions}"

# 数据绑定 - 将后端数据绑定到 UI (协议无关)
bindings:
  # 状态数据源 (自动选择 gRPC 流 / WebSocket / 轮询)
  status:
    source: "stream:status"               # 引用 api.stream_status
    refresh_ms: 100                       # 轮询模式的刷新间隔
    fields:
      x: { path: "x", format: "F3" }      # 3位小数
      y: { path: "y", format: "F3" }
      z: { path: "z", format: "F3" }
      feed: { path: "feed", format: "F0" }
      speed: { path: "speed", format: "F0" }
      state: { path: "state" }
      temp: { path: "temp", format: "F1" }
      alarm: { path: "alarm" }
      state_icon:
        path: "state"
        transform:                        # 状态图标映射
          "RUN": "▶"
          "HOLD": "⏸"
          "ALARM": "🚨"
          "IDLE": "●"
          "_default": "○"
  
  # 元数据 (静态配置)
  meta:
    source: "config:ui.footer.meta_values"
    fields:
      model: { path: "model" }
      workspace: { path: "workspace" }
      branch: { path: "branch" }
      tokens: { path: "tokens" }
      permissions: { path: "permissions" }
```

---

## 五、主题系统 (Theme 配置化)

```yaml
# theme.yaml - 主题定义
theme:
  name: "Claude 暗色"
  
  colors:
    # 基础色
    background: "#000000"
    foreground: "#F4E3D7"
    
    # 强调色
    accent_primary: "#FF7043"     # 珊瑚橙 - Logo, 关键提示
    accent_secondary: "#4DD0E1"   # 天青色 - 信息, 元数据
    accent_tertiary: "#CE93D8"    # 紫罗兰 - 统计, 坐标
    
    # 状态色
    success: "#66BB6A"            # 成功 - 绿色
    warning: "#FFEE58"            # 警告 - 黄色
    error: "#EF5350"              # 错误 - 红色
    
    # UI 元素
    panel_border: "#FF7043"
    panel_divider: "#F57C00"
    header_text: "#F4E3D7"
    prompt_border: "#7E57C2"
    prompt_text: "#EDE7F6"
    footer_notice: "#FF4081"
    footer_badge: "#4DD0E1"
    
    # 状态颜色映射
    state_colors:
      IDLE: "green"
      RUN: "cyan"
      HOLD: "yellow"
      ALARM: "red"
  
  # 图标/Emoji 配置
  icons:
    enabled: true
    set:
      success: "✓"
      error: "✗"
      warning: "⚠"
      info: "ℹ"
      home: "🏠"
      tool: "🔧"
      temp: "🌡️"
      speed: "🚀"
      position: "📍"
      alarm: "🚨"
      pause: "⏸"
      play: "▶"
      stop: "⏹"
```

---

## 六、完整配置示例

### 6.1 gRPC 模式配置

```yaml
# config-grpc.yaml - gRPC 模式
app:
  name: "kcode"
  version: "2.0.0"

transport:
  type: "grpc"
  endpoint: "localhost:50051"
  timeout_ms: 5000

# 引用通用配置
imports:
  - "commands.yaml"
  - "layout.yaml"
  - "theme.yaml"
```

### 6.2 RESTful 模式配置

```yaml
# config-rest.yaml - REST 模式
app:
  name: "kcode"
  version: "2.0.0"

transport:
  type: "rest"
  base_url: "http://localhost:8080/api/v1"
  timeout_ms: 5000
  
  auth:
    type: "bearer"
    token: "${CNC_API_TOKEN}"
  
  websocket:
    enabled: true
    url: "ws://localhost:8080/ws/status"

# 引用通用配置 (命令定义完全相同!)
imports:
  - "commands.yaml"
  - "layout.yaml"
  - "theme.yaml"
```

### 6.3 启动时选择模式

```bash
# 使用 gRPC 模式
kcode --config config-grpc.yaml

# 使用 REST 模式
kcode --config config-rest.yaml

# 或通过环境变量
KCODE_TRANSPORT=rest kcode
```

---

## 七、实现计划

### 阶段 1: 核心引擎重构
1. **配置加载器** - 支持 YAML 解析、imports、变量引用
2. **传输层抽象** - `ITransport` 接口，统一 gRPC/REST 调用
3. **动态 gRPC 客户端** - 根据 schema 配置动态调用
4. **REST 客户端** - HTTP 调用 + JSONPath 解析
5. **WebSocket 客户端** - 实时数据订阅
6. **命令解析器** - 正则匹配 + 参数提取 + 端点映射
7. **模板引擎** - 支持 `{{if}}`, `{{range}}`, 变量替换

### 阶段 2: UI 引擎
1. **布局解析器** - 将 YAML 布局转换为 Spectre.Console 组件树
2. **数据绑定引擎** - 流数据 → UI 状态 → 渲染
3. **主题引擎** - 颜色解析、图标映射

### 阶段 3: 扩展功能
1. **插件系统** - 支持外部脚本/命令
2. **配置热重载** - 修改配置无需重启
3. **配置验证器** - 启动时校验配置完整性
4. **配置生成器** - 从 .proto / OpenAPI 文件自动生成配置

---

## 八、优势总结

| 特性 | 传统方式 | 配置驱动 |
|------|----------|----------|
| 适配新设备 | 修改代码 + 重新编译 | 修改 YAML 配置 |
| 切换协议 | 重写通信层 | 改一行 `type: rest` |
| 添加新命令 | 写 C# 代码 | 添加 YAML 条目 |
| 修改 UI 布局 | 改代码 + 调试 | 改配置 + 热重载 |
| 国际化 | 资源文件 + 代码 | 配置中直接写中文 |
| 不同用户偏好 | 多套代码/配置 | 多个 config 文件切换 |

---

## 九、传输层架构

```
┌─────────────────────────────────────────────────────────────┐
│                     ITransport 接口                         │
│  - InvokeAsync(endpoint, request) → response               │
│  - SubscribeAsync(endpoint) → IAsyncEnumerable<data>       │
│  - Connect() / Disconnect()                                 │
└─────────────────────────────────────────────────────────────┘
                              │
        ┌─────────────────────┼─────────────────────┐
        ▼                     ▼                     ▼
┌───────────────┐     ┌───────────────┐     ┌───────────────┐
│ GrpcTransport │     │ RestTransport │     │VirtualTransport│
│               │     │               │     │  (测试用)      │
│ - Unary 调用  │     │ - HTTP 请求   │     │               │
│ - 流式调用    │     │ - WebSocket   │     │ - 模拟响应    │
│               │     │ - 轮询        │     │               │
└───────────────┘     └───────────────┘     └───────────────┘
```

这种架构使 KCode 成为一个**真正通用的终端 UI 框架**，可以连接：
- CNC 控制器 (gRPC 高性能通信)
- 3D 打印机 (REST API)
- IoT 设备 (WebSocket)
- 任何有 API 的服务
