#!/usr/bin/env bash
# Gera o app para macOS e abre.
# Uso: ./scripts/build-macos.sh         release: sem Profiler e sem porta aberta na rede
#      ./scripts/build-macos.sh --dev   desenvolvimento: Profiler e erros na tela, mas escuta na porta 55000 (docs/15-seguranca.md)
set -euo pipefail
source "$(dirname "$0")/unity-env.sh"
DEV=""; [ "${1:-}" = "--dev" ] && DEV="-devBuild"
echo "▶ Unity: build macOS ($([ -n "$DEV" ] && echo desenvolvimento || echo release))..."
if ! "$UNITY" -batchmode -nographics -projectPath "$PROJECT_DIR" \
     -executeMethod ErvasDoBrasil.EditorTools.BuildTools.CI_BuildMac $DEV \
     -logFile "$PROJECT_DIR/Builds/unity-macos.log"; then
  echo "❌ Build falhou. Últimas linhas do log:"; tail -40 "$PROJECT_DIR/Builds/unity-macos.log"; exit 1
fi
open "$PROJECT_DIR/Builds/macOS/ErvasDoBrasil.app"
echo "✅ App aberto."
