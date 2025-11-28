@echo off
chcp 65001 >nul
echo.
echo ========================================
echo   KCode REST API 测试脚本
echo ========================================
echo.

echo 1. 健康检查...
curl -s http://localhost:5000/health
echo.
echo.

echo 2. 获取 API 信息...
curl -s http://localhost:5000/
echo.
echo.

echo 3. 执行 G 代码: G0 X50 Y100...
curl -s -X POST http://localhost:5000/api/v1/cnc/execute -H "Content-Type: application/json" -d "{\"text\":\"G0 X50 Y100\"}"
echo.
echo.

echo 4. 获取机器状态...
curl -s http://localhost:5000/api/v1/cnc/status
echo.
echo.

echo 5. 回零命令: G28...
curl -s -X POST http://localhost:5000/api/v1/cnc/execute -H "Content-Type: application/json" -d "{\"text\":\"G28\"}"
echo.
echo.

echo 6. 设置参数 max_velocity=5000...
curl -s -X POST http://localhost:5000/api/v1/cnc/set_param -H "Content-Type: application/json" -d "{\"key\":\"max_velocity\",\"value\":5000}"
echo.
echo.

echo 7. 获取所有参数...
curl -s http://localhost:5000/api/v1/cnc/params
echo.
echo.

echo ========================================
echo   测试完成！
echo ========================================
pause
