"""Safe HarborBeach crane GLB import compatibility for Champions Unity builds.

The owner's tracked original is never committed as edited material. The CLI
can audit source-only, or create a verified runner-local import copy using a
new JSON chunk with ONLY unused UV2/3 attribute selectors removed. Binary
chunks, vertex data, material selectors, nodes and image bytes remain intact.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import sys
import tempfile

from repair_pool_p1_towels import derive_safe_uv_import, UnsafeTowelUvConversion, _parse_glb

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / "unity-city/Assets/Source/HarborBeach/Models/harbor_crane.glb"
BACKUP = ROOT / "unity-city/OriginalSources/HarborBeach/harbor_crane.glb"
REPORT = ROOT / "mistrzowie-evidence/harbor-crane-uv-compat.json"

def sha256(raw):
    return hashlib.sha256(raw).hexdigest()

def prepare(apply_runner_only=False):
    raw = SOURCE.read_bytes()
    if raw[:4] != b"glTF":
        raise ValueError("Tracked original harbor crane is not GLB")
    original_doc, original_chunks = _parse_glb(raw)
    try:
        derived, result = derive_safe_uv_import(raw)
    except UnsafeTowelUvConversion as error:
        # Some original crane materials genuinely reference TEXCOORD_2+.
        # Removing these coordinates changes material placement. Never do
        # an unsafe automatic conversion or publish substituted geometry.
        record = {
            "status": "MANUAL_UV_MAPPING_REQUIRED_SOURCE_UNCHANGED",
            "reason": str(error),
            "originalSha256": sha256(raw),
            "derivedSha256": sha256(raw),
            "originalBytes": len(raw),
            "derivedBytes": len(raw),
            "removedUnusedUV": [],
            "originalBinaryChunksPreserved": True,
            "originalMaterialAndTextureDefinitionsPreserved": True,
            "originalVisualAssetsNotReplaced": True,
            "unityImporterTested": False,
            "sourceImportCompatible": False,
            "appliedToRunnerWorkspace": False,
        }
        REPORT.parent.mkdir(parents=True, exist_ok=True)
        REPORT.write_text(json.dumps(record,indent=2,ensure_ascii=False)+"\n",
                          encoding="utf-8")
        print("BRAX_CRANE_UV_ADVISORY " + json.dumps(record,ensure_ascii=False),
              flush=True)
        return record
    doc, chunks = _parse_glb(derived)
    # Format-only change: no visual source or material selectors modified.
    for field in ("asset", "materials", "nodes", "textures", "images",
                  "bufferViews", "buffers", "accessors", "extensionsUsed",
                  "extensionsRequired", "scenes", "scene", "samplers"):
        if doc.get(field) != original_doc.get(field):
            raise ValueError("Unsafe crane GLB conversion changed "+field)
    if chunks[1:] != original_chunks[1:]:
        raise ValueError("Unsafe crane GLB conversion changed binary resources")
    if result["status"] != "UNUSED_UV_CHANNELS_REMOVED_FROM_IMPORT_COPY":
        raise ValueError("Expected unreferenced GLTFast UV extras on crane; review changed source")
    if not result.get("removedUnusedUvAttributes"):
        raise ValueError("Crane derived import removed no UV channels")
    record = {
        "status": "SOURCE_COMPATIBILITY_PROVED",
        "originalSha256": sha256(raw),
        "derivedSha256": sha256(derived),
        "originalBytes": len(raw),
        "derivedBytes": len(derived),
        "removedUnusedUV": result["removedUnusedUvAttributes"],
        "originalBinaryChunksPreserved": True,
        "originalMaterialAndTextureDefinitionsPreserved": True,
        "originalVisualAssetsNotReplaced": True,
        "unityImporterTested": False,
        "appliedToRunnerWorkspace": False,
    }
    if apply_runner_only:
        if os.environ.get("GITHUB_ACTIONS") != "true":
            raise RuntimeError("Runner-local derivation requires GitHub Actions environment")
        BACKUP.parent.mkdir(parents=True, exist_ok=True)
        if BACKUP.exists() and BACKUP.read_bytes() != raw:
            raise RuntimeError("Original source backup disagrees; do not rewrite asset")
        if not BACKUP.exists():
            BACKUP.write_bytes(raw)
        with tempfile.NamedTemporaryFile(mode="wb", dir=SOURCE.parent,
                                         prefix="harbor_crane_",suffix=".glb.partial",
                                         delete=False) as tmp:
            filename=Path(tmp.name)
            tmp.write(derived)
        try:
            os.replace(filename,SOURCE)
        finally:
            filename.unlink(missing_ok=True)
        record["appliedToRunnerWorkspace"] = True
        record["status"] = "RUNNER_ONLY_COMPATIBLE_IMPORT_COPY_READY"
    REPORT.parent.mkdir(parents=True, exist_ok=True)
    REPORT.write_text(json.dumps(record,indent=2,ensure_ascii=False)+"\n",encoding="utf-8")
    print("BRAX_CRANE_UV_SOURCE_PROOF "+json.dumps({key:record[key] for key in
        ("status","originalSha256","derivedSha256","originalBinaryChunksPreserved",
         "originalMaterialAndTextureDefinitionsPreserved","appliedToRunnerWorkspace")}),
        flush=True)
    return record

if __name__ == "__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--apply-runner-only",action="store_true")
    opts=parser.parse_args()
    prepare(opts.apply_runner_only)
