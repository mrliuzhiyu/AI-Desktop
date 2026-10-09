param([switch]$Quiet)
$ErrorActionPreference = 'Stop'
$fw = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
if (-not (Test-Path (Join-Path $fw 'csc.exe'))) { $fw = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319' }
foreach ($dll in @('csc.exe', 'WPF/PresentationFramework.dll', 'WPF/PresentationCore.dll')) {
    if (-not (Test-Path (Join-Path $fw $dll))) { throw "Missing .NET Framework tool: $dll" }
}
foreach ($tool in @('python', 'ffmpeg', 'ffprobe')) { Get-Command $tool -ErrorAction Stop | Out-Null }
python -c "import sys; sys.exit(0 if sys.version_info >= (3,11) else 1)"
if ($LASTEXITCODE -ne 0) { throw 'Python 3.11+ required' }
if (-not $Quiet) { Write-Host 'Windows build prerequisites OK' }
