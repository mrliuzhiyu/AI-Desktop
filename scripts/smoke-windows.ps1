param([Parameter(Mandatory=$true)][ValidateSet('claude','codex')][string]$Component)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$catalog = Get-Content (Join-Path $root 'components.json') -Raw | ConvertFrom-Json
$name = $catalog.$Component.name
$exe = Join-Path $root "$Component/windows/build/$name.exe"
$preview = Join-Path $root "$Component/docs/preview.jpg"
$out = Join-Path $root ("artifacts/$Component/subtitles/" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $out | Out-Null
foreach ($index in @(1,2)) {
    $target = Join-Path $out "subtitle-$index.png"
    $process = Start-Process -FilePath $exe -ArgumentList "--render-subtitle `"$preview`" `"$target`" $index" -PassThru -WindowStyle Hidden
    if (-not $process.WaitForExit(30000)) { $process.Kill(); throw 'Subtitle renderer timed out' }
    if ($process.ExitCode -ne 0) { throw 'Subtitle renderer failed' }
    if (-not (Test-Path -LiteralPath $target)) { throw 'Subtitle image missing' }
    Add-Type -AssemblyName System.Drawing
    $img = [System.Drawing.Image]::FromFile($target)
    try { if ($img.Width -lt 100 -or $img.Height -lt 100) { throw 'Invalid subtitle image' } } finally { $img.Dispose() }
}
Write-Host "Subtitle render smoke passed: $Component"
