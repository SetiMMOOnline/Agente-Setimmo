# ApiUiContractClamp

Padrao aprovado: API/UI consomem o Agente Setimmo sem abrir comando livre nem apply generico.

Regras:

- allowlist explicita;
- endpoints read-only para status, readiness, operations, production status/audit e GRF metadata;
- `canApply=false` ate uma operacao concreta passar por plano, diff, rollback, validadores e revisao.
