# KNOWLEDGE

## Papel da Knowledge Library

A Knowledge Library do Agente Setimmo existe para:

- buscar contexto
- explicar provenance
- gerar hints
- gerar conflitos
- medir coverage
- planejar criacao em dry-run
- classificar risco
- gerar relatorios humanos

Ela nao substitui:

- dados locais do projeto
- rAthena local
- revisao humana

## Fontes

Fontes principais nesta etapa:

- `global-canon`
- `divine-pride`
- `ratemyserver`
- `rathena-board`
- `robrowserlegacy`
- `robrowserlegacy-remoteclient-js`
- packs locais curados de rAthena e assets

## Diferenca entre biblioteca, fonte online, snapshot e learning candidate

- biblioteca interna: source/pack versionado no repositrio
- fonte online: origem allowlisted usada apenas em metadata-only ou controlled-point
- snapshot: metadata minima e sanitizada, nunca dump
- learning candidate: observacao review-first, nunca verdade absoluta
- authorized code reference: fonte GitHub autorizada para analise, adapter design e futura incorporacao seletiva com provenance

## Ranking de confianca

Ordem pratica:

1. LocalProjectConfig
2. LocalProjectData
3. rAthenaLocal
4. PatchClientLocal
5. GRFLocalIndex
6. ValidatedInternalKnowledge
7. InternalKnowledge
8. DivinePrideInternalReference
9. RateMyServerInternalReference
10. ControlledLiveReference
11. UnverifiedReference

## Hints

Hints agora possuem:

- `id`
- `entityType`
- `entityId`
- `entityName`
- `severity`
- `category`
- `message`
- `explanation`
- `localEvidence`
- `referenceEvidence`
- `winningSource`
- `losingSources`
- `provenance`
- `confidence`
- `priority`
- `humanReviewRecommended`
- `blocksReadOnly`
- `blocksDryRun`
- `blocksApply`
- `suggestedAction`
- `reasonNotBlocking`
- `generatedAt`

## Conflict policy

Regras:

- fonte externa sozinha nao cria blocker fatal
- local vence externo por padrao
- custom override reduz severidade contextual
- critical exige evidencia local
- `safeForApply` e validator-governed; knowledge sozinha nao autoriza escrita externa

## Coverage

`knowledge coverage --json` responde:

- contagem local por entidade
- contagem de referencia interna por entidade
- hints estimados
- conflitos
- coverage ratio
- packs stale/deprecated
- tipos com baixa cobertura

Coverage amplo nao usa live lookup.

## Sources, refresh e snapshots

Comandos:

```powershell
ragnaforge knowledge sources --json
ragnaforge knowledge source explain --id rathena-board --json
ragnaforge knowledge refresh plan --json
ragnaforge knowledge refresh due --json
ragnaforge knowledge refresh run --source robrowserlegacy --mode metadata --json
ragnaforge knowledge snapshots --json
```

Poltica desta etapa:

- `rathena-board`: metadata-only e pode retornar `skipped_by_policy`
- `robrowserlegacy`: GitHub metadata/readme/tree com autorizacao informada pelo usuario
- `robrowserlegacy-remoteclient-js`: GitHub metadata/readme/tree com autorizacao informada pelo usuario
- sem crawler
- sem paginação de forum
- sem raw HTML
- sem dump
- sem cache real no Git

## Learning

Comandos:

```powershell
ragnaforge knowledge learn candidates --json
ragnaforge knowledge learn report --format md
ragnaforge knowledge learn observe --source robrowserlegacy --topic "browser stack" --summary "..." --json
ragnaforge knowledge learn approve --id learning-robrowserlegacy-browser-stack --dry-run --json
ragnaforge knowledge learn reject --id learning-rathena-board-routing --reason "..." --json
ragnaforge knowledge learn promote --id learning-remoteclient-grf-pipeline --dry-run --json
```

Regras:

- nao e treinamento
- nao e self-modification
- exige review
- nao armazena segredo
- nao armazena raw HTML
- pode registrar observacoes tecnicas autorizadas de roBrowser

## Packs e freshness

Cada pack suporta ou infere:

- `id`
- `version`
- `schemaVersion`
- `title`
- `description`
- `theme`
- `status`
- `reviewedAt`
- `reviewedBy`
- `sourcePriority`
- `supportedEntityTypes`
- `changelog`
- `freshnessPolicy`
- `deprecationReason`
- `provenance`
- `trustPolicy`
- `conflictPolicy`

Comandos:

```powershell
ragnaforge knowledge packs --json
ragnaforge knowledge pack explain --id global-canon --json
ragnaforge knowledge pack validate --id global-canon --json
ragnaforge knowledge freshness --json
```

## Live point lookup

O contrato existe, mas o request real segue bloqueado por policy nesta build.

Mesmo quando um lookup seria util:

- `requestCount` fica `0`
- `linksFollowed=false`
- `bulkLookup=false`
- `rangeLookup=false`
- `rawHtmlStored=false`
- `dumpStored=false`
- `cacheMode=none` por padrao

## Custom / progressive

O agente marca contexto custom quando ha evidencia local, por exemplo em `db/import`.

Efeitos:

- divergencia com referencia externa pode ser esperada
- `episodeGate` pode retornar `customAllowed`
- acoes seguintes continuam em dry-run e revisao humana
