# Set GBA_ZIP_URL to a trusted release ZIP HTTPS URL before running.
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$url = $env:GBA_ZIP_URL
if (-not $url -or $url -notmatch "^https://") { throw "Set GBA_ZIP_URL to a trusted release ZIP HTTPS URL." }
$tmp = Join-Path $env:TEMP ('gba-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $tmp | Out-Null
try {
  Write-Host 'Downloading GPT Boot Animation (~35 MB)...'
  Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile "$tmp\pkg.zip"
  Expand-Archive -Path "$tmp\pkg.zip" -DestinationPath "$tmp\pkg" -Force
  & powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$tmp\pkg\GPTBootAnimation-Windows\install.ps1"
} finally {
  Remove-Item $tmp -Recurse -Force -ErrorAction SilentlyContinue
}
