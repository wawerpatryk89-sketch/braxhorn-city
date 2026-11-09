#!/usr/bin/env bash
# Casino T11 preview without Unity (owner 9.11 23:49: "od teraz renderowane zdjęcia przed buildem"): the real generator
# on C# 9 stubs, real Poly Haven / ambientCG maps, lighting as CityStaticPBR, the 3D gold signs from build_casino_sign.py.
# Needs: dotnet 8 SDK, python3 + numpy + shapely, node + playwright, npm three@0.160.0.
# usage: run_preview.sh <work dir> <node_modules/three>
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"; REPO="$(cd "$HERE/../../.." && pwd)"; HOTEL="$HERE/../hotel_sobczak_preview"
WORK="${1:?work dir}"; THREE="${2:?node_modules/three}"
SRC="$WORK/Assets/Source/Mistrzowie"; mkdir -p "$SRC"
python3 "$REPO/unity-tools/mistrzowie/build_casino_sign.py" >/dev/null
mv "$REPO/unity-city/Assets/Source/Mistrzowie/CasinoWawerSign.bytes" "$SRC/"      # generated in CI, never committed
(cd "$HERE" && dotnet build -v q -nologo | grep -E " error |Build succeeded")
(cd "$WORK" && dotnet "$HERE/bin/Debug/net8.0/casino_preview.dll" | grep -E "T11_3D_SIGN|DUMP|WARN")
(cd "$WORK" && python3 "$HERE/mesh2quads.py" casino_geometry.json casino_quads.json \
  && (python3 "$HERE/../apartments_preview/zfight.py" casino_quads.json | tail -15 || true) \
  && python3 "$HOTEL/geom2bin.py" casino_geometry.json bin -0.02 && python3 "$HOTEL/fetch_preview_textures.py" bin/scene.json tex | tail -1 \
  && node "$HOTEL/render3d.js" bin "$THREE" "$HERE/views.json" "$SRC" tex | grep -c wrote)
