"""Import the owner's original P1 SPA models + BASEN/SPA sign PNGs.

Offline, lossless, never overwrites a different original/GUID. The source
package is delivered separately because the public git repo must not act as a
redistributable asset library. This does NOT run Unity or create APK.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import re
import zipfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
UNITY=ROOT/"unity-city"
ARCHIVE_NAME="P1_SPA_ORYGINALY_MODELE_SZYLDY_DO_RELEASE_20261106.zip"
ARCHIVE_SHA256="3b86a094f3b4b31ff11597d78272e853181857f057d429f8b4d13493ab0059e9"
EXPECTED={
    "Assets/Source/Mistrzowie/PoolP1/OwnerEquipment/sauna_product_model.glb":
        "62206a6a5b5aeb83fda4ea4881bb9d7aec82d1f72b3de37105f9800e2eebaadd",
    "Assets/Source/Mistrzowie/PoolP1/OwnerEquipment/portable-massage-table.glb":
        "c82de8d72f4bc1d122d30ace216f932ce0bbee11e0a53f9ec2e188eea7dbbe5b",
    "Assets/Source/Mistrzowie/PoolP1/OwnerEquipment/double-bathroom-vanity.glb":
        "f2a92364d2e558cf8396845548bd3581dc063a1952d38007cb73a610506c7dc7",
    "Assets/Source/Mistrzowie/PoolP1/OwnerEquipment/hot-tub-original-exterior-model.glb":
        "92bb0f03f9fa6c7c3a0dcf6ae01a32497713c75382b7d7a4844f7ec02227ceda",
    "Assets/Source/Mistrzowie/PoolP1/OwnerEquipment/shower_cabin.glb":
        "64333aca51953657efc3a8c440475e14971f97e7f0efa966dee9c7aae68eb5ef",
    "Assets/Source/Mistrzowie/PoolP1/OwnerEquipment/lockers.glb":
        "0df2b853f08082e72a2e26983a7b51f3cb7990d213076d066fae21d6b2c2e2b9",
    "Assets/Source/Mistrzowie/PoolP1/OwnerEquipment/locker_room_bench_free_model.glb":
        "deeb46c2d89e15b2f129d0a84c342547d6e8914fdbbd751616e695300a5a428b",
    "Assets/Source/Mistrzowie/CitySigns/01_GLOWNE/P1_BASEN_BASEN_MIEJSKI.png":
        "e741c1132fc269e4cb8573928f88fb8b958880be249b088ab9c80351ac258f6a",
    "Assets/Source/Mistrzowie/CitySigns/01_GLOWNE/P1_SPA_SPA_WELLNESS.png":
        "6370d9e0966c8919e61ab261ac26efe0c655d1eb1f3ac96736efe3e49d059dd4",
}
META_SUFFIX=".meta"
EXTRA={"P1_SPA_SOURCE_MANIFEST.json","P1_SPA_README.txt"}

def sha(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()

def validate(archive: Path):
    if archive.name!=ARCHIVE_NAME:
        raise ValueError("Wrong owner package: "+archive.name)
    if sha(archive.read_bytes())!=ARCHIVE_SHA256:
        raise ValueError("P1 source ZIP SHA256 differs; refusing to overwrite owner originals")
    data={}
    with zipfile.ZipFile(archive) as z:
        if z.testzip() is not None:raise ValueError("ZIP integrity error")
        names=z.namelist()
        allowed=set(EXPECTED)|{p+META_SUFFIX for p in EXPECTED}|EXTRA
        if len(names)!=len(set(names)) or set(names)!=allowed:
            raise ValueError("P1 ZIP missing/extra/unexpected entries")
        manifest=json.loads(z.read("P1_SPA_SOURCE_MANIFEST.json"))
        records=manifest.get("assets",[])
        if manifest.get("schema")!="P1-SPA-OWNER-ORIGINALS-20261106" or \
            len(records)!=len(EXPECTED) or {x["path"] for x in records}!=set(EXPECTED):
            raise ValueError("P1 original source manifest mismatch")
        for r in records:
            path=r["path"]
            if Path(path).is_absolute() or ".." in Path(path).parts or "\\" in path:
                raise ValueError("Unsafe source pathname "+path)
            raw=z.read(path);meta=z.read(path+META_SUFFIX)
            if sha(raw)!=EXPECTED[path] or sha(raw)!=r["sha256"] or len(raw)!=r["bytes"]:
                raise ValueError("Modified original source: "+path)
            if sha(meta)!=r["original_meta_sha256"] or \
                not re.search(rb"^guid:\s*[a-fA-F0-9]{32}\s*$",meta,re.M):
                raise ValueError("Original Unity GUID changed: "+path)
            data[path]=raw;data[path+META_SUFFIX]=meta
    return data

def install(data, dry_run=False):
    pending=[]
    for rel,raw in sorted(data.items()):
        dst=UNITY/rel
        if dst.exists():
            if sha(dst.read_bytes())!=sha(raw):
                raise ValueError("Existing owner source/GUID differs, no silent replacement: "+str(dst))
            continue
        pending.append((dst,raw))
    if not dry_run:
        for dst,raw in pending:
            dst.parent.mkdir(parents=True,exist_ok=True)
            temp=dst.with_name(dst.name+".partial")
            temp.write_bytes(raw)
            temp.replace(dst)
    return len(pending)

if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("archive",type=Path)
    parser.add_argument("--dry-run",action="store_true")
    args=parser.parse_args()
    files=validate(args.archive)
    n=install(files,args.dry_run)
    print("P1 SPA ASSET IMPORT "+("DRY_RUN_PASS" if args.dry_run else "PASS")+
          " sources="+str(len(EXPECTED))+" total_files="+str(len(files))+
          " pending="+str(n)+" verified_hashes=YES original_GUIDs=PRESERVED"+
          " Unity_compilation=NOT_RUN")
