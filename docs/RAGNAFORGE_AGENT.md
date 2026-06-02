# Agente Setimmo

## Purpose

Agente Setimmo is the local operational layer for RagnaForge.
It reduces ambiguity for Codex, Antigravity, MCP hosts, and future API/UI bridges.

## Core outputs

- `status` for current runtime state
- `doctor` for configuration and safety checks
- `baseline` for a full operational checkpoint
- `health` for compact integration status

## Important paths

Configured through `config/paths.json`.

Key roots:

- `agentRoot`
- `ragnaforgeMainProjectPath`
- `rathenaPath`
- `patchPath`
- `grfRepositoryPath`
- `grfEditorPath`

No automation should hardcode these values.

## Safety model

The agent starts in read-only posture, but it is no longer read-only only.

What stays blocked:

- writes to GRF
- writes to Patch/client
- writes to rAthena
- edits to `.lub`
- generic shell
- generic `apply`
- generic rollback without `operationId`

What is allowed when validators approve:

- review code
- generate diffs
- create content
- apply implementation changes inside `writableRoots`
- rollback previously applied agent-owned operations

## Baseline usage

Use `baseline` when you need a complete checkpoint:

```powershell
ragnaforge baseline --json
```

Typical use cases:

- start of a coding session
- pre-release audit
- before handing work to another model
- before trusting cache-backed summaries

## Health usage

Use `health` when you need a compact dashboard summary:

```powershell
ragnaforge health --json
```

Typical use cases:

- API/UI status surfaces
- MCP host summaries
- operator dashboards
- quick count-trust checks

## Validation semantics

These top-level decisions matter most:

- `safeForReadOnlyWork`
- `safeForDryRun`
- `safeForApply`
- `safeForProductionApply`

Meaning:

- read-only work may proceed
- dry-run and diff generation may proceed
- operation-scoped apply may proceed when validators pass
- production apply remains disabled in this build

## Cache trust

The agent explains stale cache state directly instead of forcing raw file comparison.

Common stale reasons:

- `cache_not_found`
- `cache_corrupt`
- `activeProfile_mismatch`
- `configFingerprint_mismatch`
- `scanRoot_mismatch`

## Config preflight

`config set` validates path type before saving.

Examples:

- main project path must look like a RagnaForge repo
- rAthena path must look like rAthena
- GRF repository must remain read-only

After a successful config change, run:

```powershell
ragnaforge baseline --json
```
