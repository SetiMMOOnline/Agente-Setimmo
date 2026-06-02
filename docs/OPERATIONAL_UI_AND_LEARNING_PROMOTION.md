# Operational UI and Learning Promotion

## Painel operacional

A UI do RagnaForge consome endpoints allowlisted do Agente Setimmo para exibir:

- lessons;
- context packs;
- golden scenarios;
- historico de operacoes;
- auditoria de governanca de producao;
- diferenca entre `supportsApply` e `safeForApply`.

A UI nao abre apply real, rollback real, shell generico ou comando livre.

## Promocao de conhecimento

O fluxo correto e:

```text
candidates -> review -> approval -> promotion
```

Nada e promovido automaticamente.

Cada promocao deve manter:

- operationId;
- origem;
- evidence;
- decisao humana quando exigida;
- trilha de auditoria;
- teste de regressao, golden scenario, lesson ou failure pattern associado.

## Regra de autorizacao

`supportsApply=true` significa apenas que o agente possui capacidade tecnica para preparar uma operacao.

`safeForApply=true` so pode aparecer no contexto de uma operacao concreta com:

- plano;
- diff;
- rollback;
- audit log;
- escopo autorizado;
- validadores aprovados;
- revisao Codex quando exigida.

Status global deve continuar com `safeForApply=false`.
