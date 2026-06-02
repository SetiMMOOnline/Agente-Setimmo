# KNOWLEDGE_HINTS_AUTONOMOUS_CONTROLLED_REFERENCE_v1

## Objetivo
Evoluir a Knowledge Library para hints práticos, ranking, provenance, conflict policy, coverage, conflicts, ask, report e planning dry-run.

## Escopo
Agente embutido e standalone. Sem apply real, rollback real, crawler, scraping amplo, raw HTML, dump externo, cache real por padrão ou escrita em rAthena/Patch/GRF/.lub.

## Implementação
- `KnowledgeHinting.cs`: modelos, ranking, provenance, conflict policy e decisão de referência controlada.
- `FindCommand`: suporte a `--with-knowledge`.
- `KnowledgeCommand`: `search`, `explain`, `conflicts`, `coverage`, `ask` com provenance.
- `KnowledgeReportCommand`: relatório Markdown em stdout JSON.
- `PlanCommand`: plano de criação dry-run com knowledge.
- `TriageCommand`: resumo de knowledge em `--external-data` e live skipped para análise ampla.

## Política live reference
A camada de decisão existe, mas chamada HTTP real é recusada por política quando houver risco jurídico/técnico. O resultado registra requestCount=0, linksFollowed=false, bulkLookup=false, rawHtmlStored=false e warning. Testes não usam internet.

## Riscos restantes
Antes de ativar HTTP real, revisar termos/robots/uso permitido e manter mock/fake nos testes.
