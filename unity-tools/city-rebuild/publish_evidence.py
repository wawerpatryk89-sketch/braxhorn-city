#!/usr/bin/env python3
"""Preserve real Unity screenshots on a new evidence-only branch, including FAIL.

This is not a player deployment and never changes a source or production ref.
The evidence branch can be read via GitHub fetch_file(encoding="base64") when
artifact ZIP transfers are unavailable. PNG bytes remain unmodified.
"""
import base64
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess


REPO = "wawerpatryk89-sketch/braxhorn-city"
BRANCH = "codex/city-reference-rebuild-20261029"
ROOT = Path(__file__).resolve().parents[2]


def require(condition, message):
    if not condition:
        raise ValueError(message)


def digest(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def json_write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, ensure_ascii=False) + "\n")


def transfer_parts(source, destination):
    # GitHub's Contents API omits content for binaries larger than 1 MiB.
    # Preserve the exact PNG, adding small ASCII parts only for that transport.
    if source.stat().st_size <= 900_000:
        return []
    encoded = base64.b64encode(source.read_bytes()).decode("ascii")
    parts = []
    for index, start in enumerate(range(0, len(encoded), 512 * 1024)):
        name = "transport/" + source.name + f".{index:03d}.b64.txt"
        target = destination / name
        target.parent.mkdir(exist_ok=True)
        target.write_text(encoded[start:start + 512 * 1024] + "\n", encoding="ascii")
        parts.append({"path": name, "bytes": target.stat().st_size, "sha256": digest(target)})
    return parts


