# ROADMAP AGENT

## Entregue

- core seguro
- CLI
- MCP validator-governed
- `baseline --json`
- `health --json`
- knowledge read-only com build separado
- `config set` com preflight real
- cache stale details
- triage externo
- executor controlado com `review`, `fix`, `create`, `dry-run`, `apply` e `rollback`
- registry inicial de linguagens

## Estado atual

- versao: `1.2.0-operational-ux`
- testes: `267/267`
- apply safety: validator-governed
- apply real: disponivel apenas para operacoes validadas dentro de `writableRoots`
- rollback real: disponivel apenas para operacoes aplicadas pelo proprio agente
- production apply: desabilitado
- paths standalone: resolvidos de forma relativa e auditados por `status`/`doctor`
- dry-run MCP: persistencia local controlada e auditada
- limpeza segura: `cleanup --safe`

## Proximos passos provaveis

- enriquecer relatarios operacionais
- melhorar explicacao de external-data por entidade
- reduzir ruido de cache por index incremental
- ampliar adapters de linguagem com validadores opcionais externos

## Fora de escopo

- apply destrutivo fora do escopo validado
- rollback destrutivo fora do proprio historico do agente
- shell generico
- escrita em GRF
- edicao `.lub`
