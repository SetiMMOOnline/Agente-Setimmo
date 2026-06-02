# CLI

## Diagnostico e validacao

```powershell
ragnaforge status --json
ragnaforge doctor --json
ragnaforge baseline --json
ragnaforge health --json
ragnaforge scan --project --json
ragnaforge index --entities --json
ragnaforge validate --json
ragnaforge canon check --json
ragnaforge cleanup --safe --json
```

Interpretacao:

- `safeForReadOnlyWork`: leitura e auditoria seguem seguras
- `safeForDryRun`: simulacao e geracao de diff seguem seguras
- `safeForApply`: apply controlado pode prosseguir se os validadores permitirem
- `safeForProductionApply`: so fica `true` para uma operacao especifica com aprovacao formal e diff hash valido

## Fluxo de implementacao

```powershell
ragnaforge review code --target src/RagnaForge.Agent.Core/Commands/ValidateCommand.cs --workspace agent --language csharp --json
ragnaforge fix code --target src/RagnaForge.Agent.Core/Commands/ValidateCommand.cs --workspace agent --language csharp --json
ragnaforge create content --target temp/executor-smoke.ps1 --workspace agent --language powershell --name "Executor Smoke" --json
ragnaforge plan implement --target temp/executor-smoke.ps1 --workspace agent --language powershell --instruction "Create a safe smoke script." --json
ragnaforge dry-run implement --target temp/executor-smoke.ps1 --workspace agent --language powershell --instruction "Create a safe smoke script." --json
ragnaforge apply implement --operation <id> --confirm --json
ragnaforge rollback --id <id> --confirm --json
```

Regras:

- `review code` nao altera arquivos
- `fix code`, `create content` e `dry-run implement` persistem diff, rollback e logs dentro do `agentRoot`
- `apply implement` escreve somente dentro de `writableRoots`
- `rollback` so funciona para operacoes previamente aplicadas pelo proprio agente

## Operations e Production

```powershell
ragnaforge operations list --json
ragnaforge operations show --operation <id> --json
ragnaforge operations compare --left <id> --right <id> --json

ragnaforge production plan --operation <id> --environment production --json
ragnaforge production approve --operation <id> --environment production --approver "SEU_NOME" --reason "Motivo claro" --json
ragnaforge production status --operation <id> --environment production --json
ragnaforge production audit --json
```

`production apply` e `production rollback` existem apenas como fluxo formal controlado. Eles exigem confirmacao, aprovacao humana, hash do diff atual, rollback plan e path guard.

## GRF_Extractor

```powershell
ragnaforge grf list --json
ragnaforge grf inspect --source ro-update.grf --json
ragnaforge grf dry-run-extract --source ro-update.grf --json
```

O fluxo integrado de GRF e metadata-only por padrao: nao modifica GRFs reais, nao copia assets privados e nao edita `.lub`.

## Knowledge e triage

```powershell
ragnaforge knowledge sources --json
ragnaforge knowledge validate --json
ragnaforge knowledge search --query "Potion" --json
ragnaforge knowledge explain --topic "map dependencies" --json
ragnaforge knowledge conflicts --json
ragnaforge knowledge coverage --json
ragnaforge triage --external-data --json
ragnaforge export api-readiness --json
```

## Limpeza segura

```powershell
ragnaforge cleanup --safe --include-cache --include-logs --include-inputs --json
```

`cleanup --safe` remove apenas artefatos regeneraveis:

- `bin`
- `obj`
- `TestResults`
- `*.trx`
- `*.tsbuildinfo`
- cache local opcional
- logs locais opcionais
- inputs de dry-run opcionais

Ele nunca toca:

- codigo-fonte
- docs
- knowledge curado
- configuracoes example
- rAthena
- Patch/client
- GRF
- `.lub`

## Comandos genericos ainda bloqueados

```powershell
ragnaforge apply --json
ragnaforge rollback --confirm --json
```

Use sempre a forma operacional controlada com `operationId`.
