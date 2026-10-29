#!/usr/bin/env python3
"""Add two verified pavement/deck materials to the preserved original audit."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil

from audit_assets import Downloader, download_texture, write_json


EXTRA_TEXTURES = ("concrete_pavement_02", "wooden_planks")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, required=True)
    args = parser.parse_args()
    root = args.source.resolve()
    original = root / "source-audit.json"
    report = json.loads(original.read_text())
    if report.get("errors") or report.get("status") != "source_audit_complete_visual_review_pending":
        raise ValueError("Cannot extend an unsuccessful source audit")
    shutil.copyfile(original, root / "source-audit-base.json")
    report["base_audit_commit"] = report["commit"]
    report["commit"] = os.environ.get("GITHUB_SHA", "local")
    report["base_audit_run"] = 40866427252
    downloader = Downloader(root)
    errors = []
    try:
        for asset_id in EXTRA_TEXTURES:
            try:
                result = download_texture(downloader, asset_id, root)
                aliases = {}
                for role, alias in (("diffuse", "diffuse"), ("normal_gl", "normal"), ("arm", "arm")):
                    source = root / result["maps"][role]["path"]
                    destination = root / "textures" / asset_id / (alias + source.suffix.lower())
                    shutil.copyfile(source, destination)
                    with destination.open("rb") as stream:
                        checksum = hashlib.file_digest(stream, "sha256").hexdigest()
                    aliases[role] = {"path": str(destination.relative_to(root)), "sha256": checksum,
                                     "original": result["maps"][role]["path"], "image_bytes_unchanged": True}
                    downloader.files.append({"path": str(destination.relative_to(root)), "bytes": destination.stat().st_size,
                                             "sha256": checksum, "source_alias_of": result["maps"][role]["path"]})
                write_json(root / "textures" / asset_id / "runtime-files.json", aliases)
                result["runtime_aliases"] = aliases
                report["assets"].append(result)
                print("VERIFIED_RUNTIME_TEXTURE_ALIASES", asset_id, json.dumps(aliases), flush=True)
            except Exception as error:
                errors.append({"asset": asset_id, "error": str(error)})
    finally:
        report["files"].extend(downloader.files)
        report["download_bytes"] += downloader.total
        report["errors"].extend(errors)
        report["status"] = "failed" if errors else "source_audit_complete_visual_review_pending"
        write_json(original, report)
    if errors:
        raise RuntimeError(json.dumps(errors))


if __name__ == "__main__":
    main()
