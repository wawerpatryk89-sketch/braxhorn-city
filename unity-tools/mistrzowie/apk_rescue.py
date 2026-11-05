"""Preserve/recover compiled Miasto Mistrzow APK if post-build signing fails.

A quarantined file is not an installable or verified release. Restore is manual,
validates GitHub run, artifact provenance, source revision and raw APK SHA-256.
This module cannot generate/change signing keys or launch Unity.
"""
import hashlib
import json
import os
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
RAW_APK = ROOT / "build/Android/braxhorn-mistrzowie.apk"
CHECKPOINT = ROOT / "mistrzowie-rescue/raw.json"
VERSION = ROOT / "unity-city/braxhorn.versioncode"
SOURCE = ROOT / "unity-city/mistrzowie.source-commit"
WORKFLOW = ".github/workflows/mistrzowie-apk.yml"


def sha256(path):
    value = hashlib.sha256()
    with path.open("rb") as file:
        for chunk in iter(lambda: file.read(1048576), b""):
            value.update(chunk)
    return value.hexdigest()


def run_command(*args):
    return subprocess.check_output(args, text=True)


def save():
    if not RAW_APK.is_file() or RAW_APK.stat().st_size <= 1024:
        raise RuntimeError("No completed original Unity APK available for quarantine")
    marker = ROOT / "build/Android/braxhorn-mistrzowie.gotowe"
    if not marker.is_file():
        raise RuntimeError("Missing Unity completed-build marker")
    source = SOURCE.read_text().strip()
    if not re.fullmatch(r"[0-9a-f]{40}", source):
        raise RuntimeError("No validated original source commit")
    version = VERSION.read_text().strip()
    if not version.isdigit() or int(version) < 1:
        raise RuntimeError("Missing original compiled APK version")
    data = dict(
        status="QUARANTINED_APK_NOT_VERIFIED_NOT_READY_FOR_INSTALL",
        sourceCommit=source,
        workflowCommit=os.environ["GITHUB_SHA"],
        runId=os.environ["GITHUB_RUN_ID"],
        runAttempt=os.environ["GITHUB_RUN_ATTEMPT"],
        versionCode=int(version),
        sha256=sha256(RAW_APK),
        bytes=RAW_APK.stat().st_size,
        file="build/Android/braxhorn-mistrzowie.apk",
    )
    CHECKPOINT.parent.mkdir(parents=True, exist_ok=True)
    CHECKPOINT.write_text(json.dumps(data, indent=2) + "\n")
    print("BRAX_MISTRZOWIE_RAW_APK_QUARANTINED " +
          json.dumps({key:data[key] for key in ("status", "runId", "sourceCommit", "bytes", "sha256")}))
    return data


def api(path):
    return json.loads(run_command("gh", "api", path))


def restore():
    run_id = os.environ["RECOVERY_RUN_ID"].strip()
    repo = os.environ["GITHUB_REPOSITORY"]
    if not run_id.isdigit() or int(run_id) <= 0:
        raise RuntimeError("Provide exact numeric source run ID for recovery")
    original = api(f"repos/{repo}/actions/runs/{run_id}")
    if (original.get("path") != WORKFLOW or original.get("status") != "completed"
            or original.get("conclusion") != "failure"
            or original.get("repository", {}).get("full_name") != repo):
        raise RuntimeError("Recovery source is not a completed failed Champions workflow in this repository")
    archive = api(f"repos/{repo}/actions/runs/{run_id}/artifacts")
    prefix = f"UNVERIFIED-mistrzowie-{run_id}-"
    candidates = [item for item in archive.get("artifacts", [])
                  if item["name"].startswith(prefix) and not item["expired"]]
    if len(candidates) != 1:
        raise RuntimeError("Exactly one quarantined APK artifact required; source must have been saved after failure")
    artifact = candidates[0]
    name = artifact["name"]
    match = re.fullmatch(r"UNVERIFIED-mistrzowie-" + run_id + r"-(\d+)-([0-9a-f]{40})", name)
    if not match:
        raise RuntimeError("Invalid immutable quarantine artifact identifier")
    if (str(artifact["workflow_run"]["id"]) != run_id
            or artifact["workflow_run"]["head_sha"] != original["head_sha"]):
        raise RuntimeError("Quarantine artifact does not belong to the selected run")
    if RAW_APK.exists() or CHECKPOINT.exists():
        raise RuntimeError("Restore target is not empty; refusing to overwrite existing APK")
    run_command("gh", "run", "download", run_id, "--repo", repo, "--name", name, "--dir", str(ROOT))
    record = json.loads(CHECKPOINT.read_text())
    if (record.get("status") != "QUARANTINED_APK_NOT_VERIFIED_NOT_READY_FOR_INSTALL"
            or record.get("runId") != run_id or str(record.get("runAttempt")) != match.group(1)
            or record.get("sourceCommit") != match.group(2)
            or record.get("workflowCommit") != original["head_sha"]
            or record.get("file") != "build/Android/braxhorn-mistrzowie.apk"):
        raise RuntimeError("Recovered checkpoint provenance does not match immutable GitHub run")
    if (SOURCE.read_text().strip() != record["sourceCommit"]
            or VERSION.read_text().strip() != str(record["versionCode"])):
        raise RuntimeError("Recovered source commit or Android version differs from checkpoint")
    if (not RAW_APK.is_file() or RAW_APK.stat().st_size != record["bytes"]
            or sha256(RAW_APK) != record["sha256"]):
        raise RuntimeError("Recovered APK SHA-256/size mismatch, abort before any signing")
    print("BRAX_MISTRZOWIE_RECOVERED_UNVERIFIED_BYTES " +
          json.dumps({k:record[k] for k in ("sourceCommit", "runId", "bytes", "sha256")}))
    return record


if __name__ == "__main__":
    if sys.argv[1:] == ["save"]:
        save()
    elif sys.argv[1:] == ["restore"]:
        restore()
    else:
        raise SystemExit("Usage: apk_rescue.py save|restore")
