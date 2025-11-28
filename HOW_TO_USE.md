# KCode 使用指南

## 🚀 三种使用方式

### 方式 1: Virtual 模式（推荐入门）

**完全在内存中模拟，不需要真实服务**

```bash
cd kcode
dotnet run
```

这会启动一个交互式 REPL，你可以输入命令：
- `help` - 查看帮助
- `G0 X10 Y20` - 执行 G 代码
- `/set max_velocity 3000` - 设置参数
- `home` - 回零
- `status` - 查看状态
- `exit` - 退出

**配置文件**: `Config/config-virtual.yaml`

---

### 方式 2: REST API 模式（连接真实服务）

**第一步：启动 API 测试服务**（在第一个终端）

```bash
cd KcodeTestApi
dotnet run
```

等待看到：
```
===========================================
  KCode Test API Server
  监听地址: http://localhost:5000
===========================================
```

**第二步：启动 KCode 客户端**（在第二个终端）

然后在**第二个终端**中运行客户端（使用测试 REST 配置）：
```bash
cd kcode
dotnet run -- --config Config/config-rest-test.yaml
```

**配置文件**: `Config/config-v2-rest-test.yaml`

---

### 方式 3: 运行自动化测试

**测试 Virtual 模式:**
```bash
cd kcode
dotnet run -- --test-virtual
```

这会自动测试：
- ✓ 配置加载
- ✓ 命令解析
- ✓ 命令执行
- ✓ 模板渲染
- ✓ 流式数据

**测试 REST 模式:**（需要先启动 API 服务）
```bash
# 终端 1
cd KcodeTestApi
dotnet run

# 终端 2（当 API 服务运行后）
cd kcode
dotnet run -- --test-rest
```

---

## 📝 Slash 命令面板

- 输入 / 即可打开命令列表（系统命令 / API 命令 / 宏都会在同一列表中展示）
- 使用方向键逐项移动；当列表超过 8 条时会自动滚动窗口，翻页后仍按单步移动
- Tab / Enter 可以把当前选中命令写回输入框（末尾自动补一个空格）
- 再按一次 Enter 就能执行该命令，Backspace 删除 / 即可退出面板

---

## 📝 可用命令

### 系统命令
- `help` / `?` / `h` - 显示帮助
- `exit` / `quit` / `q` - 退出
- `status` / `st` - 显示状态
- `clear` / `cls` - 清屏

### G 代码命令
- `G0 X10 Y20` - 快速移动
- `G1 X10 Y20 F1000` - 直线插补
- `G28` - 回零
- `M3 S1000` - 主轴正转

### 参数设置
- `/set max_velocity 3000` - 设置参数
- `params` / `parameters` - 查看所有参数

### 宏命令
- `home` / `回零` - 所有轴回零
- `zero_work` / `清零` - 设置工件零点

---

## 🎯 快速演示

**Virtual 模式快速体验:**

```bash
# 1. 进入 kcode 目录
cd kcode

# 2. 启动
dotnet run

# 3. 尝试以下命令
> help
> G0 X10 Y20
> home
> status
> exit
```

---

## 🔧 配置文件说明

### config-virtual.yaml
- 使用内存模拟
- 不需要真实服务
- 适合测试和开发

### config-rest-test.yaml
- 连接到 localhost:5000
- 需要 KcodeTestApi 服务运行
- 真实的 HTTP 通信

### config-rest.yaml
- 生产环境配置模板
- 可修改连接到真实设备

---

## 📊 架构对比

| 特性 | Virtual 模式 | REST 模式 |
|------|-------------|-----------|
| 启动速度 | 快 | 中等 |
| 网络需求 | 无 | 需要 |
| 真实性 | 模拟 | 真实通信 |
| 适用场景 | 开发测试 | 生产环境 |

---

## 🐛 常见问题

### Q: 运行时提示"需要在交互式终端运行"
**A:** 这是正常的，ReplEngine 需要真实的交互式终端（如 PowerShell、Windows Terminal）才能正常运行。在 bash/cmd 中可能无法正常工作。

### Q: 文件被锁定无法编译
**A:** 先关闭所有运行中的 kcode.exe 进程：
```bash
taskkill /F /IM kcode.exe
```

### Q: 找不到配置文件
**A:** 现在会自动探测默认配置：在仓库根目录执行 dotnet run --project kcode/kcode.csproj 或进入 kcode 目录直接 dotnet run 都可以。仍需自定义时，可通过 --config <路径> 指定 YAML。

### Q: API 连接失败
**A:**
1. 确认 KcodeTestApi 服务正在运行
2. 检查端口 5000 是否被占用
3. 查看防火墙设置

---

## 📚 相关文档

- [架构设计](docs/architecture_v2_zh.md)
- [实施进度](docs/implementation_progress_v2.md)
- [REST API 测试](docs/rest_api_test_summary.md)
- [REST 快速指南](README_REST_TEST.md)

---

**创建日期**: 2025-11-28
**版本**: v2.0.0
