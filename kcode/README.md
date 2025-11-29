# KCode - 现代化 CNC 控制终端

<div align="center">

**配置驱动 · 多协议支持 · 插件化架构**

一个基于 .NET 9 的现代化命令行工具，通过配置文件驱动的方式实现灵活的设备控制。

[快速开始](#快速开始) · [功能特性](#功能特性) · [文档](docs/) · [示例配置](Config/)

</div>

---

## 🎯 核心理念

**KCode 是一个零业务逻辑的客户端壳**，所有功能通过配置文件定义：

- 🔌 **协议无关** - 支持 Virtual/REST/gRPC（规划中）
- 🎨 **UI 可配置** - 布局、主题、数据绑定全部来自 YAML
- ⚙️ **命令可扩展** - 系统命令/API 调用/宏/别名统一配置
- 📦 **插件化** - 传输层、命令、UI 组件均可扩展

## ✨ 功能特性

### 🚀 已实现

- ✅ **配置驱动架构** - 通过 YAML 定义应用行为
- ✅ **虚拟传输层** - 本地测试模式，无需真实设备
- ✅ **REST 传输层** - 支持 HTTP/HTTPS API 调用
- ✅ **命令系统**
  - 系统命令：help, exit, status, clear
  - API 命令：正则模式匹配，自动参数提取
  - 宏命令：多步骤命令序列
  - 别名：命令快捷方式
- ✅ **UI 引擎**
  - 动态布局系统（Spectre.Console）
  - 数据绑定和实时更新
  - 主题配置
  - 状态栏
- ✅ **模板引擎** - 支持条件/循环/变量替换
- ✅ **配置系统** - 多文件导入、变量引用、环境变量

### 🔜 规划中

- ⏳ **gRPC 传输层** - 高性能双向流式通信
- ⏳ **WebSocket 支持** - 实时数据推送
- ⏳ **插件系统** - 动态加载自定义命令和组件

## 🏃 快速开始

### 环境要求

- .NET 9 SDK
- Windows Terminal（推荐）或支持 TrueColor 的终端

### 运行

```bash
# 克隆仓库
git clone https://github.com/lusipad/kcode.git
cd kcode/kcode

# 运行（默认使用 Virtual 模式）
dotnet run

# 使用指定配置
dotnet run -- --config Config/config-rest-test.yaml
```

### 基本使用

```bash
# 系统命令
help                    # 显示帮助
status                  # 显示详细状态
clear                   # 清屏
exit                    # 退出

# G-Code 命令（通过 API 传输层）
G28                     # 回零
G0 X10 Y20 Z5          # 快速定位
G1 X100 F500           # 直线插补

# 宏命令
home                    # 所有轴回零
zero_work               # 设置工件零点
auto_probe              # 自动对刀

# 参数设置
/set max_velocity 2000  # 设置参数
params                  # 查看所有参数
```

## 📂 项目结构

```
kcode/
├── Config/                      # 配置文件
│   ├── config-virtual.yaml     # 虚拟模式（默认）
│   ├── config-rest.yaml        # REST 模式
│   └── config-rest-test.yaml   # REST 测试模式
├── Core/                        # 核心模块
│   ├── Commands/               # 命令系统
│   │   ├── CommandParser.cs
│   │   ├── CommandExecutor.cs
│   │   └── CommandRegistry.cs
│   ├── Config/                 # 配置系统
│   │   ├── ConfigModels.cs
│   │   └── ConfigLoader.cs
│   ├── Transport/              # 传输层
│   │   ├── ITransport.cs
│   │   ├── TransportFactory.cs
│   │   ├── RestTransport.cs
│   │   └── VirtualTransport.cs
│   ├── UI/                     # UI 引擎
│   │   ├── LayoutEngine.cs
│   │   ├── BindingEngine.cs
│   │   └── ComponentFactory.cs
│   └── Template/               # 模板引擎
│       └── TemplateEngine.cs
├── docs/                        # 文档
│   ├── architecture.md         # 架构设计
│   ├── quick-start.md          # 快速开始指南
│   └── development.md          # 开发计划
└── Program.cs                   # 入口文件
```

## 🎨 配置示例

### 最小配置

```yaml
app:
  name: "my-app"
  version: "1.0.0"

transport:
  type: "virtual"  # virtual | rest | grpc

commands:
  system:
    help:
      description: "显示帮助"
      action: "builtin:help"
```

### REST API 配置

```yaml
transport:
  type: "rest"
  base_url: "http://localhost:8080"
  timeout_ms: 5000
  auth:
    type: "bearer"
    token: "your-token-here"

api:
  get_status:
    method: "GET"
    path: "/api/status"
    response:
      x: "double"
      y: "double"
      state: "string"

commands:
  api:
    status:
      endpoint: "get_status"
      response_template: |
        Position: X={{.x}} Y={{.y}}
        State: {{.state}}
```

完整配置示例请参考 [Config 目录](Config/)。

## 📖 文档

- [架构设计](docs/architecture.md) - 深入了解系统架构
- [快速开始](docs/quick-start.md) - 详细的入门指南
- [开发计划](docs/development.md) - 功能开发路线图
- [配置参考](docs/configuration.md) - 配置文件完整说明（待补充）

## 🛠️ 开发

### 构建

```bash
# 调试构建
dotnet build

# 发布构建
dotnet publish -c Release -r win-x64 --self-contained
```

### 测试

```bash
# 运行虚拟模式测试
dotnet run -- --test-virtual

# 运行 REST API 测试
dotnet run -- --test-rest
```

### 代码结构原则

本项目遵循以下软件工程原则：

- **SOLID** - 单一职责、开闭原则、依赖倒置
- **DRY** - 避免代码重复
- **KISS** - 保持简单直观
- **YAGNI** - 只实现当前需要的功能

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📄 许可

MIT License

## 🙏 致谢

- UI 框架：[Spectre.Console](https://spectreconsole.net/)
- YAML 解析：[YamlDotNet](https://github.com/aaubry/YamlDotNet)
- 设计灵感：[Claude Code](https://claude.com/claude-code)

---

<div align="center">

Made with ❤️ by [lusipad](https://github.com/lusipad)

</div>
