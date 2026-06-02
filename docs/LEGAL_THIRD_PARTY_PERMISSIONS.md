# LEGAL_THIRD_PARTY_PERMISSIONS

Data do registro: 2026-05-23

## Autorizacao informada pelo usuario

O usuario informou que conversou com Dodo e recebeu autorizacao para usar os projetos abaixo no contexto do RagnaForge:

- `https://github.com/MrAntares/roBrowserLegacy`
- `https://github.com/FranciscoWallison/roBrowserLegacy-RemoteClient-JS`

## Escopo autorizado

- analisar README, docs, estrutura e arquitetura
- coletar metadata publica do GitHub
- criar knowledge packs internos
- criar adapters proprios inspirados na arquitetura
- registrar provenance, creditos e licenca
- incorporar seletivamente no futuro somente com rastreabilidade

## Limites mantidos

- nao copiar assets privados
- nao copiar credenciais ou configs sensiveis
- nao versionar clone inteiro por acidente
- nao baixar binarios externos para dentro do Git sem necessidade clara
- nao misturar codigo externo sem provenance

## Politica de licenca

- a etapa nao bloqueia apenas por GPL/GPL-3.0 para essas duas fontes especificas
- qualquer uso concreto deve preservar URL, autoria identificavel, licenca declarada e arquivos de origem consultados
- se houver incorporacao futura, registrar LICENSE/NOTICE/provenance

## Riscos restantes

- autorizacao foi informada pelo usuario e nao validada por documento externo nesta etapa
- incorporacao futura ainda exige auditoria manual de provenance e escopo

## Como auditar uso futuro

- revisar `docs/THIRD_PARTY_CODE_PROVENANCE.md`
- revisar knowledge packs e source snapshots relacionados
- confirmar que nenhum clone inteiro, `node_modules`, cache real ou assets privados entrou no Git
