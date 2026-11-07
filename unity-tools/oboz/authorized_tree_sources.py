#!/usr/bin/env python3
"""Fetch only the two Poly Haven trees explicitly named by the camp owner.

All downloads stay OUTSIDE Unity Assets. The pinned official API manifest gives
byte sizes and MD5; this script also records SHA256 of verified local bytes.
Missing optional inputs are warnings. It never imports a high-poly model.
"""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import urllib.request
from urllib.parse import urlsplit

ALLOWED = {"pine_tree_01", "tree_small_02"}
DEFAULT_MANIFEST = Path(__file__).with_name("AUTHORIZED_TREES_20261106.json")


def hashes(path: Path) -> tuple[int, str, str]:
    md5, sha, size = hashlib.md5(), hashlib.sha256(), 0
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            size += len(block); md5.update(block); sha.update(block)
    return size, md5.hexdigest(), sha.hexdigest()


def safe_relative(value: str) -> str:
    path = PurePosixPath(value)
    if not value or path.is_absolute() or ".." in path.parts or "\\" in value:
        raise ValueError("Unsafe source filename")
    return value


def read_manifest(path: Path) -> dict:
    document = json.loads(path.read_text())
    if document.get("schema") != 1 or {asset.get("id") for asset in document.get("assets", [])} != ALLOWED:
        raise ValueError("Manifest must contain exactly the two owner-named trees")
    for asset in document["assets"]:
        if asset.get("license") != "CC0-1.0" or asset.get("allowedLodLevels") != [1, 2]:
            raise ValueError("Unexpected tree license/LOD policy")
        safe_relative(asset["sourceSubdirectory"])
        for entry in asset["files"]:
            safe_relative(entry["relativePath"])
            uri = urlsplit(entry["url"])
            if uri.scheme != "https" or uri.netloc != "dl.polyhaven.org" or "/" + asset["id"] + "/" not in uri.path:
                raise ValueError("Unexpected source host/asset")
            if len(entry["md5"]) != 32 or entry["bytes"] <= 0:
                raise ValueError("Missing official checksum/size")
    return document


def fetch(manifest_path: Path, root: Path, report_path: Path) -> dict:
    if any(part.casefold() == "assets" for part in root.resolve().parts):
        raise ValueError("High-poly .blend source must stay outside Unity Assets")
    manifest = read_manifest(manifest_path)
    report = dict(title="OBÓZ TRENINGOWY — źródła dwóch wskazanych drzew",schema=1,
                  manifestSha256=hashlib.sha256(manifest_path.read_bytes()).hexdigest(),
                  assets=[],warnings=[],unityImported=False,apkBuilt=False)
    for asset in manifest["assets"]:
        record = dict(id=asset["id"],sourceSubdirectory=asset["sourceSubdirectory"],files=[],complete=True)
        report["assets"].append(record)
        for entry in asset["files"]:
            target = root / asset["sourceSubdirectory"] / entry["relativePath"]
            try:
                target.parent.mkdir(parents=True, exist_ok=True)
                if target.exists():
                    size, md5, sha = hashes(target)
                    if (size, md5) != (entry["bytes"], entry["md5"]):
                        raise ValueError("Existing source differs; preserved without overwrite: " + str(target))
                else:
                    partial = target.with_name(target.name + ".partial")
                    if partial.exists():
                        raise ValueError("Previous partial transfer retained; no blind overwrite: " + str(partial))
                    request = urllib.request.Request(entry["url"], headers={"User-Agent": "BraxhornUnity6-OwnerForest/1"})
                    with urllib.request.urlopen(request, timeout=90) as response, partial.open("xb") as output:
                        final = urlsplit(response.geturl())
                        if final.scheme != "https" or final.netloc != "dl.polyhaven.org":
                            raise ValueError("Download redirected outside official source")
                        transferred = 0
                        for block in iter(lambda: response.read(1024 * 1024), b""):
                            transferred += len(block)
                            if transferred > entry["bytes"]:
                                raise ValueError("Response exceeds pinned source size")
                            output.write(block)
                    size, md5, sha = hashes(partial)
                    if (size, md5) != (entry["bytes"], entry["md5"]):
                        raise ValueError("Official size/MD5 mismatch; partial retained outside Assets")
                    partial.replace(target)
                record["files"].append(dict(relativePath=entry["relativePath"],bytes=size,md5=md5,sha256=sha,verified=True))
            except Exception as error:
                record["complete"] = False
                warning = asset["id"] + "/" + entry["relativePath"] + ": " + str(error)
                report["warnings"].append(warning)
                print("BRAXHORN_OBOZ_AUTHORED_TREE_WARNING " + warning, flush=True)
        report_path.parent.mkdir(parents=True, exist_ok=True)
        report_path.write_text(json.dumps(report,ensure_ascii=False,indent=2) + "\n")
    report["complete"] = all(item["complete"] for item in report["assets"])
    report_path.write_text(json.dumps(report,ensure_ascii=False,indent=2) + "\n")
    return report


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--manifest", type=Path, default=DEFAULT_MANIFEST)
    parser.add_argument("--sources", type=Path, required=True)
    parser.add_argument("--report", type=Path, required=True)
    args = parser.parse_args()
    result = fetch(args.manifest, args.sources, args.report)
    print("BRAXHORN_OBOZ_AUTHORED_TREE_SOURCES complete=" + str(result["complete"]) +
          " warnings=" + str(len(result["warnings"])) + " unity_imported=false")
