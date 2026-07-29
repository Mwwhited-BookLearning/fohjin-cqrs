<#
.SYNOPSIS
    Local "CI" pass: build, then both test suites with coverage. Fails fast on the first error.

.PARAMETER OpenReports
    Open both generated HTML coverage reports in the default browser when done.
#>
param(
    [switch]$OpenReports
)

$ErrorActionPreference = 'Stop'
$scriptDir = $PSScriptRoot

& "$scriptDir\build.ps1"
& "$scriptDir\test-backend.ps1" -OpenReport:$OpenReports
& "$scriptDir\test-frontend.ps1" -OpenReport:$OpenReports

Write-Host "All good." -ForegroundColor Green
