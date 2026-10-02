# Set CBA_ZIP_URL to a trusted release ZIP HTTPS URL before running.
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$url = $env:CBA_ZIP_URL
if (-not $url -or $url -notmatch "^https://") { throw "Set CBA_ZIP_URL to a trusted release ZIP HTTPS URL." }
$tmp = Join-Path $env:TEMP ('cba-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tmp | Out-Null
try {
  Write-Host 'Downloading Claude Boot Animation (~37 MB)...'
  Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile "$tmp\pkg.zip"
  Expand-Archive -Path "$tmp\pkg.zip" -DestinationPath "$tmp\pkg" -Force
  & powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$tmp\pkg\ClaudeBootAnimation-Windows\install.ps1"
} finally {
  Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}
