# InstructionNotedPatch

Sintoma: o motor de implementacao preserva o conteudo atual e adiciona apenas `Instruction noted`.

Causa: instrucao semantica nao foi convertida em patch real.

Regra: patch desse tipo deve retornar `needs_codex_repair` com blocker `non_semantic_patch`.

Validacao: `ImplementationWorkflowTests` cobre instrucao sem patch semantico e bloqueio de comentario/TODO.
