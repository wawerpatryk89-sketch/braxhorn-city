"""Download only provider-native 2K Poly Haven PBR maps for casino T11.

Do not resize 4K to 2K, do not download 4K, and do not modify earlier A1 assets.
This script is a network-enabled PREPARATION step, NOT a Unity compile/build.
"""
from pathlib import Path
import hashlib
import io
import json
import time
import uuid
import urllib.request
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "unity-city/Assets/Source/Mistrzowie/CasinoT11/Textures2K"
SLUGS = (
    "concrete_brick_wall_001",  # existing original material, retained for older assets
    "sandstone_blocks_05",         # OWNER-APPROVED 7 Nov: solid side/rear casino masonry, native Poly Haven 2K
    "rectangular_facade_tiles",     # dark graphite exterior
    "granite_tile",                 # podium, planters and forecourt
    "clean_asphalt",                # real parking / roads
    "concrete_pavement_02",         # sidewalks
    "concrete_floor_02",           # roof
    "fabric_pattern_05",           # entrance red carpet
)
MAPS = (("diff", "jpg"), ("nor_gl", "png"), ("arm", "png"))
COLOUR_VARIANT = {"fabric_pattern_05": "col_01"}
GUID_NAMESPACE=uuid.UUID("9f795603-5fbb-4c20-b394-8826537507a2")

def fetch(url):
    last = None
    for retry in range(4):
        try:
            req=urllib.request.Request(url,headers={"User-Agent":"BRAXHORN-CasinoT11-2K/1.0"})
            with urllib.request.urlopen(req,timeout=90) as response:
                return response.read()
        except Exception as error:
            last=error
            if retry < 3: time.sleep((retry+1)*2)
    raise RuntimeError("Poly Haven 2K fetch failed: "+url) from last

def select(metadata, slug, kind, extension):
    keys = {
        "diff": (COLOUR_VARIANT[slug],) if slug in COLOUR_VARIANT else ("Diffuse", "diff"),
        "nor_gl": ("nor_gl",),
        "arm": ("arm",),
    }[kind]
    for key in keys:
        file = metadata.get(key,{}).get("2k",{}).get(extension)
        if isinstance(file,dict) and file.get("url"):
            return file
    raise RuntimeError("Missing Poly Haven native 2K "+slug+" "+kind+"."+extension+"; keys="+str(sorted(metadata)))

def check(data, metadata, filename):
    if metadata.get("size") is not None and len(data)!=metadata["size"]:
        raise ValueError("Poly Haven transfer size mismatch: "+filename)
    if metadata.get("md5") and hashlib.md5(data).hexdigest()!=metadata["md5"]:
        raise ValueError("Poly Haven transfer checksum mismatch: "+filename)
    with Image.open(io.BytesIO(data)) as image:
        if image.size!=(2048,2048):
            raise ValueError("Only genuine 2048x2048 Poly Haven maps allowed: "+filename)
    return hashlib.sha256(data).hexdigest()

def ensure_meta(path):
    # Store stable GUIDs for incremental updates; refuse changing existing IDs.
    relative=path.relative_to(ROOT/"unity-city").as_posix()
    expected=uuid.uuid5(GUID_NAMESPACE,relative).hex
    meta=Path(str(path)+".meta")
    if meta.exists():
        import re
        previous=re.search(r"^guid:\s*([0-9a-fA-F]{32})",meta.read_text(),re.M)
        if not previous or previous.group(1).lower()!=expected:
            raise ValueError("T11 original Unity GUID differs: "+str(meta))
    else:
        meta.write_text("fileFormatVersion: 2\nguid: "+expected+"\n",encoding="utf-8")


def prepare():
    DEST.mkdir(parents=True,exist_ok=True)
    ensure_meta(DEST.parent)
    ensure_meta(DEST)
    rows=[]
    for slug in SLUGS:
        metadata=json.loads(fetch("https://api.polyhaven.com/files/"+slug))
        for kind,extension in MAPS:
            source=select(metadata,slug,kind,extension)
            filename=slug+"_"+kind+"_2k."+extension
            destination=DEST/filename
            if destination.exists():
                data=destination.read_bytes()
            else:
                data=fetch(source["url"])
            digest=check(data,source,filename)
            if not destination.exists():
                temporary=destination.with_suffix(destination.suffix+".partial")
                temporary.write_bytes(data)
                temporary.replace(destination)
            ensure_meta(destination)
            rows.append({"file":filename,"sha256":digest,"source":source["url"],"bytes":len(data),"pixels":[2048,2048]})
            print("T11_POLYHAVEN_NATIVE_2K "+filename,flush=True)
    manifest=DEST.parent/"t11-polyhaven-2k-manifest.json"
    manifest.write_text(json.dumps({"status":"2K_SOURCE_VERIFIED",
        "slugs":len(SLUGS),"maps":len(rows),"sourceResolution":"2k",
        "upscaled":False,"rows":rows},indent=2),encoding="utf-8")
    ensure_meta(manifest)
    print("T11_POLYHAVEN_2K_PASS maps="+str(len(rows))+" source=2048x2048 GUIDS=STABLE")

if __name__=="__main__":
    prepare()
