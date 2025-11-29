# KCode 快速开始指南

本指南将帮助你快速上手 KCode，从安装到运行第一个命令。

---

## 📋 前置要求

### 必需

- **.NET 9 SDK** - [下载地址](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Windows Terminal**（推荐）或支持 TrueColor 的终端

### 可选

- Git（用于克隆仓库）
- Visual Studio 2022 或 VS Code（用于开发）

---

## 🚀 安装

### 方式一：从源码运行

```bash
# 1. 克隆仓库
git clone https://github.com/lusipad/kcode.git
cd kcode/kcode

# 2. 运行（默认使用 Virtual 模式）
dotnet run

# 3. 使用指定配置
dotnet run -- --config Config/config-rest-test.yaml
```

### 方式二：发布为独立可执行文件

```bash
# 发布为单文件可执行程序
dotnet publish -c Release -r win-x64 --self-contained -o publish

# 运行
cd publish
./kcode.exe
```

---

## 🎮 基本使用

### 启动程序

```bash
dotnet run
```

你将看到欢迎界面：

```
┌─────────────────────────────────────────────┐
│ kcode 2.0.0                                 │
│ 现代化命令行 CNC 控制终端                    │
└─────────────────────────────────────────────┘

Connected to: virtual

> _
```

### 系统命令

```bash
# 显示帮助
help
# 或
?
# 或
h

# 显示详细状态
status
# 或
st

# 清屏
clear
# 或
cls

# 退出程序
exit
# 或
quit
# 或
q
```

### G-Code 命令（Virtual 模式）

在 Virtual 模式下，所有命令都会被模拟执行：

```bash
# 回零
G28

# 快速定位
G0 X10 Y20 Z5

# 直线插补
G1 X100 Y50 F500

# 圆弧插补
G2 X50 Y50 I25 J0
```

### 宏命令

宏命令是预定义的多步骤操作：

```bash
# 所有轴回零
home
# 或
home_all
# 或
回零

# 设置工件零点
zero_work
# 或
清零

# 自动对刀
auto_probe
# 或
对刀
```

### 参数操作

```bash
# 查看所有参数
params

# 设置参数
/set max_velocity 2000
/set acceleration 500
```

---

## ⚙️ 配置模式

KCode 支持多种配置模式，通过 `--config` 参数切换。

### Virtual 模式（默认）

**配置文件：** `Config/config-virtual.yaml`

**特点：**
- 本地模拟，无需真实设备
- 适合学习和测试
- 模拟随机状态数据

**运行：**

```bash
dotnet run
# 或明确指定
dotnet run -- --config Config/config-virtual.yaml
```

### REST 模式

**配置文件：** `Config/config-rest.yaml` 或 `config-rest-test.yaml`

**特点：**
- 连接真实的 REST API
- 支持 HTTP/HTTPS
- 支持多种认证方式

**运行：**

```bash
dotnet run -- --config Config/config-rest-test.yaml
```

**配置示例：**

```yaml
transport:
  type: "rest"
  base_url: "http://localhost:8080"
  timeout_ms: 5000

  auth:
    type: "bearer"
    token: "your-api-token"

  endpoints:
    execute:
      method: "POST"
      path: "/api/execute"
      request_body:
        command: "{text}"
```

---

## 🔧 自定义配置

### 创建自定义配置文件

1. 复制现有配置文件：

```bash
cp Config/config-virtual.yaml Config/my-config.yaml
```

2. 编辑配置文件：

```yaml
app:
  name: "my-app"
  slogan: "我的自定义应用"

transport:
  type: "virtual"  # 或 "rest"

commands:
  system:
    hello:
      description: "打招呼"
      action: "builtin:help"

  macros:
    my_macro:
      description: "自定义宏"
      steps:
        - endpoint: "execute"
          request:
            text: "G28"
      response_template: "[green]完成！[/]"
```

3. 使用自定义配置：

```bash
dotnet run -- --config Config/my-config.yaml
```

### 配置结构说明

详细配置说明请参考：
- [架构文档](architecture.md#五配置系统-configuration-system)
- [配置示例](../Config/)

---

## 📝 常见任务

### 任务 1：连接到 REST API

1. 准备 REST API（或使用测试服务器）
2. 编辑 `Config/config-rest.yaml`：

```yaml
transport:
  type: "rest"
  base_url: "http://your-api-server:8080"

  endpoints:
    execute:
      method: "POST"
      path: "/api/execute"
```

3. 运行：

```bash
dotnet run -- --config Config/config-rest.yaml
```

4. 测试命令：

```bash
G28  # 应该调用你的 API
```

### 任务 2：添加自定义命令

编辑配置文件，添加命令：

```yaml
commands:
  api:
    my_command:
      pattern: "^mycommand (.+)$"
      endpoint: "execute"
      request_mapping:
        text: "$1"
      response_template: |
        [green]执行成功[/]: {{.message}}
```

使用：

```bash
mycommand test
```

### 任务 3：创建复杂宏

```yaml
commands:
  macros:
    complex_task:
      description: "复杂任务示例"
      steps:
        # 步骤 1: 回零
        - endpoint: "execute"
          request:
            text: "G28"

        # 步骤 2: 移动到起点
        - endpoint: "execute"
          request:
            text: "G0 X0 Y0 Z10"

        # 步骤 3: 执行加工
        - endpoint: "execute"
          request:
            text: "G1 X100 Y100 F500"

        # 步骤 4: 返回原点
        - endpoint: "execute"
          request:
            text: "G28"

      response_template: |
        [green]✅ 复杂任务完成[/]
        所有步骤已执行
```

---

## 🐛 故障排除

### 问题 1: 找不到配置文件

**错误信息：**
```
Could not find configuration file: Config/config.yaml
```

**解决方案：**
- 确保在项目根目录运行
- 使用 `--config` 明确指定配置文件路径
- 检查配置文件是否存在

### 问题 2: REST API 连接失败

**错误信息：**
```
Transport connection failed: Could not connect to http://localhost:8080
```

**解决方案：**
1. 检查 API 服务器是否运行
2. 验证 `base_url` 配置是否正确
3. 检查防火墙设置
4. 尝试使用测试模式：

```bash
dotnet run -- --test-rest
```

### 问题 3: 命令不识别

**解决方案：**
1. 使用 `help` 查看可用命令
2. 检查配置文件中的命令定义
3. 验证正则模式是否正确
4. 查看日志输出（在 `Logs/` 目录）

---

## 📚 下一步

- 📖 阅读 [架构文档](architecture.md) 深入了解系统设计
- 🔧 查看 [开发计划](development.md) 了解未来功能
- 💡 浏览 [Config 目录](../Config/) 学习更多配置示例
- 🤝 参与 [GitHub 讨论](https://github.com/lusipad/kcode/discussions)

---

## 💡 提示和技巧

### 1. 使用命令别名

定义常用命令的别名：

```yaml
commands:
  aliases:
    h: "help"
    s: "status"
    e: "exit"
    mv: "G0"
```

### 2. 自定义响应模板

使用 Spectre.Console 标记美化输出：

```yaml
response_template: |
  [bold green]✅ 成功[/]
  [dim]位置: X={{.x}} Y={{.y}}[/]
  [yellow]状态: {{.state}}[/]
```

### 3. 环境变量

在配置中使用环境变量：

```yaml
transport:
  base_url: "${API_BASE_URL}"

  auth:
    token: "${API_TOKEN}"
```

运行时设置：

```bash
# Windows
set API_BASE_URL=http://localhost:8080
set API_TOKEN=your-token
dotnet run

# Linux/macOS
API_BASE_URL=http://localhost:8080 API_TOKEN=your-token dotnet run
```

### 4. 日志调试

查看日志文件：

```bash
# Windows
type Logs\kcode.log

# Linux/macOS
cat Logs/kcode.log
```

---

**需要帮助？**
- 📧 提交 Issue: https://github.com/lusipad/kcode/issues
- 💬 讨论区: https://github.com/lusipad/kcode/discussions
