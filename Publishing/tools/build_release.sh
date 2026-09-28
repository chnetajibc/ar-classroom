#!/bin/bash
# Builds everything that gets uploaded (outputs in Publishing/dist/):
#   RealisticClassroom_v<VERSION>.unitypackage             -> Fab
#   RealisticClassroom_v<VERSION>_direct-sales.zip         -> itch.io, Gumroad, Payhip
# Steps: fill publisher details -> documentation PDF -> pre-flight validation + package export (Unity batch mode) -> zip.
# Usage:  tools/build_release.sh [--zip-only]      (--zip-only re-zips the existing package without starting Unity)
# Env:    UNITY=/path/to/Unity  ALLOW_PLACEHOLDERS=1 (test builds only; a release must have real publisher details)
set -euo pipefail
cd "$(dirname "$0")/.."
PUB="$PWD"
ROOT="$(cd .. && pwd)"
VERSION="1.0.0"
NAME="RealisticClassroom_v${VERSION}"
PKG="$PUB/dist/${NAME}.unitypackage"
ZIP="$PUB/dist/${NAME}_direct-sales.zip"
STAGE="$PUB/build/release/${NAME}"
PRODUCT="$ROOT/ARClassroom/Assets/RealisticClassroom"

# 1. publisher details (read as KEY=VALUE lines, quotes optional, no trailing comments; the file is never executed)
PUBLISHER_NAME=""; SUPPORT_EMAIL=""; WEBSITE=""; JURISDICTION=""
if [ -f publisher.env ]; then
  while IFS= read -r line || [ -n "$line" ]; do
    case "$line" in ''|'#'*) continue ;; esac
    case "$line" in *=*) ;; *) continue ;; esac
    key="$(printf '%s' "${line%%=*}" | tr -d '[:space:]')"
    val="$(printf '%s' "${line#*=}" | sed -e 's/^[[:space:]]*//' -e 's/[[:space:]]*$//' -e 's/^"\(.*\)"$/\1/' -e "s/^'\(.*\)'\$/\1/")"
    case "$key" in PUBLISHER_NAME|SUPPORT_EMAIL|WEBSITE|JURISDICTION) printf -v "$key" '%s' "$val" ;; esac
  done < publisher.env
fi
# values copied unchanged from publisher.env.example count as "not filled in"
case "$PUBLISHER_NAME" in "Your Name or Studio") PUBLISHER_NAME="" ;; esac
case "$SUPPORT_EMAIL" in *@example.com) SUPPORT_EMAIL="" ;; esac
case "$WEBSITE" in https://example.com*) WEBSITE="" ;; esac
case "$JURISDICTION" in "Your country"*) JURISDICTION="" ;; esac
esc() { printf '%s' "$1" | sed -e 's/[\\&|]/\\&/g'; }   # makes a value safe inside a sed replacement (& \ and the | delimiter)
missing=""
for v in PUBLISHER_NAME SUPPORT_EMAIL WEBSITE JURISDICTION; do [ -z "${!v}" ] && missing="$missing $v"; done
if [ -n "$missing" ]; then
  echo "publisher.env is missing or still has the example values for:$missing (copy publisher.env.example to publisher.env and fill it in)"
  [ "${ALLOW_PLACEHOLDERS:-0}" = "1" ] || exit 1
  echo "ALLOW_PLACEHOLDERS=1 -> continuing; the outputs are NOT ready to publish"
fi

# 2. documentation
tools/build_docs.sh >/dev/null

# 3. validation + export
if [ "${1:-}" != "--zip-only" ]; then
  V="$(sed -n 's/^m_EditorVersion: *//p' "$ROOT/ARClassroom/ProjectSettings/ProjectVersion.txt")"
  UNITY="${UNITY:-/Applications/Unity/Hub/Editor/$V/Unity.app/Contents/MacOS/Unity}"
  [ -x "$UNITY" ] || { echo "Unity $V not found at $UNITY (set UNITY=...)"; exit 1; }
  if pgrep -f "Unity.*-projectPath.*ARClassroom" >/dev/null || pgrep -f "Unity.app/Contents/MacOS/Unity" >/dev/null; then
    echo "A Unity Editor is running. Close the ARClassroom project (and other Editors), then run again."; exit 1
  fi
  mkdir -p dist build
  echo "Unity $V: validating and exporting (a few minutes)..."
  "$UNITY" -batchmode -projectPath "$ROOT/ARClassroom" -logFile "$PUB/build/unity_release.log" \
    -executeMethod RealisticClassroom.Publishing.PublishingPipeline.ReleaseCli -outPath "$PKG" -quit \
    || { echo "Unity failed (exit $?). See $PUB/build/unity_release.log"; exit 1; }
fi
[ -f "$PKG" ] || { echo "missing $PKG"; exit 1; }

# 4. stage the direct-sales zip
rm -rf "$STAGE"; mkdir -p "$STAGE"
cp "$PKG" "$STAGE/"
cp "$PRODUCT/Documentation/Documentation.pdf" "$STAGE/"
cp "$PRODUCT/Third-Party Notices.txt" "$STAGE/"
sed -e "s|\[PUBLISHER NAME\]|$(esc "${PUBLISHER_NAME:-[PUBLISHER NAME]}")|g" -e "s|\[SUPPORT EMAIL\]|$(esc "${SUPPORT_EMAIL:-[SUPPORT EMAIL]}")|g" \
    -e "s|\[WEBSITE\]|$(esc "${WEBSITE:-[WEBSITE]}")|g" -e "s|\[JURISDICTION\]|$(esc "${JURISDICTION:-[JURISDICTION]}")|g" legal/EULA.md > "$STAGE/EULA.md"
cat > "$STAGE/README.txt" <<TXT
Classroom Interior Pack - Teacher & Students (URP)   v${VERSION}

1. Create or open a Unity 6 project that uses the Universal Render Pipeline (for example the "Universal 3D" template).
2. Assets > Import Package > Custom Package... and choose ${NAME}.unitypackage. Import everything.
3. Optional: Tools > Realistic Classroom > Apply Recommended URP Settings.
4. Open Assets/RealisticClassroom/Scenes/Classroom_Demo.unity and press Play.

Documentation.pdf explains the demo, the prefabs, the lessons and the limitations.
Licence: EULA.md covers the scripts, tools, scene set-up and documentation. The third-party models, textures, HDRI and
animations are CC0; see "Third-Party Notices.txt".
Support: ${SUPPORT_EMAIL:-[SUPPORT EMAIL]}
TXT

# 5. no placeholders may reach a customer
if grep -l -E "\[(PUBLISHER NAME|SUPPORT EMAIL|WEBSITE|JURISDICTION)\]" "$STAGE"/*.md "$STAGE"/*.txt build/Documentation.md 2>/dev/null; then
  echo "^ placeholders left in these files"
  [ "${ALLOW_PLACEHOLDERS:-0}" = "1" ] || exit 1
fi

rm -f "$ZIP"
( cd "$STAGE/.." && zip -q -r -X "$ZIP" "$NAME" -x "*.DS_Store" )
echo
echo "Release ${VERSION}:"
ls -lh "$PKG" "$ZIP" | awk '{print "  " $5 "  " $9}'
shasum -a 256 "$PKG" "$ZIP" | sed 's|'"$PUB"'/||; s/^/  /'
unzip -l "$ZIP" | tail -n +4 | head -8
