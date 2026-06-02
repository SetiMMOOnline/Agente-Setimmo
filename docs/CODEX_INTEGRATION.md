# Codex Integration

## Operational contract

`AGENTS.md` in the repository root is the operational contract for Codex.
It defines:

- allowed directories
- blocked targets
- required validation steps
- final reporting format

`docs/AI_AGENT_CONTRACT.md` is the neutral machine-facing contract for safe CLI usage.

## Preferred execution model

Codex should prefer structured JSON from the local CLI or MCP server instead of
parsing free-form terminal text.

Recommended baseline:

```powershell
ragnaforge status --json
ragnaforge doctor --json
ragnaforge baseline --json
ragnaforge validate --json
```

## Controlled implementation flow

For write-capable work, Codex should stay inside the agent's controlled flow:

```powershell
ragnaforge review code --target <path> --workspace main --json
ragnaforge dry-run implement --target <path> --workspace main --language <key> --instruction "<text>" --json
ragnaforge apply implement --operation <id> --confirm --json
ragnaforge rollback --id <id> --confirm --json
```

Rules:

- no generic shell
- no free command execution
- no writes outside `writableRoots`
- no writes to GRF, Patch/client, rAthena, or `.lub`
- no generic `ragnaforge apply`
- no generic rollback without `operationId`

## MCP usage

Codex can use the local MCP server by stdio:

```json
{
  "mcpServers": {
    "ragnaforge": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\path\\to\\Agente_Setimmo\\src\\RagnaForge.Agent.Mcp"
      ]
    }
  }
}
```

The MCP surface now includes:

- read-only diagnostics and knowledge tools
- validator-governed implementation tools
- read-only resources
- static safety prompts

Blocked forever at the MCP boundary:

- generic shell tools
- `ragnaforge_apply`
- `ragnaforge_rollback_confirm`

## Recommended Codex habits

1. Run `status` or `baseline` first.
2. Prefer `review code` before `fix` or `apply`.
3. Treat `safeForProductionApply=false` as expected in this build.
4. Use operation-scoped apply/rollback only after diff and validator checks.
5. Keep historical report files as evidence; do not rewrite them casually.
