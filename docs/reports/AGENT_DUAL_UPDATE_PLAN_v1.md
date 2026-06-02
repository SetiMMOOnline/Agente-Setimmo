# AGENT_DUAL_UPDATE_PLAN_v1

## 1. Diretórios analisados
- Embutido: C:\Users\Allis\Desktop\Ragna_Forge\Agente_Setimmo
- Standalone oficial: E:\Ragnarok\Projeto\Agente_Setimmo

## 2. Estado Git/hash
- Embutido: dentro do repositório C:\Users\Allis\Desktop\Ragna_Forge.
- Standalone: sem repositório Git próprio detectado no diretório informado.

## 3. Diferenças estruturais relevantes
O standalone oficial agora e `E:\Ragnarok\Projeto\Agente_Setimmo`. A aplicacao das mudancas deve sincronizar somente arquivos allowlisted para o agente incorporado, sem copiar cache, logs, dist, bin, obj ou configs locais.

## 4. Arquivos alterados em ambos
- Documentos de segurança e knowledge.
- Fontes e packs da Knowledge Library.
- Código de Canon Policy/Validator/Command.
- Integração CLI/doctor/health/baseline.
- Testes de canon e bibliotecas internas.

## 5. Arquivos alterados somente em um
O README leigo é exclusivo do pacote puro.

## 6. Justificativa
Manter paridade operacional entre agente embutido e agente standalone sem sobrescrever configs locais.

## 7. Riscos
O repositório principal já estava em branch feature e tinha arquivo untracked pré-existente. Não houve checkout para main para evitar sobrescrita ou merge implícito.

## 8. Como validar equivalência
Executar build, test, status, doctor, health, baseline, validate e canon check nos dois agentes.

## 9. O que NÃO será sincronizado
Configs locais, caches, logs reais, bin/obj, dist indevido, paths.json local, repositories.local.json, .env e assets privados.

## 10. Confirmação
Não haverá cópia cega de pasta inteira. Apenas arquivos equivalentes e rastreáveis serão aplicados.
