# AGENT_GLOBAL_CANON_INTERNAL_LIBRARIES_v1

## 1. Objetivo
Implementar o Cânone Global como documento, política de runtime e fonte da Knowledge Library; adicionar Divine Pride e RateMyServer como bibliotecas internas read-only.

## 2. Escopo
Agente embutido e agente standalone. Sem apply real, rollback real, scraping, crawler, dump externo ou edição de fontes sensíveis.

## 3. Diretórios afetados
- src
- tests
- docs
- knowledge
- README.md

## 4. Cânone implementado
`docs/CANONE_GLOBAL_DE_REGRAS.md` define as regras operacionais de preservação, escopo, read-only, dry-run, diff, segurança, git, relatórios e kill-switch.

## 5. Integração
O Cânone foi integrado em `GlobalCanonPolicy`, `GlobalCanonValidator`, `CanonCommand`, `canon check --json`, `validate --canon --json`, doctor, health e baseline.

## 6. Bibliotecas internas
Foram adicionadas fontes e packs para `global-canon`, `divine-pride` e `ratemyserver`.

## 7. Por que não são integração online
Nesta v1, Divine Pride e RateMyServer são apenas referências internas com provenance. Não há adapter HTTP, scraping, crawler, login, API key, dump, raw HTML ou cache externo.

## 8. Provenance
As fontes registram URL externa como referência e deixam explícito que o uso é metadata-only.

## 9. Conflito
Dados locais/rAthena/projeto têm prioridade. Fontes externas são hints/contexto, não blockers absolutos sem evidência local.

## 10. Testes
Foram previstos testes unitários de política do Cânone e bibliotecas internas sem internet.

## 11. Validação
Build, test e smokes seguros devem ser executados nos dois agentes.

## 12. Riscos restantes
Verificar se caches/logs gerados pelos smokes permanecem fora de versionamento e do bundle final.

## 13. Próximo passo recomendado
Validação externa usando o Validation Bundle Gate v1.
