# API_UI_AGENT_CONTRACTS_v1

## Objetivo

Este documento define os contratos JSON estaveis que a futura API/UI tabulada deve consumir do Agente Setimmo.

A interface nao deve parsear texto livre. Ela deve ler os campos JSON ja normalizados.

## Regras gerais

- todos os contratos sao serializaveis em JSON
- os contratos sao read-only, dry-run ou validator-governed quando o fluxo for de implementacao
- `safeForApply` e validator-governed; apply generico continua bloqueado
- referencias externas nunca bloqueiam sozinhas
- dados locais tem prioridade

## 1. Busca de entidade

Contrato: `AgentEntityLookupContract`

Campos principais:

- `entityType`
- `query`
- `localEntity`
- `hints`
- `conflicts`
- `risk`
- `provenance`
- `externalReference`
- `episodeGate`
- `nextSafeActions`
- `safeForReadOnlyWork`
- `safeForDryRun`
- `safeForApply`

CLI tipico:

```powershell
ragnaforge find item --id 501 --with-knowledge --json
```

## 2. Hints

Contrato: `AgentKnowledgeHintContract`

Campos:

- `id`
- `severity`
- `category`
- `message`
- `explanation`
- `provenance`
- `humanReviewRecommended`
- `blocksReadOnly`
- `blocksDryRun`
- `blocksApply`
- `reasonNotBlocking`

## 3. Conflitos

Contrato: `AgentConflictContract`

Campos:

- `entityType`
- `entityId`
- `severity`
- `riskLevel`
- `explanation`
- `humanReviewRecommended`
- `blocksReadOnly`
- `blocksDryRun`
- `blocksApply`
- `reasonNotBlocking`
- `nextSafeAction`

## 4. Coverage

Contrato: `AgentCoverageContract`

Campos:

- `entityType`
- `localCount`
- `internalReferenceCount`
- `withHints`
- `conflicts`
- `coverageRatio`

## 5. Triage

Saida agregada de `triage --external-data`:

- `totalIssues`
- `totalErrors`
- `totalWarnings`
- `byCategory`
- `byRisk`
- `byEntityType`
- `topCritical`
- `topHigh`
- `duplicateItems`
- `missingAssets`
- `episodeDependencies`
- `customOverrideCandidates`
- `issues`
- `liveLookupDecisions`
- `nextSafeActions`
- `safeForReadOnlyWork`
- `safeForDryRun`
- `safeForApply`

## 6. Plan create dry-run

Contrato: `AgentEntityPlanContract`

Campos:

- `requestedEntity`
- `normalizedEntity`
- `requiredFields`
- `missingRequiredFields`
- `idConflictCheck`
- `idSuggestions`
- `safeIdRanges`
- `localMatches`
- `knowledgeMatches`
- `referenceContext`
- `controlledLiveReference`
- `assetHints`
- `dependencyHints`
- `episodeGate`
- `riskLevel`
- `conflicts`
- `warnings`
- `nextSafeActions`
- `dryRunPlan`
- `diffPreviewPlaceholder`
- `humanReviewRequired`
- `canApply`
- `safeForReadOnlyWork`
- `safeForDryRun`
- `safeForApply`

## 7. Provenance

Contrato: `AgentProvenanceContract`

Campos:

- `sourceId`
- `sourceName`
- `sourceKind`
- `origin`
- `externalReferenceUrl`
- `reviewedAt`
- `retrievedAt`
- `confidence`
- `priority`
- `trustPolicy`
- `conflictPolicy`
- `canBlock`
- `reasonNotBlocking`

## 8. Ranking de confianca

A UI deve usar:

- `priority`
- `confidence`
- `sourceKind`
- `trustPolicy`

Ela nao deve promover referencia externa acima de dado local.

## 9. Episode gate

Contrato: `AgentEpisodeGateContract`

Status possiveis nesta etapa:

- `allowed`
- `future`
- `customAllowed`
- `unknown`
- `blocked`
- `missingDependency`

## 10. Risk level

Contrato: `AgentRiskContract`

Campos:

- `level`
- `reason`

## 11. Next safe actions

Todo contrato de busca, triagem ou plano deve retornar `nextSafeActions` para a UI sugerir o proximo passo sem abrir escrita real.

## 12. Markdown report

Contrato: `AgentReportContract`

Campos:

- `reportType`
- `format`
- `markdown`
- `warnings`
- `safeForReadOnlyWork`
- `safeForDryRun`
- `safeForApply`

## 13. Controlled external reference

Contrato: `AgentExternalReferenceContract`

Campos:

- `liveLookupDecision`
- `decisionReason`
- `source`
- `requestCount`
- `timeoutMs`
- `rateLimitApplied`
- `linksFollowed`
- `bulkLookup`
- `rangeLookup`
- `rawHtmlStored`
- `dumpStored`
- `cacheMode`
- `warning`
- `reasonNotBlocking`

Nesta build, a decisao pode existir sem request real, retornando `unavailable_by_policy`.

## 14. Safety flags

Contrato: `AgentValidationSummaryContract`

Campos:

- `canApply`
- `safeForReadOnlyWork`
- `safeForDryRun`
- `safeForApply`
- `readOnlyMode`
- `applyEnabled`
- `rollbackEnabled`

## 15. Knowledge sources

Contrato: `AgentKnowledgeSourceContract`

Campos:

- `sourceId`
- `name`
- `sourceType`
- `externalReferenceUrl`
- `supportedTopics`
- `supportedEntityTypes`
- `updateMode`
- `refreshPolicy`
- `licenseNotes`
- `authorizedUse`
- `canBlock=false`

## 16. Source refresh

Contrato: `AgentKnowledgeRefreshContract`

Campos:

- `sourceId`
- `mode`
- `status`
- `requestCount`
- `timeoutMs`
- `rateLimitApplied`
- `linksFollowed`
- `paginationUsed`
- `bulkLookup`
- `rangeLookup`
- `rawHtmlStored`
- `dumpStored`
- `cacheMode`
- `updateDetected`
- `warning`

## 17. Source snapshots

Contrato: `AgentSourceSnapshotContract`

Campos:

- `id`
- `sourceId`
- `sourceVersion`
- `retrievedAt`
- `sanitized`
- `rawStored`
- `updateDetected`
- `summary`
- `warnings`

## 18. Learning candidates

Contrato: `AgentLearningCandidateContract`

Campos:

- `id`
- `sourceId`
- `topic`
- `summary`
- `status`
- `humanReviewRequired`
- `licenseNotes`
- `authorizedUse`
- `safeForApply` (sempre `false` para learning candidates nesta build)

## 19. License and authorization notes

Contratos:

- `AgentLicenseNoteContract`
- `AgentAuthorizedUseContract`

Uso previsto na UI:

- painel de fontes
- painel de permission notes
- painel de authorized code references
- painel de snapshots e freshness
- painel de learning candidates
## 20. Bootstrap da futura API/UI

Use:

```powershell
ragnaforge export api-readiness --json
```

Esse export lista:

- comandos suportados
- tipos suportados
- features suportadas
- tools MCP suportadas
- schemas JSON
- endpoints recomendados
- abas recomendadas
- paines recomendados
