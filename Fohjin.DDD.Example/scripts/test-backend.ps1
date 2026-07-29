<#
.SYNOPSIS
    Runs the fast .NET unit/integration suite (excludes the FlaUI/Playwright WinForms UI suite -
    see CLAUDE.md's Testing section for why that one's separate) with code coverage, and renders
    an HTML report.

.PARAMETER OpenReport
    Open the generated HTML coverage report in the default browser when done.

.NOTES
    Needs the dev SQL Server instance reachable at port 14330 (CLAUDE.md's dev conventions).
#>
param(
    [switch]$OpenReport
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$resultsDir = Join-Path $root 'TestResults'
$coverageReportDir = Join-Path $resultsDir 'CoverageReport'

if (Test-Path $resultsDir) { Remove-Item $resultsDir -Recurse -Force }

Write-Host "==> dotnet test (with coverage)" -ForegroundColor Cyan
dotnet test "$root\Fohjin.DDD.sln" `
    --filter "FullyQualifiedName!~BankApplication.UI" `
    --collect:"XPlat Code Coverage" `
    --results-directory $resultsDir
if ($LASTEXITCODE -ne 0) { throw "dotnet test failed" }

Write-Host "==> Generating HTML coverage report" -ForegroundColor Cyan
dotnet tool restore | Out-Null
$coverageFiles = Get-ChildItem -Path $resultsDir -Filter 'coverage.cobertura.xml' -Recurse
if (-not $coverageFiles) { throw "No coverage.cobertura.xml files found under $resultsDir" }
$reportArg = ($coverageFiles.FullName -join ';')

dotnet tool run reportgenerator `
    "-reports:$reportArg" `
    "-targetdir:$coverageReportDir" `
    "-reporttypes:Html;TextSummary"
if ($LASTEXITCODE -ne 0) { throw "reportgenerator failed" }

Get-Content (Join-Path $coverageReportDir 'Summary.txt')

Write-Host "Coverage report: $coverageReportDir\index.html" -ForegroundColor Green
if ($OpenReport) {
    Start-Process (Join-Path $coverageReportDir 'index.html')
}
