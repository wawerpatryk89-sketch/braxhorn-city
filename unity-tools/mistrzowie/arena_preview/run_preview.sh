#!/usr/bin/env bash
# Arena A1 preview without Unity (owner 9.11 23:49: "od teraz renderowane zdjęcia przed buildem"): the real building
# generator (BraxChampionsBuildings.Generate: exterior shell, hall bowl, lobby, signs, murals) on C# 9 stubs, drawn in
# building-local metres (hall centre 0,0; public front on -Z) with real maps and lighting as CityStaticPBR.
# Needs: dotnet 8 SDK, python3 + numpy + shapely + pillow + matplotlib, node + playwright, npm three@0.160.0.
# usage: run_preview.sh <work dir> <node_modules/three>
# Owner pictures are NOT in the repository: put them in <work dir>/Assets/Source/Mistrzowie/ first
#   ArenaMuralsHD/MURAL_*_6144x2048.png (+ run split_arena_murals.py <that folder>), ArenaSigns/champion_arena_*.png.
# Without them the generator reports the murals / signs as missing, exactly as a build would.
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"; REPO="$(cd "$HERE/../../.." && pwd)"; HOTEL="$HERE/../hotel_sobczak_preview"
WORK="${1:?work dir}"; THREE="${2:?node_modules/three}"
SRC="$WORK/Assets/Source/Mistrzowie"; mkdir -p "$SRC"; GEN="$REPO/unity-city/Assets/Source/Mistrzowie"
(cd "$REPO/unity-tools/mistrzowie" && python3 build_exteriors.py >/dev/null && python3 build_signs.py >/dev/null \
  && python3 build_casino_sign.py >/dev/null && python3 prepare_arena.py >/dev/null)
mv "$GEN/Exteriors.bytes" "$GEN/Signs.bytes" "$GEN/CasinoWawerSign.bytes" "$SRC/"      # generated in CI, never committed
rm -rf "$SRC/ArenaGraphics"; mv "$GEN/ArenaGraphics" "$SRC/"; rm -f "$GEN/SignFont-LICENSE.txt"
rm -f "$REPO/mistrzowie-evidence/exterior-checks.json" "$REPO/mistrzowie-evidence/sign-geometry-checks.json"
(cd "$HERE" && dotnet build -v q -nologo | grep -E " error |Build succeeded")
(cd "$WORK" && rm -rf Assets/PlayableGenerated \
  && dotnet "$HERE/bin/Debug/net8.0/arena_preview.dll" | grep -E "A1_MURAL|A1_HALL|A1_LOBBY_WALL|A1_FACADE_SIGNS|A1_SMOKED|A1_VITRINE_OWNER|OPTIONAL_FAILED|DUMP" | cut -c1-220)
(cd "$WORK" && python3 "$HOTEL/geom2bin.py" arena_geometry.json bin -0.05 | tail -1 \
  && python3 "$HOTEL/fetch_preview_textures.py" bin/scene.json tex | tail -1 \
  && PREVIEW_PICTURES="$WORK" node "$HOTEL/render3d.js" bin "$THREE" "$HERE/views.json" "$SRC" tex | grep -c wrote)
