# MCP

## Resumo

O MCP do Agente Setimmo expone ferramentas read-only e ferramentas de implementacao validator-governed.

Ele nao:

- expone shell generico
- aceita comando livre
- escreve em rAthena
- escreve em Patch/client
- escreve em GRF
- edita `.lub`
- permite apply generico sem `operationId`
- permite rollback generico sem `operationId`

## Como rodar

```powershell
dotnet run --project src\RagnaForge.Agent.Mcp
dotnet run --project src\RagnaForge.Agent.Mcp -- --list-tools
dotnet run --project src\RagnaForge.Agent.Mcp -- --list-resources
dotnet run --project src\RagnaForge.Agent.Mcp -- --list-prompts
```

## Tools read-only

- `ragnaforge_status`
- `ragnaforge_doctor`
- `ragnaforge_baseline`
- `ragnaforge_health`
- `ragnaforge_scan_project`
- `ragnaforge_config_get`
- `ragnaforge_config_validate`
- `ragnaforge_profile_list`
- `ragnaforge_profile_validate`
- `ragnaforge_index_entities`
- `ragnaforge_find_item`
- `ragnaforge_find_npc`
- `ragnaforge_find_monster`
- `ragnaforge_find_map`
- `ragnaforge_validate`
- `ragnaforge_diff`
- `ragnaforge_report`
- `ragnaforge_report_list`
- `ragnaforge_report_read`
- `ragnaforge_security_policy`
- `ragnaforge_triage`
- `ragnaforge_rollback_list`
- `ragnaforge_rollback_dry_run`
- ferramentas de knowledge, freshness, snapshots e learning

## Tools validator-governed

- `ragnaforge_review_code`
- `ragnaforge_fix_code`
- `ragnaforge_create_content`
- `ragnaforge_plan_implement`
- `ragnaforge_dry_run_implement`
- `ragnaforge_apply_implement`
- `ragnaforge_rollback_implement`
- `ragnaforge_cleanup_safe`

Essas tools so escrevem dentro de:

- `logs/`
- `inputs/dry-run/`
- arquivos alvo dentro de `writableRoots`

## Tools bloqueadas

- `ragnaforge_apply`
- `ragnaforge_rollback_confirm`

## Politica de referencia externa

Mesmo no MCP:

- no crawler
- no follow links
- no bulk
- no range
- no raw HTML
- no dump
- no cache real por padrao

## Contrato MCP

As respostas MCP seguem os mesmos contratos estaveis da CLI e agora informam `readOnly=false` apenas para tools mutantes controladas.

`safeForApply` e validator-governed.
