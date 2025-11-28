# 命令历史记录功能

**实现日期**: 2025-11-29
**状态**: ✅ 已完成

---

## 📋 功能概述

完整的命令历史记录管理系统，支持导航、持久化和搜索。

---

## ✨ 核心特性

### 1. 🔼🔽 **上下箭头导航**
- **↑ 向上箭头**：浏览更早的命令
- **↓ 向下箭头**：浏览更新的命令
- 自动记忆导航位置
- 到达末尾自动清空输入

### 2. 💾 **自动持久化**
- 历史记录自动保存到 `.kcode_history` 文件
- 启动时自动加载历史记录
- JSON 格式存储，易于编辑
- 静默失败，不影响程序运行

### 3. 🔍 **历史搜索**（API 已实现）
- 支持关键词搜索
- 大小写不敏感
- 返回最近 10 条匹配结果

### 4. 🧠 **智能管理**
- 避免连续重复命令
- 最多保存 1000 条历史
- 自动清理旧记录
- 获取统计信息

---

## 🛠️ 技术实现

### 核心类：CommandHistory

**文件位置**: `kcode/Core/CommandHistory.cs`

**主要方法**:

```csharp
public class CommandHistory
{
    // 添加命令
    public void Add(string command)

    // 向上导航（获取更早的命令）
    public string? NavigateUp()

    // 向下导航（获取更新的命令）
    public string? NavigateDown()

    // 重置导航索引
    public void ResetNavigation()

    // 搜索历史记录
    public List<string> Search(string query)

    // 获取所有历史
    public IReadOnlyList<string> GetAll()

    // 清空历史
    public void Clear()

    // 获取统计信息
    public (int Total, int Unique) GetStatistics()
}
```

---

## 🎮 使用方法

### 基本导航

1. **查看上一条命令**
   ```
   按 ↑ 键
   ```

2. **查看下一条命令**
   ```
   按 ↓ 键
   ```

3. **返回当前输入**
   ```
   按 ↓ 键直到输入框清空
   ```

4. **修改历史命令**
   ```
   1. 按 ↑ 或 ↓ 选择历史命令
   2. 修改命令内容
   3. 按 Enter 执行
   ```

### 导航行为

```
历史记录: [cmd1, cmd2, cmd3, cmd4]
                                    ↑ 当前位置（最新）

按 ↑ 键 → cmd4
按 ↑ 键 → cmd3
按 ↑ 键 → cmd2
按 ↑ 键 → cmd1
按 ↑ 键 → cmd1 (到达顶部，不再移动)

按 ↓ 键 → cmd2
按 ↓ 键 → cmd3
按 ↓ 键 → cmd4
按 ↓ 键 → (空输入，返回当前)
```

---

## 💾 持久化机制

### 历史文件

**文件名**: `.kcode_history`
**格式**: JSON
**位置**: 程序运行目录

**示例内容**:
```json
[
  "help",
  "G0 X10 Y20",
  "status",
  "G28",
  "exit"
]
```

### 自动保存时机

- ✅ 每次执行命令后
- ✅ 程序退出时
- ✅ 手动清空历史时

### 自动加载

- ✅ 程序启动时自动加载
- ✅ 静默处理加载失败
- ✅ 保留现有历史记录

---

## 🔧 配置选项

### 自定义历史文件路径

```csharp
// 在 ReplEngineV2.cs 中
_history = new CommandHistory("custom_history.json", 1000);
```

### 自定义最大历史数量

```csharp
// 默认 1000 条，可修改为其他值
_history = new CommandHistory(".kcode_history", 500);
```

---

## 📊 高级功能（API）

### 搜索历史

```csharp
var results = _history.Search("G0");
// 返回所有包含 "G0" 的命令
```

### 获取最近命令

```csharp
var recent = _history.GetRecent(10);
// 获取最近 10 条命令
```

### 获取统计信息

```csharp
var (total, unique) = _history.GetStatistics();
Console.WriteLine($"总计: {total}, 唯一: {unique}");
```

### 清空历史

```csharp
_history.Clear();
// 清空所有历史记录并保存
```

---

## 🎯 用户体验

### 智能去重

```
输入: help
输入: help   ← 不会保存（连续重复）
输入: status
输入: help   ← 会保存（不是连续重复）
```

### 导航重置

- 输入任何字符 → 重置导航
- 按 Backspace → 重置导航
- 按 Enter → 重置导航

这确保了导航行为符合用户预期。

---

## 🚀 性能特点

- ⚡ **快速导航**：O(1) 时间复杂度
- 💾 **轻量存储**：JSON 格式，易于压缩
- 🔒 **线程安全**：单线程访问，无需锁
- 📉 **内存友好**：自动限制历史数量

---

## 🔜 未来增强（可选）

- [ ] Ctrl+R 反向搜索（交互式）
- [ ] 历史命令去重选项
- [ ] 按时间戳过滤
- [ ] 导出/导入历史
- [ ] 历史命令统计分析

---

## 📝 代码示例

### 完整集成示例

```csharp
// 初始化
var history = new CommandHistory(".kcode_history", 1000);

// 添加命令
history.Add("G0 X10 Y20");
history.Add("status");

// 导航
var prev = history.NavigateUp();    // "status"
var prev2 = history.NavigateUp();   // "G0 X10 Y20"
var next = history.NavigateDown();  // "status"

// 搜索
var results = history.Search("G0"); // ["G0 X10 Y20"]

// 统计
var (total, unique) = history.GetStatistics();
Console.WriteLine($"总计: {total}, 唯一: {unique}");
```

---

## ✅ 测试清单

- [x] 上箭头导航到上一条命令
- [x] 下箭头导航到下一条命令
- [x] 到达顶部时不再向上
- [x] 到达底部时清空输入
- [x] 输入字符后重置导航
- [x] 命令自动保存到文件
- [x] 启动时自动加载历史
- [x] 避免连续重复命令
- [x] 自动限制历史数量
- [x] 搜索功能正常工作

---

**实现完成！** 🎉

命令历史记录功能已全面集成到 KCode v2，显著提升了用户体验！
