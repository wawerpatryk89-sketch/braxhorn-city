#!/usr/bin/env python3
"""Verify a pinned source artifact and expose its small metadata separately."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    report = json.loads((args.source / "source-audit.json").read_text())
    if report.get("status") != "source_audit_complete_visual_review_pending" or report.get("errors"):
        raise RuntimeError("Pinned original source audit did not succeed")
    for entry in report["files"]:
        path = args.source / entry["path"]
        with path.open("rb") as stream:
            digest = hashlib.file_digest(stream, "sha256").hexdigest()
        if digest != entry["sha256"] or path.stat().st_size != entry["bytes"]:
            raise RuntimeError(f"Original source artifact hash mismatch: {entry['path']}")
    copied = []
    for path in args.source.rglob("*"):
        if path.is_file() and path.suffix.lower() in {".json", ".gltf", ".md"}:
            destination = args.output / path.relative_to(args.source)
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(path, destination)
            copied.append(str(destination.relative_to(args.output)))
    variants = {}
    for variant in args.source.glob("models/*/1k"):
        variants[variant.parent.name] = sum(p.stat().st_size for p in variant.rglob("*") if p.is_file())
    provenance = {"source_run": 40866427252, "source_commit": report["commit"],
                  "source_artifact": "city-rebuild-curated-sources-fc135c8a10b7e54b44d2e97f84d7a662b03ad948",
                  "verified_files": len(report["files"]), "small_report_files": copied,
                  "original_1k_variant_bytes": variants,
                  "source_modified": False, "visual_acceptance": "pending"}
    (args.output / "pinned-source-artifact.json").write_text(json.dumps(provenance, indent=2) + "\n")
    print(json.dumps(provenance), flush=True)


if __name__ == "__main__":
    main()
