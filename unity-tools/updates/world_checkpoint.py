#!/usr/bin/env python3
"""Validate an explicitly completed generation checkpoint, not a successful APK."""
import argparse
import json
import os
from pathlib import Path, PurePosixPath


def validate(project, zone, key, platform="Android"):
    project = Path(project).resolve()
    data = json.loads((project / "Assets/PlayableGenerated/pamiec-rejonu.json").read_text())
    if not key or data.get("rejon") != zone or data.get("klucz") != key or data.get("platforma") != platform:
        raise ValueError("Generation checkpoint does not match this build")
    scenes = data.get("sceny")
    dependencies = data.get("zaleznosci")
    if not isinstance(scenes, list) or not scenes or not isinstance(dependencies, list) or not dependencies:
        raise ValueError("Checkpoint has no scenes/dependencies")
    if not data.get("ustawieniaNaDysku") or not data.get("ustawienia"):
        raise ValueError("Generation settings were not persisted")
    for name in scenes + dependencies:
        if not isinstance(name, str):
            raise ValueError("Invalid dependency path")
        p = PurePosixPath(name)
        if p.is_absolute() or ".." in p.parts or "\\" in name or not p.parts or p.parts[0] != "Assets":
            raise ValueError("Unsafe checkpoint path")
        target = project / name
        if not target.resolve().is_relative_to(project) or not target.is_file():
            raise ValueError("Checkpoint dependency missing or outside project: " + name)
        if not Path(str(target) + ".meta").is_file():
            raise ValueError("Checkpoint dependency has no GUID: " + name)
    for name in ("ProjectSettings/ProjectSettings.asset", "ProjectSettings/QualitySettings.asset"):
        if not (project / name).is_file():
            raise ValueError("Missing generation settings")
    return len(scenes), len(dependencies)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--project", type=Path, default=Path("unity-city"))
    parser.add_argument("--zone", required=True)
    parser.add_argument("--key", required=True)
    args = parser.parse_args()
    try:
        scenes, dependencies = validate(args.project, args.zone, args.key)
    except (ValueError, OSError, TypeError) as exc:
        print("BRAXHORN_GENERATION_CHECKPOINT rejected=" + str(exc))
        return
    print(f"BRAXHORN_GENERATION_CHECKPOINT valid=true scenes={scenes} dependencies={dependencies} apk_validated=false")
    if os.environ.get("GITHUB_OUTPUT"):
        with open(os.environ["GITHUB_OUTPUT"], "a") as f:
            f.write("valid=true\n")


if __name__ == "__main__":
    main()
