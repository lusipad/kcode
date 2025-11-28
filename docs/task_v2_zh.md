# KCode v2 - 开发任务计划

## 目标
将 kcode 重构为一个**纯配置驱动的多协议客户端壳**，实现：
- 支持 gRPC 和 RESTful 两种协议
- 零代码适配不同设备
- 动态 UI 布局
- 可扩展的命令系统

---

## 阶段 1: 核心引擎重构 (优先级: 高)

### 1.1 配置系统重构
- [ ] **ConfigModels.cs** - 定义强类型配置模型
  - TransportConfig (type, endpoint, auth, tls)
  - GrpcConfig (services, methods)
  - RestConfig (endpoints, websocket)
  - CommandsConfig (system, api, macros, aliases)
  - LayoutConfig (structure, regions)
  - ThemeConfig (colors, icons)
  - BindingsConfig (数据源映射)

- [ ] **ConfigLoader.cs** - 增强配置加载
  - 支持 `imports` 引用其他文件
  - 支持变量引用 `{theme.colors.primary}`
  - 支持环境变量 `${ENV_VAR}`
  - 配置验证和错误报告

### 1.2 传输层抽象
- [ ] **ITransport.cs** - 统一传输接口
  ```csharp
  interface ITransport
  {
      Task<Response> InvokeAsync(string endpoint, object request);
      IAsyncEnumerable<T> SubscribeAsync<T>(string endpoint);
      Task ConnectAsync();
      Task DisconnectAsync();
  }
  ```

- [ ] **GrpcTransport.cs** - gRPC 实现
  - 根据 schema 配置动态构建请求
  - 支持 unary, server_stream, client_stream, bidi_stream
  - 自动类型转换 (string → double 等)
  - 连接管理和自动重连

- [ ] **RestTransport.cs** - REST 实现
  - HTTP 客户端封装
  - 支持 GET/POST/PUT/DELETE
  - JSONPath 响应解析
  - 认证支持 (Bearer/Basic/API Key)
  - URL 参数和请求体构建

- [ ] **WebSocketClient.cs** - WebSocket 实现
  - 实时数据订阅
  - 自动重连
  - 消息解析

- [ ] **PollingAdapter.cs** - 轮询适配器
  - 将 REST GET 转换为流式数据
  - 可配置轮询间隔

### 1.3 命令系统重构
- [ ] **CommandRegistry.cs** - 命令注册表
  - 从配置加载所有命令定义
  - 支持别名解析
  - 正则模式匹配

- [ ] **CommandParser.cs** - 重构命令解析器
  - 正则捕获组提取参数
  - 参数类型转换
  - 构建请求映射 (协议无关)

- [ ] **CommandExecutor.cs** - 命令执行器
  - 执行 builtin 命令 (help, exit, clear 等)
  - 执行 api 命令 (调用 ITransport)
  - 执行 macro 命令 (多步骤序列)
  - 模板渲染响应

### 1.4 模板引擎
- [ ] **TemplateEngine.cs** - 响应模板渲染
  - 变量替换 `{{.field}}`
  - 条件渲染 `{{if .success}}...{{end}}`
  - 循环渲染 `{{range .items}}...{{end}}`
  - Spectre.Console markup 支持

---

## 阶段 2: UI 引擎 (优先级: 高)

### 2.1 布局引擎
- [ ] **LayoutEngine.cs** - 布局解析和构建
  - 解析 YAML 布局定义
  - 构建 Spectre.Console Layout 树
  - 支持 rows, columns, grid 布局

- [ ] **RegionRenderer.cs** - 区域渲染器
  - Panel 渲染
  - Grid 渲染
  - Text/List 渲染
  - 支持嵌套组件

- [ ] **ComponentFactory.cs** - 组件工厂
  - 根据 type 创建对应组件
  - 内置组件: history, input, status_bar, divider
  - 可扩展组件注册

### 2.2 数据绑定引擎
- [ ] **BindingEngine.cs** - 数据绑定
  - 订阅流数据 (gRPC 流 / WebSocket / 轮询)
  - 数据转换和格式化
  - 触发 UI 更新

- [ ] **DataContext.cs** - 数据上下文
  - 存储当前状态数据
  - 支持路径访问 `status.x`
  - 支持格式化 `{x:F3}`
  - 支持 transform 映射
  - JSONPath 支持 (用于 REST 响应)

### 2.3 主题引擎
- [ ] **ThemeEngine.cs** - 主题管理
  - 颜色解析 (hex 值, 颜色名)
  - 图标/Emoji 映射
  - 状态颜色映射

---

## 阶段 3: 集成和优化 (优先级: 中)

