# KCode v2 架构 - 实施进度

> **最新状态 (2025-11-28)**: 🎉 阶段 1 核心引擎 100% 完成！
> - ✅ 配置系统 + 传输层 + 命令系统 + 模板引擎
> - ✅ UI 引擎 (DataContext, BindingEngine, LayoutEngine, ComponentFactory)
> - ✅ REPL 引擎 V2 + 完整测试验证
> - 📝 总计 ~3200+ 行代码，13 个核心文件

## ✅ 已完成功能

### 阶段 1: 核心引擎重构 ✅ (100% 完成)

#### 1.1 配置系统 ✅
- [x] **ConfigModels.cs** - 完整的强类型配置模型
  - TransportConfig (多协议支持)
  - ApiEndpointConfig (协议无关接口定义)
  - CommandsConfig (system/api/macro/aliases/shortcuts)
  - LayoutConfig (布局结构和区域)
  - ThemeConfig (颜色和图标)
  - BindingsConfig (数据绑定)

- [x] **ConfigLoaderV2.cs** - 增强的配置加载器
  - ✅ imports 文件引用
  - ✅ 变量引用 `{path.to.value}`
  - ✅ 环境变量 `${ENV_VAR}`
  - ✅ 配置合并策略
  - ✅ 循环导入检测

#### 1.2 传输层抽象 ✅
- [x] **ITransport.cs** - 统一传输接口
  - ✅ 协议无关的 API 设计
  - ✅ InvokeAsync (一元调用)
  - ✅ SubscribeAsync (流式数据)
  - ✅ TransportResponse (统一响应格式)

- [x] **RestTransport.cs** - REST 实现
  - ✅ HTTP 请求 (GET/POST/PUT/DELETE)
  - ✅ JSONPath 响应解析 (简化版)
  - ✅ 认证支持 (Bearer/Basic/API Key)
  - ✅ URL 参数和请求体构建
  - ✅ 轮询模式支持

- [x] **TransportFactory.cs** - 传输层工厂
  - ✅ 根据配置创建传输层实例
  - ✅ VirtualTransportV2 (测试用)

#### 1.3 命令系统重构 ✅
- [x] **CommandParserV2.cs** - 命令解析器
  - ✅ 系统命令匹配
  - ✅ API 命令正则匹配
  - ✅ 参数提取 (捕获组 $1, $2, $input)
  - ✅ 宏命令识别
  - ✅ 别名展开

- [x] **CommandExecutorV2.cs** - 命令执行器
  - ✅ builtin 命令执行 (help, exit, clear, status)
  - ✅ api 命令执行 (调用传输层)
  - ✅ macro 命令执行 (多步骤序列)
  - ✅ 响应模板渲染

#### 1.4 模板引擎 ✅
- [x] **TemplateEngine.cs** - 模板渲染
  - ✅ 变量替换 `{{.field}}`
  - ✅ 格式化支持 `{{.field:F3}}`
  - ✅ 条件渲染 `{{if .success}}...{{else}}...{{end}}`
  - ✅ 循环渲染 `{{range .items}}...{{end}}`
  - ✅ Spectre.Console markup 兼容

#### 1.5 配置示例 ✅
- [x] **config-v2-rest.yaml** - REST 模式完整示例
  - ✅ 端点配置
  - ✅ 命令定义
  - ✅ 宏命令
  - ✅ 主题配置

---

## 📋 待实现功能

### 阶段 2: 传输层扩展 (优先级: 中)
- [ ] **GrpcTransportV2.cs** - 升级 gRPC 传输层
  - 动态调用支持
  - 流式数据支持
  - TLS 配置

- [ ] **WebSocketClient.cs** - WebSocket 客户端
  - 实时数据订阅
  - 自动重连
  - 消息解析

- [ ] **PollingAdapter.cs** - 轮询适配器
  - 将 REST GET 转换为流
  - 可配置轮询间隔

#### 1.6 UI 引擎 ✅
- [x] **DataContext.cs** - 数据上下文
  - ✅ 运行时数据存储和管理
  - ✅ 路径导航 (status.x, config.app.name)
  - ✅ 数据绑定表达式解析 `{status.x:F3}`
  - ✅ 格式化支持

- [x] **BindingEngine.cs** - 数据绑定引擎
  - ✅ 订阅流数据 (stream:endpoint)
  - ✅ 轮询模式支持 (refresh_ms)
  - ✅ 数据转换和格式化
  - ✅ 自动更新 DataContext

- [x] **LayoutEngine.cs** - 布局引擎
  - ✅ 渲染状态栏
  - ✅ 渲染页脚徽章
  - ✅ 基础布局构建 (rows/columns)
  - ✅ 默认布局支持

- [x] **ComponentFactory.cs** - 组件工厂
  - ✅ 内置组件 (text, panel, table, rule, markup)
  - ✅ 布局组件 (rows, columns)
  - ✅ 数据绑定支持
  - ✅ 主题颜色解析

#### 1.7 REPL 引擎 V2 ✅
- [x] **ReplEngineV2.cs** - 完整 REPL 实现
  - ✅ 集成所有 v2 组件
  - ✅ 交互式命令循环
  - ✅ 状态栏实时显示
  - ✅ 欢迎界面和主题支持

#### 1.8 测试验证 ✅
- [x] **TestV2.cs** - 核心功能测试
  - ✅ 配置加载测试
  - ✅ 传输层测试
  - ✅ 命令解析和执行测试
  - ✅ 模板渲染测试
  - ✅ 流式数据测试

---

