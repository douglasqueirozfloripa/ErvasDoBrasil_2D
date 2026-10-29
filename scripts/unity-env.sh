#!/usr/bin/env bash
# Descobre o projeto e o executável da Unity. Use: source scripts/unity-env.sh
PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
HUB_EDITORS="/Applications/Unity/Hub/Editor"
WANTED="$(sed -n 's/^m_EditorVersion: //p' "$PROJECT_DIR/ProjectSettings/ProjectVersion.txt" | tr -d '[:space:]')"

if [ -n "${UNITY_PATH:-}" ]; then
  UNITY="$UNITY_PATH"
elif [ -x "$HUB_EDITORS/$WANTED/Unity.app/Contents/MacOS/Unity" ]; then
  UNITY="$HUB_EDITORS/$WANTED/Unity.app/Contents/MacOS/Unity"
else
  LATEST="$(ls "$HUB_EDITORS" 2>/dev/null | grep '^6000' | sort -V | tail -1)"
  if [ -n "$LATEST" ]; then
    UNITY="$HUB_EDITORS/$LATEST/Unity.app/Contents/MacOS/Unity"
    echo "⚠️  Unity $WANTED não encontrada; usando $LATEST" >&2
  fi
fi

if [ -z "${UNITY:-}" ] || [ ! -x "$UNITY" ]; then
  echo "❌ Unity 6 não encontrada. Rode primeiro: ./scripts/setup-mac.sh" >&2
  exit 1
fi
mkdir -p "$PROJECT_DIR/Builds"
export PROJECT_DIR UNITY
