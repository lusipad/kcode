# KCode v2 架构设计 - 配置驱动的 gRPC 客户端壳

## 核心理念

**KCode 本身不包含任何 CNC 业务逻辑**，它是一个：
- **通用 gRPC 客户端** - 连接任意 gRPC 服务
- **配置驱动的 UI 渲染器** - 布局、颜色、数据绑定全部来自配置
- **命令路由器** - 将用户输入映射到 gRPC 调用

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
│  │ gRPC 客户端  │  │  命令路由器  │  │    布局引擎      │   │
│  │  (动态调用)  │  │  (配置解析)  │  │  (Spectre.Console)│   │
│  └──────┬───────┘  └──────────────┘  └──────────────────┘   │
└─────────┼───────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────┐
│   gRPC 服务端       │
│  (CNC 控制器)       │
│  (3D 打印机)        │
│  (任意设备)         │
└─────────────────────┘
```

---

## 一、接口定义 (Schema 配置化)

不再硬编码 proto 结构，而是在配置中描述 gRPC 服务的接口：

```yaml
# schema.yaml - 描述 gRPC 服务接口
grpc:
  endpoint: "localhost:50051"
  timeout_ms: 5000
  
  # 服务方法定义
  services:
    control:
      package: "control"
      
      methods:
        # 执行命令
        execute:
          type: "unary"                              # 请求类型：一元调用
          request: { text: "string" }                # 请求参数
          response: { success: "bool", message: "string" }  # 响应字段
        
        # 状态流
        stream_status:
          type: "server_stream"                      # 服务端流
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
        
        # 获取参数
        get_parameters:
          type: "unary"
          request: {}
          response:
            parameters: "map<string, double>"
        
        # 设置参数
        set_parameter:
          type: "unary"
          request: { key: "string", value: "double" }
          response: { success: "bool", message: "string" }
        
        # 紧急停止
        estop:
          type: "unary"
          request: {}
          response: { success: "bool", message: "string" }
        
        # 进给保持
        feed_hold:
          type: "unary"
          request: {}
          response: { success: "bool", message: "string" }
```

---

## 二、命令系统 (Commands 配置化)

所有命令都通过配置定义，包括：
- 命令名称和别名
- 参数解析规则
- 映射到哪个 gRPC 方法
- 响应如何渲染

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

  # gRPC 命令 (映射到服务方法)
  grpc:
    # 直接执行 G 代码
    gcode:
      pattern: "^[GMgm]\\d+.*"           # 正则匹配 G/M 代码
      method: "control.execute"           # 调用的 gRPC 方法
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
      method: "control.set_parameter"
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
      method: "control.get_parameters"
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
      method: "control.reset"
      response_template: "[green]✓[/] 报警已清除"

  # 宏命令 (多步骤序列)
  macros:
    home:
      aliases: ["home_all", "回零"]
      description: "所有轴回零"
      steps:
        - { method: "control.execute", request: { text: "G28" } }
      response_template: "[green]🏠 回零完成[/]"
    
    zero_work:
      aliases: ["清零"]
      description: "设置当前位置为工件零点"
      steps:
        - { method: "control.execute", request: { text: "G10 L20 P1 X0 Y0 Z0" } }
      response_template: "[green]📍 工件零点已设置[/]"
    
    auto_probe:
      aliases: ["对刀"]
      description: "自动对刀"
      steps:
        - { method: "control.execute", request: { text: "G91 G38.2 Z-50 F50" } }
        - { method: "control.execute", request: { text: "G90 G10 L20 P1 Z0" } }
        - { method: "control.execute", request: { text: "G91 G0 Z5" } }
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
    action: "grpc:control.estop"
    feedback: "[red]🚨 紧急停止![/]"
  
  Space:
    action: "grpc:control.feed_hold"
    feedback: "[yellow]⏸️ 进给保持[/]"
  
  F1:
    action: "builtin:help"
  
  F5:
    action: "builtin:status_panel"
```

---

## 三、布局系统 (Layout 配置化)

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

# 数据绑定 - 将 gRPC 流数据绑定到 UI
bindings:
  # 状态数据源 (来自 stream_status)
  status:
    source: "grpc:control.stream_status"
    refresh_ms: 100                       # 刷新间隔
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

## 四、主题系统 (Theme 配置化)

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

## 五、完整配置示例

将所有配置整合到一个文件中：

```yaml
# config.yaml - KCode 完整配置

app:
  name: "kcode"
  version: "2.0.0"

# 导入其他配置文件 (可选，支持模块化)
imports:
  - "schema.yaml"
  - "commands.yaml"
  - "layout.yaml"
  - "theme.yaml"

# 或者全部内联定义...
grpc:
  endpoint: "localhost:50051"
  timeout_ms: 5000
  reconnect_interval_ms: 3000
  
# ... (其余配置如上)
```

---

## 六、实现计划

### 阶段 1: 核心引擎重构
1. **配置加载器** - 支持 YAML 解析、imports、变量引用
2. **动态 gRPC 客户端** - 根据 schema 配置动态调用 gRPC 方法
3. **命令解析器** - 正则匹配 + 参数提取 + 方法映射
4. **模板引擎** - 支持 `{{if}}`, `{{range}}`, 变量替换

### 阶段 2: UI 引擎
1. **布局解析器** - 将 YAML 布局转换为 Spectre.Console 组件树
2. **数据绑定引擎** - gRPC 流 → UI 状态 → 渲染
3. **主题引擎** - 颜色解析、图标映射

### 阶段 3: 扩展功能
1. **插件系统** - 支持外部脚本/命令
2. **配置热重载** - 修改配置无需重启
3. **配置验证器** - 启动时校验配置完整性
4. **配置生成器** - 从 .proto 文件自动生成 schema 配置

---

## 七、优势总结

| 特性 | 传统方式 | 配置驱动 |
|------|----------|----------|
| 适配新设备 | 修改代码 + 重新编译 | 修改 YAML 配置 |
| 添加新命令 | 写 C# 代码 | 添加 YAML 条目 |
| 修改 UI 布局 | 改代码 + 调试 | 改配置 + 热重载 |
| 国际化 | 资源文件 + 代码 | 配置中直接写中文 |
| 不同用户偏好 | 多套代码/配置 | 多个 config 文件切换 |

这种架构使 KCode 成为一个**真正通用的终端 UI 框架**，不仅可以用于 CNC，还可以用于任何有 gRPC 接口的设备控制。
