"""Original Poly Haven 2K small-scale vegetation for Casino T11.

Use only native 2K glTF packages: low-size Shrub 03 for planters (17K tris)
and Quiver Tree 02 for two accent trees (154K tris), not the 5M-triangle
Tree Small 02. No 1K/4K texture sources downloaded or altered.
"""
import hashlib
import io
import json
from pathlib import Path, PurePosixPath
import re
import time
import urllib.request
import uuid

from PIL import Image
from pack_casino_vegetation_glb import pack_embedded_original

ROOT=Path(__file__).resolve().parents[2]
# Keep original external .gltf, binary and image bytes outside Unity Assets:
# glTFast fails to resolve their relative URLs in GitHub Actions Unity batchmode.
DEST=ROOT/"unity-city/OriginalSources/Mistrzowie/CasinoT11/Vegetation2K"
UNITY_DEST=ROOT/"unity-city/Assets/Source/Mistrzowie/CasinoT11/Vegetation2K"
SLUGS=("shrub_03","quiver_tree_02")
NAMESPACE=uuid.UUID("05de1a0a-7662-4828-bc5a-449716c626a4")

def get(url):
    last=None
    for i in range(3):
        try:
            request=urllib.request.Request(url,headers={"User-Agent":"Braxhorn-T11-Vegetation-2K"})
            with urllib.request.urlopen(request,timeout=120) as response:
                return response.read()
        except Exception as e:
            last=e
            if i<2:time.sleep(2*(i+1))
    raise RuntimeError("Official Poly Haven source unavailable: "+url) from last

def verified(data,item,name):
    if item.get("size") is not None and len(data)!=item["size"]:
        raise ValueError("Provider size mismatch: "+name)
    if item.get("md5") and hashlib.md5(data).hexdigest()!=item["md5"]:
        raise ValueError("Provider transfer MD5 mismatch: "+name)
    if name.lower().endswith((".png",".jpg",".jpeg")):
        with Image.open(io.BytesIO(data)) as image:
            if image.size!=(2048,2048):
                raise ValueError("Vegetation must have native 2048px maps: "+name)
    return hashlib.sha256(data).hexdigest()

def meta(path):
    root=ROOT/"unity-city"
    relative=path.relative_to(root).as_posix()
    expected=uuid.uuid5(NAMESPACE,relative).hex
    dest=Path(str(path)+".meta")
    if dest.exists():
        match=re.search(r"^guid:\s*([0-9a-fA-F]{32})",dest.read_text(),re.M)
        if not match or match.group(1).lower()!=expected:
            raise ValueError("Existing Unity GUID differs: "+str(dest))
    else:
        dest.write_text("fileFormatVersion: 2\nguid: "+expected+"\n",encoding="utf-8")

def prepare():
    DEST.mkdir(parents=True,exist_ok=True)
    UNITY_DEST.mkdir(parents=True,exist_ok=True)
    meta(UNITY_DEST)
    rows=[]
    packed=[]
    for slug in SLUGS:
        data=json.loads(get("https://api.polyhaven.com/files/"+slug))
        item=data.get("gltf",{}).get("2k",{}).get("gltf")
        if not item or "/2k/" not in item.get("url",""):
            raise ValueError("No original native 2K glTF for "+slug)
        dest=DEST/slug
        dest.mkdir(exist_ok=True)
        unity_slug=UNITY_DEST/slug
        unity_slug.mkdir(exist_ok=True)
        meta(unity_slug)
        filenames={slug+"_2k.gltf":item}
        for name,ref in item.get("include",{}).items():
            pure=PurePosixPath(name)
            if pure.is_absolute() or ".." in pure.parts or not (
                name.endswith(".bin") or name.endswith((".jpg",".png"))
            ):
                raise ValueError("Unsafe Poly Haven linked file "+name)
            if not name.endswith(".bin") and "/2k/" not in ref.get("url",""):
                raise ValueError("Only native Poly Haven 2K source textures allowed: "+name)
            if name.endswith((".jpg",".png")) and "_2k." not in name:
                raise ValueError("Only 2K vegetation maps allowed "+name)
            filenames[name]=ref
        for name,ref in filenames.items():
            path=dest/name
            path.parent.mkdir(parents=True,exist_ok=True)
             content=path.read_bytes() if path.exists() else get(ref["url"])
            checksum=verified(content,ref,name)
            if not path.exists():path.write_bytes(content)
            rows.append({"file":str(path.relative_to(ROOT/"unity-city")),"sha256":checksum,
                         "bytes":len(content),"url":ref["url"],"textureResolution":"2k"})
        embedded=unity_slug/(slug+"_2k_embedded.glb")
        packed.append(pack_embedded_original(dest/(slug+"_2k.gltf"),embedded))
        meta(embedded)
    manifest=DEST/"source-vegetation-2k.json"
    manifest.write_text(json.dumps({"originalSourceFiles":rows,"losslessEmbeddedModels":packed},indent=2),encoding="utf-8")
    print("CHAMPIONS_T11_ORIGINAL_POLYHAVEN_VEGETATION_2K_OK models=2 files="+str(len(rows))+" lossless_glb=2")
if __name__=="__main__":prepare()
