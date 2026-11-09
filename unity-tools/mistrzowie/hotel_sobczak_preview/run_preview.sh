#!/usr/bin/env bash
# Hotel Sobczak T03 preview without Unity: compile (C# 9 stubs), run the generator, render with real Poly Haven
# maps and models. Needs: dotnet 8 SDK, python3 + numpy + matplotlib, node + playwright (Chromium), npm three@0.160.0.
# usage: run_preview.sh <work dir> <three package dir (node_modules/three)>
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"; REPO="$(cd "$HERE/../../.." && pwd)"
WORK="${1:?work dir}"; THREE="${2:?node_modules/three}"
SRC="$WORK/Assets/Source/Mistrzowie"; mkdir -p "$SRC/Textures"
python3 "$REPO/unity-tools/mistrzowie/build_sobczak_sign.py" >/dev/null
cp "$REPO/unity-city/Assets/Source/Mistrzowie/HotelSobczakSign.bytes" "$SRC/"
rm -f "$REPO/unity-city/Assets/Source/Mistrzowie/HotelSobczakSign.bytes"        # generated in CI, never committed
python3 "$REPO/unity-tools/mistrzowie/prepare_hotel_sobczak_models.py" --dest "$SRC/HotelSobczak" | tail -1
python3 - "$SRC" "$REPO" <<'PY'
import re, sys, pathlib
src, repo = sys.argv[1], sys.argv[2]
sys.path.insert(0, repo + '/unity-tools/mistrzowie')
import prepare_arena_lobby_models as arena
arena.DEST = pathlib.Path(src, 'ArenaLobby'); arena.main()
mat = pathlib.Path(repo, 'unity-city/Assets/Editor/Mistrzowie/BraxChampionsHotelSobczakMaterials.cs').read_text()
slugs = re.findall(r'"([a-z0-9_]+)"', mat[mat.index('RequiredSlugs'):mat.index('};', mat.index('RequiredSlugs'))])
for s in slugs:   # VerifySources only checks that the Poly Haven maps exist: empty placeholders are enough here
    for x in ('diff_4k.jpg', 'nor_gl_4k.exr', 'arm_4k.png'):
        pathlib.Path(src, 'Textures', f'{s}_{x}').touch()
PY
(cd "$HERE" && dotnet build -v q -nologo | grep -E " error |Build succeeded")
(cd "$WORK" && dotnet "$HERE/bin/Debug/net8.0/hotel_sobczak_preview.dll" | grep -E "REBUILT|DUMP|MODELS|WARN" | grep -v LOBBY_MODEL_MISSING)
(cd "$WORK" && python3 "$HERE/geom2bin.py" hotel_geometry.json bin && python3 "$HERE/fetch_preview_textures.py" bin/scene.json tex | tail -1 \
  && cp "$HERE/views.json" . && node "$HERE/render3d.js" bin "$THREE" views.json "$SRC" tex && python3 "$HERE/render.py" hotel_geometry.json | grep plan)
