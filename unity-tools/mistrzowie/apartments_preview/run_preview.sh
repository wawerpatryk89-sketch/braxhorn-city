#!/usr/bin/env bash
# Apartments T04-T07 preview without Unity, same renderer as the hotel (real Poly Haven maps and models, lighting as
# CityStaticPBR in the game) + z-fighting check. Needs: dotnet 8 SDK, python3 + numpy + shapely, node + playwright,
# npm three@0.160.0.   usage: run_preview.sh <work dir> <node_modules/three> [T04 T05 ...]
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"; REPO="$(cd "$HERE/../../.." && pwd)"; HOTEL="$HERE/../hotel_sobczak_preview"
WORK="${1:?work dir}"; THREE="${2:?node_modules/three}"; shift 2; IDS="${*:-T04 T05 T06 T07}"
SRC="$WORK/Assets/Source/Mistrzowie"; mkdir -p "$SRC"
python3 "$REPO/unity-tools/mistrzowie/prepare_hotel_sobczak_models.py" --dest "$SRC/HotelSobczak" | tail -1
python3 - "$SRC" "$REPO" <<'PY'
import sys, pathlib
src, repo = sys.argv[1], sys.argv[2]
sys.path.insert(0, repo + '/unity-tools/mistrzowie')
import prepare_arena_lobby_models as arena
arena.DEST = pathlib.Path(src, 'ArenaLobby'); arena.main()
PY
(cd "$HERE" && dotnet build -v q -nologo | grep -E " error |Build succeeded")
(cd "$WORK" && dotnet "$HERE/bin/Debug/net8.0/apartments_preview.dll" $IDS | grep -E "APT_BUILDING|DUMP|WARN" | grep -v REGION_MATERIAL)
for id in $IDS; do
  (cd "$WORK" && python3 "$HERE/zfight.py" "${id}_geometry.json" | tail -12 || true)
  (cd "$WORK" && python3 "$HERE/views.py" "$id" > "views_$id.json" && python3 "$HOTEL/geom2bin.py" "${id}_geometry.json" "bin_$id" \
    && python3 "$HOTEL/fetch_preview_textures.py" "bin_$id/scene.json" tex | tail -1 \
    && node "$HOTEL/render3d.js" "bin_$id" "$THREE" "views_$id.json" "$SRC" tex | grep -c wrote)
done
