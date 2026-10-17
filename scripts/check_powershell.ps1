$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$files = @(Get-ChildItem (Join-Path $root 'scripts') -Filter *.ps1)
foreach ($component in @('claude', 'codex')) {
    $files += Get-ChildItem (Join-Path $root "$component/windows") -Filter *.ps1
}
$failed = $false
foreach ($file in $files) {
    $tokens = $null; $parseErrors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($file.FullName, [ref]$tokens, [ref]$parseErrors) | Out-Null
    if ($parseErrors.Count) { $parseErrors | ForEach-Object { Write-Host $_ }; $failed = $true }
}
if ($failed) { throw 'PowerShell syntax failed' }
Write-Host "PowerShell syntax OK ($($files.Count) files)"
