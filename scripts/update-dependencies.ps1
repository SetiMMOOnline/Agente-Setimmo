# Nome: update-dependencies.ps1
# Versao: 1.0
# Objetivo: Atualizar dependencias NuGet diretas conforme floating versions e validar o agente.
# Autor/IA: Codex
# Data: 2026-06-11
# Modo padrao: dry-run/read-only
# Entradas: -Apply para atualizar packages.lock.json; -RunReleaseCheck para executar release-check completo.
# Saidas: relatorio no console; packages.lock.json atualizado somente com -Apply.
# Arquivos afetados: packages.lock.json dos projetos com RestorePackagesWithLockFile.
# Riscos: atualizacoes de pacote podem alterar comportamento de testes/build.
# Como executar: powershell -ExecutionPolicy Bypass -File .\scripts\update-dependencies.ps1 [-Apply] [-RunReleaseCheck]
# Como validar: conferir build, testes, package checks e release-check quando solicitado.
# Como reverter: restaurar o diff de packages.lock.json e csproj via controle de versao ou backup.

[CmdletBinding()]
param(
    [switch]$Apply,
    [switch]$RunReleaseCheck
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root "RagnaForge.Agent.slnx"
$releaseCheck = Join-Path $root "scripts\release-check.ps1"

function Invoke-Step([string]$Name, [scriptblock]$Action) {
    Write-Host ""
    Write-Host "=== $Name ===" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -ne 0) {
        throw "Step failed: $Name"
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

Write-Host "Agente Setimmo dependency update"
Write-Host "Root: $root"
Write-Host "Mode: $(if ($Apply) { 'apply' } else { 'dry-run' })"

if ($Apply) {
    Invoke-Step "dotnet restore force-evaluate" {
        dotnet restore $solution --force-evaluate
    }
}
else {
    Invoke-Step "dotnet restore locked" {
        dotnet restore $solution --locked-mode
    }
}

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

Invoke-Step "dotnet build" {
    dotnet build $solution
}

Invoke-Step "dotnet test" {
    dotnet test $solution --no-build
}

if ($RunReleaseCheck) {
    Invoke-Step "release-check" {
        powershell -ExecutionPolicy Bypass -File $releaseCheck
    }
}

Write-Host ""
Write-Host "Dependency update validation complete." -ForegroundColor Green
