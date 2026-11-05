"""Import all 148 original Mistrzowie signs, preserving pixels and Unity GUIDs.

Requires the two separately supplied Unity-ready ZIPs, from runs 01-06 and
07-13. No sign artwork is redrawn, recolored or replaced; this is an OFFLINE
asset extraction step, not a Unity compile or build.
"""
import argparse
import hashlib
import json
from pathlib import Path
import re
import struct
import zipfile

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / "unity-city"
PREFIX = "Assets/Source/Mistrzowie/CitySigns/"
CASINO = PREFIX + "01_GLOWNE/C1_KASYNO_KASYNO_WAWER.png"
PACKAGE_SHA = {
    "MIASTO_MISTRZOW_SZYLDY_01_06_UNITY.zip": ("fcce54bdf976c343fbbeffde614f385f8ef82f5819c64f70dac84c73b105efce",70),
    "MIASTO_MISTRZOW_SZYLDY_07_13_UNITY.zip": ("e3691fc12d18c05ca42c71abfbf046a566d9af6764f49a30c41ca05a4f5eb5c7",78),
}
NAMESPACE = "974b9566-b4e7-4a61-a616-27e03334f842"

def digest_file(path):
    hash_ = hashlib.sha256()
    with Path(path).open("rb") as stream:
        for part in iter(lambda:stream.read(1048576), b""):
            hash_.update(part)
    return hash_.hexdigest()

def safe_path(name):
    from pathlib import PurePosixPath
    p=PurePosixPath(name)
    return not p.is_absolute() and ".." not in p.parts and "\\" not in name

def png_dimensions(content):
    if content[:8] != b"\x89PNG\r\n\x1a\n" or content[12:16] != b"IHDR":
        raise ValueError("Owner sign is not a valid PNG header")
    return struct.unpack(">II",content[16:24])

def inspect_package(path):
    path=Path(path)
    if path.name not in PACKAGE_SHA:
        raise ValueError("Unexpected source package name: "+path.name)
    wanted,count=PACKAGE_SHA[path.name]
    if digest_file(path)!=wanted:
        raise ValueError("Unexpected owner ZIP content SHA256: "+path.name)
    with zipfile.ZipFile(path) as archive:
        bad=archive.testzip()
        if bad:raise ValueError("Invalid ZIP CRC: "+bad)
        entries=archive.namelist()
        if len(set(entries))!=len(entries):
            raise ValueError("Repeated ZIP entry names")
        allowed_metadata={"SIGN_LIBRARY_MANIFEST.json","IMPORT-README.txt"}
        if any(not safe_path(name) or
               not(name.startswith(PREFIX) or name in allowed_metadata) for name in entries):
            raise ValueError("Unexpected or unsafe ZIP entry")
        manifest=json.loads(archive.read("SIGN_LIBRARY_MANIFEST.json"))
        assets=manifest.get("assets",[])
        if manifest.get("count")!=count or len(assets)!=count or manifest.get("asset_namespace")!=NAMESPACE:
            raise ValueError("Owner manifest mismatch")
        originals={}
        for asset in assets:
            name=asset["path"]
            if not name.startswith(PREFIX) or not name.endswith(".png") or name not in entries:
                raise ValueError("Source path missing: "+name)
            raw=archive.read(name)
            if hashlib.sha256(raw).hexdigest()!=asset["sha256"] or len(raw)!=asset["bytes"]:
                raise ValueError("Changed original sign: "+name)
            if png_dimensions(raw)!=(2048,512):
                raise ValueError("Source dimensions must remain 2048x512: "+name)
            meta=name+".meta"
            if meta not in entries:
                raise ValueError("Missing pinned Unity GUID: "+meta)
            match=re.search(r"^guid:\s*([0-9a-fA-F]{32})\s*$",archive.read(meta).decode(),re.M)
            if not match or match.group(1).lower()!=asset["unity_guid"]:
                raise ValueError("Meta GUID differs from manifest: "+meta)
            originals[name]=raw
        images=[name for name in entries if name.endswith(".png")]
        metas=[name for name in entries if name.endswith(".meta")]
        if len(images)!=count or len(metas)!=count+(3 if count==70 else 5):
            raise ValueError("Unexpected original graphic/metadata count")
        if len(entries)!=len(images)+len(metas)+2:
            raise ValueError("Unexpected files in collection")
        data={name:archive.read(name) for name in entries if name.startswith(PREFIX)}
        return data,originals

def install(packages, dry_run=False, destination=DEST):
    if len(packages)!=2 or {Path(p).name for p in packages}!=set(PACKAGE_SHA):
        raise ValueError("Both separate original ZIPs 01-06 and 07-13 are required")
    merged={}
    images={}
    for package in packages:
        data,signs=inspect_package(package)
        if set(images)&set(signs):raise ValueError("Duplicated sign identity across packages")
        images.update(signs)
        for key,value in data.items():
            if key in merged and merged[key]!=value:
                raise ValueError("Conflicting pinned Unity GUID or source: "+key)
            merged[key]=value
    if len(images)!=148 or CASINO not in images:
        raise ValueError("Must provide all 148 originals including C1 KASYNO WAWER")
    for name,raw in merged.items():
        dst=Path(destination)/name
        if dst.exists() and hashlib.sha256(dst.read_bytes()).digest()!=hashlib.sha256(raw).digest():
            raise ValueError("Refusing to overwrite changed original or GUID: "+str(dst))
    if not dry_run:
        for name,raw in sorted(merged.items()):
            dst=Path(destination)/name
            if dst.exists():continue
            dst.parent.mkdir(parents=True,exist_ok=True)
            temp=dst.with_name(dst.name+".partial")
            temp.write_bytes(raw)
            temp.replace(dst)
    return {"status":"DRY_RUN_PASS" if dry_run else "IMPORTED",
            "sourcePng":len(images),"files":len(merged),"casinoSign":CASINO,
            "unchangedArtwork":True,"unityEditorCompiled":False}

if __name__=="__main__":
    cli=argparse.ArgumentParser()
    cli.add_argument("archives",nargs=2,help="01_06_UNITY.zip 07_13_UNITY.zip")
    cli.add_argument("--dry-run",action="store_true")
    a=cli.parse_args()
    print(json.dumps(install(a.archives,a.dry_run),ensure_ascii=False))
