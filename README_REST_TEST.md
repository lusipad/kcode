# KCode v2 - REST API 测试快速指南

## 🚀 快速开始

### 1. 启动测试 API 服务

在**第一个终端**中运行：

```bash
cd KcodeTestApi
dotnet run
```

等待看到：
```
===========================================
  KCode Test API Server
  监听地址: http://localhost:5000
  开始时间: 2025/11/28 21:49:10
===========================================
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

### 2. 测试 API 端点

在**第二个终端**中运行：

```bash
# 健康检查
curl http://localhost:5000/health

# 执行 G 代码
curl -X POST http://localhost:5000/api/v1/cnc/execute \
  -H "Content-Type: application/json" \
  -d '{"text":"G0 X10 Y20"}'

# 获取机器状态
curl http://localhost:5000/api/v1/cnc/status

# 回零命令
curl -X POST http://localhost:5000/api/v1/cnc/execute \
  -H "Content-Type: application/json" \
  -d '{"text":"G28"}'

# 设置参数
curl -X POST http://localhost:5000/api/v1/cnc/set_param \
  -H "Content-Type: application/json" \
  -d '{"key":"max_velocity","value":3000}'

# 获取所有参数
curl http://localhost:5000/api/v1/cnc/params
```

### 3. 使用 KCode 客户端 (可选)

```bash
cd kcode
dotnet run -- --test-rest
```

---

## 📋 可用端点

| 方法 | 端点 | 功能 | 示例请求 |
|------|------|------|----------|
| GET | /health | 健康检查 | - |
| GET | / | API 信息 | - |
| POST | /api/v1/cnc/execute | 执行 G 代码 | `{"text":"G0 X10"}` |
| GET | /api/v1/cnc/status | 获取状态 | - |
| POST | /api/v1/cnc/set_param | 设置参数 | `{"key":"max_velocity","value":3000}` |
| GET | /api/v1/cnc/params | 获取参数 | - |
| POST | /api/v1/cnc/emergency_stop | 紧急停止 | - |
| POST | /api/v1/cnc/reset | 复位报警 | - |

---

## 📁 项目结构

```
kcode/
├── KcodeTestApi/              # REST API 测试服务 (新增)
│   ├── Program.cs
│   └── Controllers/
│       └── CncController.cs
├── kcode/                     # KCode 客户端
│   ├── Config/
│   │   ├── config-v2-rest.yaml       # REST 配置
│   │   └── config-v2-rest-test.yaml  # 测试配置 (新增)
│   ├── Core/
│   │   ├── Transport/
│   │   │   ├── ITransport.cs
│   │   │   ├── RestTransport.cs      # REST 传输层 ✅
│   │   │   └── TransportFactory.cs
│   │   └── ...
│   └── TestRestApi.cs         # REST 测试程序 (新增)
└── docs/
    └── rest_api_test_summary.md  # 测试总结 (新增)
```

---

## 🎯 测试场景

### 场景 1: 基本移动
```bash
# 1. 移动到指定位置
curl -X POST http://localhost:5000/api/v1/cnc/execute \
  -H "Content-Type: application/json" \
  -d '{"text":"G0 X100 Y50 Z10"}'

# 2. 查看当前位置
curl http://localhost:5000/api/v1/cnc/status
```

### 场景 2: 回零流程
```bash
# 1. 执行回零
curl -X POST http://localhost:5000/api/v1/cnc/execute \
  -H "Content-Type: application/json" \
  -d '{"text":"G28"}'

# 2. 确认位置归零
curl http://localhost:5000/api/v1/cnc/status
# 应该看到 X=0, Y=0, Z=0
```

### 场景 3: 参数管理
```bash
# 1. 查看当前参数
curl http://localhost:5000/api/v1/cnc/params

# 2. 修改参数
curl -X POST http://localhost:5000/api/v1/cnc/set_param \
  -H "Content-Type: application/json" \
  -d '{"key":"max_velocity","value":5000}'

# 3. 确认修改
curl http://localhost:5000/api/v1/cnc/params
```

---

## 🔧 故障排查

### 端口被占用
```bash
# Windows
netstat -ano | findstr :5000
taskkill /PID <PID> /F

# Linux/Mac
lsof -ti:5000 | xargs kill -9
```

### 重新编译
```bash
cd KcodeTestApi
dotnet build
```

---

## 📚 相关文档

- [v2 架构设计](docs/architecture_v2_zh.md)
- [实施进度](docs/implementation_progress_v2.md)
- [REST API 测试总结](docs/rest_api_test_summary.md)
- [v2 实施总结](docs/v2_implementation_summary.md)

---

## ✨ 特性亮点

- ✅ **完整的 REST API 服务**
- ✅ **模拟 CNC 设备行为**
- ✅ **JSON 请求/响应**
- ✅ **请求日志记录**
- ✅ **CORS 跨域支持**
- ✅ **健康检查端点**
- ✅ **参数管理**
- ✅ **状态实时查询**

---

**创建日期**: 2025-11-28
**版本**: v2.0.0
