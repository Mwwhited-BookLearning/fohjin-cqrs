# NSwag's TypeScript generator always wraps its output in a "namespace X { ... }" block (there's
# no setting that turns this off - see docs/11-migration-plan.md Phase 6 for what was actually
# tried). That's valid TypeScript, but esbuild - which Vite uses for both its dev server and
# production bundling - doesn't support namespaces containing runtime code (only type-only
# namespaces are erasable), so Fohjin.DDD.WebUI fails to build with the file as NSwag emits it.
# The namespace body here is just a flat sequence of "export class"/"export interface"
# declarations with no nested namespaces, so unwrapping it is a no-op other than removing the
# scope - this drops the opening "namespace X {" line and the matching final "}" line.
param(
    [string]$Path = (Join-Path $PSScriptRoot "..\Fohjin.DDD.WebUI\src\api\generated-client.ts")
)

if (-not (Test-Path $Path)) {
    Write-Host "strip-ts-namespace.ps1: $Path not found, skipping"
    exit 0
}

$lines = Get-Content -Path $Path

$namespaceLineIndex = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match '^namespace ') { $namespaceLineIndex = $i; break }
}

if ($namespaceLineIndex -lt 0) {
    Write-Host "strip-ts-namespace.ps1: no namespace wrapper found in $Path, leaving as-is"
    exit 0
}

$lines = $lines[0..($namespaceLineIndex - 1)] + $lines[($namespaceLineIndex + 1)..($lines.Count - 1)]

for ($i = $lines.Count - 1; $i -ge 0; $i--) {
    if ($lines[$i].Trim() -eq '}') {
        $lines = $lines[0..($i - 1)]
        break
    } elseif ($lines[$i].Trim() -ne '') {
        Write-Host "strip-ts-namespace.ps1: expected a trailing '}' line, found '$($lines[$i])' - leaving file unmodified"
        exit 1
    }
}

Set-Content -Path $Path -Value $lines
Write-Host "strip-ts-namespace.ps1: unwrapped namespace in $Path"
