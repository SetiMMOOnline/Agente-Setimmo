# FrontendDependenciesIncomplete

Sintoma: `tsc`, `vite`, `typescript` ou `playwright` ausentes/localmente quebrados.

Causa comum: `node_modules` removido por limpeza segura ou incompleto apos pacote limpo.

Correcao: restaurar pelo lockfile com `npm.cmd ci` dentro de `frontend`.

Regra: nao usar fallback de `npm exec` que instala pacote errado como `tsc@2`.
