# Agente Setimmo - Pacote puro

Este pacote e a versao standalone do Agente Setimmo.

Ele nao depende da API para funcionar. A API pode consumir o agente, mas o agente tambem pode ser usado diretamente por CLI, MCP, Codex, Antigravity ou outro operador local.

## Primeiro uso

1. Extraia a pasta completa.
2. Ajuste `config/paths.json` para os caminhos da sua maquina.
3. Rode:

```powershell
.\dist\agente-setimmo\agente-setimmo.exe --version
.\dist\agente-setimmo\agente-setimmo.exe status --json
.\dist\agente-setimmo\agente-setimmo.exe doctor --json
```

Tambem existe alias compativel:

```powershell
.\dist\agente-setimmo\ragnaforge.exe status --json
```

## Instalar no PATH

Opcionalmente:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\install.ps1
```

Depois abra um novo terminal e use:

```powershell
ragnaforge status --json
```

## Knowledge

As bibliotecas curadas ficam em:

```text
knowledge/packs
knowledge/sources
```

Os comandos `knowledge search`, `knowledge explain`, `knowledge entry`, `knowledge schema` e `knowledge validate` sao read-only.

Apenas `knowledge build` grava o indice controlado em `knowledge/index/knowledge.index.json`.

## Seguranca

- Apply real esta bloqueado.
- Rollback real esta bloqueado.
- MCP nao expoe ferramentas destrutivas.
- GRF original nao deve ser alterada.
- `.lub` bytecode nao deve ser editado.
- rAthena e Patch/client devem ser tratados por dry-run, diff e validacao antes de qualquer decisao humana futura.
