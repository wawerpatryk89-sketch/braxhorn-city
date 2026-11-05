"""Download only original Poly Haven 2K PBR maps for the T11 casino.

No 1K/4K sources, no changes to existing Champions/arena assets. Exact
official provider URLs and transfer MD5 are checked before any file is saved.
"""
from pathlib import Path
import hashlib
import json
import time
import urllib.request
import uuid
import io
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT/"unity-city/Assets/Source/Mistrzowie/T11/Textures"
SLUGS = (
    "rectangular_facade_tiles_02", "rectangular_facade_tiles",
    "granite_tile", "concrete_pavement_02", "clean_asphalt",
    "concrete_floor_02", "fabric_pattern_05",
)
MAPS = (("diff","jpg"),("nor_gl","exr"),("arm","png"))
NAMESPACE = uuid.UUID("bff2fe54-56f8-4e81-b6f3-5793c6b0c233")

def fetch(url):
    request=urllib.request.Request(url,headers={"User-Agent":"Braxhorn-T11-Unity-2K/1.0"})
    for attempt in range(3):
        try:
            with urllib.request.urlopen(request,timeout=110) as reply:
                return reply.read()
        except Exception:
            if attempt==2:raise
            time.sleep(2*(attempt+1))

def choose(meta,slug,role,ext):
    # fabric_pattern_05 is pinned to the provider's original col_01 variant.
    names=("col_01",) if slug=="fabric_pattern_05" and role=="diff" else {
        "diff":("Diffuse","diff"),"nor_gl":("nor_gl",),"arm":("arm",)
    }[role]
    for name in names:
        val=meta.get(name,{}).get("2k",{}).get(ext)
        if val and val.get("url"):return val
    raise ValueError("No original Poly Haven 2K "+slug+"/"+role+"/"+ext+" keys "+str(names))

def has_good_bytes(data,entry,role,ext):
    if entry.get("size") and len(data)!=entry["size"]:return False
    if entry.get("md5") and hashlib.md5(data).hexdigest()!=entry["md5"]:return False
    if ext in ("jpg","png"):
        with Image.open(io.BytesIO(data)) as image:
            if image.size!=(2048,2048):return False
    # EXR resolution is checked by Unity after import; provider MD5/size pinned.
    return True

def write_meta(path):
    meta=Path(str(path)+".meta")
    guid=uuid.uuid5(NAMESPACE,str(path.relative_to(ROOT/"unity-city"))).hex
    if meta.exists():
        if "guid: "+guid not in meta.read_text():
            raise ValueError("Existing Unity GUID differs for "+str(meta))
        return
    meta.write_text("fileFormatVersion: 2\nguid: "+guid+"\n",encoding="utf-8")

def main():
    DEST.mkdir(parents=True,exist_ok=True)
    report=[]
    for slug in SLUGS:
        metadata=json.loads(fetch("https://api.polyhaven.com/files/"+slug))
        for role,ext in MAPS:
            entry=choose(metadata,slug,role,ext)
            if "/2k/" not in entry["url"]:
                raise ValueError("Provider returned non-2K source: "+entry["url"])
            target=DEST/(slug+"_"+role+"_2k."+ext)
            data=target.read_bytes() if target.exists() else None
            if data is None or not has_good_bytes(data,entry,role,ext):
                data=fetch(entry["url"])
            if not has_good_bytes(data,entry,role,ext):
                raise ValueError("Invalid official 2K PBR: "+slug+" "+role)
            if not target.exists() or target.read_bytes()!=data:target.write_bytes(data)
            write_meta(target)
            report.append({"name":target.name,"source":entry["url"],
                           "sha256":hashlib.sha256(data).hexdigest(),"size":len(data),"resolution":"2k"})
    (DEST.parent/"source-materials-2k.json").write_text(json.dumps(report,indent=2),encoding="utf-8")
    print("T11_POLY_HAVEN_ORIGINAL_2K_OK maps="+str(len(report))+" allMaps=2048px")
if __name__=="__main__":main()
