#!/usr/bin/env python3
"""Safely extract ONLY the approved nested A1 + T11 originals from owner ZIP.

The draft GitHub Release has TWO items with tag champions-a1-20261105;
the exact approved A1/T11 original archive is nested inside
BRAXHORN_MISTRZOWIE_A1_T11_ORIGINALY_DO_BUILD.zip. Never blindly download
or choose the newest release; select the unique archive by exact asset name
and match the cryptographic digest before writing source files.
No user animations are extracted or modified here.
"""
from __future__ import annotations
import argparse
import hashlib
from pathlib import Path
import zipfile

SOURCE_NAME="BRAXHORN_MISTRZOWIE_A1_T11_ORIGINALY_DO_BUILD.zip"
SOURCE_SHA256="43ea47f02a1f2e9e4f707bb75d16ec0a3fe79ec02762929ca591e317defa1088"
INNER={
    "ARENA_A1_DO_GITHUB_POBIERZ_PONOWNIE.zip":
        "c96e39cd218b2f884560be0e89d0f24b378e7a49512486830643f6934cdc4f71",
    "ARENA_A1_KASYNO_T11_ORYGINALY_Z_GABLOTA.zip":
        "5d2c3bd341772c1f61d695a898a99c2d9e55b383bfa2bece0753cfaa9f502d55",
}

def sha256(path:Path)->str:
    h=hashlib.sha256()
    with path.open("rb") as file:
        for chunk in iter(lambda:file.read(4*1024*1024),b""):h.update(chunk)
    return h.hexdigest()

def unpack(archive:Path,dest:Path,dry_run:bool=False)->None:
    if archive.name!=SOURCE_NAME or not archive.is_file():
        raise ValueError("Wrong or absent original A1/T11 archive: "+str(archive))
    if sha256(archive)!=SOURCE_SHA256:
        raise ValueError("A1/T11 outer owner source digest mismatch")
    # No need to unpack the owner's unused Mixamo animations: only TWO exact
    # inner ZIPs are required to generate Arena A1 and Casino T11.
    with zipfile.ZipFile(archive) as z:
        if z.testzip() is not None:raise ValueError("Original A1/T11 archive CRC invalid")
        for name,expected in INNER.items():
            if name not in z.namelist():raise ValueError("Approved owner inner ZIP missing: "+name)
            if "/" in name or "\\" in name:raise ValueError("Unsafe archive member name")
            with z.open(name,"r") as src:
                digest=hashlib.sha256()
                if not dry_run:
                    dest.mkdir(parents=True,exist_ok=True)
                    target=dest/name
                    if target.exists():raise FileExistsError("Refuse to overwrite source "+str(target))
                    temporary=dest/(name+".partial")
                    try:
                        with temporary.open("wb") as output:
                            for chunk in iter(lambda:src.read(4*1024*1024),b""):
                                digest.update(chunk);output.write(chunk)
                        if digest.hexdigest()!=expected:raise ValueError("Nested owner original changed: "+name)
                        temporary.replace(target)
                    finally:
                        temporary.unlink(missing_ok=True)
                else:
                    for chunk in iter(lambda:src.read(4*1024*1024),b""):
                        digest.update(chunk)
                    if digest.hexdigest()!=expected:
                        raise ValueError("Nested owner source checksum mismatch: "+name)
            print("A1_T11_INNER_ORIGINAL_VERIFIED "+name+" sha256="+expected)
    print("A1_T11_NESTED_SOURCE_PASS noAnimationsImported=true dryRun="+str(dry_run))

if __name__=="__main__":
    cli=argparse.ArgumentParser(description=__doc__)
    cli.add_argument("archive",type=Path)
    cli.add_argument("destination",type=Path)
    cli.add_argument("--dry-run",action="store_true")
    args=cli.parse_args()
    unpack(args.archive,args.destination,args.dry_run)
