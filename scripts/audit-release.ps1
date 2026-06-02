<#
Nome: audit-release.ps1
Versao: 1.0
Objetivo: validar um ZIP de release do Agente Setimmo sem extrair lixo/sigilos.
Autor/IA: Codex
Data: 2026-06-02
Modo padrao: read-only
Entradas: -ZipPath
Saidas: tabela de violacoes e exit code
Arquivos afetados: pasta temporaria sob $env:TEMP
Riscos: baixo; remove apenas a pasta temporaria criada pelo proprio script
Como executar: powershell -ExecutionPolicy Bypass -File .\scripts\audit-release.ps1 -ZipPath <zip>
Como validar: o script retorna 0 quando o pacote esta limpo
Como reverter: apague o ZIP auditado se ele falhar
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$ZipPath
)

$ErrorActionPreference = "Stop"

function New-SafeTempDir {
    $name = "agente_setimmo_audit_" + [System.Guid]::NewGuid().ToString("N")
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

$fullZip = [System.IO.Path]::GetFullPath($ZipPath)
if (-not (Test-Path -LiteralPath $fullZip)) {
    throw "ZIP not found: $fullZip"
}

$tempDir = New-SafeTempDir
try {
    Expand-Archive -LiteralPath $fullZip -DestinationPath $tempDir -Force
    $violations = New-Object System.Collections.Generic.List[object]
    $forbidden = @(
        '\.git(/|\\|$)',
        '(^|/|\\)\.env($|\.)',
        'repositories\.local\.json$',
        'node_modules(/|\\)',
        '(^|/|\\)bin(/|\\)',
        '(^|/|\\)obj(/|\\)',
        'TestResults(/|\\)',
        '\.trx$',
        '\.tsbuildinfo$',
        '(^|/|\\)(cache|logs|tmp|temp)(/|\\)(?!\.gitkeep$).+',
        '\.(grf|gpf|thor|spr|act|bmp|tga|rsw|gnd|gat|rsm|pal)$',
        '\.(zip|rar)$'
    )

    Get-ChildItem -LiteralPath $tempDir -Recurse -Force | ForEach-Object {
        if ($_.PSIsContainer) { return }
        $fileFull = [System.IO.Path]::GetFullPath($_.FullName)
        $relative = $fileFull.Substring($tempDir.TrimEnd('\', '/').Length + 1).Replace('\', '/')
        foreach ($pattern in $forbidden) {
            if ($relative -match $pattern) {
                $violations.Add([pscustomobject]@{ Path = $relative; Rule = $pattern }) | Out-Null
                break
            }
        }
    }

    if ($violations.Count -gt 0) {
        $violations | Format-Table -AutoSize
        Write-Error "Release audit failed with $($violations.Count) violation(s)."
    }

    Write-Host "Release audit passed: $fullZip"
}
finally {
    Remove-SafeTempDir $tempDir
}
