# Context packs

Context packs sao resumos pequenos para reduzir contexto bruto enviado ao Codex.

Comandos:

```powershell
dotnet run --project src\RagnaForge.Agent.Cli -- context pack list --json
dotnet run --project src\RagnaForge.Agent.Cli -- context pack generate --area governance --json
dotnet run --project src\RagnaForge.Agent.Cli -- context pack show --name governance-pack.md --json
```

Packs iniciais:

- `governance-pack.md`
- `implementation-engine-pack.md`

Cada pack deve conter objetivo, estado atual, arquivos relevantes, comandos de teste, riscos, blockers, warnings e proximos passos. Operacoes com risco medio/alto devem apontar para um context pack em vez de depender de dumps grandes.
