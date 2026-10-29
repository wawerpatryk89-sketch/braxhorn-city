#!/usr/bin/env python3
"""Convert original WebP to PNG losslessly and verify source GLB hashes.
Always fail rather than replace missing sources with decorative primitives."""
from pathlib import Path
from hashlib import sha256
import json
import sys

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "unity-city/Assets/StreamingAssets/Estate"
MANIFEST = json.loads((ASSETS / "EstateSourceManifest.json").read_text(encoding="utf-8"))
assert MANIFEST["doorCount"] == 24 and len(MANIFEST["sourceModels"]) == 21
assert len(MANIFEST["collision"]) == 1013
assert len(MANIFEST["vegetation"]) == 30
assert (ASSETS / "EstateProceduralAndDoors.glb").stat().st_size == 8094192

errors = []
for asset in MANIFEST["binaryAssets"]:
    if not asset["present"]:
        continue
    path = ASSETS / (asset["name"] + ".glb")
    if not path.is_file():
        errors.append(f'MISSING BINARY {path.name}')
        continue
    actual = sha256(path.read_bytes()).hexdigest()
    if actual != asset["sha256"]:
        errors.append(f'MODIFIED ORIGINAL {path.name} actual={actual} source={asset["sha256"]}')
for name in ["brax_color", "brax_normal"]:
    source = ASSETS / (name + ".webp")
    if not source.is_file(): errors.append(f"MISSING BRAX ORIGINAL TEXTURE {source.name}")
if errors:
    print("\n".join(errors),file=sys.stderr)
    raise SystemExit(1)
from PIL import Image
for name in ["brax_color", "brax_normal"]:
    source = ASSETS / (name + ".webp")
    out = ASSETS / (name + ".png")
    with Image.open(source) as im:
        im.convert("RGB").save(out, format="PNG", compress_level=7)
        print(f"{name}: original {im.size}, lossless PNG {out.stat().st_size} bytes")
report = {
    "status": "SOURCES_MATCH_MANIFEST_NOT_UNITY_COMPILED",
    "original_assets_verified": len([a for a in MANIFEST["binaryAssets"] if a["present"]]),
    "model_instances":len(MANIFEST["sourceModels"]),
    "procedural_export_bytes":(ASSETS/"EstateProceduralAndDoors.glb").stat().st_size,
    "missing_original_models":MANIFEST["missingOriginalModels"],
    "unity_compiled":False,
    "visual_1_to_1_verified":False,
}
print(json.dumps(report,ensure_ascii=False,indent=2))
(ROOT / "unity-city/Assets/EstateMigration/Generated/UnitySourcePreflight.json").write_text(
    json.dumps(report,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
