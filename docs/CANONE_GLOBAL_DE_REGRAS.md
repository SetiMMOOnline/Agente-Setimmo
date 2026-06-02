# Cânone Global de Regras

## 0. Diretriz Suprema
Nenhuma ação deve quebrar, sobrescrever, remover, enfraquecer ou comprometer algo que já funciona. Antes de inovar, preservar. Antes de alterar, entender. Antes de executar, validar. Antes de concluir, provar.

## 1. Protocolo de Pré-Execução
Toda tarefa deve verificar escopo, riscos, arquivos sensíveis, comandos destrutivos, necessidade de dry-run, diff, backup, rollback plan e evidência final antes de qualquer mudança.

## 2. Regra de Escopo Fechado
Toda tarefa deve declarar objetivo, diretórios permitidos, arquivos que podem ser alterados, arquivos proibidos, comandos permitidos, comandos proibidos, testes e critérios de aceite.

## 3. Modo Seguro por Padrao
O agente opera em modo read-only por padrao. Dry-run e diff-preview sao permitidos. Apply e rollback genericos continuam bloqueados. Apply e rollback operation-scoped so podem ocorrer dentro de `writableRoots`, com validadores, diff, rollback plan e confirmacao explicita. Edicao de GRF, rAthena, Patch/client e `.lub` permanece bloqueada.

## 4. Política de Arquivos e Diretórios
Não criar estrutura paralela sem necessidade técnica. Não criar arquivos duplicados com nomes vagos. Não escrever em diretórios sensíveis. Não apagar sem autorização explícita.

## 5. Política de Backup, Dry-run e Diff
Mudança com risco real exige simulação, diff, validação e plano de recuperação. Backup é obrigatório quando houver risco de perda.

## 6. Política de Comandos Destrutivos
Comandos destrutivos são bloqueados por padrão. Remoção de artefatos regeneráveis só é aceita com lista explícita, justificativa e validação.

## 7. Protocolo de Análise Sistêmica
Erros devem ser analisados por padrão, causa comum, lote afetado, riscos de regressão e testes que provem a correção.

## 8. Protocolo “Valide Tudo”
“Valide tudo” exige revisão estrutural, segurança, qualidade, testes, arquivos sensíveis, apply/rollback, path traversal, logs, caches e artefatos proibidos.

## 9. Smoke Test Obrigatório
Antes de operação em massa, executar amostras mínimas representativas quando aplicável.

## 10. Política de Versionamento
Git deve ser verificado antes e depois. Commits devem ser pequenos, rastreáveis, explicáveis e sem lixo, segredo, cache real, logs reais, dumps ou assets privados.

## 11. Política de Dados Sensíveis
Segredos, tokens, .env, repositories.local.json, paths locais, dumps, raw HTML externo e assets privados nunca devem ser expostos, copiados, versionados ou logados indevidamente.

## 12. Política de Automações e Scripts
Scripts devem ter escopo claro, modo seguro, validação de caminho e tratamento de erro. Shell genérico e comando livre são proibidos.

## 13. Política de Documentação e Logs
Tarefa relevante deve registrar relatório em docs/reports. Logs reais e caches reais não devem ser versionados.

## 14. Política de Dependências
Dependência nova exige justificativa. Build, teste e validação não podem depender de internet sem decisão explícita.

## 15. Política de Refatoração
Refatoração deve preservar comportamento existente e ter testes proporcionais ao risco.

## 16. Política de Banco de Dados e Dados em Lote
Dados em lote exigem schema, simulação, validação e backup quando houver risco real de perda.

## 17. Política de Interface e Experiência do Usuário
UI deve preservar fluxos existentes. Não criar botão apply, rollback, reparar tudo ou fluxo destrutivo.

## 18. Política de Erros
Erros não devem ser escondidos. Blocker não pode ser mascarado como warning.

## 19. Política de Qualidade Final
Entrega só termina com objetivo atendido, testes executados, riscos documentados e evidência verificável.

## 20. Formato Obrigatório de Relatório Final
Relatório final deve conter objetivo, escopo, diretório, branch/hash, arquivos lidos/alterados/criados/removidos, comandos, build, testes, smoke, auditoria de segurança, riscos, pendências e veredito.

## 21. Kill-switch
Se o usuário disser Para, Pare, Cancela, Abortar, Interrompa, Stop ou Cancel, a execução deve parar imediatamente e aguardar nova instrução.

## 22. Instrucao Final para IA
Nao quebre, nao apague, nao invente, nao oculte, nao assuma e nao execute no escuro. No RagnaForge, o agente existe para pipeline seguro: scan, resolucao de dependencia, review, dry-run, validacao, diff-preview, relatorio, apply controlado e rollback controlado somente quando os validadores aprovarem.
