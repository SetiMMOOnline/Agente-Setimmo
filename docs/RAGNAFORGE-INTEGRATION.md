# Integracao com RagnaForge

O RagnaForge consome o Agente Setimmo por comandos allowlisted. A API nao aceita comando livre e nao expoe shell generico.

Comandos allowlisted adicionados nesta rodada:

- `context pack list --json`
- `lessons list --json`
- `golden scenarios run --json`

Endpoints do projeto principal:

- `GET /api/agent/context-packs`
- `GET /api/agent/lessons`
- `GET /api/agent/golden-scenarios`

Todos sao read-only. Nenhum endpoint de apply/rollback foi criado. A API publica continua usando `safeForApply=false` e `canApply=false`; `supportsApply=true` aparece apenas como capacidade tecnica do agente, nao como permissao operacional.

Sincronizacao solo/embutido:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\sync-agent-setimmo.ps1 -Direction EmbeddedToStandalone -Apply
```

O script copia somente arquivos allowlisted de source, tests, docs, knowledge, context packs, scripts e configuracoes seguras. Ele nao copia cache, logs, dist, bin, obj, `.env`, `repositories.local.json` ou `paths.json` local.
