"""Champions-only Unity import isolation of two UNUSED, UV-incompatible GLBs.

No changes to Git-tracked assets. Intended for an ephemeral GitHub Actions
worker AFTER restoring the exact original P1 zip and BEFORE Unity editor import.

Harbor crane and original P1 towels are not instantiated in the Champions
mobile scene. glTFast cannot currently import their material-specific UV2/3,
so preserve them byte-for-byte outside /Assets in this worker, not as a
fake/white placeholder. Never touch any other assets or any other game zone.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import sys

from audit_owner_glb_geometry import parse_glb

ROOT=Path(__file__).resolve().parents[2]
ASSETS=ROOT/"unity-city/Assets"
EVIDENCE=ROOT/"mistrzowie-evidence/isolated-unused-uv-glb.json"
MODELS=[
    ("SPA_TOWELS_NOT_IN_MOBILE_SCENE",
     "Source/Mistrzowie/PoolP1/OwnerEquipment/stack_of_towels_2k.glb",
     "unity-city/OriginalSources/Mistrzowie/PoolP1/stack_of_towels_2k.glb",
     "BraxChampionsPoolP1Equipment.cs"),
    ("HARBOR_CRANE_NOT_PLACED_BY_CHAMPIONS_FLEET",
     "Source/HarborBeach/Models/harbor_crane.glb",
     "unity-city/OriginalSources/HarborBeach/harbor_crane.glb",
     "BraxChampionsHarbour.cs"),
]

def sha(data:bytes):
    return hashlib.sha256(data).hexdigest()

def audit():
    editor=ASSETS/"Editor/Mistrzowie"
    if not editor.is_dir():
        raise RuntimeError("Champions Editor source unavailable; cannot isolate safely")
    all_code=[]
    for folder in (editor,ASSETS/"Mistrzowie"):
        if folder.is_dir():
            all_code.extend(folder.rglob("*.cs"))
    result=[]
    for label,relative,backup,source_script in MODELS:
        source=ASSETS/relative
        dest=ROOT/backup
        if not source.is_file():
            raise FileNotFoundError("Cannot isolate unverified original GLB: "+str(source))
        # No literal lookup of this GLB in the actual Champions scene writer.
        references=[str(p.relative_to(ROOT)) for p in all_code
                    if source.name in p.read_text(encoding="utf-8")]
        if references:
            raise RuntimeError("This model is referenced by Champions and may be visible: "
                               +source.name+" "+", ".join(references))
        code=(editor/source_script).read_text(encoding="utf-8")
        if label.startswith("SPA") and "stack of towels is deferred" not in code:
            raise RuntimeError("Towel deferral proof no longer present")
        if label.startswith("HARBOR"):
            if "harbor_crane.glb" in code or '"yacht","yacht.glb"' not in code:
                raise RuntimeError("Harbor port source usage changed; human review required")
        raw=source.read_bytes()
        analysis=parse_glb(raw,str(source.relative_to(ROOT)))
        if analysis["errors"] or not analysis["extraUVs"]:
            raise RuntimeError("Expected incompatible UV source not confirmed: "
                               +str(analysis["errors"]))
        if not any(x>=2 for x in analysis["materialUVs"]):
            raise RuntimeError("Unsupported UV2 material usage no longer confirmed")
        if dest.is_file() and sha(dest.read_bytes())!=sha(raw):
            raise RuntimeError("Owner original archive differs; refuse to replace")
        result.append({
            "label":label,
            "workingPath":str(source.relative_to(ROOT)),
            "preservedPath":str(dest.relative_to(ROOT)),
            "bytes":len(raw),
            "sha256":sha(raw),
            "materialUVs":analysis["materialUVs"],
            "notReferencedByCurrentChampionsScene":True,
            "originalSourcePreserved":True,
            "ownerVisualsChanged":False,
            "quarantinedFromTemporaryUnityImport":False,
        })
    return result

def run(apply=False):
    if apply and (os.environ.get("GITHUB_ACTIONS")!="true"
                  or os.environ.get("MISTRZOWIE_PIPELINE")!="20261105-A1-T11"):
        raise RuntimeError("ONLY the explicitly selected Champions GitHub Actions pipeline may apply")
    items=audit()
    if apply:
        # Both sources have already been verified, and output is outside Assets.
        for row in items:
            src=ROOT/row["workingPath"]
            dst=ROOT/row["preservedPath"]
            dst.parent.mkdir(parents=True,exist_ok=True)
            if not dst.exists():
                shutil.copy2(src,dst)
            if sha(dst.read_bytes())!=row["sha256"]:
                raise RuntimeError("Original cannot be recovered byte-for-byte")
            # Only runner's temporary copy is moved, not a committed source.
            src.unlink()
            meta=Path(str(src)+".meta")
            if meta.exists():
                owner_meta=Path(str(dst)+".meta")
                if owner_meta.exists() and owner_meta.read_bytes()!=meta.read_bytes():
                    raise RuntimeError("Original meta GUID backup collision")
                if not owner_meta.exists():shutil.copy2(meta,owner_meta)
                meta.unlink()
            row["quarantinedFromTemporaryUnityImport"]=True
    status=("UNUSED_UV_SOURCES_ISOLATED_IN_RUNNER" if apply
            else "RUNNER_ISOLATION_DRY_RUN_PASS")
    payload={
        "status":status,
        "scope":"Champions APK runner only; no tracked source asset modified",
        "unityEditorTested":False,"apkCompiled":False,
        "originalModelsPreserved":True,
        "models":items,
    }
    EVIDENCE.parent.mkdir(parents=True,exist_ok=True)
    EVIDENCE.write_text(json.dumps(payload,indent=2,ensure_ascii=False)+"\n",
                        encoding="utf8")
    print("BRAX_MISTRZOWIE_UNUSED_GLB "+json.dumps({
        "status":status,"sha256":[r["sha256"] for r in items],
        "files":[r["workingPath"] for r in items]},ensure_ascii=False),flush=True)
    return payload

if __name__=="__main__":
    parser=argparse.ArgumentParser()
    parser.add_argument("--apply-runner-only",action="store_true")
    options=parser.parse_args()
    run(options.apply_runner_only)