### 3.1 REPL 重构
- [ ] **ReplEngine.cs** - 重构主循环
  - 使用 LayoutEngine 渲染
  - 使用 BindingEngine 更新数据
  - 使用 CommandExecutor 处理输入

### 3.2 快捷键系统
- [ ] **ShortcutManager.cs** - 快捷键管理
  - 从配置加载快捷键
  - 全局按键监听
  - 执行绑定的 action

### 3.3 错误处理
- [ ] 统一错误处理和用户提示
- [ ] gRPC 连接断开时的优雅降级
- [ ] 配置错误的友好提示

---

## 阶段 4: 扩展功能 (优先级: 低)

### 4.1 配置热重载
- [ ] 监视配置文件变化
- [ ] 增量更新 (不重启)
- [ ] `/reload` 命令

### 4.2 配置生成器
- [ ] 从 .proto 文件生成 gRPC schema 配置
- [ ] 从 OpenAPI/Swagger 文件生成 REST schema 配置
- [ ] 交互式配置向导

### 4.3 插件系统
- [ ] 外部脚本支持 (Python/PowerShell)
- [ ] 自定义组件加载

---

## 文件结构 (重构后)

```
kcode/
├── Config/
│   ├── config.yaml           # 主配置 (引用其他文件)
│   ├── config-grpc.yaml      # gRPC 模式配置
│   ├── config-rest.yaml      # REST 模式配置
│   ├── schema.yaml           # API 接口定义
│   ├── commands.yaml         # 命令定义 (协议无关)
│   ├── layout.yaml           # UI 布局定义
│   └── theme.yaml            # 主题定义
├── Core/
│   ├── Config/
│   │   ├── ConfigModels.cs   # 配置模型定义
│   │   ├── ConfigLoader.cs   # 配置加载器
│   │   └── ConfigValidator.cs
│   ├── Transport/
│   │   ├── ITransport.cs     # 传输层接口
│   │   ├── GrpcTransport.cs  # gRPC 实现
│   │   ├── RestTransport.cs  # REST 实现
│   │   ├── WebSocketClient.cs
│   │   ├── PollingAdapter.cs
│   │   └── VirtualTransport.cs # 测试用
│   ├── Commands/
│   │   ├── CommandRegistry.cs
│   │   ├── CommandParser.cs
│   │   ├── CommandExecutor.cs
│   │   └── BuiltinCommands.cs
│   ├── Template/
│   │   └── TemplateEngine.cs
│   └── ReplEngine.cs
├── UI/
│   ├── Layout/
│   │   ├── LayoutEngine.cs
│   │   ├── RegionRenderer.cs
│   │   └── ComponentFactory.cs
│   ├── Binding/
│   │   ├── BindingEngine.cs
│   │   └── DataContext.cs
│   ├── Theme/
│   │   └── ThemeEngine.cs
│   └── Components/
│       ├── HistoryPanel.cs
│       ├── InputBox.cs
│       ├── StatusBar.cs
│       └── ...
├── Protos/
│   └── control.proto         # 保留用于强类型场景
└── Program.cs
```

---

## 配置示例

### 最小 gRPC 配置

```yaml
# config.yaml - 最小 gRPC 配置
app:
  name: "kcode"
  version: "2.0.0"

transport:
  type: "grpc"
  endpoint: "localhost:50051"
  
commands:
  system:
    help: { action: "builtin:help" }
    exit: { action: "builtin:exit" }
  
  api:
    execute:
      pattern: ".*"
      endpoint: "execute"
      request_mapping:
        text: "$input"

layout:
  simple: true                # 使用默认布局

theme:
  preset: "claude_dark"       # 使用预设主题
```

### 最小 REST 配置

```yaml
# config.yaml - 最小 REST 配置
app:
  name: "kcode"
  version: "2.0.0"

transport:
  type: "rest"
  base_url: "http://localhost:8080/api"
  
  endpoints:
    execute:
      method: "POST"
      path: "/command"
      request:
        body: { text: "string" }
      response:
        success: "$.success"
        message: "$.message"
  
commands:
  system:
    help: { action: "builtin:help" }
    exit: { action: "builtin:exit" }
  
  api:
    execute:
      pattern: ".*"
      endpoint: "execute"
      request_mapping:
        text: "$input"

layout:
  simple: true

theme:
  preset: "claude_dark"
```

---

## 下一步行动

1. **立即开始**: 阶段 1.1 - 配置模型定义
2. **并行开发**: 阶段 2.1 - 布局引擎原型
3. **迭代验证**: 每完成一个模块就测试集成

是否开始实施？
