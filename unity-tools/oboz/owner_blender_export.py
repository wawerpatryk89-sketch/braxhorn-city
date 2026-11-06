#!/usr/bin/env python3
"""Convert verified real owner .blend meshes to glTF for Unity 6 camp ONLY.

Requires blender installed on the GitHub runner. No source downsampling or
mesh decimation. Reject missing/invalid assets before Unity compilation.
"""
from __future__ import annotations
import hashlib
import json
from pathlib import Path
import shutil
import struct
import subprocess
import sys

ROOT=Path(__file__).resolve().parents[2]
STAGE=ROOT/"unity-tools/oboz/private_blender"
TARGET=ROOT/"unity-city/Assets/Source/Prywatne/Oboz/OwnerAssets"
EXPORT_SCRIPT=ROOT/"unity-tools/oboz/owner_blender_export_bpy.py"
LOCKED={
 "grass_medium_01_2k":"87f1d30de8fb49a5146a1ea2f1070533c753b484ee5fc26bf7a9ce83f7315fb9",
 "grass_medium_02_2k":"493063e65b09c9f8fb79d764e550dd3ca54ebea119859b41a171354fc8306e8f",
 "drawer_cabinet_4k":"70a2d48a6d0f2d27f51f4bc739831f97a6fbdd256a83c1dc116e773ec0a1df5e",
}
def sha(path):
    h=hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda:f.read(1<<20),b""):h.update(chunk)
    return h.hexdigest()

def check_glb(path):
    raw=path.read_bytes()
    if len(raw)<64 or raw[:4]!=b"glTF" or int.from_bytes(raw[4:8],"little")!=2 or int.from_bytes(raw[8:12],"little")!=len(raw):
        raise ValueError("Not a valid original mesh GLB: "+str(path))
    jlen=int.from_bytes(raw[12:16],"little")
    if raw[16:20]!=b"JSON" or jlen+20>len(raw):
        raise ValueError("Broken GLB JSON")
    data=json.loads(raw[20:20+jlen])
    if not data.get("meshes") or not data.get("nodes"):
        raise ValueError("GLB lacks source geometry")
    return len(data["meshes"])

def main():
    executable=shutil.which("blender")
    if not executable:raise RuntimeError("Official Blender is not installed; cannot build from fake grass")
    from owner_assets_intake import ensure_stable_meta
    audit=[]
    for name,digest in LOCKED.items():
        source=STAGE/(name+".blend")
        if not source.is_file() or sha(source)!=digest:
            raise ValueError("Missing/unverified owner Blender original: "+name)
        textures=TARGET/name/"textures"
        if not textures.is_dir() or len(list(textures.iterdir()))<3:
            raise ValueError("Owner original PBR textures missing: "+name)
        target=TARGET/name/(name+".glb")
        target.parent.mkdir(parents=True,exist_ok=True)
        if target.exists():
            check_glb(target)
            count=1
            print("BRAXHORN_OBOZ_BLEND_CACHED: "+name)
        else:
            subprocess.run([executable,"--background","--threads","2",
                "--python",str(EXPORT_SCRIPT),"--",str(source),str(target),str(textures)],
                cwd=ROOT,check=True,timeout=900)
            count=check_glb(target)
        # These files stay in the ignored PRIVATE Assets/Source/Prywatne tree.
        ensure_stable_meta(TARGET,Path(name)/(name+".glb"))
        audit.append({"name":name,"glb_sha256":sha(target),"mesh_count":count,
                      "bytes":target.stat().st_size,
                      "original_blend_sha256":digest,
                      "source_mesh_unmodified":True})
    print("BRAXHORN_OBOZ_GENUINE_BLEND_GLB=PASS "+
          json.dumps(audit,sort_keys=True,ensure_ascii=False))

if __name__=="__main__":
    main()
