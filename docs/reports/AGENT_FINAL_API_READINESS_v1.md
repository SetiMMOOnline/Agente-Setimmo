# AGENT_FINAL_API_READINESS_v1

## Objetivo

Fechar o agente como motor auxiliar da futura API/UI tabulada.

## O que esta pronto

- busca com knowledge hints
- conflitos estruturados
- coverage
- triagem externa em JSON e Markdown
- packs, explain, validate e freshness
- plan create dry-run para item, equipment, monster, npc, map, skill e quest
- export `api-readiness`
- MCP read-only alinhado

## O que a futura API/UI deve consumir

- `find ... --with-knowledge`
- `knowledge conflicts`
- `knowledge coverage`
- `triage --external-data`
- `plan create ... --with-knowledge`
- `report --knowledge --format md`
- `report --external-data --format md`
- `report --entity-plan --format md`
- `report --readiness-summary --format md`
- `export api-readiness --json`

## Referencia externa

Situacao desta build:

- contrato pronto
- decisao de lookup pronta
- live HTTP real bloqueado por policy
- nenhum crawler
- nenhum follow links
- nenhum bulk
- nenhum range
- nenhum raw HTML
- nenhum dump
- nenhum cache real por padrao

## Triage atual

Estado consolidado apos correcao do parser:

- `1083` warnings externos
- `0` errors ativos
- `safeForReadOnlyWork=true`
- `safeForDryRun=true`
- `safeForApply=false`

## Politica custom/progressive

- divergencia com referencia externa pode ser esperada
- `db/import` conta como sinal de override custom
- `episodeGate` pode retornar `customAllowed`
- a revisao humana continua obrigatoria

## MCP

Tools principais para a proxima etapa:

- `ragnaforge_knowledge_search`
- `ragnaforge_knowledge_conflicts`
- `ragnaforge_knowledge_coverage`
- `ragnaforge_external_data_triage`
- `ragnaforge_pack_freshness`
- `ragnaforge_plan_create_entity`
- `ragnaforge_generate_knowledge_report`
- `ragnaforge_api_readiness_export`
- `ragnaforge_canon_check`

## Proxima etapa recomendada

Construir a API/UI tabbed interactive workspace sobre os contratos JSON ja exportados, mantendo:

- read-only
- dry-run
- diff-preview
- human review
- `safeForApply=false`
