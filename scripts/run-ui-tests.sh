#!/usr/bin/env bash
# Testes de interface (PlayMode, Assets/Tests/PlayMode): a Unity gera um app de teste do macOS com o jogo e os testes
# e abre esse app. Uma janela do jogo aparece por cerca de um minuto enquanto uma jogadora simulada joga:
# anda com o joystick virtual, aperta E/I/B/Esc e clica nos botões da tela.
#   resultado: Builds/TestResults-PlayMode.xml · log: Builds/unity-ui-tests.log · fotos de cada passo: Builds/ui-tests/
# Como o run-tests.sh e o build-macos.sh, não roda com o editor da Unity aberto neste projeto.
set -uo pipefail
source "$(dirname "$0")/unity-env.sh"
RESULTS="$PROJECT_DIR/Builds/TestResults-PlayMode.xml"
LOG="$PROJECT_DIR/Builds/unity-ui-tests.log"
SHOTS="$PROJECT_DIR/Builds/ui-tests"

# o app de teste tem a mesma empresa e o mesmo produto do jogo: grava em Application.persistentDataPath/ui-tests
setting() { sed -n "s/^  $1: //p" "$PROJECT_DIR/ProjectSettings/ProjectSettings.asset"; }
APP_DATA="$HOME/Library/Application Support/$(setting companyName)/$(setting productName)/ui-tests"

rm -rf "$APP_DATA" "$SHOTS" "$RESULTS"
echo "▶ Testes de interface: gerando o app de teste e jogando (uma janela do jogo vai abrir)..."
"$UNITY" -batchmode -projectPath "$PROJECT_DIR" -runTests -testPlatform StandaloneOSX \
  -testResults "$RESULTS" -logFile "$LOG"
CODE=$?

if [ -f "$RESULTS" ]; then
  /usr/bin/python3 - "$RESULTS" <<'PY'
import sys, xml.etree.ElementTree as ET
r = ET.parse(sys.argv[1]).getroot()
print(f"Total: {r.get('total')}  ✅ {r.get('passed')}  ❌ {r.get('failed')}  ⏭  {r.get('skipped')}")
for tc in r.iter('test-case'):
    ok = tc.get('result') == 'Passed'
    print(f"  {'✅' if ok else '❌'} {tc.get('name')} ({float(tc.get('duration', 0)):.1f} s)")
    out = tc.find('output')
    for line in (out.text or '').splitlines() if out is not None else []:
        if '[Desempenho]' in line:  # o teste de FPS deixa a nota (ruim / regular / bom / excelente) no log
            print(f"     📊 {line.split('[Desempenho] ', 1)[-1].strip()}")
    if not ok:
        msg = tc.find('./failure/message')
        print(f"     {(msg.text or '').strip()[:300] if msg is not None else ''}")
PY
else
  echo "Sem arquivo de resultados. Veja o log: $LOG"; tail -40 "$LOG"
fi

if [ -f "$APP_DATA/desempenho.txt" ]; then
  mkdir -p "$SHOTS"
  cp "$APP_DATA/desempenho.txt" "$SHOTS/desempenho.txt"
  echo "📊 $(head -1 "$APP_DATA/desempenho.txt")"
  tail -n +2 "$APP_DATA/desempenho.txt"
fi
if [ -d "$APP_DATA/fotos" ]; then
  mkdir -p "$SHOTS"
  cp -R "$APP_DATA/fotos/." "$SHOTS/"
  echo "📸 $(find "$SHOTS" -name '*.png' | wc -l | tr -d ' ') fotos em Builds/ui-tests/"
fi
rm -rf "$APP_DATA"
exit $CODE
