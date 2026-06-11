# Relatorio - dependency auto-update controlled

## Resumo

Foi aplicado um modelo de autoatualizacao controlada para as dependencias NuGet diretas do agente.

O projeto nao ficou com atualizacao silenciosa e irrestrita durante qualquer build. Em vez disso, as dependencias diretas usam floating versions por major, o lockfile preserva reproducibilidade, e o script `scripts/update-dependencies.ps1` executa restore controlado, checks de pacote, build e testes.

## Objetivo

Permitir que as bibliotecas do Agente Setimmo sejam atualizadas com menos atrito, sem enfraquecer estabilidade, rastreabilidade ou validacao.

## Escopo

Diretorio de trabalho:

```text
E:\Ragnarok\Projeto\Agente_Setimmo
```

Arquivos alterados:

```text
tests/RagnaForge.Agent.Core.Tests/RagnaForge.Agent.Core.Tests.csproj
tests/RagnaForge.Agent.Core.Tests/packages.lock.json
```

Arquivos criados:

```text
scripts/update-dependencies.ps1
docs/reports/2026-06-11_DEPENDENCY-AUTO-UPDATE-CONTROLLED_v1.md
temp/release-check-autoupdate.log
logs/operations/*.json gerados pelo release-check e smoke operacional
logs/diffs/*.diff.json gerados pelo dry-run operacional
logs/rollbacks/*.rollback.json gerados pelo dry-run operacional
```

Arquivos removidos:

```text
Nenhum.
```

## Decisao tecnica

Dependencias diretas do projeto de testes foram alteradas para floating version por major:

```text
coverlet.collector: 10.*
Microsoft.NET.Test.Sdk: 18.*
xunit.v3: 3.*
xunit.runner.visualstudio: 3.*
```

O lockfile continua ativo via `RestorePackagesWithLockFile=true`. Assim, o build normal continua reproducivel, enquanto `scripts/update-dependencies.ps1 -Apply` atualiza o lockfile com seguranca quando houver patch/minor novo dentro do major permitido.

Atualizacao automatica de major foi bloqueada por desenho. Major upgrade continua exigindo revisao humana/Codex porque pode quebrar API, runner, cobertura ou comportamento dos testes.

## Como executar

Dry-run/read-only:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\update-dependencies.ps1
```

Aplicar atualizacao controlada no lockfile:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\update-dependencies.ps1 -Apply
```

Aplicar e executar tambem o release-check completo:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\update-dependencies.ps1 -Apply -RunReleaseCheck
```

## Validacao executada

```text
powershell -ExecutionPolicy Bypass -File .\scripts\update-dependencies.ps1 -Apply
dotnet build .\RagnaForge.Agent.slnx
dotnet test .\RagnaForge.Agent.slnx --no-build
powershell -ExecutionPolicy Bypass -File .\scripts\release-check.ps1
dotnet list .\RagnaForge.Agent.slnx package --outdated --format json
dotnet list .\RagnaForge.Agent.slnx package --deprecated --format json
dotnet list .\RagnaForge.Agent.slnx package --vulnerable --include-transitive --format json
dist\agente-setimmo\ragnaforge.exe health --json
dist\agente-setimmo\ragnaforge.exe validate --json
```

## Resultado

Build:

```text
Compilacao com exito.
0 Aviso(s)
0 Erro(s)
```

Testes:

```text
Aprovado: 303
Falha: 0
Ignorado: 0
Total: 303
```

Setimmo validate:

```text
0 issues
0 errors
0 warnings
```

Pacotes:

```text
Outdated: nenhum marcador latestVersion encontrado.
Deprecated: nenhum marcador de deprecacao encontrado.
Vulnerable: nenhum marcador de vulnerabilidade encontrado.
```

Release-check:

```text
Concluido com exit code 0.
O smoke criou temp/release-smoke.ps1, aplicou a operacao controlada e executou rollback com sucesso.
Arquivo temporario temp/release-smoke.ps1 ausente apos rollback.
```

## Seguranca

Nenhuma credencial foi criada, lida ou exposta.

Nenhuma dependencia nova foi adicionada. Foram ajustadas apenas as versoes das dependencias NuGet diretas ja existentes.

O script novo nao executa remocao, movimentacao, truncamento ou operacao destrutiva. O modo padrao e dry-run/read-only, e a escrita no lockfile ocorre apenas com `-Apply`.

## Riscos restantes

Atualizacoes futuras ainda dependem da disponibilidade do feed NuGet local/remoto.

Atualizacao de major permanece manual por seguranca.

O `git` nao estava disponivel no PATH desta sessao, portanto branch, hash e diff Git nao foram validados por comando Git.

## Veredito

Aprovado.
