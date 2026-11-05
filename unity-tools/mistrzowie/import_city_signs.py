"""Import BOTH original sign libraries (01-06, 07-13) without resizing/retouching.

The SHA-pinned ZIPs contain 148 unique original 2048x512 PNG files and
stable UUIDv5 Unity .meta files. Read-only dry-run; never overwrites edits.
No network, no Unity invocation, no build.
"""
import argparse
import hashlib
import json
import pathlib
import struct
import uuid
import zipfile

ROOT=pathlib.Path(__file__).resolve().parents[2]
PROJECT=ROOT/"unity-city"
BASE="Assets/Source/Mistrzowie/CitySigns/"
MAIN_CASINO=BASE+"01_GLOWNE/C1_KASYNO_KASYNO_WAWER.png"
MAIN_CASINO_SHA="78edc9f51c772d20cb84c43f0850716a63c5a772fa0b326a7aea15ccfe39bf1b"
GUID_NAMESPACE=uuid.UUID("974b9566-b4e7-4a61-a616-27e03334f842")
PACKS={
    "fcce54bdf976c343fbbeffde614f385f8ef82f5819c64f70dac84c73b105efce":(70,73,145,"01-06"),
    "e3691fc12d18c05ca42c71abfbf046a566d9af6764f49a30c41ca05a4f5eb5c7":(78,83,163,"07-13"),
}

def digest(path):
    h=hashlib.sha256()
    with pathlib.Path(path).open("rb") as stream:
        for chunk in iter(lambda:stream.read(1048576),b""):h.update(chunk)
    return h.hexdigest()

def inspect(path):
    checksum=digest(path)
    if checksum not in PACKS:raise ValueError("Unknown/modified original sign archive SHA256: "+checksum)
    expected_png,expected_meta,expected_all,part=PACKS[checksum]
    with zipfile.ZipFile(path) as archive:
        if archive.testzip() is not None:raise ValueError("Corrupted sign archive")
        names=archive.namelist()
        if len(names)!=len(set(names)) or len(names)!=expected_all:raise ValueError("Wrong entry count/duplicate sign path")
        signs=[name for name in names if name.startswith(BASE) and name.endswith(".png")]
        metas=[name for name in names if name.startswith(BASE) and name.endswith(".meta")]
        if len(signs)!=expected_png or len(metas)!=expected_meta:raise ValueError("Wrong original PNG or GUID count")
        for name in names:
            parts=pathlib.PurePosixPath(name).parts
            if ".." in parts or name.startswith("/") or not (name.startswith(BASE) or name in ("SIGN_LIBRARY_MANIFEST.json","IMPORT-README.txt")):
                raise ValueError("Unexpected archive path: "+name)
        manifest=json.loads(archive.read("SIGN_LIBRARY_MANIFEST.json"))
        if manifest["schema"]!="BRAXHORN_CITY_SIGNS_V1" or manifest["count"]!=expected_png:
            raise ValueError("Original manifest mismatch")
        catalog=manifest["assets"]
        if len(catalog)!=expected_png or {r["path"] for r in catalog}!=set(signs):
            raise ValueError("Original sign manifest does not match images")
        for row in catalog:
            png=row["path"];blob=archive.read(png)
            if blob[:8]!=b"\\x89PNG\\r\\n\\x1a\\n" or struct.unpack(">II",blob[16:24])!=(2048,512):
                raise ValueError("Original graphic dimensions changed: "+png)
            if hashlib.sha256(blob).hexdigest()!=row["sha256"]:
                raise ValueError("Original image digest mismatch: "+png)
            if row["unity_guid"]!=uuid.uuid5(GUID_NAMESPACE,png).hex:
                raise ValueError("Original sign GUID changed: "+png)
            if png+".meta" not in metas:raise ValueError("Missing sign GUID sidecar: "+png)
        for meta in metas:
            original=meta[:-5]
            wanted=uuid.uuid5(GUID_NAMESPACE,original).hex
            raw=archive.read(meta).decode("utf-8")
            if ("guid: "+wanted) not in raw:raise ValueError("Different Unity GUID metadata: "+meta)
        if part=="01-06":
            if MAIN_CASINO not in signs or hashlib.sha256(archive.read(MAIN_CASINO)).hexdigest()!=MAIN_CASINO_SHA:
                raise ValueError("Wrong C1 sign: expected owner-supplied KASYNO WAWER")
        return sorted(name for name in names if name.startswith(BASE)),part,expected_png

def import_archive(path,dry_run=False):
    path=pathlib.Path(path)
    names,part,count=inspect(path)
    with zipfile.ZipFile(path) as archive:
        # Preflight every existing file before making a single write.
        for name in names:
            target=PROJECT/name
            if target.is_file() and hashlib.sha256(target.read_bytes()).digest()!=hashlib.sha256(archive.read(name)).digest():
                raise ValueError("Refusing overwrite of existing Unity source or GUID: "+name)
        if not dry_run:
            for name in names:
                target=PROJECT/name
                if target.is_file():continue
                target.parent.mkdir(parents=True,exist_ok=True)
                tmp=target.with_name(target.name+".tmp")
                tmp.write_bytes(archive.read(name))
                tmp.replace(target)
    return {"status":"DRY_RUN_PASS" if dry_run else "PASS","part":part,"signs":count,"files":len(names),"sha256":digest(path)}

def check_all():
    directory=PROJECT/BASE
    if not directory.is_dir():raise ValueError("No owner signs have been imported")
    pngs=sorted(directory.rglob("*.png"))
    if len(pngs)!=148:raise ValueError(f"Expected 148 owner's original signs, got {len(pngs)}")
    if hashlib.sha256((PROJECT/MAIN_CASINO).read_bytes()).hexdigest()!=MAIN_CASINO_SHA:
        raise ValueError("Missing or changed owner casino C1 sign")
    for png in pngs:
        rel=png.relative_to(PROJECT).as_posix()
        meta=pathlib.Path(str(png)+".meta")
        if not meta.is_file() or ("guid: "+uuid.uuid5(GUID_NAMESPACE,rel).hex) not in meta.read_text():
            raise ValueError("Missing original Unity metadata "+rel)
    return {"status":"PASS","unique_signs":len(pngs),"casino":"T11 WAWER original C1"}

if __name__=="__main__":
    cli=argparse.ArgumentParser()
    cli.add_argument("archives",nargs="*",help="01-06 ZIP and 07-13 ZIP")
    cli.add_argument("--dry-run",action="store_true")
    cli.add_argument("--verify",action="store_true")
    args=cli.parse_args()
    if not args.verify:
        if len(args.archives)!=2:cli.error("Both original library archives 01-06 and 07-13 are required")
        outputs=[import_archive(p,args.dry_run) for p in args.archives]
        if {x["part"] for x in outputs}!={"01-06","07-13"}:
            raise ValueError("One half of the 13-part original sign library is missing")
        for item in outputs:print(json.dumps(item,ensure_ascii=False))
    if args.verify or not args.dry_run:print(json.dumps(check_all(),ensure_ascii=False))
