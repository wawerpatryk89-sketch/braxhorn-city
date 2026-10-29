#!/usr/bin/env python3
"""Fail closed if GameCI reports success after Unity aborted its buildMethod.

The Unity runner can report an action-level success even when -executeMethod
throws before BuildPipeline.BuildPlayer. This guard checks actual WebGL payloads
and build-signed city/character reports before any packaging or publication.
"""
import json
import os
from pathlib import Path
import re


ROOT = Path(__file__).resolve().parents[2]
EXPECTED = {
    "*.loader.js": 1000,
    "*.data.unityweb": 1024 * 1024,
    "*.framework.js.unityweb": 1000,
    "*.wasm.unityweb": 1024 * 1024,
}


def verify(root: Path, commit: str) -> dict:
    if not re.fullmatch(r"[a-f0-9]{40}", commit):
        raise ValueError("An exact 40-character GITHUB_SHA is required")
    player = root / "build" / "WebGL"
    assets = player / "Build"
    if not assets.is_dir():
        raise RuntimeError(
            "Unity buildMethod did not produce build/WebGL/Build. "
            "Do not trust the green game-ci step; inspect Unity exception logs."
        )
    found = {}
    for pattern, minimum in EXPECTED.items():
        matches = list(assets.glob(pattern))
        if len(matches) != 1:
            raise RuntimeError(f"Unity player incomplete: {pattern} matched {len(matches)} files")
        if matches[0].stat().st_size < minimum:
            raise RuntimeError(f"Unity player file missing/truncated: {matches[0]}")
        found[pattern] = matches[0].name
    for filename in ("playable.json", "city-migration.json"):
        path = player / filename
        if not path.is_file():
            raise RuntimeError(
                f"Unity buildMethod did not complete: {filename} missing. "
                "The action-level green status does not certify a Unity player."
            )
        data = json.loads(path.read_text(encoding="utf-8"))
        if data.get("commit") != commit:
            raise RuntimeError(
                f"Unity build provenance mismatch in {filename}: "
                f"{data.get('commit')!r} != {commit}"
            )
        found[filename] = data["commit"]
    return found


def main() -> None:
    sha = os.environ.get("GITHUB_SHA", "")
    found = verify(ROOT, sha)
    print("CITY_UNITY_PLAYER_VERIFIED", sha, json.dumps(found, sort_keys=True), flush=True)


if __name__ == "__main__":
    main()
