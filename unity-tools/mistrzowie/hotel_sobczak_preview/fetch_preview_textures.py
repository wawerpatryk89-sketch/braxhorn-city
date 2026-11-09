"""Preview only: 1K diffuse + normal (GL) maps of every Poly Haven slug used in bin/scene.json, resolved through
api.polyhaven.com/files (the build uses the pinned 4K sources from prepare_assets.py). Never writes into the repository."""
import json
import sys
import urllib.request
from pathlib import Path

UA = {"User-Agent": "BRAXHORN-CITY-preview/1.0"}
COLOR_KEYS = {"fabric_pattern_05": "col_01"}


def get(url):
    return urllib.request.urlopen(urllib.request.Request(url, headers=UA), timeout=60).read()


scene, dest = Path(sys.argv[1]), Path(sys.argv[2])
dest.mkdir(parents=True, exist_ok=True)
for slug in sorted({m["slug"] for m in json.load(open(scene))["materials"] if m["slug"]}):
    files = None
    for kind, key in (("diff", COLOR_KEYS.get(slug, "Diffuse")), ("nor_gl", "nor_gl")):
        out = dest / f"{slug}_{kind}_1k.jpg"
        if out.exists():
            continue
        files = files or json.loads(get("https://api.polyhaven.com/files/" + slug))
        entry = files.get(key) or files.get("Diffuse") or next(v for k, v in files.items() if k.startswith("col"))
        out.write_bytes(get(entry["1k"]["jpg"]["url"]))
    print("tex", slug, flush=True)
