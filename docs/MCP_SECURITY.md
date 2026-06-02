# MCP Security

Agente Setimmo MCP v1 e uma superficie validator-governed.

## Garantias

- sem shell
- sem comando arbitrario
- sem path traversal
- sem path absoluto em campos sensiveis
- sem escrita em rAthena
- sem escrita em Patch/client
- sem escrita em GRF
- sem edicao `.lub`
- respostas com limite de tamanho
- recursos sempre dentro de `agentRoot`

## Escritas permitidas

Apenas:

- manifests
- diffs
- rollback plans
- cleanup seguro
- applies e rollbacks dentro de `writableRoots`

## Escritas proibidas

- GRF
- Patch/client
- rAthena
- `.lub`
- qualquer path fora do boundary permitido

## Bloqueios permanentes

- `ragnaforge_apply`
- `ragnaforge_rollback_confirm`

O MCP so pode aplicar ou reverter usando operacoes registradas pelo proprio agente.
