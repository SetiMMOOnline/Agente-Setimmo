# Production Executor e GRF_Extractor

Este documento descreve o estado operacional atual do Agente Setimmo como executor controlado.

## Resumo

O agente agora diferencia tres niveis:

1. **Dry-run / diff**: prepara plano, diff e rollback sem tocar o alvo.
2. **Apply controlado**: aplica uma operacao criada pelo proprio agente dentro de `writableRoots`, com rollback e pos-validacao.
3. **Production**: promocao formal de uma operacao especifica, exigindo aprovacao humana, hash do diff, rollback existente, escopo autorizado e auditoria.

`safeForApply` e calculado pelos validadores normais. `safeForProductionApply` so fica `true` para uma operacao especifica que tenha aprovacao formal vinculada ao hash atual do diff.

## Comandos

```powershell
dotnet run --project src\RagnaForge.Agent.Cli -- operations list --json
dotnet run --project src\RagnaForge.Agent.Cli -- operations show --operation <id> --json
dotnet run --project src\RagnaForge.Agent.Cli -- operations compare --left <id> --right <id> --json

dotnet run --project src\RagnaForge.Agent.Cli -- production plan --operation <id> --environment production --json
dotnet run --project src\RagnaForge.Agent.Cli -- production review --operation <id> --environment production --json
dotnet run --project src\RagnaForge.Agent.Cli -- production approve --operation <id> --environment production --approver "SEU_NOME" --reason "Motivo claro" --json
dotnet run --project src\RagnaForge.Agent.Cli -- production status --operation <id> --environment production --json
dotnet run --project src\RagnaForge.Agent.Cli -- production audit --json
```

`production apply` e `production rollback` existem, mas continuam protegidos por:

- `operationId` valido;
- operacao registrada pelo agente;
- diff e rollback existentes;
- hash do diff igual ao aprovado;
- aprovacao nao expirada;
- path guard;
- bloqueio de arquivo sensivel;
- bloqueio de GRF/GPF/THOR/SPR/ACT/BMP/TGA/RSW/GND/GAT/RSM/PAL;
- bloqueio de `.lub`;
- ausencia de shell generico.

## GRF_Extractor

A integracao com `GRF_Extractor` e deliberadamente conservadora.

Comandos:

```powershell
dotnet run --project src\RagnaForge.Agent.Cli -- grf list --json
dotnet run --project src\RagnaForge.Agent.Cli -- grf inspect --source ro-update.grf --json
dotnet run --project src\RagnaForge.Agent.Cli -- grf dry-run-extract --source ro-update.grf --json
dotnet run --project src\RagnaForge.Agent.Cli -- grf extract --operation <id> --confirm --json
```

O fluxo integrado nao modifica GRFs reais. O `extract` atual cria apenas evidencia metadata-only em `temp/grf-operations/<id>/output/EXTRACTION_MANIFEST.json`. Ele nao copia payload privado nem altera o container original.

Se for preciso extracao real no futuro, ela deve ser promovida por politica propria, com fixtures publicas, destino isolado, rollback/cleanup, auditoria e aprovacao explicita.

## MCP

Ferramentas novas:

- `ragnaforge_operations_list`
- `ragnaforge_operations_show`
- `ragnaforge_operations_compare`
- `ragnaforge_production_status`
- `ragnaforge_production_audit`
- `ragnaforge_production_approve`
- `ragnaforge_production_apply`
- `ragnaforge_production_rollback`
- `ragnaforge_grf_list`
- `ragnaforge_grf_inspect`
- `ragnaforge_grf_dry_run_extract`
- `ragnaforge_grf_extract`

Nenhuma ferramenta aceita comando livre. Nenhuma ferramenta expoe shell generico.

## API/UI do RagnaForge

A API principal expoe apenas endpoints read-only para esta camada:

- `/api/agent/operations`
- `/api/agent/operations/{operationId}`
- `/api/agent/production/audit`
- `/api/agent/production/status/{operationId}`
- `/api/agent/grf/inventory`
- `/api/agent/grf/metadata`

A UI usa esses endpoints na aba de relatorios/operacoes para mostrar historico, governanca de producao e metadata GRF. Ela nao aprova, nao aplica, nao reverte e nao aceita comando livre.

## Limites mantidos

- rAthena permanece read-only fora de operacoes explicitamente autorizadas por `writableRoots`.
- Patch/client permanece protegido.
- GRFs reais permanecem read-only.
- `.lub` permanece bloqueado.
- `repositories.local.json`, `.env` e secrets continuam proibidos.
- Warnings aconselham; blockers reais bloqueiam apply/producao.
