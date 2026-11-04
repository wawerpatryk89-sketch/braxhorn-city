#!/usr/bin/env python3
"""Private, encrypted fallback when Actions evicts a large Unity cache.

Only a dedicated draft release is used. Upload all uniquely named encrypted
parts before replacing its JSON index. Keep the previous snapshot for recovery.
This is CI storage; it is deliberately NOT a game-content download server.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
import time
from unity_cache import digest, output, profile

PART_BYTES = 1536 * 1024 * 1024
PART_NAME = re.compile(r"cache-[0-9]+-[0-9]+-[0-9a-f]{12}-[0-9]{4}\.enc\Z")


def gh(*args):
    result = subprocess.run(["gh", *args], check=True, text=True, capture_output=True)
    return result.stdout


def release_tag(expected, ref):
    return "unity-ci-cache-" + expected["key"].removeprefix("library-android-v2-") + "-" + hashlib.sha256(ref.encode()).hexdigest()[:12]


def view(repo, tag):
    data = json.loads(gh("release", "view", tag, "--repo", repo,
                         "--json", "isDraft,body,assets"))
    if data.get("isDraft") is not True:
        raise ValueError("Refusing a non-draft cache release")
    return data


def read_index(data, expected):
    body = json.loads(data.get("body") or "{}")
    if body.get("schema") != 1 or body.get("profile") != expected:
        raise ValueError("Durable cache profile mismatch")
    return body


def validate_snapshot(snapshot):
    if not isinstance(snapshot, dict) or not re.fullmatch(r"[0-9a-f]{64}", snapshot.get("sha256", "")):
        raise ValueError("Invalid cache snapshot")
    parts = snapshot.get("parts")
    if not isinstance(parts, list) or not 1 <= len(parts) <= 64:
        raise ValueError("Invalid part count")
    seen = set()
    for part in parts:
        name = part.get("name", "")
        if not PART_NAME.fullmatch(name) or name in seen:
            raise ValueError("Invalid/duplicate cache part")
        seen.add(name)
        if not isinstance(part.get("bytes"), int) or not 0 < part["bytes"] <= PART_BYTES:
            raise ValueError("Invalid part size")
        if not re.fullmatch(r"[0-9a-f]{64}", part.get("sha256", "")):
            raise ValueError("Invalid part checksum")
    if sum(p["bytes"] for p in parts) != snapshot.get("bytes"):
        raise ValueError("Invalid total size")


def download(repo, tag, archive, expected):
    # Brak wcześniejszego checkpointu jest NORMALNYM cache-miss, a nie awarią.
    # Błąd autoryzacji/inny błąd serwera nadal zgłaszamy wykonawcy.
    try:
        data = view(repo, tag)
    except subprocess.CalledProcessError as exc:
        err = (exc.stderr or "") + " " + (exc.stdout or "")
        if "not found" in err.lower() or "404" in err:
            print("BRAXHORN_CACHE_DURABLE no_snapshot=true — pełny import bazowy")
            output("downloaded", "false")
            return
        raise
    index = read_index(data, expected)
    archive = Path(archive)
    archive.parent.mkdir(parents=True, exist_ok=True)
    last_error = None
    for snapshot in (index.get("current"), index.get("previous")):
        if not snapshot:
            continue
        try:
            validate_snapshot(snapshot)
            if shutil.disk_usage(archive.parent).free < snapshot["bytes"] + max(p["bytes"] for p in snapshot["parts"]):
                raise ValueError("Insufficient disk space for durable cache download")
            temp = archive.with_name(archive.name + ".partial")
            with tempfile.TemporaryDirectory(dir=archive.parent) as directory, temp.open("wb") as dst:
                for part in snapshot["parts"]:
                    gh("release", "download", tag, "--repo", repo, "--pattern", part["name"], "--dir", directory)
                    path = Path(directory) / part["name"]
                    if path.stat().st_size != part["bytes"] or digest(path) != part["sha256"]:
                        raise ValueError("Downloaded cache part failed verification")
                    with path.open("rb") as src:
                        shutil.copyfileobj(src, dst, 1024 * 1024)
                    path.unlink()
            if temp.stat().st_size != snapshot["bytes"] or digest(temp) != snapshot["sha256"]:
                raise ValueError("Downloaded archive failed verification")
            temp.replace(archive)
            print("BRAXHORN_CACHE_DURABLE restored=true snapshot=" + snapshot["sha256"])
            output("downloaded", "true")
            return
        except (ValueError, OSError, subprocess.CalledProcessError) as exc:
            last_error = exc
            archive.with_name(archive.name + ".partial").unlink(missing_ok=True)
    raise ValueError("No valid durable snapshot") from last_error


def upload(repo, tag, archive, expected):
    archive = Path(archive)
    # Receipt is produced only by the full encrypted cache writer.
    receipt = json.loads(archive.with_name(archive.name + ".json").read_text())
    if receipt["profile"] != expected or receipt["sha256"] != digest(archive):
        raise ValueError("Archive receipt mismatch")
    run = os.environ.get("GITHUB_RUN_ID", "")
    attempt = os.environ.get("GITHUB_RUN_ATTEMPT", "")
    if not run.isdecimal() or not attempt.isdecimal():
        raise ValueError("This operation requires a CI run identity")
    try:
        data = view(repo, tag)
    except subprocess.CalledProcessError:
        # A permission/network error still cannot overwrite another release: the
        # create command must succeed and view must then verify it is a draft.
        gh("release", "create", tag, "--repo", repo, "--draft", "--target", os.environ["GITHUB_SHA"],
           "--title", "Unity CI encrypted cache (not a game release)", "--notes",
           json.dumps(dict(schema=1, profile=expected, current=None, previous=None)))
        data = view(repo, tag)
    index = read_index(data, expected)
    old = index.get("current")
    if old and old.get("sha256") == receipt["sha256"]:
        return
    identity = f"cache-{run}-{attempt}-{receipt['sha256'][:12]}"
    snapshot = dict(sha256=receipt["sha256"], bytes=receipt["bytes"], parts=[])
    started = time.monotonic()
    with tempfile.TemporaryDirectory(dir=archive.parent) as tmp, archive.open("rb") as src:
        number = 0
        while True:
            first = src.read(min(1024 * 1024, PART_BYTES))
            if not first:
                break
            part = Path(tmp) / f"{identity}-{number:04d}.enc"
            with part.open("wb") as dst:
                dst.write(first)
                remaining = PART_BYTES - len(first)
                while remaining:
                    block = src.read(min(1024 * 1024, remaining))
                    if not block:
                        break
                    dst.write(block)
                    remaining -= len(block)
            entry = dict(name=part.name, bytes=part.stat().st_size, sha256=digest(part))
            # Names are content-specific. Re-running a failed upload can replace
            # only the same encrypted bytes, never the last usable snapshot.
            gh("release", "upload", tag, str(part), "--repo", repo, "--clobber")
            snapshot["parts"].append(entry)
            part.unlink()
            number += 1
        validate_snapshot(snapshot)
        uploaded = view(repo, tag)
        available = {a["name"]: a for a in uploaded["assets"]}
        for part in snapshot["parts"]:
            if part["name"] not in available or available[part["name"]].get("size") != part["bytes"]:
                raise ValueError("Incomplete remote snapshot; index is unchanged")
        new_index = dict(schema=1, profile=expected, current=snapshot, previous=old)
        notes = Path(tmp) / "index.json"
        notes.write_text(json.dumps(new_index, indent=2))
        gh("release", "edit", tag, "--repo", repo, "--notes-file", str(notes))
        # Confirm the index before pruning only this tool's older cache parts.
        verified = read_index(view(repo, tag), expected)
        if verified.get("current") != snapshot:
            raise ValueError("Snapshot index verification failed")
        keep = {p["name"] for s in (snapshot, old) if s for p in s["parts"]}
        for name in available:
            if PART_NAME.fullmatch(name) and name not in keep:
                gh("release", "delete-asset", tag, name, "--repo", repo, "--yes")
    print(f"BRAXHORN_CACHE_DURABLE saved=true bytes={receipt['bytes']} seconds={time.monotonic()-started:.1f}")
    output("saved", "true")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("download", "upload"))
    parser.add_argument("--project", type=Path, default=Path("unity-city"))
    parser.add_argument("--zone", required=True)
    parser.add_argument("--archive", type=Path, required=True)
    args = parser.parse_args()
    expected = profile(args.project, args.zone, "6000.0.58f2")
    repo = os.environ["GITHUB_REPOSITORY"]
    tag = release_tag(expected, os.environ["GITHUB_REF"])
    (download if args.command == "download" else upload)(repo, tag, args.archive, expected)


if __name__ == "__main__":
    main()
