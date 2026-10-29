#!/usr/bin/env bash
# Prepara o Mac para desenvolver o Ervas do Brasil:
#   Xcode + Simulador (já existentes) → Unity Hub → Unity 6.3 LTS + módulo iOS → ajustes do projeto.
# Uso: ./scripts/setup-mac.sh            (escolhe a 6000.3 LTS mais recente)
#      UNITY_VERSION=6000.3.Xf1 ./scripts/setup-mac.sh
set -euo pipefail
PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
HUB="/Applications/Unity Hub.app/Contents/MacOS/Unity Hub"
ARCH="$(uname -m)"; [ "$ARCH" = "arm64" ] && HUB_ARCH="arm64" || HUB_ARCH="x86_64"

step() { printf "\n\033[1;32m▶ %s\033[0m\n" "$*"; }
warn() { printf "\033[1;33m⚠️  %s\033[0m\n" "$*"; }

step "1/6 Xcode e Simulador"
if ! xcode-select -p >/dev/null 2>&1; then
  warn "Xcode não encontrado. Instale pela App Store e rode: sudo xcode-select -s /Applications/Xcode.app"; exit 1
fi
xcodebuild -version | head -1
SIMS="$(xcrun simctl list devices available | grep -c 'iPhone' || true)"
echo "Simuladores de iPhone disponíveis: $SIMS"
[ "$SIMS" -gt 0 ] || warn "Nenhum simulador de iPhone. Abra o Xcode ▸ Settings ▸ Platforms e baixe o iOS."

step "2/6 Unity Hub"
if [ ! -x "$HUB" ]; then
  if command -v brew >/dev/null 2>&1; then
    brew install --cask unity-hub
  else
    warn "Homebrew não encontrado. Baixe o Unity Hub em https://unity.com/download e rode este script de novo."; exit 1
  fi
fi
echo "Unity Hub OK"

step "3/6 Versão da Unity"
VERSION="${UNITY_VERSION:-}"
if [ -z "$VERSION" ]; then
  VERSION="$("$HUB" -- --headless editors --releases 2>/dev/null | grep -Eo '6000\.3\.[0-9]+f[0-9]+' | sort -V | tail -1 || true)"
fi
if [ -z "$VERSION" ]; then
  VERSION="$("$HUB" -- --headless editors --releases 2>/dev/null | grep -Eo '6000\.[0-9]+\.[0-9]+f[0-9]+' | sort -V | tail -1 || true)"
fi
[ -n "$VERSION" ] || { warn "Não consegui listar as versões. Rode com UNITY_VERSION=6000.3.Xf1 (veja em unity.com/releases)."; exit 1; }
echo "Usando Unity $VERSION ($HUB_ARCH)"

step "4/6 Instalando editor + módulo iOS (pode demorar)"
EDITOR_DIR="/Applications/Unity/Hub/Editor/$VERSION"
if [ ! -d "$EDITOR_DIR" ]; then
  "$HUB" -- --headless install --version "$VERSION" --architecture "$HUB_ARCH" --module ios --childModules
else
  echo "Editor já instalado; garantindo o módulo iOS..."
  "$HUB" -- --headless install-modules --version "$VERSION" --module ios --childModules || true
fi
[ -d "$EDITOR_DIR/PlaybackEngines/iOSSupport" ] && echo "Módulo iOS OK" || warn "Módulo iOS não encontrado — instale pelo Unity Hub ▸ Installs ▸ Add modules ▸ iOS Build Support."

step "5/6 Ajustando o projeto para $VERSION"
printf 'm_EditorVersion: %s\n' "$VERSION" > "$PROJECT_DIR/ProjectSettings/ProjectVersion.txt"
TF_JSON="$EDITOR_DIR/Unity.app/Contents/Resources/PackageManager/BuiltInPackages/com.unity.test-framework/package.json"
if [ -f "$TF_JSON" ]; then
  TF_VER="$(grep -m1 '"version"' "$TF_JSON" | sed -E 's/.*"version": *"([^"]+)".*/\1/')"
  sed -i '' -E "s/(\"com.unity.test-framework\": *\")[^\"]+/\1$TF_VER/" "$PROJECT_DIR/Packages/manifest.json"
  echo "Test Framework fixado em $TF_VER"
fi

step "6/6 Pronto!"
cat <<MSG
Falta só um passo manual (uma vez):
  1. Abra o Unity Hub, faça login com sua Unity ID e ative a licença Personal (Preferences ▸ Licenses).
  2. Em Projects ▸ Add ▸ selecione: $PROJECT_DIR

Depois, pelo terminal:
  ./scripts/lint.sh             # análise estática do C# (precisa do .NET SDK: brew install --cask dotnet-sdk)
  ./scripts/run-tests.sh        # testes unitários do core (EditMode)
  ./scripts/build-ios-sim.sh    # build + abre no Simulador do iPhone
  ./scripts/build-macos.sh      # build do app para macOS

Ou dentro da Unity: menu "Ervas do Brasil" ▸ Criar ou abrir cena principal ▸ Play.
MSG
