#!/usr/bin/env bash
# Roda os testes EditMode (core do jogo) em modo batch e mostra o resumo.
set -uo pipefail
source "$(dirname "$0")/unity-env.sh"
RESULTS="$PROJECT_DIR/Builds/TestResults-EditMode.xml"
LOG="$PROJECT_DIR/Builds/unity-tests.log"
echo "▶ Rodando testes EditMode com $UNITY ..."
"$UNITY" -batchmode -nographics -projectPath "$PROJECT_DIR" -runTests -testPlatform EditMode \
  -testResults "$RESULTS" -logFile "$LOG"
CODE=$?
if [ -f "$RESULTS" ]; then
  /usr/bin/python3 - "$RESULTS" <<'PY'
import sys, xml.etree.ElementTree as ET
r = ET.parse(sys.argv[1]).getroot()
print(f"Total: {r.get('total')}  ✅ {r.get('passed')}  ❌ {r.get('failed')}  ⏭  {r.get('skipped')}")
for tc in r.iter('test-case'):
    if tc.get('result') == 'Failed':
        msg = tc.find('./failure/message')
        print(f"  ❌ {tc.get('fullname')}: {(msg.text or '').strip()[:200] if msg is not None else ''}")
PY
else
  echo "Sem arquivo de resultados. Veja o log: $LOG"; tail -40 "$LOG"
fi
exit $CODE
