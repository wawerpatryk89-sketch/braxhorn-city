#!/usr/bin/env python3
"""Review-only content baseline attestation for Unity 6 BRAXHORN OBÓZ.

It does NOT run Unity, build an APK, publish a baseline or declare phone readiness.
The sealed source snapshot is encrypted and only for a matching future base APK.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
PROJECT = ROOT / "unity-city"
BASE = PROJECT / "BraxContentBuild/base"
RESULT = PROJECT / "BraxContentBuild/content-candidate-review.json"
SCENE = "Assets/PlayableGenerated/Oboz/BraxhornObozReview.unity"


def digest(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as stream:
        for buf in iter(lambda: stream.read(8 * 1024 * 1024), b""):
            h.update(buf)
    return h.hexdigest()


def verify(project: Path = PROJECT) -> dict:
    base = project / "BraxContentBuild/base"
    file = base / "manifest.json"
    m = json.loads(file.read_text("utf-8"))
    zone = m.get("zone")
    identifier = m.get("baseId", "")
    runtime = m.get("runtimeId", "")
    if zone != "oboz" or not re.fullmatch(r"oboz-[0-9a-f]{32}", identifier):
        raise ValueError("Not an immutable oboz base")
    if not re.fullmatch(r"[a-f0-9]{64}", runtime):
        raise ValueError("Missing exact runtime fingerprint")
    if m.get("firstScene") != "BraxhornObozReview" or m.get("scenes") != ["BraxhornObozReview"]:
        raise ValueError("Wrong camp scene set — no other zone may enter this baseline")
    content = project / "Assets/StreamingAssets/BraxContent"
    records = m.get("files")
    if not isinstance(records, list) or not records:
        raise ValueError("Missing catalog and bundle inventory")
    seen = set()
    for rec in records:
        name, length, sha = rec.get("name"), rec.get("bytes"), rec.get("sha256")
        if not isinstance(name, str) or not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9_.-]{0,180}", name):
            raise ValueError("Invalid content filename")
        if name in seen:
            raise ValueError("Duplicate baseline content entry")
        seen.add(name)
        path = content / name
        if not path.is_file() or path.stat().st_size != length or digest(path) != sha:
            raise ValueError("Missing/corrupt baseline catalog or bundle: " + name)
    if not any(x.endswith(".bundle") for x in seen):
        raise ValueError("No actual Addressables bundles generated")
    if m.get("catalog") not in seen:
        raise ValueError("No catalog referenced by the baseline")
    state = base / "addressables_content_state.bin"
    if not state.is_file() or state.stat().st_size < 16:
        raise ValueError("Missing original addressables_content_state.bin")
    scene_record = (base / "scene-guids.txt").read_text("utf-8").splitlines()
    if len(scene_record) != 1 or not re.fullmatch(re.escape(SCENE) + r"=[0-9a-f]{32}", scene_record[0]):
        raise ValueError("Unknown or changed camp scene GUID")
    scene = project / SCENE
    if not scene.is_file() or not (project / (SCENE + ".meta")).is_file():
        raise ValueError("The original authored scene and stable .meta are required")
    resource = json.loads((project / "Assets/Resources/BraxContentBase.json").read_text("utf-8"))
    if (resource.get("zone"), resource.get("baseId"), resource.get("runtimeId")) != (zone, identifier, runtime):
        raise ValueError("Base resource and content manifest mismatch")
    if list((ROOT / "build/ObozAndroid").glob("*.apk")):
        raise ValueError("A standalone APK was produced where only content is permitted")
    return {
        "zone": zone,
        "baseId": identifier,
        "runtimeId": runtime,
        "sceneGuid": scene_record[0].split("=")[1],
        "files": len(records),
        "transferBytes": sum(int(x["bytes"]) for x in records),
        "contentStateSha256": digest(state),
        "catalog": m["catalog"],
        "sourceCommit": os.environ.get("GITHUB_SHA", "local-not-yet-released"),
        "apkBuilt": False,
        "phoneTested": False,
        "baseApkIntegrated": False,
        "status": "ADDRESSABLES_BASE_CANDIDATE_ONLY",
    }


def main() -> int:
    p = argparse.ArgumentParser()
    p.add_argument("operation", choices=("validate", "seal"))
    p.add_argument("--project", type=Path, default=PROJECT)
    p.add_argument("--output", type=Path)
    args = p.parse_args()
    receipt = verify(args.project)
    result = args.project / "BraxContentBuild/content-candidate-review.json"
    result.parent.mkdir(parents=True, exist_ok=True)
    result.write_text(json.dumps(receipt, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    if args.operation == "seal":
        sys.path.insert(0, str(ROOT / "unity-tools/updates"))
        from content_snapshot import pack, identity
        manifest = json.loads((args.project / "BraxContentBuild/base/manifest.json").read_text("utf-8"))
        archive = args.output
        if archive is None:
            raise ValueError("An explicit encrypted archive --output is required")
        if archive.exists():
            raise FileExistsError("Never overwrite another immutable baseline candidate")
        pack(args.project, archive, identity(manifest))
        print("BRAXHORN_OBOZ_CANDIDATE_SEALED sha256=" + digest(archive))
    print("BRAXHORN_OBOZ_CONTENT_CHECK " + json.dumps(receipt, sort_keys=True))
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (ValueError, KeyError, OSError, json.JSONDecodeError) as e:
        print("::error::BRAXHORN_OBOZ_CONTENT_FAIL " + str(e), file=sys.stderr)
        sys.exit(2)
