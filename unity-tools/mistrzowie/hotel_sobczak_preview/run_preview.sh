#!/usr/bin/env bash
# Hotel Sobczak T03 preview without Unity: compile (C# 9 stubs), run the generator, render.
# Needs: dotnet 8 SDK, python3 + numpy + matplotlib, node + playwright (Chromium), three@0.160.0 (npm).
# usage: run_preview.sh <work dir> <path to three.min.js>
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"; REPO="$(cd "$HERE/../../.." && pwd)"
WORK="${1:?work dir}"; THREE="${2:?three.min.js}"
mkdir -p "$WORK/Assets/Source/Mistrzowie/Textures"
python3 "$REPO/unity-tools/mistrzowie/build_sobczak_sign.py" >/dev/null
cp "$REPO/unity-city/Assets/Source/Mistrzowie/HotelSobczakSign.bytes" "$WORK/Assets/Source/Mistrzowie/"
# VerifySources only checks that the Poly Haven maps exist: empty placeholders are enough here
python3 - "$WORK" "$REPO" <<'PY'
import re, sys, pathlib
work, repo = sys.argv[1], sys.argv[2]
src = pathlib.Path(repo, 'unity-city/Assets/Editor/Mistrzowie/BraxChampionsHotelSobczakMaterials.cs').read_text()
slugs = re.findall(r'"([a-z0-9_]+)"', src[src.index('RequiredSlugs'):src.index('};', src.index('RequiredSlugs'))])
for s in slugs:
    for x in ('diff_4k.jpg', 'nor_gl_4k.exr', 'arm_4k.png'):
        pathlib.Path(work, 'Assets/Source/Mistrzowie/Textures', f'{s}_{x}').touch()
PY
(cd "$HERE" && dotnet build -v q -nologo | grep -E " error |Build succeeded")
(cd "$WORK" && dotnet "$HERE/bin/Debug/net8.0/hotel_sobczak_preview.dll" | grep -v LOBBY_MODEL_MISSING)
(cd "$WORK" && python3 "$HERE/geom2bin.py" hotel_geometry.json bin && cp "$HERE/views.json" . \
  && node "$HERE/render3d.js" bin "$THREE" views.json | grep -v "^page:" && python3 "$HERE/render.py" hotel_geometry.json | grep plan)
