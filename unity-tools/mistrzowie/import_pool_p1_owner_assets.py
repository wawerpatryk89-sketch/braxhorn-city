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
from repair_pool_p1_towels import UnsafeTowelUvConversion, derive_safe_uv_import, sha256 as towel_sha256

ROOT=Path(__file__).resolve().parents[2]
ARCHIVE_NAME="P1_BASEN_SPA_ORIGINALNE_WYPOSAZENIE_20261106.zip"
ARCHIVE_SHA256="5dd31e8422b2bddc046a0692448ce8522f7a22aae3a5fa617e9a0d74a74f1263"
MODEL_PREFIX="unity-city/Assets/Source/Mistrzowie/PoolP1/OwnerEquipment/"
SIGN_PREFIX="unity-city/Assets/Source/Mistrzowie/CitySigns/01_GLOWNE/"
ALLOWED_DIRS=(MODEL_PREFIX,SIGN_PREFIX)
SOURCE_SIGN_NAMES=("P1_BASEN_BASEN_MIEJSKI.png","P1_SPA_SPA_WELLNESS.png")
TOWEL_ASSET=MODEL_PREFIX+"stack_of_towels_2k.glb"
# Outside Unity Assets: byte-for-byte owner original; only the safe copy is
# exposed to glTFast inside Assets, with the original stable owner .meta GUID.
TOWEL_BACKUP="unity-city/OriginalSources/Mistrzowie/PoolP1/stack_of_towels_2k.glb"
TOWEL_REPORT="mistrzowie-evidence/p1-towels-uv-import.json"

def sha(data):
    return hashlib.sha256(data).hexdigest()

def valid_name(name):
    path=Path(name)
    return (not path.is_absolute() and ".." not in path.parts and "\\" not in name
        and (name.startswith(ALLOWED_DIRS) or name==MODEL_PREFIX.rstrip("/")+".meta")
        and (name.endswith(".glb")
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
    original_assets=verify(archive)
    # Do not modify the signed owner ZIP or any original mesh/material/texture.
    # The towel GLB is NOT placed in the mobile scene (94k triangles). However
    # Unity pre-imports every file in Assets; unreferenced UV2/UV3 broke that
    # import. Build a safe derived import with the owner's unchanged GUID.
    assets=dict(original_assets)
    original=original_assets.get(TOWEL_ASSET)
    if original is None:
        raise ValueError("P1 owner towel source absent from verified original ZIP")
    try:
        compatible,towel_report=derive_safe_uv_import(original)
    except UnsafeTowelUvConversion as error:
        # The asset is optional and not instantiated anywhere in P1. Do not
        # destroy or visually downgrade it: report and leave its bytes intact.
        compatible=original
        towel_report={
            "status":"MANUAL_UV_REVIEW_REQUIRED_ORIGINAL_IMPORT_RETAINED",
            "warning":str(error),
            "originalSha256":towel_sha256(original),
            "importSha256":towel_sha256(original),
            "originalBinaryChunksPreserved":True,
            "originalTexturesPreserved":True,
        }
    assets[TOWEL_ASSET]=compatible
    assets[TOWEL_BACKUP]=original
    towel_report["placedInMobileScene"]=False
    towel_report["ownerSourcePreservedOutsideUnityAssets"]=True
    towel_report["unityTested"]=False
    pending=[]
    root=destination.resolve()
    for path,contents in sorted(assets.items(),key=lambda x:(x[0]!=TOWEL_BACKUP,x[0])):
        file=(destination/path).resolve()
        if not file.is_relative_to(root):
            raise ValueError("Unsafe P1 asset path")
        if file.exists():
            actual=sha(file.read_bytes())
            if actual!=sha(contents):
                # Permit ONLY the byte-for-byte owner original -> derived
                # import transition after recording its separate original.
                if path!=TOWEL_ASSET or actual!=sha(original):
                    raise ValueError("Refuse to replace an existing Unity source/GUID: "+path)
                pending.append((file,contents))
        else:
            pending.append((file,contents))
    if dry_run:
        print("P1_OWNER_ASSETS_DRY_RUN_PASS originals=16 pending="+str(len(pending))+
              " signPng=2 sourceModelDataPreserved=yes"+
              " towelImport="+towel_report["status"])
        return
    for file,contents in pending:
        file.parent.mkdir(parents=True,exist_ok=True)
        temporary=file.with_name(file.name+".partial")
        try:
            temporary.write_bytes(contents)
            os.replace(temporary,file)
        finally:
            temporary.unlink(missing_ok=True)
    # Manifest is evidence, NOT a gate on geometry/visual quality.
    report=(destination/TOWEL_REPORT)
    report.parent.mkdir(parents=True,exist_ok=True)
    report.write_text(json.dumps(towel_report,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print("P1_OWNER_ASSETS_IMPORTED originals=16 installed="+str(len(pending))+
          " verifiedOriginalSigns=2 UnityRun=no towelImport="+towel_report["status"])

if __name__=="__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archive",type=Path)
    parser.add_argument("--dry-run",action="store_true")
    args=parser.parse_args()
    install(args.archive,args.dry_run)
