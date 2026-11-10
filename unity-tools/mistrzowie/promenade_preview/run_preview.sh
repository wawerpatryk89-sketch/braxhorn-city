#!/usr/bin/env bash
# Waterfront preview without Unity (owner 9.11: "od teraz renderowane zdjęcia przed buildem"; 10.11 promenade + port):
# the real foundation (legacy_foundation.py -> Foundation.json), the 3D lettering (build_harbour_signs.py), and the real
# generators of the harbour, railings, promenade furniture, kiosks + equipment and beach on C# 9 stubs, rendered with
# the real maps (Poly Haven / ambientCG 1K) and the owner's boat GLBs. Generated inputs go to the work dir, never into
# the repository. The owner's encrypted packs (palms, shop food) are not in the preview: their places stay empty.
# Needs: dotnet 8 SDK, python3 + numpy + shapely + pillow + matplotlib, node + playwright, npm three@0.160.0.
# usage: run_preview.sh <work dir> <node_modules/three>      (VIEWS=<views.json> to pick other views)
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"; REPO="$(cd "$HERE/../../.." && pwd)"; HOTEL="$HERE/../hotel_sobczak_preview"
WORK="$(mkdir -p "${1:?work dir}" && cd "$1" && pwd)"; THREE="${2:?node_modules/three}"
SRC="$REPO/unity-city/Assets/Source/Mistrzowie"; DST="$WORK/Assets/Source/Mistrzowie"; mkdir -p "$DST"
(cd "$REPO/unity-tools/mistrzowie" && python3 legacy_foundation.py >/dev/null && python3 build_harbour_signs.py >/dev/null)
mv -f "$SRC/Foundation.json" "$SRC/HarbourSigns.bytes" "$DST/"                     # generated in CI, never committed
git -C "$REPO" clean -fq mistrzowie-evidence; git -C "$REPO" checkout -- mistrzowie-evidence
ln -sfn "$SRC/PromenadeFood" "$DST/PromenadeFood"
ln -sfn "$REPO/unity-city/Assets/Source/HarborBeach" "$WORK/Assets/Source/HarborBeach"
(cd "$HERE" && dotnet build -v q -nologo | grep -E " error |Build succeeded")
mkdir -p "$WORK/shots"; python3 -c "import json,sys;V=json.load(open(sys.argv[1]));[v.update(out='shots/'+v['out']) for v in V];json.dump(V,open(sys.argv[2],'w'))" "${VIEWS:-$HERE/views.json}" "$WORK/views.json"
cd "$WORK" && rm -rf Assets/PlayableGenerated
dotnet "$HERE/bin/Debug/net8.0/promenade_preview.dll" Assets/Source/Mistrzowie/Foundation.json | grep -E "CHAMPIONS|OPTIONAL_FAILED|DUMP" | cut -c1-220
python3 "$HOTEL/geom2bin.py" promenade_geometry.json bin | tail -1
python3 "$HOTEL/fetch_preview_textures.py" bin/scene.json tex | tail -1
PREVIEW_PICTURES="$WORK" node "$HOTEL/render3d.js" bin "$THREE" views.json "$WORK" tex | grep -c wrote
