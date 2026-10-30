"""Stage the operator-delivered Poly Haven roof PBR maps in ISOLATED Unity R2.
Every 4K source is byte-verified against the actual uploaded archives. No fallback.
Convert real 4K PBR input into iPhone-friendly 2K textures: albedo, OpenGL normal,
and Unity Standard metallic(0)/smoothness(1-roughness) alpha packing.
"""
import hashlib
import io
import json
import os
import pathlib
import time
import urllib.request

os.environ["OPENCV_IO_ENABLE_OPENEXR"] = "1"
import cv2
import numpy as np
from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[2]
TARGET = ROOT / "unity-city/Assets/Source/City/R2Approved"
EVIDENCE = ROOT / "city-review"
TARGET.mkdir(parents=True, exist_ok=True)
EVIDENCE.mkdir(parents=True, exist_ok=True)
BASE = "https://dl.polyhaven.org/file/ph-assets/Textures/"
MAPS = [
    ("GreyRoof", "albedo", "jpg", "grey_roof_01", "grey_roof_01_diff_4k.jpg",
     "599a76538d75cd814a8ade220100fdd5fb4fb57b0c66f16e567583dec0aed2b4"),
    ("GreyRoof", "roughness", "jpg", "grey_roof_01", "grey_roof_01_rough_4k.jpg",
     "113a6930087e288d52111fa70b4f0816287054f5ffcbbf3c256a6e5b4b7811fc"),
    ("GreyRoof", "normal", "exr", "grey_roof_01", "grey_roof_01_nor_gl_4k.exr",
     "0c35a3f361db145deda3db38e8bd0be0a01f2592e12cd1bfe3f0c7dbfaa32aca"),
    ("RoofTiles", "albedo", "jpg", "roof_tiles", "roof_tiles_diff_4k.jpg",
     "accad55fa8473f7ae9a7b49101d1c077824a4e428e9a553398599efd43755945"),
    ("RoofTiles", "roughness", "exr", "roof_tiles", "roof_tiles_rough_4k.exr",
     "249deb7615b654bb12b675f21067a251fb2785257295b17e6457654b685eda2a"),
    ("RoofTiles", "normal", "exr", "roof_tiles", "roof_tiles_nor_gl_4k.exr",
     "9cd788b665c4cd67023edbc53800dcd3f27b0e42b5af4dae1bea23cd9beb2590"),
]
verified = {}
provenance = {
    "scope": "TEST_R2_ONLY",
    "source": "Operator-delivered Poly Haven CC0 maps; exact 4K SHA-256 checked",
    "sourceMaps": [],
    "generatedMaps": [],
    "unitySceneApplied": False,
    "visualAcceptance": False,
}

for key, kind, ext, asset, filename, expected in MAPS:
    url = BASE + ext + "/4k/" + asset + "/" + filename
    last_error = None
    for attempt in range(3):
        try:
            request = urllib.request.Request(url, headers={"User-Agent": "Braxhorn-R2-verified-PBR/1.0"})
            with urllib.request.urlopen(request, timeout=150) as response:
                content = response.read()
            break
        except (TimeoutError, OSError) as exc:
            last_error = exc
            if attempt == 2:
                raise RuntimeError("Verified source unavailable: " + url) from last_error
            time.sleep(2 ** attempt)
    sha = hashlib.sha256(content).hexdigest()
    if sha != expected:
        raise RuntimeError("Operator PBR source SHA-256 mismatch: " + filename + " " + sha)
    verified[(key, kind)] = (content, ext)
    provenance["sourceMaps"].append({
        "name": filename, "url": url, "sha256": sha,
        "sourceArchive": ("grey_roof_01_4k.blend.zip" if key == "GreyRoof" else "roof_tiles_4k.blend.zip"),
    })

for key in ("GreyRoof", "RoofTiles"):
    albedo, _ = verified[(key, "albedo")]
    with Image.open(io.BytesIO(albedo)) as photo:
        if photo.size != (4096, 4096):
            raise RuntimeError("Unexpected operator PBR resolution: " + key + " diffuse")
        im = photo.convert("RGB").resize((2048, 2048), Image.Resampling.LANCZOS)
        out = TARGET / ("R2" + key + "Albedo.jpg")
        im.save(out, quality=95, subsampling=0, optimize=True)
    for kind in ("roughness", "normal"):
        raw, ext = verified[(key, kind)]
        if ext == "jpg":
            with Image.open(io.BytesIO(raw)) as im:
                if im.size != (4096, 4096):
                    raise RuntimeError("Wrong roughness source resolution: " + key)
                array = np.asarray(im.convert("L").resize((2048, 2048), Image.Resampling.LANCZOS))
                values = array.astype(np.float32) / 255.0
        else:
            array = cv2.imdecode(np.frombuffer(raw, dtype=np.uint8), cv2.IMREAD_UNCHANGED)
            if array is None or array.shape[:2] != (4096, 4096):
                raise RuntimeError("Unable to decode exact 4K EXR source " + key + "/" + kind)
            values = cv2.resize(array, (2048, 2048), interpolation=cv2.INTER_AREA)
        values = np.clip(values, 0.0, 1.0)
        if kind == "normal":
            if len(values.shape) != 3 or values.shape[2] != 3:
                raise RuntimeError("Wrong source normal map channel count: " + key)
            # OpenCV decodes EXR as BGR; PBR source has OpenGL (+Y) normals.
            values = cv2.cvtColor(values, cv2.COLOR_BGR2RGB)
            normal = np.rint(values * 255).astype(np.uint8)
            out = TARGET / ("R2" + key + "Normal.png")
            Image.fromarray(normal, "RGB").save(out, optimize=True)
        else:
            if values.ndim == 3:
                raise RuntimeError("Unexpected multichannel roof roughness: " + key)
            smooth = np.rint((1.0 - values) * 255).astype(np.uint8)
            rgba = np.zeros((2048, 2048, 4), dtype=np.uint8)
            rgba[:, :, 3] = smooth
            out = TARGET / ("R2" + key + "MetallicSmoothness.png")
            Image.fromarray(rgba, "RGBA").save(out, optimize=True)
        provenance["generatedMaps"].append({
            "path": str(out.relative_to(ROOT)),
            "sha256": hashlib.sha256(out.read_bytes()).hexdigest(),
            "pixels": [2048, 2048],
        })
    provenance["generatedMaps"].append({
        "path": str((TARGET / ("R2" + key + "Albedo.jpg")).relative_to(ROOT)),
        "sha256": hashlib.sha256((TARGET / ("R2" + key + "Albedo.jpg")).read_bytes()).hexdigest(),
        "pixels": [2048, 2048],
    })

(EVIDENCE / "r2-pbr-source-provenance.json").write_text(
    json.dumps(provenance, indent=2) + "\n", encoding="utf-8"
)
print("R2_OPERATOR_SOURCE_FULL_PBR_VERIFIED", json.dumps({
    "sourceCount": len(provenance["sourceMaps"]),
    "outputCount": len(provenance["generatedMaps"]),
    "outputNames": [pathlib.Path(x["path"]).name for x in provenance["generatedMaps"]],
}))
