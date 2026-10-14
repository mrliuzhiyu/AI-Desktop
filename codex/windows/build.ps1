# 用 Windows 自带的 .NET Framework 编译器把 src\GPTBootAnimation.cs 编成 exe，不需要装任何 SDK
# 用法：powershell -ExecutionPolicy Bypass -File build.ps1 [-Out 输出目录]
param([string]$Out = "$PSScriptRoot\build")
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
& (Join-Path $repoRoot 'scripts/doctor.ps1') -Quiet
python (Join-Path $repoRoot 'scripts/prepare_media.py') codex (Join-Path $Out 'assets')
if ($LASTEXITCODE -ne 0) { throw 'Media preparation failed' }
$fw = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
if (-not (Test-Path "$fw\csc.exe")) { $fw = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319" }
$csc = "$fw\csc.exe"
if (-not (Test-Path $csc)) { throw "找不到系统自带的 C# 编译器（$csc）。需要 Windows 10/11。" }
$wpf = "$fw\WPF"
New-Item -ItemType Directory -Force -Path $Out | Out-Null
$refs = @("$wpf\PresentationFramework.dll", "$wpf\PresentationCore.dll", "$wpf\WindowsBase.dll",
          "$fw\System.Xaml.dll", "$fw\System.Windows.Forms.dll", "$fw\System.Drawing.dll",
          "$fw\System.Web.Extensions.dll", "$fw\System.Core.dll") | ForEach-Object { "/r:$_" }
$cscArgs = @('/nologo', '/target:winexe', '/optimize+', '/codepage:65001', '/platform:anycpu', "/out:$Out\GPTBootAnimation.exe") + $refs + @("$PSScriptRoot\src\GPTBootAnimation.cs")
& $csc @cscArgs
if ($LASTEXITCODE -ne 0) { throw "编译失败" }
New-Item -ItemType Directory -Force -Path "$Out\assets" | Out-Null
Copy-Item "$PSScriptRoot/assets/*.ttf" "$Out/assets/" -Force
Copy-Item "$PSScriptRoot/assets/*OFL.txt" "$Out/assets/" -Force
foreach ($asset in @('intro.mp4', 'loop.mp4', 'LibreBodoni-Italic.ttf', 'LibreBodoni-OFL.txt')) {
    if (-not (Test-Path (Join-Path $Out "assets/$asset"))) { throw "Missing asset: $asset" }
}
Copy-Item "$PSScriptRoot\uninstall.ps1" "$Out\" -Force
Write-Host "编译完成：$Out\GPTBootAnimation.exe"
