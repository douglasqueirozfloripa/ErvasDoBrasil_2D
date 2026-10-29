#!/usr/bin/env bash
# Análise estática do C# (lint), sem abrir a Unity:
#   1/2 compila os 5 assemblies (Core, Game, Editor, Tests.EditMode, Tests.PlayMode) com o Roslyn e os analisadores
#       do .NET (CA*), de estilo (IDE*), do Microsoft.Unity.Analyzers (UNT*) e, no Core e no Game, as APIs proibidas
#       de tools/lint/BannedSymbols.txt (RS0030: rede, processos externos); qualquer aviso reprova.
#   2/2 dotnet format --verify-no-changes: formatação e estilo conforme o .editorconfig.
# Uso: ./scripts/lint.sh          verifica (código de saída 1 se houver problema)
#      ./scripts/lint.sh --fix    corrige o que dá automaticamente (formatação, usings, estilo) e verifica de novo
# Pode rodar com o editor da Unity aberto: o lint usa os projetos de tools/lint, não a pasta Library.
set -uo pipefail
source "$(dirname "$0")/unity-env.sh"
SLN="$PROJECT_DIR/tools/lint/ErvasDoBrasil.Lint.sln"
FIX=0; [ "${1:-}" = "--fix" ] && FIX=1

# .NET SDK (o instalador oficial não põe o dotnet no PATH do zsh)
DOTNET="$(command -v dotnet || true)"
for c in /usr/local/share/dotnet/dotnet "$HOME/.dotnet/dotnet" /opt/homebrew/bin/dotnet; do
  [ -z "$DOTNET" ] && [ -x "$c" ] && DOTNET="$c"
done
if [ -z "$DOTNET" ]; then
  echo "❌ .NET SDK não encontrado. Instale com: brew install --cask dotnet-sdk"; exit 1
fi
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

# DLLs da UnityEngine/UnityEditor da mesma versão da Unity que compila o jogo
SCRIPTING="$(cd "$(dirname "$UNITY")/../Resources/Scripting" && pwd)"
if ! ls "$PROJECT_DIR"/Library/PackageCache/com.unity.ext.nunit*/net40/unity-custom/nunit.framework.dll >/dev/null 2>&1; then
  echo "❌ NUnit não encontrado em Library/PackageCache. Rode ./scripts/run-tests.sh (ou abra o projeto na Unity) uma vez."; exit 1
fi
if [ ! -f "$PROJECT_DIR/Library/ScriptAssemblies/UnityEngine.TestRunner.dll" ]; then
  echo "❌ UnityEngine.TestRunner.dll não encontrada em Library/ScriptAssemblies. Rode ./scripts/run-tests.sh (ou abra o projeto na Unity) uma vez."; exit 1
fi

# caminhos relativos e uma linha por problema
tidy() { sed -E "s#$PROJECT_DIR/##g; s# \[[^]]*\.csproj\]##; s# \(https://[^)]*\)##" | sort -u; }

if [ "$FIX" = 1 ]; then
  echo "▶ Corrigindo formatação e estilo (dotnet format)..."
  "$DOTNET" format "$SLN" --severity warn -v q 2>&1 | tidy
fi

UNITY_VER="$(basename "$(cd "$(dirname "$UNITY")/../../.." && pwd)")"
echo "▶ 1/2 Analisadores (CA, IDE, UNT, RS) nos 5 assemblies: .NET SDK $("$DOTNET" --version), DLLs da Unity $UNITY_VER..."
BUILD_OUT="$("$DOTNET" build "$SLN" -nologo -v q --no-incremental -warnaserror -clp:NoSummary \
             -p:UnityScripting="$SCRIPTING" 2>&1)"
BUILD=$?
PROBLEMS="$(printf '%s\n' "$BUILD_OUT" | grep -E ': (error|warning) ' | tidy)"
if [ $BUILD -eq 0 ]; then
  echo "✅ Nenhum aviso."
else
  if [ -n "$PROBLEMS" ]; then printf '%s\n' "$PROBLEMS"; echo "❌ $(printf '%s\n' "$PROBLEMS" | wc -l | tr -d ' ') problema(s)."
  else printf '%s\n' "$BUILD_OUT" | tail -30; echo "❌ O build do lint falhou."; fi
fi

echo "▶ 2/2 Formatação e estilo (dotnet format, regras do .editorconfig)..."
FORMAT_OUT="$("$DOTNET" format "$SLN" --verify-no-changes --no-restore --severity warn -v q 2>&1)"
FORMAT=$?
if [ $FORMAT -eq 0 ]; then
  echo "✅ Tudo no padrão."
else
  printf '%s\n' "$FORMAT_OUT" | tidy
  echo "❌ Formatação fora do padrão. Corrija com: ./scripts/lint.sh --fix"
fi

if [ $BUILD -eq 0 ] && [ $FORMAT -eq 0 ]; then echo "✅ Lint OK"; exit 0; fi
exit 1
