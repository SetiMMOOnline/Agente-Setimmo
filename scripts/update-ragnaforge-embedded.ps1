<#
Nome: update-ragnaforge-embedded.ps1
Versao: 1.0
Objetivo: sincronizar o Agente Setimmo standalone para o agente incorporado no RagnaForge.
Autor/IA: Codex
Data: 2026-06-02
Modo padrao: dry-run
Entradas: -Apply
Saidas: plano ou copia allowlisted via script do RagnaForge
Arquivos afetados: E:\Ragnarok\Projeto\Ragna Forge\Agente_Setimmo quando -Apply for usado
Riscos: medio; sobrescreve apenas entradas allowlisted do agente incorporado
Como executar: powershell -ExecutionPolicy Bypass -File .\scripts\update-ragnaforge-embedded.ps1 [-Apply]
Como validar: build/test do RagnaForge e do Agente Setimmo
Como reverter: usar Git no RagnaForge para revisar/reverter a diff
#>
param(
    [switch]$Apply
)

$ErrorActionPreference = "Stop"

$ragnaForgeRoot = "E:\Ragnarok\Projeto\Ragna Forge"
$syncScript = Join-Path $ragnaForgeRoot "scripts\sync-agent-setimmo.ps1"
if (-not (Test-Path -LiteralPath $syncScript)) {
    throw "RagnaForge sync script not found: $syncScript"
}

$arguments = @("-ExecutionPolicy", "Bypass", "-File", $syncScript, "-Direction", "StandaloneToEmbedded")
if ($Apply) {
    $arguments += "-Apply"
}

Write-Host "Running RagnaForge embedded agent sync. Apply=$Apply"
& powershell @arguments
