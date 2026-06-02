# GlobalSafeForApplyConfusion

Decisao: capabilities globais dizem o que o Agente Setimmo sabe fazer; autorizacao operacional diz o que uma operacao concreta pode fazer agora.

Regra:

- `supportsApply=true` pode aparecer em status/contratos globais.
- `safeForApply=true` so pode aparecer em operacao concreta com plano, diff, rollback, validadores e revisao.
- API/UI publica mantem `canApply=false` fora desse contrato operacional.
