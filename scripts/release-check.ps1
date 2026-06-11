# Agente Setimmo release check.
# Builds, tests, publishes, and verifies installed-safe CLI behavior.

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root "RagnaForge.Agent.slnx"
$install = Join-Path $root "scripts\install.ps1"
$exe = Join-Path $root "dist\agente-setimmo\agente-setimmo.exe"

function Invoke-Step([string]$Name, [scriptblock]$Action) {
    Write-Host ""
    Write-Host "=== $Name ===" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -ne 0) {
        throw "Step failed: $Name"
    }
}

function Assert-JsonContains([string]$Name, [string]$Output, [string]$Pattern) {
    if ($Output -notmatch $Pattern) {
        Write-Host $Output
        throw "$Name did not match expected pattern: $Pattern"
    }
}

function Assert-PackageJsonClean([string]$Name, [string]$Output, [string[]]$ProblemMarkers) {
    foreach ($marker in $ProblemMarkers) {
        if ($Output -match $marker) {
            Write-Host $Output
            throw "$Name reported package maintenance issues matching: $marker"
        }
    }
}

Invoke-Step "dotnet clean" { dotnet clean $solution }
Invoke-Step "dotnet restore locked" { dotnet restore $solution --locked-mode }
Invoke-Step "dotnet package outdated check" {
    $out = dotnet list $solution package --outdated --format json | Out-String
    Write-Host $out
    Assert-PackageJsonClean "package-outdated" $out @('"latestVersion"\s*:')
}
Invoke-Step "dotnet package deprecated check" {
    $out = dotnet list $solution package --deprecated --format json | Out-String
    Write-Host $out
    Assert-PackageJsonClean "package-deprecated" $out @('"deprecationReasons"\s*:', '"alternativePackage"\s*:')
}
Invoke-Step "dotnet package vulnerable check" {
    $out = dotnet list $solution package --vulnerable --include-transitive --format json | Out-String
    Write-Host $out
    Assert-PackageJsonClean "package-vulnerable" $out @('"vulnerabilities"\s*:')
}
Invoke-Step "dotnet build" { dotnet build $solution }
Invoke-Step "dotnet test" { dotnet test $solution }
Invoke-Step "dotnet publish/install" { & $install -AgentRoot $root }

if (-not (Test-Path $exe)) {
    throw "Installed executable not found: $exe"
}

$env:RAGNAFORGE_AGENT_ROOT = $root
$env:Path = "$(Split-Path -Parent $exe);$env:Path"

Invoke-Step "ragnaforge --version" {
    $out = & ragnaforge --version | Out-String
    Write-Host $out
    Assert-JsonContains "version" $out '"version"\s*:'
}

Invoke-Step "ragnaforge status --json" {
    $out = & ragnaforge status --json | Out-String
    Write-Host $out
    Assert-JsonContains "status" $out '"ok"\s*:\s*true'
}

Invoke-Step "ragnaforge doctor --json" {
    $out = & ragnaforge doctor --json | Out-String
    Write-Host $out
    Assert-JsonContains "doctor" $out '"ok"\s*:\s*true'
}

Invoke-Step "ragnaforge export api-readiness --json" {
    $out = & ragnaforge export api-readiness --json | Out-String
    Write-Host $out
    Assert-JsonContains "api-readiness" $out '"ok"\s*:\s*true'
    Assert-JsonContains "api-readiness-mode" $out '"mode"\s*:\s*"export-api-readiness"'
}

Invoke-Step "ragnaforge field test run --json" {
    $out = & ragnaforge field test run --json | Out-String
    Write-Host $out
    Assert-JsonContains "field-test" $out '"ok"\s*:\s*true'
    Assert-JsonContains "field-test-failed" $out '"failed"\s*:\s*0'
}

$smokeTarget = Join-Path $root "temp\release-smoke.ps1"
$script:smokeOperationId = $null

Invoke-Step "ragnaforge dry-run implement smoke" {
    $out = & ragnaforge dry-run implement --target temp/release-smoke.ps1 --workspace agent --language powershell --instruction "Create a release smoke script that proves install-time implementation works." --json | Out-String
    Write-Host $out
    Assert-JsonContains "dry-run-implement" $out '"ok"\s*:\s*true'
    $json = $out | ConvertFrom-Json
    $script:smokeOperationId = $json.operationId
    if ([string]::IsNullOrWhiteSpace($script:smokeOperationId)) {
        throw "Dry-run implement smoke did not return an operationId."
    }
}

Invoke-Step "ragnaforge apply implement smoke" {
    $out = & ragnaforge apply implement --operation $script:smokeOperationId --confirm --json | Out-String
    Write-Host $out
    Assert-JsonContains "apply-implement-smoke" $out '"ok"\s*:\s*true'
    if (-not (Test-Path $smokeTarget)) {
        throw "Implementation smoke apply did not create: $smokeTarget"
    }
}

Invoke-Step "ragnaforge rollback smoke" {
    $out = & ragnaforge rollback --id $script:smokeOperationId --confirm --json | Out-String
    Write-Host $out
    Assert-JsonContains "rollback-smoke" $out '"ok"\s*:\s*true'
    if (Test-Path $smokeTarget) {
        throw "Implementation smoke rollback did not remove: $smokeTarget"
    }
}

Write-Host ""
Write-Host "=== ragnaforge apply --json ===" -ForegroundColor Cyan
$applyOut = & ragnaforge apply --json | Out-String
Write-Host $applyOut
Assert-JsonContains "apply" $applyOut '"ok"\s*:\s*false'
Assert-JsonContains "apply-next" $applyOut '"nextRequiredAction"\s*:\s*"run_dry_run_implement"'

Write-Host ""
Write-Host "Release check complete." -ForegroundColor Green