## 📋 待实现功能

### 阶段 2: 传输层扩展 (优先级: 中)
- [ ] **GrpcTransportV2.cs** - 升级 gRPC 传输层
  - 动态调用支持
  - 流式数据支持
  - TLS 配置

- [ ] **WebSocketClient.cs** - WebSocket 客户端
  - 实时数据订阅
  - 自动重连
  - 消息解析

- [ ] **PollingAdapter.cs** - 轮询适配器
  - 将 REST GET 转换为流
  - 可配置轮询间隔

### 阶段 3: 增强功能 (优先级: 中)
- [ ] 创建测试 REST API 服务
- [ ] 布局引擎完整实现 (grid/tabs/live 等高级布局)
- [ ] 历史记录组件
- [ ] 输入建议和自动完成
- [ ] 性能测试和优化

---

## 🚀 快速开始 (v2 架构)

### 1. 使用 v2 配置

```csharp
using Kcode.Core.Config;
using Kcode.Core.Transport;
using Kcode.Core.Commands;

// 加载配置
var loader = new ConfigLoaderV2();
var config = loader.Load("Config/config-v2-rest.yaml");

// 创建传输层
var transport = TransportFactory.Create(config.Transport);
await transport.ConnectAsync();

// 创建命令系统
var parser = new CommandParserV2(config);
var executor = new CommandExecutorV2(transport, config);

// 解析和执行命令
var command = parser.Parse("G0 X10 Y20");
if (command != null)
{
    var result = await executor.ExecuteAsync(command);
    Console.WriteLine(result.Output);
}
```

### 2. 配置文件示例

**REST 模式:**
```yaml
transport:
  type: "rest"
  base_url: "http://localhost:8080/api/v1"

commands:
  api:
    gcode:
      pattern: "^[GMgm]\\d+.*"
      endpoint: "execute"
      request_mapping:
        text: "$input"
      response_template: |
        {{if .success}}
        [green]✓[/] {{.message}}
        {{else}}
        [red]✗[/] {{.message}}
        {{end}}
```

**Virtual 模式 (测试):**
```yaml
transport:
  type: "virtual"

commands:
  system:
    help:
      action: "builtin:help"
```

---

## 📊 架构对比

| 特性 | v1 (当前) | v2 (新架构) |
|------|-----------|-------------|
| 协议支持 | 仅 gRPC | gRPC + REST + Virtual |
| 配置方式 | 硬编码 + 部分 YAML | 完全配置驱动 |
| 命令系统 | 固定命令集 | 动态命令定义 |
| 模板渲染 | 无 | 完整模板引擎 |
| 响应格式 | 固定格式 | 可配置模板 |
| 扩展性 | 需要修改代码 | 仅需修改配置 |
| 测试性 | 依赖真实服务 | VirtualTransport 模拟 |

---

## 🎯 下一步计划

### 阶段 1: 核心引擎 ✅ (已完成)
- [x] 配置系统
- [x] 传输层抽象
- [x] 命令系统
- [x] 模板引擎
- [x] UI 引擎
- [x] REPL 引擎
- [x] 测试验证

### 阶段 2: 实战应用 (当前重点)
1. **立即行动** (优先级: 高)
   - [ ] 创建简单的 REST API 测试服务
   - [ ] 实际场景测试 (连接真实设备/服务)
   - [ ] 性能优化和调试

2. **短期目标** (1-2 周)
   - [ ] gRPC 传输层升级 (GrpcTransportV2)
   - [ ] WebSocket 客户端实现
   - [ ] 布局引擎增强 (grid/tabs/live)

3. **长期目标** (1 个月+)
   - [ ] 配置热重载
   - [ ] 配置生成器 (从 .proto/OpenAPI 生成)
   - [ ] 插件系统
   - [ ] 历史记录和自动完成

---

## 💡 使用示例

### 执行 G 代码
```
> G0 X10 Y20
✓ Virtual: Executed command 'G0 X10 Y20'
```

### 设置参数
```
> /set max_velocity 3000
📝 参数已更新: Parameter 'max_velocity' set to 3000
```

### 宏命令
```
> home
🏠 回零完成
```

### 帮助
```
> help
Available Commands:

System Commands:
  help - 显示帮助信息
  exit - 退出程序
  clear - 清屏

API Commands:
  gcode - 执行 G 代码
  set - 设置参数 /set <键> <值>

Macros:
  home - 所有轴回零
  auto_probe - 自动对刀
```

---

## 🔧 调试技巧

### 1. 使用 Virtual Transport 测试
```yaml
transport:
  type: "virtual"
```
无需真实后端即可测试所有命令流程。

### 2. 测试模板渲染
```csharp
var engine = new TemplateEngine();
var context = new Dictionary<string, object?>
{
    ["success"] = true,
    ["message"] = "Hello!"
};
var output = engine.Render("{{if .success}}[green]{{.message}}[/]{{end}}", context);
```

### 3. 测试命令解析
```csharp
var parser = new CommandParserV2(config);
var cmd = parser.Parse("G0 X10");
// 检查: cmd.Type == CommandType.Api
// 检查: cmd.Parameters["text"] == "G0 X10"
```

---

## ✨ 架构优势

1. **零代码适配新设备** - 仅需修改 YAML 配置
2. **协议切换简单** - 改一行 `type: rest` 即可
3. **命令系统灵活** - 支持正则匹配、参数提取、模板渲染
4. **易于测试** - VirtualTransport 无需真实服务
5. **可扩展** - 清晰的接口设计，易于添加新功能
