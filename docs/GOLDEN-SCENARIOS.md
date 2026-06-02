# Golden scenarios

Golden scenarios sao smoke tests operacionais pequenos que provam decisoes importantes do agente.

Comando:

```powershell
dotnet run --project src\RagnaForge.Agent.Cli -- golden scenarios run --json
```

Cenarios cobertos nesta versao:

- `InstructionNotedPatch`: patch placebo deve ser bloqueado.
- `GlobalSafeForApplyConfusion`: capacidade global nao vira autorizacao.
- `FrontendDependenciesIncomplete`: dependencias devem ser restauradas pelo lockfile.
- `ApiUiContractClamp`: API publica continua com `safeForApply=false`.
- `CodexSupervisedGate`: confianca baixa/media exige revisao Codex.

Novos cenarios devem ser curtos, deterministas e seguros para rodar em ambiente local sem tocar rAthena, Patch/client, GRF real ou `.lub`.
