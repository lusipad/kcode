# kcode - CNC 控制终端

一个基于 .NET 9 的现代化命令行 CNC 控制工具，灵感来源于 Claude Code 的交互体验。

## 特性

- 现代 UI：Claude Code 风格配色与丰富提示
- Slash 命令面板：输入 `/` 即可浏览所有命令，方向键逐项移动，`Tab/Enter` 可立即写回输入框
- 实时状态栏：持久化显示坐标、速度和机器状态
- 配置驱动：YAML 配置自定义宏、命令别名、页眉页脚模板、快捷键
- 安全控制：边界检查、急停热键、进给保持
- 可插拔传输层：配置驱动的 `TransportFactory`，可在 Virtual / REST 之间切换

## 快速开始

### 环境要求

- .NET 9 SDK
- Windows Terminal（推荐）或支持 TrueColor 的终端

### 运行

```bash
cd kcode
dotnet run
```

如需切换不同配置，可指定 `--config`（默认自动探测 `Config/config-virtual.yaml`）：

```bash
dotnet run -- --config Config/config-rest-test.yaml
```

> 提示：在仓库根目录执行 `dotnet run --project kcode/kcode.csproj` 也会自动找到默认配置文件。

### Slash 命令面板
- 输入 `/` 即可唤出命令列表
- 使用方向键逐项移动（翻页时也会跟随滚动）
- `Tab` 或 `Enter` 会把当前命令写入输入框，并保留光标
- 再次按 `Enter` 即可执行当前命令

### 配置驱动
- `Config/config-*.yaml` 控制 UI 模板、快捷键、命令/宏、以及传输层类型。
- 传输层默认 `virtual`；需要接入远端控制时，可改用 `config-rest-test.yaml` 或在 `config-rest.yaml` 中配置真实 API 。

### 基本命令

- `home` - 机器回零（别名 -> `G28`）
- `G0 X10 Y20` - 快速定位
- `help` - 显示帮助
- `status` - 显示详细状态
- `/set key value` - 参数设置
- `clear` - 清屏
- `exit` - 退出

## 配置

编辑 `Config/config-virtual.yaml`（或 `config-rest*.yaml`）可以自定义：
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
├── Config/          # v2 配置 (virtual / rest / rest-test)
├── Core/            # 核心 (ReplEngine, Commands, Transport, UI 绑定)
├── UI/              # 主题辅助与消息输出
├── docs/            # 项目文档
├── TestVirtualMode.cs      # 虚拟模式自动化测试
├── TestRestApi.cs   # REST API 测试
└── Program.cs       # 入口（默认运行 v2）
```

## License

MIT
