# KCode 开发计划

本文档记录 KCode v2 的开发进度和未来规划。

---

## 📊 当前状态 (v2.0.0)

**发布日期：** 2025-11-29
**版本：** v2.0.0
**状态：** ✅ 核心功能已完成

### 总体进度

- ✅ **阶段 1: 核心引擎重构** - 100% 完成
- ✅ **阶段 2: UI 引擎** - 100% 完成
- ⏳ **阶段 3: 高级特性** - 0% (规划中)
- ⏳ **阶段 4: 生态建设** - 0% (规划中)

---

## ✅ 已完成功能

### 阶段 1: 核心引擎重构

#### 1.1 配置系统 ✅

- [x] **ConfigModels.cs** - 定义强类型配置模型
  - [x] TransportConfig (type, endpoint, auth, tls)
  - [x] RestConfig (endpoints, headers, auth)
  - [x] CommandsConfig (system, api, macros, aliases)
  - [x] LayoutConfig (structure, regions)
  - [x] ThemeConfig (colors, icons)
  - [x] BindingsConfig (数据源映射)

- [x] **ConfigLoader.cs** - 配置加载
  - [x] YAML 解析
  - [x] 配置验证
  - [x] 路径自动探测
  - [ ] ⏳ 支持 `imports` 引用其他文件
  - [ ] ⏳ 支持变量引用 `{theme.colors.primary}`
  - [ ] ⏳ 支持环境变量 `${ENV_VAR}`

#### 1.2 传输层抽象 ✅

- [x] **ITransport.cs** - 统一传输接口
  ```csharp
  interface ITransport
  {
      Task<TransportResponse> InvokeAsync(string endpoint, Dictionary<string, object>? request);
      IAsyncEnumerable<TransportResponse> SubscribeAsync(string endpoint);
      Task ConnectAsync();
      Task DisconnectAsync();
  }
  ```

- [x] **VirtualTransport.cs** - 虚拟传输实现
  - [x] 本地模拟执行
  - [x] 模拟状态流
  - [x] 随机数据生成

- [x] **RestTransport.cs** - REST 实现
  - [x] HTTP 客户端封装
  - [x] 支持 GET/POST/PUT/DELETE
  - [x] 认证支持 (Bearer/Basic/API Key)
  - [x] URL 参数和请求体构建
  - [x] 轮询适配器（模拟流式数据）
  - [ ] ⏳ JSONPath 响应解析

- [ ] ⏳ **GrpcTransport.cs** - gRPC 实现（规划中）
  - [ ] 根据 schema 配置动态构建请求
  - [ ] 支持 unary, server_stream, client_stream, bidi_stream
  - [ ] 自动类型转换
  - [ ] 连接管理和自动重连

- [ ] ⏳ **WebSocketClient.cs** - WebSocket 实现（规划中）
  - [ ] 实时数据订阅
  - [ ] 自动重连
  - [ ] 消息解析

#### 1.3 命令系统 ✅

- [x] **CommandRegistry.cs** - 命令注册表
  - [x] 从配置加载所有命令定义
  - [x] 支持别名解析
  - [x] 正则模式匹配

- [x] **CommandParser.cs** - 命令解析器
  - [x] 正则捕获组提取参数
  - [x] 参数类型转换
  - [x] 构建请求映射 (协议无关)

- [x] **CommandExecutor.cs** - 命令执行器
  - [x] 执行 builtin 命令 (help, exit, clear, status)
  - [x] 执行 api 命令 (调用 ITransport)
  - [x] 执行 macro 命令 (多步骤序列)
  - [x] 模板渲染响应

#### 1.4 模板引擎 ✅

- [x] **TemplateEngine.cs** - 响应模板渲染
  - [x] 变量替换 `{{.field}}`
  - [x] 条件渲染 `{{if .success}}...{{end}}`
  - [ ] ⏳ 循环渲染 `{{range .items}}...{{end}}`
  - [x] Spectre.Console markup 支持

