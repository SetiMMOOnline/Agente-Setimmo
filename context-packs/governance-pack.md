# Setimmo Context Pack: governance

## Objetivo

Fornecer ao Codex um pacote curto para revisao supervisionada sem despejar logs grandes.

## Estado atual

- Modo: codex-supervised.
- Capabilities globais sao separadas de autorizacao operacional.
- `safeForApply` generico permanece falso fora de uma operacao concreta.
- `safeForProductionApply` exige aprovacao humana, diff, rollback e auditoria.

## Comandos uteis

```powershell
dotnet test RagnaForge.Agent.slnx
dotnet run --project src\RagnaForge.Agent.Cli -- validate --json
dotnet run --project src\RagnaForge.Agent.Cli -- operations list --json
```

## Riscos e limites

- Nao usar shell generico.
- Nao tocar GRF/rAthena/Patch/.lub sem politica especifica.
- Patches nao semanticos devem retornar `needs_codex_repair`.

## Proximo passo seguro

Gerar dry-run, revisar diff, confirmar rollback e entao pedir revisao Codex quando o risco/confidence exigir.