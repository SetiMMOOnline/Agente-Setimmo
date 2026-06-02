# Field Test Harness

O Field Test Harness valida o Agente Setimmo fora do proprio laboratorio imediato, mas ainda dentro de uma sandbox local segura.

## O que ele cobre

- C#
- JavaScript/TypeScript
- Python
- Lua
- PowerShell
- Shell

Para cada stack, o harness simula:

1. review;
2. plan;
3. dry-run;
4. apply seguro em sandbox;
5. rollback do arquivo criado;
6. relatorio comparativo.

## Comando

```powershell
dotnet run --project src\RagnaForge.Agent.Cli -- field test run --json
```

Para manter a sandbox e inspecionar os arquivos:

```powershell
dotnet run --project src\RagnaForge.Agent.Cli -- field test run --keep-sandbox --json
```

## Politica

- Nao executa shell.
- Nao escreve no RagnaForge.
- Nao escreve em rAthena.
- Nao escreve no Patch/client.
- Nao toca GRF real.
- Nao edita `.lub`.
- Escreve apenas em `temp/field-tests/<operationId>` dentro do agentRoot.
- `safeForApply` permanece `false`, porque a autorizacao de apply real depende de uma operacao concreta.

## MCP

O MCP expoe `ragnaforge_field_test_run`. A ferramenta e marcada como mutating porque cria sandbox temporaria, mas continua bloqueada fora do agentRoot e nao executa comandos livres.
