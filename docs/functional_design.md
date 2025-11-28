# CNC 控制终端 - 功能设计分析 (Functional Design Analysis)

## 核心设计理念
将 "Claude Code" 的**对话式交互**与 CNC 的**实时控制**相结合。
传统 CNC 界面通常是复杂的按钮和菜单堆叠，而 CLI (命令行) 提供了极高的效率和灵活性，特别是对于熟练的操作员。

---

## 1. 快速指令执行 (Command Execution)
**目标**: 提供比点击按钮更快的操作方式，同时降低 G 代码的记忆门槛。

### 功能分析
*   **混合指令系统 (Hybrid Command System)**
    *   **原生 G 代码**: `G0 X100`, `M3 S1000`。这是核心，必须支持。
    *   **自然语言别名 (Natural Language Aliases)**:
        *   `home` -> `G28` (回零)
        *   `zero x` -> `G92 X0` (X轴清零)
        *   `tool 1` -> `M6 T1` (换刀)
    *   **系统命令**: `/connect`, `/load file.nc`, `/sim` (开启模拟模式)。

*   **智能辅助 (IntelliSense & Autocomplete)**
    *   **G 代码补全**:
        *   输入 `G` 或 `M` 时，弹出下拉列表显示可用代码及其描述。
        *   *示例*: 输入 `G0` -> 提示 `G00 [Rapid Move] (快速定位)`。
    *   **参数名补全**:
        *   输入 `/set` 后按 `Tab`，列出所有可配置参数。
        *   输入 `/set X_` -> 自动补全为 `X_MAX_ACCEL`。
    *   **文件路径补全**:
        *   输入 `/load` 后支持路径 Tab 补全 (如 `/load D:\Pro` -> `D:\Projects\`)。
    *   **上下文感知提示**:
        *   当输入 `G0` 后，提示文本显示 `G0 X<数值> Y<数值> Z<数值>`，指导用户输入参数。

*   **安全机制 (Safety Guard)**
    *   **危险操作确认**: 输入 `zero all` 或 `reboot` 时，CLI 应暂停并要求确认 (Y/n)。
    *   **状态互斥**: 机器在 `RUN` 状态时，拒绝执行移动指令并报错。

### 交互体验 (Claude Code 风格)
> **User**: `move x 50`
> **System**:
> 🟢 **已执行**: 向 X 轴正方向移动 50mm
> *当前坐标: X: 50.000, Y: 0.000, Z: 0.000*

---

## 2. 状态查看 (Status Monitoring)
**目标**: 让操作员一眼就能看到机器在做什么，是否有异常。

### 功能分析
*   **实时数据 (Real-time Data)**
    *   **DRO (数显)**: X, Y, Z, A 轴的机械坐标和工件坐标。这是**最高优先级**信息。
    *   **负载/速度**: 主轴转速 (RPM)，进给速度 (F)，主轴负载 (%)。

*   **机器状态 (Machine State)**
    *   **模式**: IDLE (空闲), JOG (点动), AUTO (自动), ALARM (报警)。
    *   **IO 状态**: 限位开关、急停按钮、冷却液开关状态。

### 交互体验 (Claude Code 风格)
*   **持久化页脚 (Sticky Footer)**:
    *   屏幕底部固定 1-2 行，始终显示 DRO 和核心状态。无论上方对话如何滚动，这部分永远可见。
    *   **Emoji 增强**: 使用 Emoji 图标美化每个数据项，提升视觉愉悦度。
        *   `📍 X: 100.0` | `📍 Y: 50.0` | `📍 Z: 10.0`
        *   `🚀 F: 1500` | `🌪️ S: 8000`
        *   `🌡️ T: 45°C` | `⏱️ 00:12:30`
    *   使用颜色编码：🟢 正常, 🟡 警告, 🔴 报警。

---

## 2.1 日志系统 (Logging System)
用户关心的“日志”分为两类，处理方式不同：

1.  **操作日志 (Operation Log)**:
    *   即“聊天记录”。所有的指令输入和系统反馈（如“移动完成”）都会保留在屏幕上，向上滚动。
    *   *持久化*: 自动保存到 `logs/session_YYYYMMDD.txt`，方便追溯。

2.  **系统/调试日志 (System/Debug Log)**:
    *   机器底层的报错、调试信息（如“串口丢包”、“电机过热警告”）。
    *   **显示位置**:
        *   **普通模式**: 仅在出现 `Warning/Error` 时，以红色/黄色卡片形式插入到聊天流中。
        *   **调试模式**: 输入 `/logs` 进入**全屏日志查看器** (类似 `less` 或 `tail -f`)，按 `q` 退出。
        *   **实时面板**: 可选开启一个侧边栏或底部小窗（Split View）实时滚动显示底层日志。
*   **仪表盘视图 (Dashboard View)**:
    *   输入 `/status` 清空屏幕，显示全屏的详细状态面板（包含 IO 灯、电机负载柱状图等），按任意键返回命令行。

---

## 3. 参数查看与修改 (Parameter Management)
**目标**: 解决传统 CNC 系统参数查找困难、修改繁琐的问题。

### 功能分析
*   **参数检索 (Search & Retrieval)**
    *   **模糊搜索**: 输入 `/find speed` 列出所有包含 "speed" 的参数（如 `MAX_SPEED_X`, `JOG_SPEED`）。
    *   **分类查看**: `/params axis` (轴参数), `/params spindle` (主轴参数)。

*   **参数修改 (Modification)**
    *   **快速修改**: `/set X_MAX 500`。
    *   **批量导出/导入**: `/export config.json` (方便备份)。

### 交互体验 (Claude Code 风格)
*   **表格化展示**: 使用 ASCII 表格展示参数，清晰列出 `ID`, `Name`, `Value`, `Description`。
*   **修改反馈**:
    > **User**: `/set X_MAX 600`
    > **System**:
    > 📝 **参数已更新**: `X_MAX` 从 `500` -> `600`
    > *⚠️ 注意: 需要重启控制器生效*

---

## 4. 场景模拟 (Scenario Walkthrough)

### 场景：加工一个零件
1.  **准备**:
    *   用户输入: `/connect COM3` -> 系统: "✅ 已连接控制器"
    *   用户输入: `home` -> 系统: "🏠 正在回零..." (显示进度条) -> "✅ 回零完成"
2.  **对刀**:
    *   用户输入: `move x 10 y 10` (移动到工件角)
    *   用户输入: `zero xy` (清零工件坐标)
3.  **运行**:
    *   用户输入: `/load part1.nc` -> 系统: "📄 文件已加载 (1200行)"
    *   用户输入: `run`
    *   **界面变化**: 命令行进入 "监控模式"，显示当前执行的 G 代码行号、剩余时间。页脚实时跳动。
4.  **异常**:
    *   机器触发限位 -> **页脚变红**，显示 "🔴 ALARM: X Limit Hit"。
    *   用户输入: `reset` (复位报警)。

---

## 总结
这个设计将 CNC 操作从 "翻菜单" 变成了 "对话"。
- **高频操作** (移动、启停) -> **极简指令**。
- **复杂信息** (参数、状态) -> **结构化视图**。
- **核心安全** -> **实时页脚监控**。

---

## 5. 关键缺失功能补充 (Critical Additions)
基于 CNC 实际加工场景，我认为以下功能也是**至关重要**的：

### 5.1 全局安全快捷键 (Global Safety Hotkeys)
CLI 最大的风险是打字速度慢，遇到紧急情况无法快速响应。必须支持**全局单键拦截**：
*   **`ESC` 或 `Ctrl+C`**: **紧急停止 (E-Stop)**。无论当前在输入什么，立即发送停机指令。
*   **`Space` (空格键)**: **进给保持 (Feed Hold)**。暂停机器运动，再次按下恢复。
*   *设计*: 这些按键在后台线程监听，优先级高于任何 UI 渲染。

### 5.2 预演与仿真 (Verification & Simulation)
用户提到的“仿真”其实包含两个层面，在 CLI 中我们重点做**安全检查**：

*   **层面 1：数学边界检查 (Bounds Check) - [核心安全功能]**
    *   **目的**: 防止撞机和超程。这是“预演”的核心意义。
    *   **行为**: 不移动机器，快速解析 G 代码，计算出刀具路径的**最大/最小包围盒 (Bounding Box)**。
    *   **判定**: 如果 `Z_Min < Z_Limit`，直接报错拦截。这是纯数学计算，速度极快。
    *   **输出**: "❌ 错误: 第 15 行 Z 轴将移动到 -55.0，超出了软限位 (-50.0)。"

*   **层面 2：路径预览 (Visual Preview) - [辅助功能]**
    *   **目的**: 确认形状对不对。
    *   **行为**: 在 CLI 中用 ASCII 字符或简单的 2D 绘图（Spectre.Console Canvas）画出刀路轨迹。
    *   **输出**: 显示一个简单的 X-Y 平面轨迹图，让用户确认“哦，这确实是个圆形”。

### 5.3 刀具管理 (Tool Management)
没有刀具长度补偿 (TLO)，CNC 几乎无法工作。
*   **刀具表**: `/tools` 显示刀具列表 (ID, 长度, 直径, 描述)。
*   **快速对刀**: 
    *   `/touchoff T1` (自动执行对刀宏，测量 T1 长度并更新刀具表)。

---

## 6. AI-Native 体验与美学设计 (UX & Aesthetics)
参考 Claude Code, Gemini CLI 等现代工具，我们将引入以下设计元素，使 CNC 界面摆脱“工业软件”的冰冷感：

### 6.1 视觉美学 (Visual Polish)
*   **配色方案 (Theme)**: 严格复刻 **Claude Code** 的配色，追求专业与克制。
    *   **背景**: 纯黑 (`#000000`) 或 极深灰 (`#1A1A1A`)。
    *   **Logo/强调**: **珊瑚橙 (Coral)** - 用于 Logo 和关键提示。
    *   **信息/元数据**: **天青色 (Cyan)** - 用于路径、型号、速度 (F/S)。
    *   **统计/度量**: **紫罗兰 (Purple/Lavender)** - 用于 Token 消耗、坐标数值。
    *   **成功/状态**: **森林绿 (Green)** - 用于 `IDLE` 状态和成功消息。
    *   *实现*: 使用 Spectre.Console 的 RGB 颜色定义。
*   **排版与留白 (Typography & Spacing)**:
    *   关键数据 (DRO) 使用**大号/粗体**字体 (通过 ASCII Art 或大字库)。
    *   表格和面板增加 Padding，避免密集排布，提升阅读舒适度。
*   **图标化 (Iconography & Emojis)**:
    *   **Emoji First**: 大量使用 **Emoji** (如 🟢, ⚠️, 🚨, 🏠, 🔧) 来标识状态。
    *   *优势*: Emoji 更加亲切、现代，且在所有终端上无需额外字体即可显示，符合 "Claude Code" 的风格。

### 6.2 微交互 (Micro-interactions)
*   **思考状态 (Thinking State)**:
    *   执行耗时操作（如回零、加载大文件）时，显示优雅的 **Spinner** 动画 (⠋⠙⠹⠸...)，而不是卡死不动。
*   **幽灵文本 (Ghost Text)**:
    *   输入时，根据历史记录或上下文，在光标后用灰色显示预测内容。按 `RightArrow` 采纳。
    *   *体验*: 就像 Copilot 在写代码时那样。

### 6.3 智能反馈 (Smart Feedback)
*   **自然语言解释**:
    *   不要只报 `Error 404`。
    *   **AI 风格**: "🤔 我无法理解 'G999'。您是指 'G99' (返回R点) 吗？"
*   **进度可视化**:
    *   加工过程中，使用**渐变色进度条**显示当前工序进度。

---

## 7. 配置与扩展 (Configuration & Extensibility)
为了适应不同的机器和用户习惯，系统必须高度可配置且易于扩展。

### 7.1 配置文件 (Configuration)
*   **格式**: 采用 `JSON` 或 `YAML` (如 `config.yaml`)，易于阅读和版本控制。
*   **内容**:
    *   **连接设置**: 默认串口、波特率。
    *   **机器参数**: 轴限位、最大速度、加速度。
    *   **UI 主题**: 自定义颜色、Emoji 偏好。
*   **管理**:
    *   `/config`: 打印当前配置摘要。
    *   `/config edit`: 调用系统默认编辑器打开配置文件。
    *   `/config reload`: 热重载配置，无需重启软件。

### 7.2 宏与脚本 (Macros & Scripting)
*   **G 代码宏 (G-Code Macros)**:
    *   允许用户定义常用操作的快捷指令。
    *   *示例*: 在配置中定义 `probe_z` -> `G38.2 Z-10 F10; G10 L20 P1 Z0`。
    *   *使用*: 用户只需输入 `probe_z` 即可执行复杂序列。
*   **自定义命令 (Custom Commands)**:
    *   支持将外部脚本 (Python/PowerShell) 绑定到 CLI 命令。
    *   *示例*: `/camera` -> 调用外部摄像头程序拍照并保存。

### 7.3 配置示例 (Configuration Example)
用户可以通过编辑 `config.yaml` 轻松添加自定义指令：

```yaml
macros:
  # 简单的 G 代码别名
  home_all: "G28"
  
  # 复杂的宏序列 (自动对刀)
  auto_probe: 
    - "G91 G38.2 Z-50 F50"  # 探针下探
    - "G90 G10 L20 P1 Z0"   # 设为零点
    - "G91 G0 Z5"           # 抬刀

custom_commands:
  # 绑定外部脚本
  camera_snap: 
    cmd: "python scripts/snap.py"
    description: "Take a photo of the workpiece"

### 7.4 页脚配置 (Footer Configuration)
用户可以完全自定义底部状态栏的布局，决定显示哪些数据以及使用什么 Emoji：

```yaml
footer:
  layout: [ "coords", "spindle", "system" ] # 定义显示顺序
  
  sections:
    coords:
      template: "📍 X:{x:F3} Y:{y:F3} Z:{z:F3}"
      color: "purple"
    
    spindle:
      template: "🚀 F:{feed} 🌪️ S:{speed}"
      color: "cyan"
      
    system:
      template: "{status_icon} {status_text} | 🌡️ {temp}°C"
      color: "green"
```
这样，如果您更关心加工时间而不是温度，只需修改 `template` 即可。
```
用户只需在 CLI 输入 `home_all` 或 `camera_snap` 即可执行。