### 阶段 2: UI 引擎 ✅

#### 2.1 布局引擎 ✅

- [x] **LayoutEngine.cs** - 布局解析和构建
  - [x] 解析 YAML 布局定义
  - [x] 构建 Spectre.Console Layout 树
  - [x] 支持 rows 布局
  - [ ] ⏳ 支持 columns, grid 布局

- [x] **ComponentFactory.cs** - 组件工厂
  - [x] Panel 组件
  - [x] Grid 组件
  - [x] Table 组件
  - [ ] ⏳ Chart 组件
  - [ ] ⏳ Progress Bar 组件

#### 2.2 数据绑定 ✅

- [x] **BindingEngine.cs** - 数据绑定引擎
  - [x] 从传输层绑定实时数据
  - [x] 从配置绑定静态数据
  - [x] 支持字段映射
  - [x] 自动类型转换

- [x] **DataContext.cs** - 数据上下文
  - [x] 统一数据源管理
  - [x] 数据变更通知
  - [x] 缓存机制

#### 2.3 REPL 引擎 ✅

- [x] **ReplEngine.cs** - REPL 主循环
  - [x] 用户输入处理
  - [x] 命令执行
  - [x] 结果显示
  - [x] 异常处理

- [x] **SpectreReplView.cs** - Spectre.Console 视图
  - [x] 终端 UI 渲染
  - [x] 实时状态更新
  - [x] 输入提示
  - [ ] ⏳ 命令历史
  - [ ] ⏳ 自动补全

---

## ⏳ 进行中/规划中

### 阶段 3: 高级特性

#### 3.1 gRPC 传输层

**优先级：高**
**预计工期：2 周**

- [ ] gRPC 客户端实现
- [ ] Protobuf 动态消息构建
- [ ] 四种调用模式支持
- [ ] TLS/SSL 配置
- [ ] 自动重连机制

#### 3.2 WebSocket 支持

**优先级：中**
**预计工期：1 周**

- [ ] WebSocket 客户端
- [ ] 实时消息处理
- [ ] 心跳机制
- [ ] 断线重连

#### 3.3 配置系统增强

**优先级：中**
**预计工期：1 周**

- [ ] 多文件导入 (`imports`)
- [ ] 变量引用和替换
- [ ] 环境变量支持
- [ ] 配置验证增强
- [ ] 配置热重载

#### 3.4 UI 增强

**优先级：低**
**预计工期：2 周**

- [ ] 命令历史 (↑↓ 键)
- [ ] 命令自动补全 (Tab 键)
- [ ] 多窗格布局
- [ ] 图表组件
- [ ] 进度条组件
- [ ] 交互式表单

#### 3.5 模板引擎增强

**优先级：低**
**预计工期：3 天**

- [ ] 循环渲染 `{{range .items}}...{{end}}`
- [ ] 嵌套模板
- [ ] 自定义函数
- [ ] 格式化函数 (date, number, etc.)

### 阶段 4: 生态建设

#### 4.1 插件系统

**优先级：中**
**预计工期：3 周**

- [ ] 插件接口定义
- [ ] 动态加载机制
- [ ] 插件生命周期管理
- [ ] 插件隔离和安全
- [ ] 插件市场 (规划)

#### 4.2 测试和文档

**优先级：高**
**预计工期：持续**

- [ ] 单元测试覆盖 (目标 80%)
- [ ] 集成测试
- [ ] 性能测试
- [ ] 配置文件完整文档
- [ ] API 参考文档
- [ ] 视频教程

#### 4.3 工具和辅助

**优先级：低**
**预计工期：2 周**

- [ ] 可视化配置编辑器
- [ ] 配置验证工具
- [ ] 日志查看器
- [ ] 性能分析工具

#### 4.4 多语言支持

**优先级：低**
**预计工期：1 周**

- [ ] i18n 框架集成
- [ ] 中文本地化
- [ ] 英文本地化
- [ ] 可配置语言切换

---

## 🐛 已知问题

