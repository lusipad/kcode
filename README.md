# kcode - CNC 控制终端

一个基于 .NET 9 的现代化命令行 CNC 控制工具，灵感来源于 Claude Code 的交互体验。

## 特性

- 现代 UI：Claude Code 风格配色与丰富提示
- 实时状态栏：持久化显示坐标、速度和机器状态
- 配置驱动：YAML 配置自定义宏、命令别名、页眉页脚模板、快捷键
- 安全控制：边界检查、急停热键、进给保持
- 可插拔传输层：`IControlTransport` 抽象，默认虚拟机，可切换 gRPC

## 快速开始

### 环境要求

- .NET 9 SDK
- Windows Terminal（推荐）或支持 TrueColor 的终端

### 运行

```bash
cd kcode
dotnet run
```

### 配置驱动
- `Config/config.yaml` 控制 UI 模板（页眉/页脚）、快捷键映射、命令别名/系统命令列表、宏、传输层类型。
- 传输层默认 `virtual`，需要接入远端控制时可将 `transport.type` 改为 `grpc`，并设置 `endpoint` 与 `timeout_ms`。

### 基本命令

- `home` - 机器回零（别名 -> `G28`）
- `G0 X10 Y20` - 快速定位
- `/help` - 显示帮助
- `/status` - 显示详细状态
- `/params` - 参数表
- `/preview <gcode>` - 预览包络盒和路径
- `/exit` - 退出

## 配置

编辑 `Config/config.yaml` 可以自定义：
- 宏定义
- 命令别名与系统命令
- UI 模板（页眉/页脚）与快捷键
- 传输层类型与连接参数

示例：
```yaml
macros:
  auto_probe:
    - "G91 G38.2 Z-50 F50"
    - "G90 G10 L20 P1 Z0"
    - "G91 G0 Z5"
```

## 项目文档

- [功能设计](docs/functional_design.md) - 完整的功能设计文档
- [实施计划](docs/implementation_plan.md) - 技术实施方案
- [任务清单](docs/task.md) - 开发任务跟踪
- [开发进展](docs/walkthrough.md) - 当前开发状态

## 项目结构

```
kcode/
├── Config/          # 配置文件
├── Core/            # 核心逻辑 (REPL, 控制器, Transport 抽象)
├── UI/              # 界面渲染
├── docs/            # 项目文档
└── Program.cs       # 入口
```

## License

MIT
