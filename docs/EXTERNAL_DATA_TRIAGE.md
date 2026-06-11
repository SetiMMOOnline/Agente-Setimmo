# Triagem de Dados Externos - Agente Setimmo

Data de atualizacao: 2026-06-11

Este documento explica como o Agente Setimmo interpreta inconsistencias entre dados rAthena, Patch/client e assets Ragnarok.

## Estado Atual

Resultado validado apos a correcao da regra de mapas:

- `validate --json`: 0 issues.
- Erros: 0.
- Warnings: 0.
- Escopo `external-data`: 0.
- Cache de entidades: trusted.
- Modo de lookup de assets client: `loose-files-plus-client-archives`.
- GRFs client detectados no Patch: 4.

## O Que Foi Corrigido

Antes, o validador emitia 1083 warnings de mapas:

- 1078 `MAP_NO_CLIENT_FILES`.
- 5 `MAP_INCOMPLETE_CLIENT`.

Esses warnings eram falsos positivos no perfil atual porque o cliente possui GRFs no Patch. A ausencia de arquivos soltos em `data/` nao prova ausencia real no cliente quando ha containers GRF disponiveis.

O Setimmo agora registra, durante `index --entities`, se existem GRFs no Patch/client. Quando existem GRFs, a validacao de mapas nao transforma ausencia de arquivos soltos em warning. O agente so deve acusar falta de trio client quando tiver evidencia suficiente, nao por nao conseguir enxergar dentro do container.

## Regra Atual

### Sem GRF client detectado

Se nao houver GRF no Patch/client, o Setimmo usa apenas arquivos soltos como evidencia.

Nesse caso:

- mapa do servidor sem `.rsw`, `.gnd` e `.gat` soltos pode gerar `MAP_NO_CLIENT_FILES`;
- mapa com trio solto parcial pode gerar `MAP_INCOMPLETE_CLIENT`.

### Com GRF client detectado

Se houver GRF no Patch/client, o Setimmo nao assume que um mapa esta ausente so porque nao ha arquivo solto correspondente.

Nesse caso:

- `MAP_NO_CLIENT_FILES` nao e emitido somente por ausencia de loose files;
- `MAP_INCOMPLETE_CLIENT` nao e emitido somente por ausencia de loose files complementares;
- a validacao permanece conservadora ate existir um indice real de conteudo GRF.

## Limite Conhecido

O Setimmo integrado ainda nao indexa o conteudo interno de GRFs reais. A integracao atual de GRF e metadata-only e preserva containers originais em read-only.

Portanto, a conclusao correta e:

- nao ha warning comprovado no estado atual;
- nao ha prova automatica de que todos os mapas existem dentro dos GRFs;
- uma validacao mais forte exige um `GRFLocalIndex` ou ferramenta autorizada de listagem de conteudo GRF sem extracao destrutiva.

## Como Validar

Comando recomendado:

```powershell
E:\Ragnarok\Projeto\Agente_Setimmo\dist\agente-setimmo\ragnaforge.exe index --entities --json
E:\Ragnarok\Projeto\Agente_Setimmo\dist\agente-setimmo\ragnaforge.exe validate --json
E:\Ragnarok\Projeto\Agente_Setimmo\dist\agente-setimmo\ragnaforge.exe health --json
```

Resultado esperado:

- `validate`: 0 issues.
- `health`: ok.
- `cacheTrusted`: true.
- `trustedCounts`: true.

## Proximo Passo Recomendado

Criar uma etapa futura de `GRFLocalIndex` read-only para listar nomes de arquivos dentro dos GRFs autorizados, sem extrair payload privado e sem escrever em Patch/client. Isso permitiria diferenciar com precisao:

- mapa presente em arquivo solto;
- mapa presente em GRF;
- mapa realmente ausente;
- trio client parcialmente presente.