### 高优先级

- [ ] REST 传输层轮询间隔配置未生效
- [ ] 模板引擎循环语法未实现

### 中优先级

- [ ] 配置文件变量引用未实现
- [ ] 环境变量支持未实现
- [ ] 日志级别配置未实现

### 低优先级

- [ ] 命令历史功能缺失
- [ ] 自动补全功能缺失
- [ ] 多窗格布局支持不完整

---

## 📅 开发里程碑

### v2.0.0 - 核心功能 ✅ (2025-11-29)

- ✅ 配置驱动架构
- ✅ Virtual 和 REST 传输层
- ✅ 命令系统完整实现
- ✅ UI 布局引擎
- ✅ 模板引擎基础功能

### v2.1.0 - gRPC 支持 ⏳ (计划 2025-12)

- [ ] gRPC 传输层
- [ ] 配置系统增强
- [ ] 模板引擎增强

### v2.2.0 - UI 增强 ⏳ (计划 2026-01)

- [ ] 命令历史和补全
- [ ] 多窗格布局
- [ ] 交互式组件

### v2.3.0 - 插件系统 ⏳ (计划 2026-02)

- [ ] 插件框架
- [ ] 示例插件
- [ ] 插件文档

### v3.0.0 - 生态完善 ⏳ (计划 2026-Q2)

- [ ] 可视化配置编辑器
- [ ] 插件市场
- [ ] 多语言支持
- [ ] 性能优化

---

## 🤝 贡献指南

我们欢迎各种形式的贡献！

### 贡献方式

1. **报告 Bug** - 提交详细的问题描述
2. **功能建议** - 提出新功能想法
3. **代码贡献** - 提交 Pull Request
4. **文档改进** - 完善文档和示例
5. **测试** - 编写测试用例

### 开发流程

1. Fork 项目
2. 创建功能分支 (`git checkout -b feature/amazing-feature`)
3. 提交更改 (`git commit -m 'Add amazing feature'`)
4. 推送到分支 (`git push origin feature/amazing-feature`)
5. 创建 Pull Request

### 代码规范

- 遵循 C# 编码规范
- 添加 XML 文档注释
- 编写单元测试
- 保持 SOLID 原则
- 代码审查通过后合并

---

## 📊 技术债务

### 需要重构的部分

1. **配置加载器** - 增强错误处理和验证
2. **模板引擎** - 性能优化
3. **日志系统** - 统一日志格式和级别

### 性能优化

1. **配置缓存** - 减少重复解析
2. **UI 渲染** - 减少不必要的重绘
3. **数据绑定** - 优化更新机制

---

## 📝 决策记录

### ADR-001: 选择 Spectre.Console 作为 UI 框架

**日期：** 2025-11-28
**状态：** 已采纳
**背景：** 需要一个现代化的终端 UI 框架
**决策：** 使用 Spectre.Console
**理由：**
- 丰富的组件库
- 优秀的 TrueColor 支持
- 活跃的社区
- MIT 许可

### ADR-002: 协议无关的传输层抽象

**日期：** 2025-11-28
**状态：** 已采纳
**背景：** 需要支持多种通信协议
**决策：** 定义 ITransport 接口抽象
**理由：**
- 降低耦合
- 易于扩展
- 统一API

### ADR-003: 配置驱动架构

**日期：** 2025-11-28
**状态：** 已采纳
**背景：** 需要灵活的配置方式
**决策：** 使用 YAML 配置文件驱动所有行为
**理由：**
- 零代码适配不同设备
- 配置即文档
- 易于维护和版本控制

---

## 🔗 相关资源

- [架构文档](architecture.md)
- [快速开始](quick-start.md)
- [GitHub 仓库](https://github.com/lusipad/kcode)
- [Issue 追踪](https://github.com/lusipad/kcode/issues)
- [讨论区](https://github.com/lusipad/kcode/discussions)

---

**最后更新：** 2025-11-29
**维护者：** [@lusipad](https://github.com/lusipad)
