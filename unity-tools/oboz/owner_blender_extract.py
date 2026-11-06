#!/usr/bin/env python3
"""Recover exact original compressed Blender mesh sources from owner's 5 ZIPs.

The previous importer restored only image maps, skipping *.blend and leaving
Grass Medium 01/02 and the 4K cabinet without REAL geometry in Unity.
Never add Blender sources to public git or Unity's Assets import tree.
"""
from __future__ import annotations
import argparse
import gzip
import hashlib
import io
from pathlib import Path
import subprocess
import zipfile

ROOT=Path(__file__).resolve().parents[2]
STAGING=ROOT/"unity-tools/oboz/private_blender"
HASHES={
 "grass_medium_01_2k":(7142652,"87f1d30de8fb49a5146a1ea2f1070533c753b484ee5fc26bf7a9ce83f7315fb9"),
 "grass_medium_02_2k":(1848566,"493063e65b09c9f8fb79d764e550dd3ca54ebea119859b41a171354fc8306e8f"),
 "drawer_cabinet_4k":(2807012,"70a2d48a6d0f2d27f51f4bc739831f97a6fbdd256a83c1dc116e773ec0a1df5e"),
}
def sha(b):return hashlib.sha256(b).hexdigest()

def decode(data):
    if data.startswith(bytes.fromhex("28b52ffd")):
        p=subprocess.run(["zstd","--decompress","--stdout"],input=data,
                         capture_output=True,check=True)
        return p.stdout
    if data.startswith(bytes.fromhex("1f8b")):return gzip.decompress(data)
    if data.startswith(b"BLENDER-"):return data
    raise ValueError("Unknown compressed Blender asset format")

def retrieve(archive_paths):
    source_files={}
    for path in archive_paths:
        with zipfile.ZipFile(path) as z:
            if z.testzip() is not None:raise ValueError("Damaged owner ZIP: "+str(path))
            for member in z.namelist():
                base=Path(member).name
                key=base.removesuffix(".blend.zip")
                if key not in HASHES or base!=key+".blend.zip":continue
                if key in source_files:raise ValueError("Duplicate original Blender source "+key)
                raw=z.read(member)
                with zipfile.ZipFile(io.BytesIO(raw)) as inner:
                    expected={key+".blend"}
                    if set(inner.namelist())!=expected or inner.testzip() is not None:
                        raise ValueError("Unexpected original Blender archive "+key)
                    packed=inner.read(key+".blend")
                decoded=decode(packed)
                size,digest=HASHES[key]
                if len(decoded)!=size or sha(decoded)!=digest or not decoded.startswith(b"BLENDER-"):
                    raise ValueError("Original Blender binary hash/format mismatch: "+key)
                source_files[key]=decoded
    if set(source_files)!=set(HASHES):
        raise ValueError("Original Grass Medium 01/02 and cabinet Blender meshes incomplete: "+
                         str(sorted(set(HASHES)-set(source_files))))
    return source_files

def main():
    ap=argparse.ArgumentParser()
    g=ap.add_mutually_exclusive_group(required=True)
    g.add_argument("--package",type=Path)
    g.add_argument("--parts",type=Path,nargs=5)
    args=ap.parse_args()
    files=retrieve([args.package] if args.package else args.parts)
    STAGING.mkdir(parents=True,exist_ok=True)
    for key,data in sorted(files.items()):
        dest=STAGING/(key+".blend")
        if dest.exists():
            if sha(dest.read_bytes())!=sha(data):
                raise ValueError("Different Blender original at private staging path "+str(dest))
        else:
            with dest.open("xb") as out:out.write(data)
    print("BRAXHORN_OBOZ_OWNER_BLEND_RECOVER=PASS meshes=3 "+
          "sourceMeshes=ORIGINAL decompress=zstd/gzip noDecimation=true noPublicGit=true")

if __name__=="__main__":
    main()
