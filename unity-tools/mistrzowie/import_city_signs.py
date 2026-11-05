"""Lossless, idempotent import of the owner's City of Champions sign collection 01-06.

Original graphics are copied byte-for-byte. An authenticated SHA256
pins the source archive; .meta keeps the same GUID on every Unity import.
No network, game build, or Unity startup is performed by this script.
"""
import argparse
import hashlib
import pathlib
import zipfile
import json

ROOT=pathlib.Path(__file__).resolve().parents[2]
PROJECT=ROOT/"unity-city"
ARCHIVE_SHA256="fcce54bdf976c343fbbeffde614f385f8ef82f5819c64f70dac84c73b105efce"
BASE="Assets/Source/Mistrzowie/CitySigns/"
MAIN_CASINO=BASE+"01_GLOWNE/C1_KASYNO_KASYNO_WAWER.png"
MAIN_CASINO_SHA256="78edc9f51c772d20cb84c43f0850716a63c5a772fa0b326a7aea15ccfe39bf1b"

def sha(path):
    h=hashlib.sha256()
    with pathlib.Path(path).open("rb") as f:
        for chunk in iter(lambda:f.read(1024*1024),b""):
            h.update(chunk)
    return h.hexdigest()

def inspect(zip_path):
    with zipfile.ZipFile(zip_path) as z:
        bad=z.testzip()
        if bad:raise ValueError("Corrupt sign archive entry: "+bad)
        names=z.namelist()
        if len(set(names))!=len(names):raise ValueError("Repeated ZIP paths")
        images=[s for s in names if s.startswith(BASE) and s.endswith(".png")]
        metadata=[s for s in names if s.startswith(BASE) and s.endswith(".meta")]
        if len(images)!=70 or len(metadata)!=73 or len(names)!=145:
            raise ValueError(f"Expected 70 PNG, 73 .meta and 145 total entries, got {len(images)}/{len(metadata)}/{len(names)}")
        if MAIN_CASINO not in images or hashlib.sha256(z.read(MAIN_CASINO)).hexdigest()!=MAIN_CASINO_SHA256:
            raise ValueError("T11 source is not the owner's original C1 KASYNO WAWER")
        for name in names:
            if not (name.startswith(BASE) or name in ("SIGN_LIBRARY_MANIFEST.json","IMPORT-README.txt")):
                raise ValueError("Unexpected path "+name)
            if pathlib.PurePosixPath(name).is_absolute() or ".." in pathlib.PurePosixPath(name).parts:
                raise ValueError("Unsafe ZIP path "+name)
        manifest=json.loads(z.read("SIGN_LIBRARY_MANIFEST.json"))
        if manifest.get("count")!=70 or len(manifest.get("assets",[]))!=70:
            raise ValueError("Wrong original sign catalog")
        for item in manifest["assets"]:
            name=item["path"]
            if name not in images:raise ValueError("Catalog missing "+name)
            if hashlib.sha256(z.read(name)).hexdigest()!=item["sha256"]:
                raise ValueError("Sign SHA mismatch "+name)
        return sorted(name for name in names if name.startswith(BASE))

def import_archive(path,dry_run=False):
    path=pathlib.Path(path)
    if sha(path)!=ARCHIVE_SHA256:raise ValueError("Archive hash differs from approved original signs collection")
    names=inspect(path)
    with zipfile.ZipFile(path) as z:
        # Compare all destinations BEFORE writing any file.
        for name in names:
            target=PROJECT/name
            if target.exists() and hashlib.sha256(target.read_bytes()).digest()!=hashlib.sha256(z.read(name)).digest():
                raise ValueError("Refusing to overwrite existing Unity asset or GUID: "+name)
        if dry_run:return {"status":"DRY_RUN_PASS","files":len(names)}
        for name in names:
            target=PROJECT/name
            if target.exists():continue
            target.parent.mkdir(parents=True,exist_ok=True)
            temp=target.with_name(target.name+".partial")
            with temp.open("wb") as f:f.write(z.read(name))
            temp.replace(target)
    return {"status":"PASS","files":len(names),"signs":70,"guidSidecars":73}

if __name__=="__main__":
    cli=argparse.ArgumentParser()
    cli.add_argument("archive",help="MIASTO_MISTRZOW_SZYLDY_01_06_UNITY.zip")
    cli.add_argument("--dry-run",action="store_true")
    args=cli.parse_args()
    print(json.dumps(import_archive(args.archive,args.dry_run),ensure_ascii=False))
