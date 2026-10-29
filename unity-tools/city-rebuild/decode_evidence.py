#!/usr/bin/env python3
"""Reassemble connector-fetched ASCII evidence parts and verify the exact PNG.

First fetch evidence-manifest.json and its named transport parts from the exact
evidence commit using GitHub fetch_file (UTF-8). Save them under one directory,
then run this helper with that manifest and the selected original filename.
Files <=900 KB can instead be read directly using fetch_file encoding=base64.
"""
import argparse
import base64
import hashlib
import json
from pathlib import Path, PurePosixPath


def safe_path(root, value):
    relative = PurePosixPath(value)
    if relative.is_absolute() or ".." in relative.parts or "\\" in value:
        raise ValueError("Unsafe evidence path")
    return root / value


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("manifest", type=Path)
    parser.add_argument("filename")
    args = parser.parse_args()
    manifest = json.loads(args.manifest.read_text())
    entry = manifest["files"][args.filename]
    transfer = entry["transport"]
    if transfer["encoding"] != "concatenated-base64-text":
        raise ValueError("Unexpected evidence transport")
    encoded = []
    for part in transfer["parts"]:
        data = safe_path(args.manifest.parent, part["path"]).read_bytes()
        if len(data) != part["bytes"] or hashlib.sha256(data).hexdigest() != part["sha256"]:
            raise ValueError("Transport part differs from the original evidence manifest")
        encoded.append(data.strip())
    data = base64.b64decode(b"".join(encoded), validate=True)
    if len(data) != entry["bytes"] or hashlib.sha256(data).hexdigest() != entry["sha256"]:
        raise ValueError("Reassembled evidence differs from the original captured bytes")
    output = safe_path(args.manifest.parent, args.filename)
    if output.exists() and output.read_bytes() != data:
        raise ValueError("Refusing to replace a different evidence file")
    output.write_bytes(data)
    print("EXACT_EVIDENCE_READY", str(output), len(data), entry["sha256"])


if __name__ == "__main__":
    main()
