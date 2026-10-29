# Ambiente de desenvolvimento (Mac)

## Pré-requisitos
| Item | Situação |
|---|---|
| macOS (Apple Silicon ou Intel) | ✅ |
| Xcode + Simulador iOS | ✅ já instalado (usado pelo projeto Flutter) |
| Homebrew | recomendado (instala o Unity Hub) |
| Conta Unity (Unity ID) | grátis, para a licença Personal |

## Setup automático
```bash
cd ~/Dev/ErvasDoBrasil_2D
./scripts/setup-mac.sh
```
O script:
1. confere o Xcode e lista os simuladores de iPhone
2. instala o **Unity Hub** (`brew install --cask unity-hub`) se faltar
3. escolhe a **Unity 6.3 LTS** mais recente (ou a de `UNITY_VERSION=6000.3.Xf1`)
4. instala o editor + **iOS Build Support** (o build para Mac já vem junto)
5. grava a versão em `ProjectSettings/ProjectVersion.txt` e alinha a versão do Test Framework com a do editor

## Passo manual (uma vez)
1. Abra o **Unity Hub** e faça login.
2. Vá em **Preferences ▸ Licenses ▸ Add ▸ Get a free personal license**.
3. Em **Projects ▸ Add ▸ Add project from disk**, escolha `~/Dev/ErvasDoBrasil_2D`.

> A primeira abertura demora alguns minutos, porque a Unity gera as pastas `Library/` e `ProjectSettings/` completas.

## Setup manual (alternativa)
1. Instale o Unity Hub (unity.com/download).
2. **Installs ▸ Install Editor ▸ 6000.3.x LTS** com o módulo **iOS Build Support**.
3. Adicione o projeto no Hub. Se a versão for diferente, aceite a troca de versão.

## Problemas comuns
| Sintoma | Solução |
|---|---|
| `Unity 6 não encontrada` nos scripts | rode o `setup-mac.sh` ou exporte `UNITY_PATH=/Applications/Unity/Hub/Editor/<versão>/Unity.app/Contents/MacOS/Unity` |
| Batchmode falha por licença | abra o Hub uma vez e ative a licença Personal |
| Erro de pacote `com.unity.test-framework` | abra o Package Manager, remova e adicione o *Test Framework* (ou rode o `setup-mac.sh` de novo) |
| Aviso "Active Input Handling ajustado" | reinicie a Unity (o protótipo usa o Input Manager clássico) |
| Nenhum simulador listado | Xcode ▸ Settings ▸ Platforms ▸ baixar iOS |
