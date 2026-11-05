"""Unpack one original owner graphics ZIP for a single Arena A1 + Casino T11 build.

The wrapper contains the two verified original city-sign archives (148 PNGs)
and the verified 3-original-mural archive. No resampling, recoding, image
generation, or GUID mutations. Fails before costly Unity import when incomplete.
Run only as a preparation step in a manually dispatched workflow.
"""
import argparse
import hashlib
import json
from pathlib import Path
import zipfile

ARCHIVE_NAME="MIASTO_MISTRZOW_SZYLDY_148_I_MURALE_3.zip"
ARCHIVE_SHA256="5e921af08a22716ae6051fd3858c8569de9542ddddc53375871adf639abcf330"
FILES={
    "MIASTO_MISTRZOW_SZYLDY_01_06_UNITY.zip":(
        87517783,"fcce54bdf976c343fbbeffde614f385f8ef82f5819c64f70dac84c73b105efce"),
    "MIASTO_MISTRZOW_SZYLDY_07_13_UNITY.zip":(
        98471287,"e3691fc12d18c05ca42c71abfbf046a566d9af6764f49a30c41ca05a4f5eb5c7"),
    "ARENA_A1_TRZY_MURALE_ORIGINALY.zip":(
        1378688,"171865e56bff25d23c11d819befd6525d9ae49c08e4b7a1320ae63be6b43736e"),
}
ENTRY_SET=set(FILES)|{"MANIFEST.json"}

def sha_stream(stream):
    digest=hashlib.sha256()
    for chunk in iter(lambda:stream.read(1024*1024),b""):
        digest.update(chunk)
    return digest.hexdigest()

def unpack(archive,destination,dry_run=False):
    archive=Path(archive)
    destination=Path(destination)
    if archive.name!=ARCHIVE_NAME:
        raise ValueError("Expected only the owner's complete graphics package "+ARCHIVE_NAME)
    if not archive.is_file():
        raise FileNotFoundError(archive)
    with archive.open("rb") as file:
        if sha_stream(file)!=ARCHIVE_SHA256:
            raise ValueError("Original 151-graphic ZIP SHA-256 mismatch")
    with zipfile.ZipFile(archive) as z:
        names=z.namelist()
        if len(names)!=len(ENTRY_SET) or set(names)!=ENTRY_SET:
            raise ValueError("Original graphics ZIP has missing/extra/duplicate entries")
        if z.testzip():
            raise ValueError("Original graphics wrapper ZIP CRC failed")
        m=json.loads(z.read("MANIFEST.json"))
        if m.get("signs")!=148 or m.get("murals")!=3:
            raise ValueError("Expected 148 signs + 3 murals")
        sources={item["filename"]:item for item in m.get("sources",[])}
        if set(sources)!=set(FILES):
            raise ValueError("Unexpected internal source archives")
        # Compare all files and destination byte hashes BEFORE writing anything.
        for name,(size,hash_) in FILES.items():
            info=z.getinfo(name)
            if info.file_size!=size or info.compress_type!=zipfile.ZIP_STORED:
                raise ValueError("Size/compression changed for original "+name)
            if sources[name].get("size")!=size or sources[name].get("sha256")!=hash_:
                raise ValueError("Manifest SHA/size differs: "+name)
            with z.open(name) as file:
                if sha_stream(file)!=hash_:
                    raise ValueError("Original inner ZIP content altered: "+name)
            target=destination/name
            if target.exists():
                with target.open("rb") as file:
                    if sha_stream(file)!=hash_:
                        raise ValueError("Refusing overwrite of existing different source "+str(target))
        if dry_run:
            return {"status":"DRY_RUN_PASS","signs":148,"murals":3,"originalArchives":len(FILES)}
        destination.mkdir(parents=True,exist_ok=True)
        for name in sorted(FILES):
            target=destination/name
            if target.exists():continue
            partial=destination/(name+".partial")
            try:
                with z.open(name) as source,partial.open("wb") as output:
                    for chunk in iter(lambda:source.read(1024*1024),b""):
                        output.write(chunk)
                partial.replace(target)
            finally:
                if partial.exists():partial.unlink()
        return {"status":"UNPACKED_AND_VERIFIED","signs":148,"murals":3,"originalArchives":len(FILES)}

if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("zip",help=ARCHIVE_NAME)
    parser.add_argument("destination",help="Temporary runner directory (not Unity Assets)")
    parser.add_argument("--dry-run",action="store_true")
    a=parser.parse_args()
    print(json.dumps(unpack(a.zip,a.destination,a.dry_run),ensure_ascii=False))
