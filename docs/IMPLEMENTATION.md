# Implementation Engine e modo Codex-supervised

Estado: v1 operacional controlado.

O Agente Setimmo nao deve fingir uma correcao semantica quando nao consegue gerar um patch real. A partir desta rodada, o fluxo `dry-run implement` tem tres saidas esperadas:

- `planned`: existe diff real, rollback manifest e qualidade minima do patch.
- `needs_codex_repair`: Setimmo nao conseguiu gerar uma mudanca semantica confiavel.
- `blocked`: escopo, path, segredo, arquivo sensivel ou politica impediram a operacao.

Patches invalidos:

- diff vazio;
- patch que apenas adiciona comentario;
- patch que apenas adiciona TODO;
- patch com `Instruction noted`;
- patch que nao altera o alvo logico solicitado.

O modo `codex-supervised` significa que Setimmo prepara contexto, diff, risco e rollback, mas Codex revisa ou corrige quando a confianca semantica for baixa/media. `supportsApply=true` e uma capacidade do agente. Nao e autorizacao global. Fora de uma operacao concreta, `safeForApply=false`, `canApply=false`, `applyEnabled=false` e `rollbackEnabled=false`.

Gates de confianca:

- `0.00-0.49`: bloqueado, precisa de Codex.
- `0.50-0.74`: Codex-supervised obrigatorio.
- `0.75-0.89`: permitido somente em dev/local com plano, diff, rollback e validadores.
- `0.90+`: tarefas simples e reversiveis podem seguir se a politica permitir.

Producao continua bloqueada por padrao. `safeForProductionApply=true` exige aprovacao humana registrada, hash do diff, escopo autorizado, rollback/snapshot, trilha de auditoria e validadores criticos OK.
