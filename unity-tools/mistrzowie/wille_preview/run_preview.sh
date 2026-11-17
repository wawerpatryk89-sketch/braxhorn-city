#!/usr/bin/env bash
# Villas for sale preview without Unity (owner 9.11 23:49: "od teraz renderowane zdjęcia przed buildem").
# Needs: dotnet 8 SDK, python3 + numpy + shapely, node + playwright, npm three@0.160.0.
# usage: run_preview.sh <work dir with Assets/Source/Mistrzowie> <node_modules/three> [villa 1..3]
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"; HOTEL="$HERE/../hotel_sobczak_preview"
WORK="${1:?work dir}"; THREE="${2:?node_modules/three}"; NR="${3:-1}"
SRC="$WORK/Assets/Source/Mistrzowie"; mkdir -p "$SRC"
(cd "$HERE" && dotnet build -v q -nologo | grep -E " error |Build succeeded")
(cd "$WORK" && dotnet "$HERE/bin/Debug/net8.0/wille_preview.dll" "$NR" | grep -E "MISTRZOWIE_WILLA|DUMP|WARN")
(cd "$WORK" && python3 "$HERE/../casino_preview/mesh2quads.py" wille_geometry.json wille_quads.json \
  && (python3 "$HERE/../apartments_preview/zfight.py" wille_quads.json | tail -15 || true) \
  && python3 "$HOTEL/geom2bin.py" wille_geometry.json bin -4.5 && python3 "$HOTEL/fetch_preview_textures.py" bin/scene.json tex | tail -1 \
  && node "$HOTEL/render3d.js" bin "$THREE" "$HERE/views.json" "$SRC" tex | grep -c wrote)
