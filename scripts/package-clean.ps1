<#
Nome: package-clean.ps1
Versao: 1.0
Objetivo: gerar ZIP limpo do Agente Setimmo por allowlist.
Autor/IA: Codex
Data: 2026-06-02
Modo padrao: package seguro
Entradas: -OutputZip
Saidas: ZIP limpo auditado
Arquivos afetados: staging temporario e ZIP de saida
Riscos: baixo; remove apenas staging temporario criado pelo proprio script e sobrescreve o ZIP informado
Como executar: powershell -ExecutionPolicy Bypass -File .\scripts\package-clean.ps1
Como validar: audit-release.ps1 roda automaticamente
Como reverter: apague o ZIP gerado
#>
param(
    [string]$OutputZip = (Join-Path ([Environment]::GetFolderPath("Desktop")) "Agente_Setimmo_release.zip")
)

$ErrorActionPreference = "Stop"

function Get-Root {
    $root = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot ".."))
    if (-not (Test-Path -LiteralPath (Join-Path $root "RagnaForge.Agent.slnx"))) {
        throw "This script must run from the Agente Setimmo repository."
    }
    return $root.TrimEnd('\', '/')
}

function New-SafeTempDir {
    $name = "agente_setimmo_package_" + [System.Guid]::NewGuid().ToString("N")
    $dir = Join-Path ([System.IO.Path]::GetTempPath()) $name
    New-Item -ItemType Directory -Path $dir | Out-Null
    return [System.IO.Path]::GetFullPath($dir)
}

function Remove-SafeTempDir([string]$Path) {
    $full = [System.IO.Path]::GetFullPath($Path)
    $temp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    if (-not $full.StartsWith($temp, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to remove non-temp directory: $full"
    }
    if (Test-Path -LiteralPath $full) {
        Remove-Item -LiteralPath $full -Recurse -Force
    }
}

function Copy-AllowlistedPath([string]$Root, [string]$Staging, [string]$RelativePath) {
    $normalizedRoot = [System.IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $source = Join-Path $Root $RelativePath
    if (-not (Test-Path -LiteralPath $source)) {
        return
    }

    $target = Join-Path $Staging $RelativePath
    if ((Get-Item -LiteralPath $source).PSIsContainer) {
        Get-ChildItem -LiteralPath $source -Recurse -Force | ForEach-Object {
            if ($_.PSIsContainer) { return }
            $fileFull = [System.IO.Path]::GetFullPath($_.FullName)
            $sourceRelative = $fileFull.Substring($normalizedRoot.Length + 1)
            if (Test-IsForbiddenPackagePath $sourceRelative) { return }
            $fileTarget = Join-Path $Staging $sourceRelative
            New-Item -ItemType Directory -Path (Split-Path -Parent $fileTarget) -Force | Out-Null
            Copy-Item -LiteralPath $_.FullName -Destination $fileTarget -Force
        }
    }
    else {
        if (Test-IsForbiddenPackagePath $RelativePath) { return }
        $targetParent = Split-Path -Parent $target
        New-Item -ItemType Directory -Path $targetParent -Force | Out-Null
        Copy-Item -LiteralPath $source -Destination $target -Force
    }
}

function Test-IsForbiddenPackagePath([string]$RelativePath) {
    $normalized = $RelativePath.Replace('\', '/')
    return (
        $normalized -match '(^|/)(bin|obj|TestResults|node_modules|dist|cache|logs|tmp|temp)(/|$)' -or
        $normalized -match '\.(trx|coverage|tsbuildinfo|grf|gpf|thor|spr|act|bmp|tga|rsw|gnd|gat|rsm|pal|zip|rar)$' -or
        $normalized -match '(^|/)\.env($|\.)' -or
        $normalized -match '(^|/)repositories\.local\.json$' -or
        $normalized -match '(^|/)paths\.json$'
    )
}

$root = Get-Root
$output = [System.IO.Path]::GetFullPath($OutputZip)
$staging = New-SafeTempDir

$allowlist = @(
    "src",
    "tests",
    "docs",
    "knowledge",
    "context-packs",
    "inputs\dry-run",
    "scripts",
    "config\paths.example.json",
    "config\safety.json",
    "config\ragnaforge.agent.json",
    "config\scope-policies.json",
    "config\production-policy.json",
    "config\language-policies.json",
    "config\grf-policy.json",
    ".github",
    ".gitignore",
    ".gitattributes",
    "README.md",
    "README_MEMBROS.md",
    "AI_AGENT_CONTRACT.md",
    "AGENTS.md",
    "RagnaForge.Agent.slnx",
    "global.json"
)

try {
    foreach ($relative in $allowlist) {
        Copy-AllowlistedPath $root $staging $relative
    }

    $forbidden = Get-ChildItem -LiteralPath $staging -Recurse -Force | Where-Object {
        -not $_.PSIsContainer -and (
            $_.FullName -match '\\node_modules\\' -or
            $_.FullName -match '\\bin\\' -or
            $_.FullName -match '\\obj\\' -or
            $_.Name -match '\.(trx|tsbuildinfo|grf|gpf|thor|spr|act|bmp|tga|rsw|gnd|gat|rsm|pal|zip|rar)$' -or
            $_.Name -eq ".env" -or
            $_.Name -eq "repositories.local.json"
        )
    }
    if ($forbidden) {
        $forbidden | Select-Object FullName | Format-Table -AutoSize
        throw "Staging contains forbidden files."
    }

    if (Test-Path -LiteralPath $output) {
        Remove-Item -LiteralPath $output -Force
    }
    Compress-Archive -Path (Join-Path $staging "*") -DestinationPath $output -Force
    if (-not (Test-Path -LiteralPath $output)) {
        throw "Package ZIP was not created: $output"
    }
    powershell -ExecutionPolicy Bypass -File (Join-Path $root "scripts\audit-release.ps1") -ZipPath $output
    if ($LASTEXITCODE -ne 0) {
        throw "Release audit failed for $output"
    }
    Write-Host "Clean package generated: $output"
}
finally {
    Remove-SafeTempDir $staging
}
