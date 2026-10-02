# 编译 → 装到 %LOCALAPPDATA%\Programs\ClaudeBootAnimation → 设为开机自启 → 弹出设置窗口
# 不会修改 Claude 的任何文件
$ErrorActionPreference = 'Stop'
$dest = "$env:LOCALAPPDATA\Programs\ClaudeBootAnimation"
Get-Process ClaudeBootAnimation -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500
if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
& "$PSScriptRoot\build.ps1" -Out $dest
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'ClaudeBootAnimation' -Value "`"$dest\ClaudeBootAnimation.exe`""
Start-Process "$dest\ClaudeBootAnimation.exe" -ArgumentList '--settings'
Write-Host ''
Write-Host '✨ 装好了！右下角托盘出现 ✨ 图标，也弹出了设置窗口。'
Write-Host '   下次打开 Claude 就会播放开机动画。想马上看：托盘 ✨ 右键 →「播放一次开屏动画」。'
