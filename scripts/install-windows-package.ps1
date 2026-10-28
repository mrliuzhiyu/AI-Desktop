# Release-package installer; APP_NAME is replaced by the packager.
$ErrorActionPreference = 'Stop'
$appName = '__APP_NAME__'
$dest = Join-Path $env:LOCALAPPDATA "Programs/$appName"
foreach ($file in @("$appName.exe", 'assets/intro.mp4', 'assets/loop.mp4', 'uninstall.ps1')) {
    if (-not (Test-Path (Join-Path $PSScriptRoot $file))) { throw "Incomplete release: $file" }
}
Get-Process -Name $appName -ErrorAction SilentlyContinue | Stop-Process -Force
New-Item -ItemType Directory -Force -Path $dest | Out-Null
foreach ($file in @("$appName.exe", 'uninstall.ps1', 'LICENSE', 'ASSETS-LICENSE.md', 'CREDITS.md')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $file) -Destination $dest -Force
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'assets') -Destination $dest -Recurse -Force
$exe = Join-Path $dest "$appName.exe"
Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name $appName -Value "`"$exe`""
Start-Process -FilePath $exe -ArgumentList '--settings'
Write-Host 'Installed successfully.'
