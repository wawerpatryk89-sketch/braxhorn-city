"""Verify ONLY original owner-supplied Miasto Mistrzów K1..K12 signs.

The verified owner's bundle contains 148 original 2048x512 CitySigns.
K01..K12 in that older bundle all read KAWIARNIA, not the 12 DIFFERENT
pavilion categories in the later 2026-11-05 promenade reference.
Do not guess or reuse a sign for an unrelated business.

- This tool NEVER draws, recolors, edits or overwrites a PNG or Unity .meta.
- Optionally imports the unchanged, SHA-pinned original ZIPs.
- A binding JSON file consumed by Unity is written only if all twelve sign
  choices are backed by actual source bytes and pass SHA+size validation.
- --strict blocks the expensive Unity build when any sign is unmatched.
"""
import argparse
import hashlib
import io
import json
from pathlib import Path
import struct
import sys
import tempfile
import zipfile

from import_city_signs import PACKAGE_SHA,inspect_package,install
from promenade_layout import PAVILIONS

ROOT=Path(__file__).resolve().parents[2]
MAPPING=Path(__file__).with_name("promenade_original_signs.json")
SOURCE_ROOT=ROOT/"unity-city"
BINDING=SOURCE_ROOT/"Assets/Source/Mistrzowie/PromenadeOriginalSignBinding.json"
OUTPUT=ROOT/"mistrzowie-evidence/promenade-owner-signs-status.json"
PREFIX="Assets/Source/Mistrzowie/CitySigns/"


def sha(path):
    digest=hashlib.sha256()
    with Path(path).open("rb") as f:
        for part in iter(lambda:f.read(1024*1024),b""):
            digest.update(part)
    return digest.hexdigest()


def original_zip_contents(outer):
    """Only use signed filename+SHA expected by import_city_signs.py."""
    outer=Path(outer)
    with zipfile.ZipFile(outer) as bundle, tempfile.TemporaryDirectory(prefix="brax-owner-signs-") as td:
        if bundle.testzip():
            raise ValueError("Owner source ZIP CRC failed")
        available=set(bundle.namelist())
        if set(PACKAGE_SHA)-available:
            raise ValueError("148-source archive is missing original inner packages: "
                             +", ".join(sorted(set(PACKAGE_SHA)-available)))
        local=[]
        for name in PACKAGE_SHA:
            target=Path(td)/name
            with bundle.open(name) as stream,target.open("wb") as f:
                for chunk in iter(lambda:stream.read(1024*1024),b""):
                    f.write(chunk)
            # Exact owner SHA, manifest, actual PNG bytes, each original GUID.
            inspect_package(target)
            local.append(target)
        return list(local)  # temporary paths cannot escape this context


def import_verified_outer(outer):
    """Validated import inside tempdir; existing source bytes never overwritten."""
    outer=Path(outer)
    with zipfile.ZipFile(outer) as bundle, tempfile.TemporaryDirectory(prefix="brax-owner-signs-") as td:
        if bundle.testzip():
            raise ValueError("Owner source ZIP CRC failed")
        local=[]
        for name in PACKAGE_SHA:
            if name not in bundle.namelist():
                raise ValueError("Missing original owner package: "+name)
            target=Path(td)/name
            with bundle.open(name) as stream,target.open("wb") as f:
                for chunk in iter(lambda:stream.read(1024*1024),b""):
                    f.write(chunk)
            inspect_package(target)
            local.append(target)
        imported=install(local,dry_run=False,destination=SOURCE_ROOT)
        if imported.get("sourcePng")!=148:
            raise ValueError("Original CitySigns count changed")
        return imported


