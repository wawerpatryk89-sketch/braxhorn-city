"""Download selected, demonstrably free CC0 gym models for BRAXHORN test only.

The original Oxygen3D user's gym ZIP is not in the linked repository (metadata
only). Do not pretend it was imported. Use independent, source-audited glTFs
from https://3dassets.dev/packs/gym-and-fitness-studio instead.
No rings, heavy bags, bundled room or low-poly asset set are imported.
"""
import hashlib
import json
import pathlib
import re
import struct
import urllib.request

ROOT = pathlib.Path(__file__).resolve().parents[2]
DST = ROOT / "build-source/Fitness"
CDN = "https://cdn.3dassets.dev/assets/"
MANIFEST = "https://3dassets.dev/api/v1/packs/gym-and-fitness-studio"
# Only static strength-training props. First two have manually audited official
# model URLs; other model IDs must be resolved by exact name from the official API.
CHOICES = [
    ("PremiumDumbbellRack", "Three-Tier Dumbbell Rack, Loaded", "33561"),
    ("PremiumPlateTree", "Weight Plate Tree, Loaded", "33566"),
    ("PremiumFlatBench", "Flat Utility Bench", None),
    ("PremiumInclineBench", "Adjustable Bench, Inclined", None),
    ("PremiumKettlebellRack", "Kettlebell Rack, Six Bells", None),
]
HEADERS = {"User-Agent": "BRAXHORN-commercial-game-CC0-audit/1.0"}

def get(url):
    req = urllib.request.Request(url, headers=HEADERS)
    with urllib.request.urlopen(req, timeout=70) as response:
        limit = 12 * 1024 * 1024
        raw = response.read(limit + 1)
        if len(raw) > limit: raise RuntimeError("Asset exceeded maximum acceptable file size")
        return raw

def model_url(item):
    if not isinstance(item, dict): return None
    for k in ("cdnUrl", "cdn_url", "modelUrl", "downloadUrl", "url", "fileUrl"):
        v = item.get(k)
        if isinstance(v, str) and v.startswith(CDN) and v.endswith("/model.glb"):
            return v
    for key in ("file", "model", "asset", "download"):
        child = item.get(key)
        if isinstance(child, dict):
            result = model_url(child)
            if result: return result
    return None

def find_items(doc):
    found = []
    def walk(x):
        if isinstance(x, dict):
            url = model_url(x)
            if url:
                fields = [x.get("name"), x.get("title"), x.get("label"),
                          x.get("slug"), x.get("displayName")]
                for child in (x.get("asset"), x.get("model")):
                    if isinstance(child, dict):
                        fields += [child.get("name"),child.get("title"),child.get("slug")]
                title = " ".join(y for y in fields if isinstance(y, str))
                found.append((title, url))
            for v in x.values(): walk(v)
        elif isinstance(x, list):
            for v in x: walk(v)
    walk(doc)
    return found

def verify_glb(raw, label):
    if len(raw) < 1500 or raw[:4] != b"glTF" or len(raw) > 8 * 1024 * 1024:
        raise ValueError(label + ": not a bounded glTF2 binary")
    version, total = struct.unpack_from("<II", raw, 4)
    size, chunk = struct.unpack_from("<I4s", raw, 12)
    if version != 2 or total != len(raw) or chunk != b"JSON" or size > 2 * 1024 * 1024:
        raise ValueError(label + ": invalid GLB header")
    obj = json.loads(raw[20:20+size])
    if len(obj.get("meshes",[])) < 1 or len(obj.get("materials",[])) < 1 or len(obj.get("nodes",[])) < 1:
        raise ValueError(label + ": empty model")
    banned = {"EXT_meshopt_compression", "KHR_draco_mesh_compression"}
    if banned.intersection(obj.get("extensionsRequired", [])):
        raise ValueError(label + ": unreviewed mesh decoder needed")
    return {"nodes":len(obj["nodes"]), "meshes":len(obj["meshes"]),
            "materials":len(obj["materials"]),"extensions":obj.get("extensionsUsed",[])}

def main():
    response = json.loads(get(MANIFEST))
    records = find_items(response)
    print("FREE_FITNESS_MANIFEST_MATCHES",len(records),
          [x[0][:90] for x in records[:52]])
    DST.mkdir(parents=True, exist_ok=True)
    provenance = []
    for name, expected, fixed_id in CHOICES:
        matches = [(title,url) for title,url in records
                   if expected.casefold() in title.casefold()]
        if matches:
            title, url = matches[0]
        elif fixed_id:
            title, url = expected, CDN + fixed_id + "/v1/model.glb"
        else:
            raise RuntimeError("Official pack manifest missing required fitness model: " + expected)
        if not re.fullmatch(r"https://cdn\.3dassets\.dev/assets/[0-9]+/v1/model\.glb",url):
            raise RuntimeError("Unexpected asset URL "+url)
        if fixed_id and url != CDN + fixed_id + "/v1/model.glb":
            raise RuntimeError("Unverified change in pinned official asset "+name)
        raw = get(url)
        structure = verify_glb(raw,name)
        target = DST / (name + ".glb")
        target.write_bytes(raw)
        provenance.append({"name":name,"sourceTitle":title,"sourceUrl":url,"bytes":len(raw),
            "sha256":hashlib.sha256(raw).hexdigest(),"model":structure,
            "license":"CC0-1.0","licenseSource":"https://3dassets.dev/terms",
            "review":"automated structure only; render approval still pending"})
        print("FITNESS_REAL_CC0_MODEL",name,len(raw),structure)
    (DST/"download-receipt.json").write_text(json.dumps(
        {"pack":MANIFEST,"scope":"isolated-kickboxing-room-only","noRings":True,
         "noHeavyBagReplacement":True,"modelCount":len(provenance),"assets":provenance},indent=2))
    print("FITNESS_CC0_DOWNLOAD_OK",len(provenance))

if __name__ == "__main__": main()
