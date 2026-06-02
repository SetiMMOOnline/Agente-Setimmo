# Antigravity Integration

## Recommended configuration

| Setting | Recommended value |
|---|---|
| Mode | Planning for complex work |
| Development | Review-driven development |
| Terminal Execution Policy | Request Review |
| Artifact Review Policy | Review plans, diffs, and applies |
| Terminal Sandbox | Enabled when available |
| JavaScript Execution Policy | Request Review or Disabled |
| Non-Workspace File Access | Disabled by default |

## Rules

Shared rules for Antigravity live in `.agents/rules/`:

- `ragnaforge-safety.md`
- `ragnaforge-code-style.md`
- `ragnaforge-ai-operator.md`

## Workflows

Saved workflows live in `.agents/workflows/`:

- `audit-agent.md`
- `safe-status-doctor.md`
- `scan-project-readonly.md`
- `review-diff.md`
- `final-report.md`
- `mcp-smoke-test.md`

## CLI usage

```powershell
dotnet run --project src/RagnaForge.Agent.Cli -- status --json
dotnet run --project src/RagnaForge.Agent.Cli -- doctor --json
dotnet run --project src/RagnaForge.Agent.Cli -- baseline --json
```

## Controlled implementation flow

```powershell
dotnet run --project src/RagnaForge.Agent.Cli -- review code --target <path> --workspace main --json
dotnet run --project src/RagnaForge.Agent.Cli -- dry-run implement --target <path> --workspace main --language <key> --instruction "<text>" --json
dotnet run --project src/RagnaForge.Agent.Cli -- apply implement --operation <id> --confirm --json
dotnet run --project src/RagnaForge.Agent.Cli -- rollback --id <id> --confirm --json
```

## External path strategy

### Strategy A (preferred)

- Open the workspace at the current `agentRoot`
- Use CLI or MCP to reach configured external paths through allowlisted config
- Do not grant wide Desktop access just to inspect a few paths

### Strategy B (only if necessary)

- Temporarily allow specific non-workspace reads
- Review each action
- Disable the extra access again after the audit

## Important rules

- Never use Always Proceed on this project
- Never expose generic shell
- Never bypass validator-governed apply/rollback
- Never write to GRF, Patch/client, rAthena, or `.lub`
- Follow `docs/AI_AGENT_CONTRACT.md` as the primary neutral contract

## MCP

Compatible MCP hosts can run:

```powershell
dotnet run --project src/RagnaForge.Agent.Mcp
```

The MCP surface exposes safe diagnostics plus validator-governed implementation tools.
Resources remain read-only. Generic apply and generic rollback remain blocked.
