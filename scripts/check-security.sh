#!/usr/bin/env bash
# Confere no app do macOS já gerado que o jogo não abre a rede (docs/15-seguranca.md):
#   1/3 boot.config sem a conexão do Profiler (build de desenvolvimento escuta na porta 55000 e anuncia IP e nome do Mac)
#   2/3 com o jogo aberto, observa os sockets do processo (lsof) por alguns segundos: nenhuma porta, nenhuma conexão
#   3/3 Player.log sem o anúncio "Player connection" na rede local
# Uso: ./scripts/check-security.sh   (se o jogo estiver aberto, examina o processo que já está rodando e não o fecha;
#                                     senão abre o app, examina desde a abertura e fecha)
set -uo pipefail
PROJECT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
APP="$PROJECT_DIR/Builds/macOS/ErvasDoBrasil.app"
LOG="$HOME/Library/Logs/Douglas Queiroz/Ervas do Brasil/Player.log"
SECONDS_TO_WATCH=10
FAIL=0

[ -d "$APP" ] || { echo "❌ App não encontrado em $APP. Gere com ./scripts/build-macos.sh"; exit 1; }

echo "▶ 1/3 boot.config do app..."
if grep -q '^player-connection' "$APP/Contents/Resources/Data/boot.config"; then
  grep '^player-connection' "$APP/Contents/Resources/Data/boot.config" | sed 's/^/   /'
  echo "❌ Build de desenvolvimento: o app abre a conexão do Profiler na rede."; FAIL=1
else
  echo "✅ Sem conexão do Profiler."
fi

PID="$(pgrep -f "$APP/Contents/MacOS/" | head -1 || true)"
LAUNCHED=0
if [ -z "$PID" ]; then
  open -n "$APP"
  for _ in $(seq 1 40); do PID="$(pgrep -f "$APP/Contents/MacOS/" | head -1 || true)"; [ -n "$PID" ] && break; sleep 0.25; done
  [ -n "$PID" ] || { echo "❌ O app não abriu."; exit 1; }
  LAUNCHED=1
  echo "▶ 2/3 Abri o jogo (pid $PID); observando a rede por ${SECONDS_TO_WATCH}s desde a abertura..."
else
  echo "▶ 2/3 O jogo já está aberto (pid $PID); observando a rede por ${SECONDS_TO_WATCH}s (não vou fechá-lo)..."
fi

SOCKETS="$(for _ in $(seq 1 $((SECONDS_TO_WATCH * 4))); do lsof -nP -a -p "$PID" -i 2>/dev/null | tail -n +2; sleep 0.25; done | sort -u)"
if [ -n "$SOCKETS" ]; then
  printf '%s\n' "$SOCKETS" | sed 's/^/   /'
  echo "❌ O jogo abriu sockets de rede."; FAIL=1
else
  echo "✅ Nenhuma porta escutando e nenhuma conexão."
fi

echo "▶ 3/3 Player.log..."
if grep -qE 'Player connection|Multi-casting' "$LOG" 2>/dev/null; then
  grep -E 'Player connection|Multi-casting' "$LOG" | head -3 | sed 's/^/   /'
  echo "❌ O jogo se anunciou na rede local."; FAIL=1
else
  echo "✅ Nenhum anúncio na rede."
fi

[ "$LAUNCHED" = 1 ] && osascript -e 'quit app "Ervas do Brasil"' >/dev/null 2>&1
if [ "$FAIL" = 0 ]; then echo "✅ Segurança de rede OK"; exit 0; fi
exit 1
