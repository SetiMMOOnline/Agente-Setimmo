# MCP Tools

## Read-only

- `ragnaforge_status`
- `ragnaforge_doctor`
- `ragnaforge_baseline`
- `ragnaforge_health`
- `ragnaforge_scan_project`
- `ragnaforge_index_entities`
- `ragnaforge_validate`
- `ragnaforge_diff`
- `ragnaforge_report`
- `ragnaforge_report_list`
- `ragnaforge_report_read`
- `ragnaforge_security_policy`
- `ragnaforge_triage`
- `ragnaforge_rollback_list`
- `ragnaforge_rollback_dry_run`
- tools de knowledge e readiness
- `ragnaforge_operations_list`
- `ragnaforge_operations_show`
- `ragnaforge_operations_compare`
- `ragnaforge_production_status`
- `ragnaforge_production_audit`
- `ragnaforge_grf_list`
- `ragnaforge_grf_inspect`

## Validator-governed writes

- `ragnaforge_fix_code`
- `ragnaforge_create_content`
- `ragnaforge_dry_run_implement`
- `ragnaforge_apply_implement`
- `ragnaforge_rollback_implement`
- `ragnaforge_cleanup_safe`
- `ragnaforge_production_approve`
- `ragnaforge_production_apply`
- `ragnaforge_production_rollback`
- `ragnaforge_grf_dry_run_extract`
- `ragnaforge_grf_extract`

## Validator-governed no-write planning

- `ragnaforge_review_code`
- `ragnaforge_plan_implement`

## Explicitly blocked

- `ragnaforge_apply`
- `ragnaforge_rollback_confirm`

Generic apply e generic rollback continuam bloqueados. O fluxo permitido e sempre operation-based, com `operationId`, confirmacao explicita e validadores.

As ferramentas de production exigem approval humano e hash de diff. As ferramentas GRF nao alteram containers reais; o fluxo de extract gera apenas metadata controlada em pasta temporaria do agente.
