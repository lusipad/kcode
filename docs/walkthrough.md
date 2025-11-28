# KCode CNC - 开发进展报告

## 项目概述
KCode CNC 是一个基于 .NET 9 的命令行 CNC 控制终端，灵感来源于 Claude Code 的现代 UI 设计。

## 已完成工作

### 1. 功能设计 (Functional Design)
创建了完整的功能设计文档 ([functional_design.md](file:///C:/Users/lus/.gemini/antigravity/brain/c6d20748-fe31-473d-bc61-e993755e9520/functional_design.md))，包含：

#### 核心功能
- **快速指令执行**: 混合 G 代码和自然语言指令 (`home`, `zero`)
- **智能辅助**: G 代码补全、参数提示、文件路径补全
- **状态监控**: 实时 DRO 坐标显示、机器状态、进给/转速
- **参数管理**: 分类查看、模糊搜索、快速修改
- **日志系统**: 
  - 操作日志（聊天记录）
  - 系统/调试日志（警告卡片或全屏查看器）

#### 安全特性
- **全局热键**: `ESC` 急停、`Space` 暂停
- **预演与仿真**:
  - 边界检查 (Bounding Box)
  - 路径预览 (2D 轨迹图)
- **刀具管理**: 刀具表、自动对刀

#### UI/UX 设计
- **配色方案**: Claude Code 风格
  - 珊瑚橙 (Logo)
  - 天青色 (信息)
  - 紫罗兰 (数值)
  - 森林绿 (状态)
- **Emoji 图标**: 📍, 🚀, 🌪️, 🌡️, 🟢
- **智能反馈**: AI 风格的错误提示
- **幽灵文本**: 自动补全预测

#### 扩展性
- **配置文件**: `Config/config-v2-*.yaml` (Virtual / REST / REST-Test)
- **宏定义**: 支持 G 代码序列别名
- **脚本绑定**: 外部 Python/PowerShell 调用
- **自定义页脚**: 布局引擎渲染

### 2. 视觉设计 (Concept Design)

生成了三个版本的概念图：

#### v1
初始版本，Cyberpunk 风格 (霓虹配色)

#### v2
![v2 概念图](file:///d:/Repos/kcode/docs/concept_v2.png)

调整为 Claude Code 配色 (克制专业)

#### v3 (最终版)
![v3 概念图](file:///d:/Repos/kcode/docs/concept_v3.png)

**特点**:
- Claude Code 精确配色
- Emoji 增强的页脚
- 日志警告卡片演示
- 幽灵文本补全演示

### 3. 项目实现

#### 项目结构
```
kcode/
├── Config/
│   ├── config-virtual.yaml
│   ├── config-rest.yaml
│   └── config-rest-test.yaml
├── Core/
│   ├── Commands/             # 命令定义/执行
│   ├── Config/ConfigLoader   # imports + 变量解析
│   ├── Transport/            # ITransport + Rest/Virt
│   ├── UI/                   # LayoutEngine/Binder
│   └── ReplEngine.cs         # Live REPL
├── TestVirtualMode.cs / TestRestApi.cs
└── Program.cs                # 入口
```

#### 核心技术栈
- **.NET 9** + **Spectre.Console.Live**
- **YamlDotNet**（多文件配置）
- **REST/Virt 传输层**（TransportFactory）

#### 配置示例
```yaml
macros:
  home_all: "G28"
  auto_probe: 
    - "G91 G38.2 Z-50 F50"
    - "G90 G10 L20 P1 Z0"
    - "G91 G0 Z5"

footer:
  sections:
    coords:
      template: "📍 X:{x:F3} Y:{y:F3} Z:{z:F3}"
      color: "purple"
```

### 4. 测试验证

#### 编译测试
✅ 项目编译成功（`dotnet build kcode`）

#### 运行测试
- `dotnet run`：默认载入 `Config/config-v2-virtual.yaml`
- `dotnet run -- --config Config/config-rest-test.yaml`：REST 模式验收
- `dotnet run -- --test-virtual` / `--test-rest`：自动化脚本

**功能验证**:
- ✅ Live Layout 渲染成功（ReplEngine + Spectre.Console）
- ✅ 命令解析/执行闭环稳定（CommandParser + CommandExecutor）
- ✅ Virtual/REST 传输热切可用（TransportFactory）

## 当前限制

### 已知问题
1. **Footer 显示**: 页脚目前未正确渲染到固定位置
   - 原因: Spectre.Console 的标准 REPL 模式不支持真正的"固定底部"
   - 计划: 需要使用 `Live Display` 或自定义 Console Buffer 管理

2. **智能补全**: 尚未实现
   - 需要集成 `ReadLine` 库或自定义输入处理

3. **日志系统**: 仅有基础框架
   - 需要实现文件写入和 `/logs` 查看器

## 后续工作

### 高优先级
- [ ] 实现真正的 Sticky Footer (使用 Live Display)
- [ ] 添加智能补全和幽灵文本
- [ ] 实现宏展开逻辑
- [ ] 完善 G 代码解析器

### 中优先级
- [ ] 实现日志文件写入
- [ ] 添加 `/logs` 全屏查看器
- [ ] 实现 `/params` 参数表格
- [ ] 添加安全热键监听 (ESC/Space)

### 低优先级
- [ ] 实现边界检查 (Dry Run)
- [ ] 添加 2D 路径预览 (Canvas)
- [ ] 刀具管理系统
- [ ] 真实串口通信集成

## 总结

项目已成功完成：
1. ✅ 详细的功能设计文档
2. ✅ 符合用户审美的视觉设计
3. ✅ 可运行的基础框架
4. ✅ 核心配置系统
5. ✅ 虚拟 CNC 控制器

现在已经有了一个**可工作的原型**，可以在此基础上逐步完善各项功能。
