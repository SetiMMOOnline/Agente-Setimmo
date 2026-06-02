# KNOWLEDGE_REFRESH_LEARNING_LAYER_v1

## Resumo

Esta etapa adiciona uma camada controlada de fontes online, snapshots sanitizados, learning candidates review-first e referencias autorizadas de roBrowser para o Agente Setimmo.

## Fontes novas

- `rathena-board`
- `robrowserlegacy`
- `robrowserlegacy-remoteclient-js`

## O que foi habilitado

- sources internos com policy completa
- refresh plan e due list
- refresh run metadata-only para GitHub autorizado
- skip conservador por policy para forum
- snapshots sanitizados versionados
- learning candidates review-first
- export API/UI com contratos de source/refresh/learning
- MCP read-only para fontes, freshness, snapshots e learning

## O que continua bloqueado

- apply real
- rollback real
- shell generico
- crawler de forum
- paginação de forum
- raw HTML
- dump externo
- cache real versionado
- auto-promocao de packs

## roBrowser autorizado

O uso de `roBrowserLegacy` e `roBrowserLegacy-RemoteClient-JS` foi registrado como autorizado por relato do usuario, com provenance, licenca e rastreabilidade obrigatorias.

## Refresh policy

- `rathena-board`: metadata-only, review-first, pode retornar `skipped_by_policy`
- `robrowserlegacy`: GitHub metadata/readme/tree, sem vendoring acidental
- `robrowserlegacy-remoteclient-js`: GitHub metadata/readme/tree, sem vendoring acidental

## Learning policy

- `observe` gera candidato sanitizado
- `approve` e `promote` ficam em dry-run
- `reject` registra decisao somente no retorno
- segredo e raw HTML sao bloqueados

## Proximos passos

- consumir `knowledge sources`, `knowledge freshness`, `knowledge refresh plan`, `knowledge snapshots`, `knowledge learn candidates` e `export api-readiness` na futura API/UI tabulada
- manter qualquer incorporacao futura de codigo roBrowser em revisao manual com provenance

safeForApply=false
