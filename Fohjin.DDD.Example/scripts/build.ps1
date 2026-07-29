<#
.SYNOPSIS
    Builds the whole solution: the .NET backend and the Vue frontend.
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

# See CLAUDE.md's "Node.js on this machine" note: a broken npm.bat/node.bat earlier on PATH
# would otherwise shadow the real install.
$realNodeBin = 'C:\Program Files\nodejs'
if ((Test-Path $realNodeBin) -and ($env:Path -notlike "*$realNodeBin*")) {
    $env:Path = "$realNodeBin;$env:Path"
}

Write-Host "==> dotnet build" -ForegroundColor Cyan
dotnet build "$root\Fohjin.DDD.sln"
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }

Write-Host "==> npm run build (Fohjin.DDD.WebUI)" -ForegroundColor Cyan
Push-Location "$root\Fohjin.DDD.WebUI"
try {
    npm run build
    if ($LASTEXITCODE -ne 0) { throw "npm run build failed" }
} finally {
    Pop-Location
}

Write-Host "Build succeeded." -ForegroundColor Green
