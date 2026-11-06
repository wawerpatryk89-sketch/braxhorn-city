"""P1 owner-only assets: checksum-locked offline extractor, no Unity build.

Use only the private owner-delivered P1_BASEN_SPA_ORIGINALNE_WYPOSAZENIE_20261106.zip.
Keep original GLB/FBX/sign byte streams, owner sign GUIDs and selected model GUIDs.
A different pack or stale asset is a hard error, NOT permission to replace assets.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import struct
import zipfile

ROOT=Path(__file__).resolve().parents[2]
ARCHIVE_NAME="P1_BASEN_SPA_ORIGINALNE_WYPOSAZENIE_20261106.zip"
ARCHIVE_SHA256="5dd31e8422b2bddc046a0692448ce8522f7a22aae3a5fa617e9a0d74a74f1263"
MODEL_PREFIX="unity-city/Assets/Source/Mistrzowie/PoolP1/OwnerEquipment/"
SIGN_PREFIX="unity-city/Assets/Source/Mistrzowie/CitySigns/01_GLOWNE/"
ALLOWED_DIRS=(MODEL_PREFIX,SIGN_PREFIX)
SOURCE_SIGN_NAMES=("P1_BASEN_BASEN_MIEJSKI.png","P1_SPA_SPA_WELLNESS.png")

def sha(data):
    return hashlib.sha256(data).hexdigest()

def valid_name(name):
    path=Path(name)
    return (not path.is_absolute() and ".." not in path.parts and "\\" not in name
        and name.startswith(ALLOWED_DIRS) and (name.endswith(".glb")
            or name.endswith(".fbx") or name.endswith(".png") or name.endswith(".meta")))

def verify(archive:Path):
    if archive.name!=ARCHIVE_NAME:
        raise ValueError("P1 owner source archive name mismatch: "+archive.name)
    if not archive.is_file() or sha(archive.read_bytes())!=ARCHIVE_SHA256:
        raise ValueError("P1 owner source ZIP SHA256 mismatch")
    with zipfile.ZipFile(archive) as z:
        if z.testzip() is not None:raise ValueError("P1 ZIP CRC failed")
        members=z.namelist()
        if len(members)!=len(set(members)):raise ValueError("Duplicate P1 ZIP entries")
        mandatory={"P1_OWNER_SOURCE_MANIFEST.json","LICENCES_3D.txt"}
        assets={n for n in members if n not in mandatory}
        if mandatory-set(members) or not assets or any(not valid_name(n) for n in assets):
            raise ValueError("Invalid P1 owner ZIP members")
        info=z.infolist()
        if any(i.is_dir() or (i.external_attr >> 16)&0o170000==0o120000 for i in info):
            raise ValueError("P1 ZIP has unsupported directories or symlinks")
        manifest=json.loads(z.read("P1_OWNER_SOURCE_MANIFEST.json"))
        records=manifest["files"]
        if len(records)!=16 or set(records)!=set(n for n in assets if not n.endswith(".meta")):
            raise ValueError("P1 expected exactly 16 original source files")
        for path,data in records.items():
            raw=z.read(path)
            if len(raw)!=data["bytes"] or sha(raw)!=data["sha256"]:
                raise ValueError("P1 owner source changed: "+path)
            if path.endswith(".png"):
                if raw[:8]!=b"\x89PNG\r\n\x1a\n" or struct.unpack(">II",raw[16:24])!=(2048,512):
                    raise ValueError("Original BASEN/SPA sign is not 2048x512: "+path)
            elif path.endswith(".glb") and raw[:4]!=b"glTF":
                raise ValueError("P1 input GLB broken: "+path)
            meta=path+".meta"
            if meta not in assets:raise ValueError("P1 original stable GUID absent: "+meta)
            text=z.read(meta).decode()
            if not re.search(r"^guid:\s*[0-9a-fA-F]{32}\s*$",text,re.M):
                raise ValueError("Invalid P1 Unity GUID: "+meta)
        for name in SOURCE_SIGN_NAMES:
            if SIGN_PREFIX+name not in records:raise ValueError("Original sign missing: "+name)
        return {name:z.read(name) for name in assets}

def install(archive:Path,dry_run=False,destination=ROOT):
    assets=verify(archive)
    pending=[]
    for path,contents in sorted(assets.items()):
        file=(destination/path).resolve()
        if not file.is_relative_to(destination.resolve()):
            raise ValueError("Unsafe P1 asset path")
        if file.exists():
            if sha(file.read_bytes())!=sha(contents):
                raise ValueError("Refuse to replace an existing Unity source/GUID: "+path)
        else:pending.append((file,contents))
    if dry_run:
        print("P1_OWNER_ASSETS_DRY_RUN_PASS originals=16 pending="+str(len(pending))+
              " signPng=2 sourceModelDataPreserved=yes")
        return
    for file,contents in pending:
        file.parent.mkdir(parents=True,exist_ok=True)
        temporary=file.with_name(file.name+".partial")
        try:
            temporary.write_bytes(contents)
            os.replace(temporary,file)
        finally:temporary.unlink(missing_ok=True)
    print("P1_OWNER_ASSETS_IMPORTED originals=16 installed="+str(len(pending))+
          " verifiedOriginalSigns=2 UnityRun=no")

if __name__=="__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archive",type=Path)
    parser.add_argument("--dry-run",action="store_true")
    args=parser.parse_args()
    install(args.archive,args.dry_run)
