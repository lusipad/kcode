# KCode - 现代化 CNC 控制终端

<div align="center">

**配置驱动 · 多协议支持 · 插件化架构**

一个基于 .NET 9 的现代化命令行工具，通过配置文件驱动的方式实现灵活的设备控制。

[快速开始](#快速开始) · [文档](kcode/docs/) · [示例配置](kcode/Config/)

</div>

---

## 🎯 核心理念

**KCode 是一个零业务逻辑的客户端壳**，所有功能通过配置文件定义：

- 🔌 **协议无关** - 支持 Virtual/REST/gRPC（规划中）
- 🎨 **UI 可配置** - 布局、主题、数据绑定全部来自 YAML
- ⚙️ **命令可扩展** - 系统命令/API 调用/宏/别名统一配置
- 📦 **插件化** - 传输层、命令、UI 组件均可扩展

## ✨ 功能特性

- ✅ **配置驱动架构** - 通过 YAML 定义应用行为
- ✅ **多传输层支持** - Virtual（测试）/ REST（HTTP API）/ gRPC（规划中）
- ✅ **完整命令系统** - 系统命令、API 调用、宏、别名
- ✅ **动态 UI 引擎** - 布局、主题、数据绑定
- ✅ **模板引擎** - 支持条件/循环/变量替换

## 🚀 快速开始

### 环境要求

- .NET 9 SDK - [下载地址](https://dotnet.microsoft.com/download/dotnet/9.0)
- Windows Terminal（推荐）或支持 TrueColor 的终端

### 运行

```bash
# 克隆仓库
git clone https://github.com/lusipad/kcode.git
cd kcode

# 运行（默认使用 Virtual 模式）
cd kcode
dotnet run

# 使用指定配置
dotnet run -- --config Config/config-rest-test.yaml
```

### 基本使用

```bash
# 系统命令
help                    # 显示帮助
status                  # 显示详细状态
exit                    # 退出

# G-Code 命令
G28                     # 回零
G0 X10 Y20 Z5          # 快速定位

# 宏命令
home                    # 所有轴回零
zero_work               # 设置工件零点

# 参数操作
/set max_velocity 2000  # 设置参数
params                  # 查看所有参数
```

## 📁 项目结构

```
kcode/
├── kcode/                       # 主项目
│   ├── Config/                 # 配置文件
│   │   ├── config-virtual.yaml
│   │   ├── config-rest.yaml
│   │   └── config-rest-test.yaml
│   ├── Core/                   # 核心模块
│   │   ├── Commands/          # 命令系统
│   │   ├── Config/            # 配置系统
│   │   ├── Transport/         # 传输层
│   │   ├── UI/                # UI 引擎
│   │   └── Template/          # 模板引擎
│   ├── docs/                   # 📖 项目文档
│   │   ├── README.md          # 文档索引
│   │   ├── architecture.md    # 架构设计
│   │   ├── quick-start.md     # 快速开始
│   │   └── development.md     # 开发计划
│   └── Program.cs              # 入口文件
├── kcode.Tests/                # 单元测试
├── KcodeTestApi/               # REST API 测试服务器
└── README.md                   # 本文件
```

## 📖 文档

完整文档位于 [`kcode/docs/`](kcode/docs/) 目录：

- **[快速开始指南](kcode/docs/quick-start.md)** - 详细的入门教程
- **[架构设计](kcode/docs/architecture.md)** - 系统架构和设计理念
- **[开发计划](kcode/docs/development.md)** - 功能路线图和进度
- **[配置示例](kcode/Config/)** - 各种配置文件示例

## 🧪 测试

### 单元测试

```bash
dotnet test kcode.Tests/kcode.Tests.csproj
```

### 虚拟模式测试

```bash
cd kcode
dotnet run -- --test-virtual
```

### REST API 测试

```bash
# 终端 1: 启动测试 API 服务器
cd KcodeTestApi
dotnet run

# 终端 2: 运行 KCode
cd kcode
dotnet run -- --config Config/config-rest-test.yaml
```

## 🛠️ 开发

### 构建

```bash
# 调试构建
dotnet build

# 发布构建
dotnet publish kcode/kcode.csproj -c Release -r win-x64 --self-contained
```

### 贡献

欢迎提交 Issue 和 Pull Request！详见 [开发计划](kcode/docs/development.md#-贡献指南)。

## 📄 许可

MIT License

## 🙏 致谢

- UI 框架：[Spectre.Console](https://spectreconsole.net/)
- YAML 解析：[YamlDotNet](https://github.com/aaubry/YamlDotNet)
- 设计灵感：[Claude Code](https://claude.com/claude-code)

---

<div align="center">

**[详细文档](kcode/docs/) · [问题反馈](https://github.com/lusipad/kcode/issues) · [讨论区](https://github.com/lusipad/kcode/discussions)**

Made with ❤️ by [lusipad](https://github.com/lusipad)

</div>
