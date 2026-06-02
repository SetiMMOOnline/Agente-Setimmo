# Setimmo Implementation Engine Pack

## Estado

O motor de implementacao usa `SemanticPatchPlanner`, `PatchQualityGate` e `ImplementationConfidenceScorer`.

## Regras

- Sem patch semantico: `needs_codex_repair`.
- Confianca abaixo de `0.50`: bloqueio.
- Risco medio/alto: `codex-supervised`.
- Patch com `Instruction noted`: invalido.
