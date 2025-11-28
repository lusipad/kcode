# kcode 开发环境设置脚本
# 运行此脚本来配置 dotnet 环境变量

Write-Host "正在配置 kcode 开发环境..." -ForegroundColor Cyan

# 添加 dotnet 到当前会话 PATH
$env:PATH += ";C:\Program Files\dotnet"

Write-Host "✅ dotnet 已添加到 PATH" -ForegroundColor Green
Write-Host "dotnet 版本: $(dotnet --version)" -ForegroundColor Yellow

# 提示永久配置方法
Write-Host "`n💡 提示: 如果想永久添加 dotnet 到 PATH，请运行：" -ForegroundColor Cyan
Write-Host '[System.Environment]::SetEnvironmentVariable("Path", $env:Path + ";C:\Program Files\dotnet", [System.EnvironmentVariableTarget]::User)' -ForegroundColor Gray
