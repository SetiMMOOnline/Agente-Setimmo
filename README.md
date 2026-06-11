# Agente Setimmo

Agente local seguro para o ecossistema RagnaForge.

Diretorio oficial local:

```text
E:\Ragnarok\Projeto\Agente_Setimmo
```

Repositorio oficial:

```text
https://github.com/SetiMMOOnline/Agente-Setimmo
```

Nesta build ele opera em seis modos conceituais:

- `Observe`
- `Plan`
- `DryRun`
- `Apply`
- `Rollback`
- `Production` existe como promocao formal, bloqueada ate haver aprovacao humana, hash de diff valido, rollback e escopo autorizado

O agente consegue revisar codigo, gerar planos, produzir diffs controlados, aplicar mudancas dentro de `writableRoots` e reverter apenas operacoes que ele proprio registrou. Ele continua bloqueando escrita em:

- rAthena
- Patch/client
- GRF
- `.lub`
- qualquer path fora de `agentRoot` e `writableRoots`
- shell generico e comando livre

## Estado atual

- `status`, `doctor`, `baseline`, `health`, `scan`, `index`, `validate`, `triage`, `knowledge` e `export api-readiness` estao funcionais
- `supportsApply` e `supportsRollback` sao capacidades tecnicas
- `safeForApply`, `canApply`, `applyEnabled` e `rollbackEnabled` ficam `false` em status globais
- `safeForApply` so pode ser `true` dentro de uma operacao concreta com plano, diff, rollback, validadores e revisao quando exigida
- `safeForProductionApply` so fica `true` para uma operacao especifica apos aprovacao formal vinculada ao hash do diff
- `operationProfile` controla o atrito do standalone:
  - `strict`: medium/high risk continua em `codex-supervised`
  - `local-dev`: low e parte de medium risk podem seguir com menos friccao dentro de `writableRoots`
  - `sandbox`: parecido com `local-dev`, mas pensado para fixture/sandbox do proprio agente
- MCP expoe ferramentas read-only e ferramentas de implementacao validator-governed
- GRF_Extractor e integrado em modo metadata/controlado: inspeciona e prepara saida no `temp` do agente, sem modificar GRFs reais
- Field Test Harness valida C#, TS/JS, Python, Lua, PowerShell e Shell em sandbox local, com review/plan/dry-run/apply seguro/rollback sem tocar projetos reais

## Fluxo de implementacao

Fluxo obrigatorio:

1. `review` ou `plan`
2. `dry-run`
3. revisao do Codex quando o patch cair em `codex-supervised`
4. `apply implement --operation <id> --confirm`, somente em escopo local permitido
5. pos-validacao
6. `rollback --id <id> --confirm`, se necessario

Para promocao formal de uma operacao especifica:

1. `operations show --operation <id>`
2. `production plan --operation <id> --environment production`
3. `production approve --operation <id> --environment production --approver "<nome>" --reason "<motivo>"`
4. `production apply --operation <id> --environment production --confirm`
5. `production rollback --operation <id> --environment production --confirm`, se for preciso reverter uma operacao aplicada pelo proprio agente

## Comandos principais

