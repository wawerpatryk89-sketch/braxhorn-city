"""Lossless import of the owner's THREE original panoramic A1 murals.

The central entrance mural and both new side-wall murals share stable Unity
GUIDs. Import is idempotent and never overwrites different original bytes.
No network, Unity import, automatic build or art edits are performed here.
"""
from pathlib import Path
import argparse
import hashlib
import json
import zipfile

ROOT=Path(__file__).resolve().parents[2]
DEST=ROOT/"unity-city"
NAME="ARENA_A1_TRZY_MURALE_ORIGINALY.zip"
ARCHIVE_SHA256="171865e56bff25d23c11d819befd6525d9ae49c08e4b7a1320ae63be6b43736e"
PREFIX="Assets/Source/Mistrzowie/ArenaMurals/"
FOLDER_META="Assets/Source/Mistrzowie/ArenaMurals.meta"
SOURCES={
    "BRAXHORN_CHAMPION_FIGHTER_ORIGINAL.jpg":(
        "94ed97faea3125ebde2e8cb9137465202aac2a03d0eb97b9abb213128c68156f",
        "964968cb8a1b51f98810b93ebf638e62"),
    "BRAXHORN_A1_FIGHTERS_LEFT_ORIGINAL.jpg":(
        "97f52b27d4811afe833f45a37a3705084c824204053a25b7f7a4695209c14c5e",
        "7fd43010b2d45f21abd1825996fd21e7"),
    "BRAXHORN_A1_BELT_RIGHT_ORIGINAL.jpg":(
        "0e43f5e68d12e3705a8eed5ceb81da1c489647ff0742dd7edc4389588a2c64a2",
        "899ea5f5ffe25ecaaac8d271b59aa8d0")
}
FOLDER_GUID="4a9279b478bb5b949e2ac8306523f245"
FILES={FOLDER_META}|{PREFIX+name for name in SOURCES}|{PREFIX+name+".meta" for name in SOURCES}
ALLOWED=FILES|{"ARENA_THREE_MURALS_MANIFEST.json","IMPORT_README.txt"}

def sha(blob):
    return hashlib.sha256(blob).hexdigest()

def install(archive,dry_run=False):
    path=Path(archive)
    if not path.is_file():raise FileNotFoundError("Missing original three-mural package: "+str(path))
    if sha(path.read_bytes())!=ARCHIVE_SHA256:
        raise ValueError("Owner three-mural archive checksum differs; refusing source mutation")
    with zipfile.ZipFile(path) as z:
        bad=z.testzip()
        if bad:raise ValueError("Corrupt panoramic source: "+bad)
        names=z.namelist()
        if len(names)!=len(ALLOWED) or set(names)!=ALLOWED:
            raise ValueError("Unexpected/missing mural image or metadata: "+str(sorted(set(names)^ALLOWED)))
        manifest=json.loads(z.read("ARENA_THREE_MURALS_MANIFEST.json"))
        if manifest.get("sourceEdited") is not False or len(manifest.get("assets",[]))!=3:
            raise ValueError("Invalid original art manifest")
        if manifest.get("width")!=1536 or manifest.get("height")!=512:
            raise ValueError("Wrong panoramic source dimensions")
        recorded={a["path"]:a for a in manifest["assets"]}
        for name,(expected_digest,guid) in SOURCES.items():
            rel=PREFIX+name
            item=recorded.get(rel)
            if not item or item.get("sha256")!=expected_digest or item.get("guid")!=guid:
                raise ValueError("Owner mural identity changed: "+name)
            if sha(z.read(rel))!=expected_digest:
                raise ValueError("Original JPEG bytes changed: "+name)
            if ("guid: "+guid) not in z.read(rel+".meta").decode("utf-8"):
                raise ValueError("Unity original mural GUID differs: "+name)
        if ("guid: "+FOLDER_GUID) not in z.read(FOLDER_META).decode("utf-8"):
            raise ValueError("Original ArenaMurals folder GUID was replaced")
        # Validate all existing targets *before* creating any new files.
        for name in sorted(FILES):
            target=DEST/name
            if target.exists() and sha(target.read_bytes())!=sha(z.read(name)):
                raise ValueError("Refusing overwrite of existing original mural or GUID: "+str(target))
        actions=[]
        for name in sorted(FILES):
            target=DEST/name
            if target.exists():actions.append("UNCHANGED "+name)
            elif dry_run:actions.append("DRY_RUN "+name)
            else:
                target.parent.mkdir(parents=True,exist_ok=True)
                temp=target.with_name(target.name+".partial")
                temp.write_bytes(z.read(name))
                temp.replace(target)
                actions.append("INSTALLED "+name)
        return actions

if __name__=="__main__":
    cli=argparse.ArgumentParser()
    cli.add_argument("archive",help=NAME)
    cli.add_argument("--dry-run",action="store_true")
    args=cli.parse_args()
    result=install(args.archive,args.dry_run)
    for line in result:print(line)
    print("CHAMPION_A1_ALL_THREE_MURALS_VERIFIED original=3 guids=4 UnityBuild=NOT_STARTED")
