#!/usr/bin/env python3
"""Download the CC0 Poly Haven models furnishing the A1 arena entrance lobby.

Owner 9.11: "Na wejściu za szybą mogą być jakieś sofy kwiaty coś żeby tam było".
Owner render f2faacc9: black leather lounge seating and green potted plants
behind the ground-floor glass on both sides of the main entrance.

Exact files, byte sizes and MD5 are pinned from api.polyhaven.com/files on 9.11;
a changed provider file fails the build instead of silently changing the look.
Texel density follows real size (owner: "odpowiednie pod skalę"): leather seating
2K, plants 1K (a 1.35 m plant at 1K is ~1.3 mm per pixel).
Writes only unity-city/Assets/Source/Mistrzowie/ArenaLobby/<slug>/. No Unity, no APK.
"""
from hashlib import md5
import json
from pathlib import Path, PurePosixPath
import time
import urllib.request

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "unity-city/Assets/Source/Mistrzowie/ArenaLobby"
API = "https://api.polyhaven.com/files/"
CDN = "https://dl.polyhaven.org/file/ph-assets/Models/"
USER_AGENT = "BRAXHORN-CITY-Unity6-PolyHaven-CC0-Staging/1.0"

MODELS = {
    'sofa_02': ('2k', {
        'sofa_02_2k.gltf': (4183, 'd58bdde4cb00612fc070bc3e7caba265'),
        'textures/sofa_02_arm_2k.jpg': (401876, '4cdaf48033316b61832d220f1dcc48ef'),
        'textures/sofa_02_nor_gl_2k.jpg': (463146, 'd22e982ab8dac6a54ab06839efb0bafa'),
        'textures/sofa_02_diff_2k.jpg': (323319, '37808357cc7d00fd62bda197cf89e6ae'),
        'sofa_02.bin': (74864, '024d6f57a7c2ee7915430cd6e1c2651e'),
    }),
    'modern_arm_chair_01': ('2k', {
        'modern_arm_chair_01_2k.gltf': (5121, '1fec9694d156cbed5dc8757576cd16a5'),
        'textures/modern_arm_chair_01_pillow_diff_2k.jpg': (843013, 'c3def4cb2bcef8ee07c90c55c20a637e'),
        'textures/modern_arm_chair_01_pillow_nor_gl_2k.jpg': (1074878, '2549b1d93eb4ec13e8ad9edd1327d2f0'),
        'modern_arm_chair_01.bin': (240728, '5a8f2ea1c79ea484140e484b2a621670'),
        'textures/modern_arm_chair_01_legs_diff_2k.jpg': (1660925, 'cfed247a17cafd9f59a7f01ed01fdbf4'),
        'textures/modern_arm_chair_01_legs_arm_2k.jpg': (2144468, 'a62c231c4f42be2f85f6b42a7cc1fc8e'),
        'textures/modern_arm_chair_01_legs_nor_gl_2k.jpg': (2076117, '52c08726e839e2c1c87863e6f5a87411'),
        'textures/modern_arm_chair_01_pillow_arm_2k.jpg': (1176399, '1984906adcaef79cefe2d20cb5be4c7e'),
    }),
    'potted_plant_01': ('1k', {
        'potted_plant_01_1k.gltf': (7969, '199e9e3d68f5447142db4066d60ae3f0'),
        'textures/potted_plant_01_pot_rough_1k.jpg': (173192, '8f2e400f94843a9239229bfe99d16688'),
        'textures/potted_plant_01_leaves_nor_gl_1k.jpg': (177105, '0a7ea996bb2a4e9e19a5299a5b654406'),
        'textures/potted_plant_01_pot_diff_1k.jpg': (204900, '67114dcd68232a6da88c627e144e65c4'),
        'potted_plant_01.bin': (5345188, '18377bd8d99b20a8d5fc5cf03cbf94b2'),
        'textures/potted_plant_01_leaves_rough_1k.jpg': (91706, '6251bfd59ced0360c16d9c286adbb131'),
        'textures/potted_plant_01_pot_nor_gl_1k.jpg': (200349, '7388c8cba421175fd70969df077988a1'),
        'textures/potted_plant_01_leaves_diff_1k.jpg': (124730, '8cbaaa24c18c61be4201c473773dbffc'),
    }),
    'potted_plant_02': ('1k', {
        'potted_plant_02_1k.gltf': (6653, '9999d7e6c51018a85551840e1bdae656'),
        'textures/potted_plant_02_leaves_diff_1k.jpg': (167700, 'e8a026bd7bfa6497729d855c59da03ce'),
        'potted_plant_02.bin': (1852244, 'cdbd4619b2596c9c469ad34676085fa8'),
        'textures/potted_plant_02_pot_rough_1k.jpg': (114886, '68ed80fa32791fb0e81df61a75de40b4'),
        'textures/potted_plant_02_leaves_rough_1k.jpg': (102396, 'ba934a78cc6496d934c014eb8a33af37'),
        'textures/potted_plant_02_pot_diff_1k.jpg': (155244, '97e149c93eb0d962e491d4c82d3ce477'),
        'textures/potted_plant_02_leaves_nor_gl_1k.jpg': (127232, '2420d5c0ab87426f5318a5e91d1ea3bb'),
        'textures/potted_plant_02_pot_nor_gl_1k.jpg': (108343, 'fbb44ccc1c6564b2837b79a11ed45d58'),
    }),
}


def fetch(url):
    if not (url.startswith(CDN) or url.startswith(API)):
        raise ValueError("Disallowed source: " + url)
    for attempt in range(4):
        try:
            req = urllib.request.Request(url, headers={"User-Agent": USER_AGENT})
            with urllib.request.urlopen(req, timeout=120) as r:
                return r.read()
        except Exception:
            if attempt == 3:
                raise
            time.sleep(2 * (attempt + 1))


def main():
    total = 0
    for slug, (res, files) in MODELS.items():
        out = DEST / slug
        info = json.loads(fetch(API + slug))["gltf"][res]["gltf"]
        urls = {info["url"].rsplit("/", 1)[-1]: info["url"]}
        urls.update({rel: item["url"] for rel, item in info["include"].items()})
        if set(urls) != set(files):
            raise RuntimeError(f"Poly Haven {slug} {res}: file list differs from the 9.11 pin")
        for rel, (size, digest) in files.items():
            if PurePosixPath(rel).is_absolute() or ".." in PurePosixPath(rel).parts:
                raise ValueError("unsafe path " + rel)
            path = out / rel
            blob = path.read_bytes() if path.exists() else None
            if blob is None or len(blob) != size or md5(blob).hexdigest() != digest:
                blob = fetch(urls[rel])
            if len(blob) != size or md5(blob).hexdigest() != digest:
                raise RuntimeError(f"Poly Haven {slug} {rel}: size/MD5 differs from the 9.11 pin")
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_bytes(blob)
            total += size
        print(f"BRAX_A1_LOBBY_MODEL {slug} {res} files={len(files)}", flush=True)
    print(f"BRAX_A1_LOBBY_MODELS OK models={len(MODELS)} bytes={total} (Powered by Poly Haven, CC0)")


if __name__ == "__main__":
    main()
