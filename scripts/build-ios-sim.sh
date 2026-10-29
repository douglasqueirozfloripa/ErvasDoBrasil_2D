#!/usr/bin/env bash
# Gera o projeto Xcode (SDK do Simulador), compila e abre o jogo no Simulador do iPhone.
# Uso: ./scripts/build-ios-sim.sh            (usa o iPhone já ligado ou o primeiro disponível)
#      SIM_NAME="iPhone 16" ./scripts/build-ios-sim.sh
#      ./scripts/build-ios-sim.sh --dev   build de desenvolvimento: Profiler, mas escuta na rede (docs/15-seguranca.md)
set -euo pipefail
source "$(dirname "$0")/unity-env.sh"
DEV=""; [ "${1:-}" = "--dev" ] && DEV="-devBuild"
BUNDLE_ID="com.douglasqueiroz.ervasdobrasil"
OUT="$PROJECT_DIR/Builds/iOS-Simulator"
DERIVED="$PROJECT_DIR/Builds/DerivedData"

echo "▶ 1/4 Unity: exportando projeto Xcode (SDK do simulador, $([ -n "$DEV" ] && echo desenvolvimento || echo release))..."
if ! "$UNITY" -batchmode -nographics -projectPath "$PROJECT_DIR" \
     -executeMethod ErvasDoBrasil.EditorTools.BuildTools.CI_BuildIosSimulator $DEV \
     -logFile "$PROJECT_DIR/Builds/unity-ios.log"; then
  echo "❌ Build da Unity falhou. Últimas linhas do log:"; tail -40 "$PROJECT_DIR/Builds/unity-ios.log"; exit 1
fi

echo "▶ 2/4 Xcode: compilando para iphonesimulator..."
xcodebuild -project "$OUT/Unity-iPhone.xcodeproj" -scheme Unity-iPhone -configuration Debug \
  -sdk iphonesimulator -destination "generic/platform=iOS Simulator" -derivedDataPath "$DERIVED" \
  CODE_SIGNING_ALLOWED=NO build | tail -5
APP="$(find "$DERIVED/Build/Products/Debug-iphonesimulator" -maxdepth 1 -name '*.app' | head -1)"
[ -n "$APP" ] || { echo "❌ .app não encontrado"; exit 1; }

echo "▶ 3/4 Simulador..."
UDID="$(xcrun simctl list devices available -j | /usr/bin/python3 -c '
import json, os, sys
want = os.environ.get("SIM_NAME", "")
devs = [d for rt, ds in json.load(sys.stdin)["devices"].items() if "iOS" in rt for d in ds if "iPhone" in d["name"]]
pick = [d for d in devs if d["name"] == want] or [d for d in devs if d["state"] == "Booted"] or devs
print(pick[-1]["udid"] if pick else "")')"
[ -n "$UDID" ] || { echo "❌ Nenhum simulador de iPhone disponível"; exit 1; }
xcrun simctl boot "$UDID" 2>/dev/null || true
open -a Simulator

echo "▶ 4/4 Instalando e abrindo $APP"
xcrun simctl install "$UDID" "$APP"
xcrun simctl launch "$UDID" "$BUNDLE_ID"
echo "✅ Ervas do Brasil rodando no simulador."