```powershell
dotnet build RagnaForge.Agent.slnx
dotnet test RagnaForge.Agent.slnx

dotnet run --project src\RagnaForge.Agent.Cli -- status --json
dotnet run --project src\RagnaForge.Agent.Cli -- doctor --json
dotnet run --project src\RagnaForge.Agent.Cli -- baseline --json
dotnet run --project src\RagnaForge.Agent.Cli -- health --json
dotnet run --project src\RagnaForge.Agent.Cli -- validate --json
dotnet run --project src\RagnaForge.Agent.Cli -- export api-readiness --json

dotnet run --project src\RagnaForge.Agent.Cli -- review code --target src/RagnaForge.Agent.Core/Commands/ValidateCommand.cs --workspace agent --language csharp --json
dotnet run --project src\RagnaForge.Agent.Cli -- fix code --target src/RagnaForge.Agent.Core/Commands/ValidateCommand.cs --workspace agent --language csharp --json
dotnet run --project src\RagnaForge.Agent.Cli -- create content --target temp/executor-smoke.ps1 --workspace agent --language powershell --name "Executor Smoke" --json
dotnet run --project src\RagnaForge.Agent.Cli -- plan implement --target temp/executor-smoke.ps1 --workspace agent --language powershell --instruction "Create a safe smoke script." --json
dotnet run --project src\RagnaForge.Agent.Cli -- dry-run implement --target temp/executor-smoke.ps1 --workspace agent --language powershell --instruction "Create a safe smoke script." --json
dotnet run --project src\RagnaForge.Agent.Cli -- apply implement --operation <id> --confirm --json
dotnet run --project src\RagnaForge.Agent.Cli -- rollback --id <id> --confirm --json

dotnet run --project src\RagnaForge.Agent.Cli -- operations list --json
dotnet run --project src\RagnaForge.Agent.Cli -- operations show --operation <id> --json
dotnet run --project src\RagnaForge.Agent.Cli -- production status --operation <id> --environment production --json
dotnet run --project src\RagnaForge.Agent.Cli -- production approve --operation <id> --environment production --approver "SEU_NOME" --reason "Motivo claro" --json
dotnet run --project src\RagnaForge.Agent.Cli -- production audit --json

dotnet run --project src\RagnaForge.Agent.Cli -- field test run --json
dotnet run --project src\RagnaForge.Agent.Cli -- field test run --keep-sandbox --json

dotnet run --project src\RagnaForge.Agent.Cli -- grf list --json
dotnet run --project src\RagnaForge.Agent.Cli -- grf inspect --source ro-update.grf --json
dotnet run --project src\RagnaForge.Agent.Cli -- grf dry-run-extract --source ro-update.grf --json

dotnet run --project src\RagnaForge.Agent.Cli -- cleanup --safe --include-cache --include-inputs --json
dotnet run --project src\RagnaForge.Agent.Mcp -- --list-tools
```

## Release e sincronizacao

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\package-clean.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\audit-release.ps1 -ZipPath "$env:USERPROFILE\Desktop\Agente_Setimmo_release.zip"
powershell -ExecutionPolicy Bypass -File .\scripts\update-ragnaforge-embedded.ps1
powershell -ExecutionPolicy Bypass -File .\scripts\update-ragnaforge-embedded.ps1 -Apply
```

`update-ragnaforge-embedded.ps1` usa o script allowlisted do RagnaForge e replica o agente standalone para `E:\Ragnarok\Projeto\Ragna Forge\Agente_Setimmo`.

## Suporte inicial por linguagem

- HTML
- CSS
- Bootstrap
- PHP
- Java
- JavaScript
- Node.js
- Shell Script
- C
- C++
- C#
- Python
- Lua
- PowerShell

O registry de linguagens detecta ecossistema, gera scaffold simples, valida sintaxe basica quando possivel e normaliza conteudo antes do diff.

## O que continua bloqueado

- `ragnaforge apply --json`
- rollback sem `--id`
- qualquer operacao sem diff/rollback/confirmacao quando exigidos
- qualquer tentativa de tocar rAthena, Patch/client, GRF ou `.lub`
- qualquer tentativa de shell generico
- GRF real nao e extraido para conteudo privado pelo fluxo padrao; o modo integrado gera evidencia metadata-only controlada

## Documentacao

- [Production Executor e GRF](docs/PRODUCTION_EXECUTOR_AND_GRF.md)
- [Field Test Harness](docs/FIELD_TEST_HARNESS.md)
- [Operational UI and Learning Promotion](docs/OPERATIONAL_UI_AND_LEARNING_PROMOTION.md)
- [Implementation Engine](docs/IMPLEMENTATION.md)
- [Context Packs](docs/CONTEXT-PACKS.md)
- [Golden Scenarios](docs/GOLDEN-SCENARIOS.md)
- [RagnaForge Integration](docs/RAGNAFORGE-INTEGRATION.md)
- [CLI](docs/CLI.md)
- [Safety](docs/SAFETY.md)
- [MCP](docs/MCP.md)
- [MCP Tools](docs/MCP_TOOLS.md)
- [MCP Security](docs/MCP_SECURITY.md)
- [Knowledge](docs/KNOWLEDGE.md)
- [API/UI Contracts](docs/API_UI_AGENT_CONTRACTS_v1.md)
- [Roadmap](docs/ROADMAP_AGENT.md)
