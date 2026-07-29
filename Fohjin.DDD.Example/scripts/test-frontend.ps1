<#
.SYNOPSIS
    Runs the Vue app's Vitest suite with coverage (src/events/eventBus.test.ts,
    src/events/refreshRules.test.ts - see CLAUDE.md's Testing section).

.PARAMETER OpenReport
    Open the generated HTML coverage report in the default browser when done.
#>
param(
    [switch]$OpenReport
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$webUiDir = Join-Path $root 'Fohjin.DDD.WebUI'

# See CLAUDE.md's "Node.js on this machine" note: a broken npm.bat/node.bat earlier on PATH
# would otherwise shadow the real install.
$realNodeBin = 'C:\Program Files\nodejs'
if ((Test-Path $realNodeBin) -and ($env:Path -notlike "*$realNodeBin*")) {
    $env:Path = "$realNodeBin;$env:Path"
}

Write-Host "==> npm run test:coverage (Fohjin.DDD.WebUI)" -ForegroundColor Cyan
Push-Location $webUiDir
try {
    npm run test:coverage
    if ($LASTEXITCODE -ne 0) { throw "npm run test:coverage failed" }
} finally {
    Pop-Location
}

$reportPath = Join-Path $webUiDir 'coverage\index.html'
Write-Host "Coverage report: $reportPath" -ForegroundColor Green
if ($OpenReport -and (Test-Path $reportPath)) {
    Start-Process $reportPath
}
