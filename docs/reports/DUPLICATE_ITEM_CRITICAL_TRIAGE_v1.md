# DUPLICATE_ITEM_CRITICAL_TRIAGE_v1

## Objetivo

Registrar a triagem do conflito critico de item duplicado reportado antes desta rodada.

## Diagnostico

O conflito nao era um duplicado real de banco. Era um falso positivo do parser YAML do agente.

Problema antigo:

- o parser lia a linha `- Id: <valor> # comentario`
- o comentario inline permanecia junto do numero
- `int.TryParse(...)` falhava
- o ID virava `0`
- dois itens diferentes caiam como `Id=0`
- a validacao acusava `ITEM_DUPLICATE_ID_SERVER`

## Evidencia local

Arquivo analisado:

- `E:\Ragnarok\Testes\rAthena_teste\db\re\item_db_usable.yml`

Entradas reais:

- `FA_Armor_Reform_1` com `Id: 102396`
- `FA_Armor_Reform_2` com `Id: 102397`

Os nomes eram diferentes e os IDs reais tambem eram diferentes.

## Correcao aplicada

Arquivo do agente corrigido:

- `src/RagnaForge.Agent.Core/Parsing/EntityParsers.cs`

Mudanca:

- o parser agora remove comentario inline depois de `#` antes de converter o ID
- a mesma protecao foi aplicada para item e monster YAML

## Testes adicionados

- `tests/RagnaForge.Agent.Core.Tests/EntityParsersTests.cs`

Casos:

- item com comentario inline no `Id`
- monster com comentario inline no `Id`

## Resultado

- o conflito critico de item duplicado saiu do indice atual
- `knowledge conflicts --entity-type item --json` deixa de reportar esse falso positivo
- o total de issues caiu de `1084` para `1083`
- o `1 error` foi eliminado
- permanecem warnings externos de assets/mapas

## Politica mantida

- nenhuma base real foi editada
- nenhum arquivo de rAthena foi corrigido automaticamente
- nenhuma escrita em Patch/client/GRF/.lub foi feita
- `safeForApply=false` continua obrigatorio
