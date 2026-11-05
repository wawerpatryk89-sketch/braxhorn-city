"""Fetch the EXACT Shuriken GLB submitted by the owner for P1.

Copyright: Shuriken Studio free model licence 2026-10-25, permits in-game
use/modification, prohibits distributing model files as a standalone asset pack.
This script fetches from the publisher in CI; the raw GLB is NOT committed
to an unauthorised public model library. Never substitute another asset.
"""
from __future__ import annotations
import hashlib
import json
from pathlib import Path
from urllib.request import Request, urlopen
from uuid import UUID, uuid5

ROOT=Path(__file__).resolve().parents[2]
DEST=ROOT/"unity-city/Assets/Source/Mistrzowie/PoolP1/OwnerEquipment"
NAME="raft-slide-water-park.glb"
SOURCE="https://shuriken.studio/models/download/1b4fca38-6ce0-4866-ae74-27bab9c9241d"
EXPECTED_SHA256="e001b4e47c59e43c31155b8d4c8e2f3f3caa3ac9627de9753d10a02802cc7a8f"
EXPECTED_SIZE=281288
GUID_NS=UUID("210a7947-e8e3-450c-972f-e9d90be0e1aa")

def checksum(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()

def pin_meta(path: Path):
    rel=path.relative_to(ROOT/"unity-city").as_posix()
    expected=uuid5(GUID_NS,rel).hex
    meta=Path(str(path)+".meta")
    if meta.exists():
        import re
        found=re.search(r"^guid:\s*([0-9a-f]{32})\s*$",meta.read_text(),re.M)
        if not found or found[1]!=expected:
            raise ValueError("P1 slide Unity GUID changed: "+str(meta))
    else:
        meta.write_text("fileFormatVersion: 2\nguid: "+expected+"\n",encoding="utf-8")
    return expected

def prepare():
    DEST.mkdir(parents=True,exist_ok=True)
    pin_meta(DEST.parent)
    pin_meta(DEST)
    target=DEST/NAME
    if target.exists():
        contents=target.read_bytes()
    else:
        request=Request(SOURCE,headers={"User-Agent":"BRAXHORN-CITY-Unity6-P1/1.0"})
        with urlopen(request,timeout=90) as response:
            contents=response.read(EXPECTED_SIZE+1)
    if len(contents)!=EXPECTED_SIZE or checksum(contents)!=EXPECTED_SHA256 or contents[:4]!=b"glTF":
        raise ValueError("Owner-approved raft slide asset mismatch: bytes, sha256 or GLB header")
    if not target.exists():
        partial=target.with_suffix(".glb.partial")
        partial.write_bytes(contents)
        partial.replace(target)
    guid=pin_meta(target)
    manifest=DEST/"SOURCE_RECEIPT.json"
    manifest.write_text(json.dumps({
        "status":"ORIGINAL_OWNER_SUPPLIED_GLTF_VERIFIED",
        "asset":NAME,"source":SOURCE,"sha256":EXPECTED_SHA256,
        "size":EXPECTED_SIZE,"unityGuid":guid,
        "sourceModelUnits":"as supplied",
        "nativeBoundsMetres":{"x":[-33.35,44.30],"y":[-.01,33.0],"z":[-59.73,67.44]},
        "licensedForGames":"https://shuriken.studio/legal/models",
        "buildUnity":"NOT_RUN"
    },indent=2)+"\n",encoding="utf-8")
    pin_meta(manifest)
    print("P1_RAFT_SLIDE_SOURCE_VERIFIED sha256="+EXPECTED_SHA256+" GUID="+guid)

if __name__=="__main__":
    prepare()
