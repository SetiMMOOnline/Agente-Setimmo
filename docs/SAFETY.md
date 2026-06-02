# SAFETY

## Principio central

O Agente Setimmo nao trabalha por permissao cega. Ele trabalha por validadores.

Isso significa:

- leitura e analise primeiro
- diff e rollback antes do apply
- confirmacao explicita para apply e rollback
- bloqueio imediato para alvos proibidos
- nenhuma escrita fora de `agentRoot` e `writableRoots`

## Modos

- `Observe`
- `Plan`
- `DryRun`
- `Apply`
- `Rollback`
- `Production`

`Production` nao e permissao global. Ele e uma promocao formal por operacao, bloqueada ate existir aprovacao humana, hash de diff valido, rollback e escopo autorizado.

## O que pode ficar `true`

- `safeForReadOnlyWork`
- `safeForDryRun`
- `safeForApply`
- `safeForProductionApply`, somente para uma operacao especifica aprovada e vinculada ao hash atual do diff

## O que continua bloqueado

- escrita em rAthena
- escrita em Patch/client
- escrita em GRF
- edicao `.lub`
- shell generico
- comando livre
- path traversal
- qualquer write fora de `writableRoots`

## Quando o agente pode aplicar

O agente so pode aplicar quando:

- existe plano
- existe diff
- existe rollback
- o alvo esta dentro do escopo permitido
- o conteudo final passa pelos validadores
- nao ha segredo detectado
- nao ha shell generico
- ha confirmacao explicita

## Quando production pode aplicar

Production so pode aplicar quando:

- a operacao foi criada pelo proprio agente;
- existe manifest em `logs/operations`;
- existe diff em `logs/diffs`;
- existe rollback em `logs/rollbacks`;
- o diff atual bate com o hash aprovado;
- a aprovacao humana nao expirou;
- o path guard autoriza todos os arquivos afetados;
- a operacao nao toca arquivos sensiveis ou assets privados;
- a auditoria local foi registrada.

## Quando o agente deve bloquear

- path traversal
- caminho absoluto em entrada MCP/CLI quando o fluxo exige relativo
- GRF, Patch/client, rAthena ou `.lub`
- production sem aprovacao humana
- production com hash de diff diferente do aprovado
- diff invalido
- ausencia de rollback
- segredo detectado
- operacao destrutiva sem politica
- tentativa de shell arbitrario

## MCP dry-run controlado

O MCP `dry-run` persiste entradas locais somente quando necessario para rastreabilidade:

- pasta: `inputs/dry-run`
- limite de tamanho
- boundary dentro do `agentRoot`
- limpeza de expirados
- audit log em `logs/operations`

Isso nunca autoriza escrita externa.

## Cleanup seguro

`cleanup --safe` remove apenas artefatos regeneraveis:

- `bin`
- `obj`
- `TestResults`
- `*.trx`
- `*.tsbuildinfo`
- cache local opcional
- logs locais opcionais
- inputs de dry-run opcionais

Nunca:

- codigo-fonte
- docs
- knowledge curado
- examples
- configs example
- dados externos sensiveis

## Resumo operacional

- `safeForApply` deixou de ser hardcoded
- apply e rollback existem apenas para operacoes do proprio agente
- production foi promovido de conceito documental para fluxo formal com validadores
- GRF_Extractor e integrado em modo metadata-only controlado
- o agente segue proibido de tocar o ecossistema externo do servidor
