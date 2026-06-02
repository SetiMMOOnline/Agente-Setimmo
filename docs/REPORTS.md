# Reports Guide

Agente Setimmo generates detailed reports for every operation.

## Locations

- **Operation Manifests**: `logs/operations/` (JSON)
- **Diffs**: `logs/diffs/` (Unified Diff)
- **Rollback Plans**: `logs/rollbacks/` (JSON)
- **Markdown Reports**: `logs/reports/` (Readable MD)

## Report Command

Generate a human-readable report for the last operation:

`ragnaforge report --last --format md`

## Safety Information

Reports explicitly state:

- `safeForAutomation`: whether the operation respected the safety rules
- `applied`: whether the operation was actually applied
- `rollbackPlanPath`: the rollback plan path for agent-owned operations

Generic apply and generic rollback remain blocked.

Controlled apply and controlled rollback are available only for:

- operations created by the agent itself
- targets inside `writableRoots`
- flows that passed validators and explicit confirmation
