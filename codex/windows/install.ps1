# 编译 → 装到 %LOCALAPPDATA%\Programs\GPTBootAnimation → 设为开机自启 → 弹出设置窗口
# 不会修改 ChatGPT 的任何文件
$ErrorActionPreference = 'Stop'
$dest = "$env:LOCALAPPDATA\Programs\GPTBootAnimation"
Get-Process GPTBootAnimation -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 500
if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
& "$PSScriptRoot\build.ps1" -Out $dest
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'GPTBootAnimation' -Value "`"$dest\GPTBootAnimation.exe`""
Start-Process "$dest\GPTBootAnimation.exe" -ArgumentList '--settings'
Write-Host ''
Write-Host '🌙 装好了！右下角托盘出现月亮图标，也弹出了设置窗口。'
Write-Host '   下次打开 ChatGPT 就会播放开机动画。想马上看：托盘月亮图标右键 →「播放一次开屏动画」。'