def inspect_assets():
    manifest=json.loads(MAPPING.read_text(encoding="utf-8"))
    planned={p["id"]:p for p in PAVILIONS}
    originals=manifest["entries"]
    if [x["id"] for x in originals]!=list(planned):
        raise ValueError("Owner sign mapping must have exact K1..K12 order")
    result=[]
    accepted=[]
    for spec in originals:
        kiosk=planned[spec["id"]]
        if spec["required"]!=kiosk["name"]:
            raise ValueError("Pavilion label differs from owner's reference: "+spec["id"])
        rel=spec.get("owner_path")
        digest=spec.get("sha256")
        state="UNASSIGNED_OWNER_ARTWORK"
        if rel is not None:
            if not isinstance(rel,str) or not rel.startswith(PREFIX) or ".." in Path(rel).parts:
                raise ValueError("Unsafe or non-original CitySigns source path: "+str(rel))
            if not isinstance(digest,str) or len(digest)!=64:
                raise ValueError("Original SHA-256 required: "+spec["id"])
            file=SOURCE_ROOT/rel
            if file.exists():
                if sha(file)!=digest:
                    raise ValueError("Owner original sign image changed: "+str(file))
                with file.open("rb") as f:
                    h=f.read(24)
                if h[:8]!=b"\x89PNG\r\n\x1a\n" or h[12:16]!=b"IHDR" or struct.unpack(">II",h[16:24])!=(2048,512):
                    raise ValueError("Owner original sign must stay 2048x512: "+str(file))
                meta=Path(str(file)+".meta")
                if not meta.is_file():
                    raise ValueError("Original Unity GUID sidecar missing: "+str(meta))
                state="ORIGINAL_OWNER_PNG_VERIFIED"
                accepted.append(dict(id=spec["id"],path=rel,sha256=digest))
            else:
                state="ORIGINAL_OWNER_PNG_NOT_AVAILABLE_IN_BUILD_CHECKOUT"
        result.append(dict(id=spec["id"],label=spec["required"],owner_path=rel,status=state))
    ready=len(accepted)==12
    return dict(status="READY_ONLY_ORIGINALS" if ready else "BLOCKED_UNMATCHED_OWNER_SIGNS",
                ready=ready,verified=len(accepted),required=12,
                neverGenerated=True,photoReplaced=False,unityRan=False,
                entries=result),accepted


def main(argv=None):
    args=argparse.ArgumentParser()
    args.add_argument("--outer-archive",help="Owner ZIP containing both signed, original 70+78 sign ZIPs")
    args.add_argument("--import-originals",action="store_true",help="Extract and validate immutable owner PNG+meta bytes")
    args.add_argument("--strict",action="store_true",help="Block expensive Android/Unity build when any sign is missing")
    a=args.parse_args(argv)
    if a.import_originals:
        if not a.outer_archive:raise SystemExit("--import-originals requires --outer-archive")
        import_verified_outer(a.outer_archive)
    elif a.outer_archive:
        # A pure inspection still validates the exact original bundle,
        # but does not extract or change user files.
        with zipfile.ZipFile(a.outer_archive) as z:
            if z.testzip() is not None:raise ValueError("Archive CRC failed")
            if not set(PACKAGE_SHA).issubset(set(z.namelist())):
                raise ValueError("Owner 148 archive missing 70+78 source files")
    report,accepted=inspect_assets()
    OUTPUT.parent.mkdir(parents=True,exist_ok=True)
    OUTPUT.write_text(json.dumps(report,ensure_ascii=False,indent=2)+"\n")
    print("CHAMPIONS_PROMENADE_ORIGINAL_SIGN_GATE "+report["status"]+
          " originals="+str(len(accepted))+"/12")
    for entry in report["entries"]:
        if entry["status"]!="ORIGINAL_OWNER_PNG_VERIFIED":
            print("  "+entry["id"]+" | "+entry["label"]+" | "+entry["status"])
    if report["ready"]:
        BINDING.parent.mkdir(parents=True,exist_ok=True)
        BINDING.write_text(json.dumps(dict(signs=accepted),ensure_ascii=False,indent=2)+"\n")
    elif BINDING.exists():
        # Never leave a stale success-binding in a failed staging attempt.
        raise ValueError("Stale binding exists; remove from staging area, never overwrite source assets")
    return 0 if report["ready"] or not a.strict else 2


if __name__=="__main__":
    sys.exit(main())