def main():
    require(os.environ.get("GITHUB_REPOSITORY") == REPO and
            os.environ.get("GITHUB_REF") == "refs/heads/" + BRANCH,
            "Evidence export is restricted to the isolated city source branch")
    commit = os.environ.get("GITHUB_SHA", "")
    require(re.fullmatch(r"[0-9a-f]{40}", commit), "QA commit is missing")
    target = json.loads((Path(__file__).with_name("review-target.json")).read_text())
    require(re.fullmatch(r"[0-9a-f]{40}", target.get("commit", "")), "Build source is not pinned")
    source = ROOT / "city-runtime-evidence"
    destination = Path(os.environ["RUNNER_TEMP"]) / "city-evidence-git"
    proof = ROOT / "city-evidence-publication-report"
    require(source.is_dir() and not destination.exists(), "Evidence input is missing or staging already exists")
    require(not any(p.is_symlink() for p in source.rglob("*")), "Evidence artifact contains links")
    images = sorted(source.glob("*.png"))
    require(images, "No real screenshot was captured; no evidence branch will be fabricated")
    report_path = source / "report.json"
    report = json.loads(report_path.read_text()) if report_path.is_file() else None
    if report is not None:
        require(report.get("sourceCommit") == target["commit"] and report.get("qaCommit") == commit,
                "Actual QA report does not belong to this candidate and QA commit")
    files = images + [source / n for n in ("report.json", "reservations.json", "traffic-cycle.json", "browser.log")
                      if (source / n).is_file()]
    require(len(files) <= 100, "Unexpected number of evidence files")
    require(sum(p.stat().st_size for p in files) < 80 * 1024 * 1024,
            "Small screenshot/report export exceeded its 80 MiB budget")
    destination.mkdir()
    inventory = {}
    for path in files:
        require(re.fullmatch(r"[A-Za-z0-9_-]+\.(png|json|log)", path.name), "Unsafe evidence filename")
        require(0 < path.stat().st_size <= 8 * 1024 * 1024, "Evidence file is unexpectedly large: " + path.name)
        if path.suffix == ".png":
            with path.open("rb") as stream:
                require(stream.read(8) == b"\x89PNG\r\n\x1a\n", "Screenshot is not a PNG: " + path.name)
        shutil.copyfile(path, destination / path.name)
        require(digest(destination / path.name) == digest(path), "Evidence copy changed")
        inventory[path.name] = {"bytes": path.stat().st_size, "sha256": digest(path)}
        parts = transfer_parts(path, destination)
        if parts:
            inventory[path.name]["transport"] = {"encoding": "concatenated-base64-text", "parts": parts}
    branch = "evidence/city-rebuild-" + commit[:12]
    manifest = {
        "schema": 1, "purpose": "Actual Unity runtime screenshots and QA, not a game deployment",
        "sourceCommit": target["commit"], "sourceRun": target["runId"],
        "qaCommit": commit, "qaRun": os.environ["GITHUB_RUN_ID"],
        "qaPassed": report.get("passed") if report else None,
        "qaStatus": "completed report" if report else "review interrupted before report was written",
        "branch": branch, "files": inventory,
        "imageBytesUnmodified": True, "physicalPhoneTested": False,
        "visualAcceptance": "pending image review", "productionPublication": False,
        "fullTraceAndVideo": "Preserved separately in the same QA run's workflow artifacts",
    }
    json_write(destination / "evidence-manifest.json", manifest)
    # Only this new evidence branch disables its own automatic Git deployment.
    # No existing branch's vercel.json or project setting is modified.
    json_write(destination / "vercel.json", {"git": {"deploymentEnabled": {branch: False}}})
    (destination / "README.md").write_text(
        "# Actual Unity runtime evidence\n\n"
        + "Source commit: `" + target["commit"] + "`\n\nQA commit: `" + commit + "`\n\n"
        + "QA passed: `" + str(manifest["qaPassed"]).lower() + "`. Failures are preserved for inspection.\n\n"
        + "These PNGs are exact browser captures of the Unity candidate; no generated or replacement images. "
        + "This branch is evidence only. It does not replace the live game. Physical phone and final visual acceptance remain open.\n")

    env = os.environ.copy()
    token = env.pop("GH_TOKEN")
    auth = base64.b64encode(("x-access-token:" + token).encode()).decode()
    env.update(GIT_CONFIG_COUNT="1", GIT_CONFIG_KEY_0="http.https://github.com/.extraheader",
               GIT_CONFIG_VALUE_0="AUTHORIZATION: basic " + auth)

    def git(*args, auth=False):
        return subprocess.check_output(["git", "-c", "core.hooksPath=/dev/null", *args],
                                       cwd=destination, env=env if auth else None, text=True).strip()

    git("init", "--initial-branch=" + branch)
    git("config", "user.name", "github-actions[bot]")
    git("config", "user.email", "41898282+github-actions[bot]@users.noreply.github.com")
    git("remote", "add", "origin", "https://github.com/" + REPO + ".git")
    ref = "refs/heads/" + branch
    require(not git("ls-remote", "--heads", "origin", ref, auth=True),
            "Evidence for this QA commit already exists; refusing to overwrite it")
    git("add", "--all")
    expected = set(inventory) | {"evidence-manifest.json", "vercel.json", "README.md"}
    expected.update(part["path"] for entry in inventory.values()
                    for part in entry.get("transport", {}).get("parts", []))
    staged = set(git("diff", "--cached", "--name-only", "-z").split("\0")) - {""}
    require(staged == expected, "Out-of-scope file in evidence commit")
    git("commit", "-m", "evidence(city): preserve actual Unity review " + commit[:12])
    git("push", "origin", "HEAD:" + ref, auth=True)
    publication = {"branch": branch, "commit": git("rev-parse", "HEAD"),
                   "sourceCommit": target["commit"], "qaCommit": commit,
                   "qaPassed": manifest["qaPassed"], "files": inventory,
                   "productionPublication": False}
    json_write(proof / "publication.json", publication)
    print("CITY_EVIDENCE_PUBLISHED " + json.dumps(publication, separators=(",", ":")))
    with open(os.environ["GITHUB_STEP_SUMMARY"], "a") as summary:
        summary.write("## Exact Unity screenshots available for inspection\n\n")
        summary.write(f"Evidence branch: `{branch}`\n\nCommit: `{publication['commit']}`\n\n")
        summary.write("QA result (preserved even on failure): `" + str(manifest["qaPassed"]).lower() + "`\n\n")
        summary.write("Read `evidence-manifest.json` at that commit, then fetch exact PNG paths with `encoding: base64`. This is not a Unity player deployment or a phone test.\n")


if __name__ == "__main__":
    main()
