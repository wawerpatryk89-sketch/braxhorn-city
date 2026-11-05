"""Reproducible, lossless import of the owner's ORIGINAL A1 panoramic mural.

The JPEG bytes never change. The two stable .meta sidecars prevent Unity GUID
churn. All original fighters/belt/logotype and colours stay exactly supplied.
"""
from pathlib import Path
import argparse
import hashlib
import json
import zipfile

ROOT=Path(__file__).resolve().parents[2]
DEST=ROOT/"unity-city"
NAME="BRAXHORN_A1_MURAL_ORIGINAL_GITHUB.zip"
ZIP_SHA256="e303f79cf021f758dd492c6cc72d6b08c043dda1ef98fce38dd7003bea4450a7"
IMAGE="Assets/Source/Mistrzowie/ArenaMurals/BRAXHORN_CHAMPION_FIGHTER_ORIGINAL.jpg"
FOLDER="Assets/Source/Mistrzowie/ArenaMurals"
IMAGE_SHA256="94ed97faea3125ebde2e8cb9137465202aac2a03d0eb97b9abb213128c68156f"
IMAGE_GUID="964968cb8a1b51f98810b93ebf638e62"
FOLDER_GUID="4a9279b478bb5b949e2ac8306523f245"
FILES={IMAGE,IMAGE+".meta",FOLDER+".meta"}
ALLOWED=FILES|{"ARENA_MURAL_MANIFEST.json","IMPORT-README.txt"}

def digest(blob):
    return hashlib.sha256(blob).hexdigest()

def install(source, dry_run=False):
    src=Path(source)
    if not src.is_file():raise FileNotFoundError("Required original mural ZIP missing: "+str(src))
    if digest(src.read_bytes())!=ZIP_SHA256:
        raise ValueError("Mural ZIP changed; never replace the owner's image silently")
    with zipfile.ZipFile(src) as z:
        bad=z.testzip()
        if bad:raise ValueError("Corrupt mural ZIP entry: "+bad)
        names=set(z.namelist())
        if names!=ALLOWED or len(z.namelist())!=len(ALLOWED):
            raise ValueError("Incorrect original mural package content: "+repr(sorted(names)))
        if digest(z.read(IMAGE))!=IMAGE_SHA256:
            raise ValueError("Mural JPEG pixels or source bytes differ from owner upload")
        for name,guid in ((IMAGE+".meta",IMAGE_GUID),(FOLDER+".meta",FOLDER_GUID)):
            if "guid: "+guid not in z.read(name).decode("utf-8"):
                raise ValueError("Incorrect stable Unity GUID for "+name)
        manifest=json.loads(z.read("ARENA_MURAL_MANIFEST.json"))
        if (manifest.get("asset")!=IMAGE or
            manifest.get("width")!=1536 or manifest.get("height")!=512 or
            manifest.get("sha256")!=IMAGE_SHA256 or
            manifest.get("pixelEdits") is not False):
            raise ValueError("Mural manifest does not match owner's original JPEG")
        # Check all existing paths before writing any file. Never overwrite.
        for name in sorted(FILES):
            path=DEST/name
            if path.exists() and digest(path.read_bytes())!=digest(z.read(name)):
                raise ValueError("Refusing to overwrite existing original image/GUID: "+str(path))
        result=[]
        for name in sorted(FILES):
            path=DEST/name
            if path.exists():
                result.append("UNCHANGED "+name)
            elif dry_run:
                result.append("DRY-RUN "+name)
            else:
                path.parent.mkdir(parents=True,exist_ok=True)
                temp=path.with_name(path.name+".partial")
                temp.write_bytes(z.read(name))
                temp.replace(path)
                result.append("INSTALLED "+name)
        return result

if __name__=="__main__":
    p=argparse.ArgumentParser()
    p.add_argument("archive",help=NAME)
    p.add_argument("--dry-run",action="store_true")
    v=p.parse_args()
    for line in install(v.archive,v.dry_run):print(line)
    print("CHAMPION_A1_ORIGINAL_MURAL_SOURCE_VERIFIED; Unity not launched")
