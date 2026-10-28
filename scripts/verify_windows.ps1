param([Parameter(Mandatory=$true)][ValidateSet('claude','codex')][string]$Component,
      [Parameter(Mandatory=$true)][string]$Directory)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$catalog = Get-Content (Join-Path $root 'components.json') -Raw | ConvertFrom-Json
$versions = Get-Content (Join-Path $root 'versions.json') -Raw | ConvertFrom-Json
$name = $catalog.$Component.name
$info = (Get-Item -LiteralPath (Join-Path $Directory "$name.exe")).VersionInfo
if ($info.FileVersion -ne "$($versions.$Component).0") { throw 'Binary file version mismatch' }
Write-Host "Binary version OK: $Component"
