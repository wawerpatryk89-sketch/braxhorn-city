#!/usr/bin/env python3
"""Prepare/publish one reviewed Unity artifact on a new preview-only Git branch.

No Vercel credentials are needed: the existing Git integration handles preview
deployment. A receipt includes an exact Git-source MCP request if that integration
does not react to a bot push. No production ref or project setting is changed.
"""
import argparse
import base64
import datetime
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess
import urllib.request


REPOSITORY = "wawerpatryk89-sketch/braxhorn-city"
SOURCE_BRANCH = "codex/city-reference-rebuild-20261029"
WORKFLOW = ".github/workflows/unity-city-rebuild.yml"
PROJECT = "prj_qeJX3ynFW1b21OYRsZlXKChzSRDh"
TEAM = "team_z2Zm14RoeGXMXDd5Dorzotxj"
REPO_ID = 1436397588
SECTIONS = {
    "touch-and-camera", "public-map", "gym-door-route", "eleven-reservations",
    "traffic-full-cycle", "metro-walk-down-and-back", "district-visual-review",
    "separate-estate-and-return", "browser-background-return",
}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha(value):
    return isinstance(value, str) and re.fullmatch(r"[0-9a-f]{40}", value)


def digest(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def write_json(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2, ensure_ascii=False) + "\n")


def read_request(path):
    marker = json.loads(path.read_text())
    require(marker.get("schema") == 1 and marker.get("status") == "requested",
            "Preview is not requested. Complete actual QA and image review first.")
    for field in ("sourceCommit", "qaCommit"):
        require(sha(marker.get(field)), field + " must be an exact SHA")
    for field in ("sourceRun", "qaRun"):
        require(type(marker.get(field)) is int and marker[field] > 0,
                field + " must be a completed run ID")
    require(marker.get("visualReview") == "reviewed-for-test",
            "The actual candidate images have not been reviewed for a test preview")
    images = marker.get("reviewedImages")
    require(isinstance(images, list) and images and len(images) == len(set(images)),
            "List the actual inspected QA screenshots")
    for name in images:
        require(isinstance(name, str) and re.fullmatch(r"[A-Za-z0-9_-]+\.png", name),
                "Only direct QA screenshot filenames are accepted")
    require(isinstance(marker.get("knownIssues"), list) and
            all(isinstance(v, str) and v.strip() for v in marker["knownIssues"]),
            "knownIssues must be an explicit list of remaining issues")
    marker["previewBranch"] = "preview/city-rebuild-" + marker["sourceCommit"][:12]
    return marker


def verify_context():
    require(os.environ.get("GITHUB_REPOSITORY") == REPOSITORY,
            "Preview publication is restricted to the original repository")
    require(os.environ.get("GITHUB_REF") == "refs/heads/" + SOURCE_BRANCH,
            "Preview publication is restricted to the isolated city source branch")
    require(sha(os.environ.get("GITHUB_SHA")), "Publication request commit is missing")


def github(path):
    require(path.startswith("/repos/" + REPOSITORY + "/"), "Unexpected GitHub resource")
    request = urllib.request.Request("https://api.github.com" + path, headers={
        "Authorization": "Bearer " + os.environ["GH_TOKEN"],
        "Accept": "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28", "User-Agent": "Braxhorn-City-Preview",
    })
    with urllib.request.urlopen(request, timeout=30) as response:
        return json.load(response)


def resolve(marker, evidence):
    verify_context()
    observed = {}
    for prefix, job in (("source", "unity-city-build"), ("qa", "unity-city-review")):
        run_id, commit = marker[prefix + "Run"], marker[prefix + "Commit"]
        run = github(f"/repos/{REPOSITORY}/actions/runs/{run_id}")
        require(run.get("id") == run_id and run.get("head_sha") == commit and
                run.get("head_branch") == SOURCE_BRANCH and run.get("path") == WORKFLOW,
                prefix + " run does not match the selected workflow, branch and commit")
        require(run.get("status") == "completed" and run.get("conclusion") == "success",
                prefix + " run must be complete and successful")
        jobs = github(f"/repos/{REPOSITORY}/actions/runs/{run_id}/jobs?per_page=100")["jobs"]
        chosen = [j for j in jobs if j.get("name") == job]
        require(len(chosen) == 1 and chosen[0].get("conclusion") == "success",
                "The actual " + job + " job did not pass (a skipped job is not a pass)")
        observed[prefix] = {"run": run_id, "commit": commit, "job": chosen[0]["id"]}
    write_json(evidence / "verified-runs.json", observed)
    outputs = {
        "source_run": marker["sourceRun"], "source_commit": marker["sourceCommit"],
        "player_artifact": "city-rebuild-player-" + marker["sourceCommit"],
        "qa_run": marker["qaRun"],
        "qa_artifact": "city-rebuild-runtime-review-" + marker["qaCommit"],
        "preview_branch": marker["previewBranch"],
    }
    with open(os.environ["GITHUB_OUTPUT"], "a") as stream:
        for key, value in outputs.items():
            stream.write(f"{key}={value}\n")
    print("EXACT_REVIEWED_CITY_RUNS", json.dumps(observed))


def require_files(root, files):
    require(isinstance(files, dict) and files, "Artifact has no integrity manifest")
    for name, expected in files.items():
        relative = PurePosixPath(name)
        require(isinstance(name, str) and not relative.is_absolute() and
                ".." not in relative.parts and "\\" not in name and
                not any(part.startswith(".") for part in relative.parts),
                "Unsafe artifact path")
        path = root / name
        require(path.is_file() and not path.is_symlink() and digest(path) == expected,
                "Artifact integrity mismatch: " + name)
        require(path.stat().st_size < 99 * 1024 * 1024,
                "A file exceeds the isolated Git preview budget: " + name)


def stage(marker, player, qa_root, destination, evidence):
    verify_context()
    require(not destination.exists(), "Refusing to replace an existing preview staging directory")
    require(not any(p.is_symlink() for p in player.rglob("*")), "Source artifact contains links")
    release = json.loads((player / "release.json").read_text())
    require(release.get("scope") == "isolated-city-rebuild-preview" and
            release.get("sourceCommit") == marker["sourceCommit"] and
            str(release.get("sourceRun")) == str(marker["sourceRun"]) and
            release.get("productionPublication") is False,
            "Downloaded player is not the selected isolated Unity build")
    require(release.get("loadingFix", {}).get("revision") == "ios-stream-01" and
            release["loadingFix"].get("compiledDataBytesUnchanged") is True,
            "The previously fixed native streaming loader must be preserved")
    require_files(player, release.get("files"))
    actual = {p.relative_to(player).as_posix() for p in player.rglob("*") if p.is_file()}
    require(actual == set(release["files"]) | {"release.json"}, "Unmanifested player files")
    require("createUnityInstance" in (player / "index.html").read_text() and
            "<base " not in (player / "index.html").read_text(),
            "The preview must start the actual Unity loader at its isolated root")
    qa_path = qa_root / "report.json"
    qa = json.loads(qa_path.read_text())
    require(qa.get("sourceCommit") == marker["sourceCommit"] and
            qa.get("qaCommit") == marker["qaCommit"] and
            qa.get("buildId") == release["buildId"] and qa.get("passed") is True,
            "Runtime QA does not match the exact successful candidate")
    require(all(c.get("passed") is not False for c in qa.get("checks", [])) and
            len([c for c in qa.get("checks", []) if c.get("passed") is True]) >= 20,
            "Runtime QA contains failures or insufficient actual checks")
    passed_sections = {s["name"] for s in qa.get("sections", []) if s.get("passed") is True}
    require(SECTIONS.issubset(passed_sections), "Required Unity walkthrough sections did not pass")
    reviewed = {}
    for name in marker["reviewedImages"]:
        image = qa_root / name
        require(image.is_file() and not image.is_symlink() and image.stat().st_size > 1024,
                "An image listed as reviewed is absent: " + name)
        reviewed[name] = digest(image)
    shutil.copytree(player, destination)
    original_config = json.loads((destination / "vercel.json").read_text())
    config = {"framework": None, "buildCommand": "", "installCommand": "",
              "outputDirectory": ".", "cleanUrls": False,
              "headers": original_config.get("headers", []) + [
                  {"source": "/", "headers": [{"key": "Cache-Control", "value": "no-store"}]},
                  {"source": "/release.json", "headers": [{"key": "Cache-Control", "value": "no-store"}]},
              ]}
    write_json(destination / "vercel.json", config)
    summary = {"passed": True, "sourceCommit": marker["sourceCommit"],
               "qaCommit": marker["qaCommit"], "qaRun": marker["qaRun"],
               "platform": qa.get("platform"), "checks": qa["checks"],
               "sections": qa["sections"], "timings": qa.get("timings"),
               "physicalPhoneAcceptance": qa.get("physicalPhoneAcceptance"),
               "visualAcceptance": "reviewed for test only; final city acceptance remains open"}
    write_json(destination / "preview-qa-summary.json", summary)
    receipt = {
        "schema": 1, "status": "prepared-not-pushed", "environment": "preview",
        "repository": REPOSITORY, "branch": marker["previewBranch"],
        "sourceCommit": marker["sourceCommit"], "sourceRun": marker["sourceRun"],
        "qaCommit": marker["qaCommit"], "qaRun": marker["qaRun"],
        "buildId": release["buildId"], "requestCommit": os.environ["GITHUB_SHA"],
        "visualReview": marker["visualReview"], "reviewedImages": reviewed,
        "knownIssues": marker["knownIssues"], "qaReportSha256": digest(qa_path),
        "candidateReleaseSha256": digest(player / "release.json"),
        "deploymentOnlyChanges": ["vercel.json", "preview-qa-summary.json",
                                  "preview-publication.json", "deployment-files.json"],
        "compiledPlayerUnchanged": True, "liveUnityVerified": False,
        "physicalPhoneAcceptance": "not tested", "finalCityAcceptance": "open",
        "productionPublication": False,
        "preparedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
    }
    write_json(destination / "preview-publication.json", receipt)
    files = {p.relative_to(destination).as_posix(): digest(p)
             for p in destination.rglob("*") if p.is_file()}
    write_json(destination / "deployment-files.json", files)
    receipt["deploymentFilesSha256"] = digest(destination / "deployment-files.json")
    receipt["fileCount"] = len(files) + 1
    receipt["bytes"] = sum(p.stat().st_size for p in destination.rglob("*") if p.is_file())
    write_json(evidence / "publication.json", receipt)
    print("ISOLATED_CITY_PREVIEW_PREPARED", receipt["branch"], receipt["fileCount"], receipt["bytes"])


def push(marker, destination, evidence):
    verify_context()
    receipt_path = evidence / "publication.json"
    receipt = json.loads(receipt_path.read_text())
    require(receipt.get("status") == "prepared-not-pushed" and
            receipt.get("requestCommit") == os.environ["GITHUB_SHA"] and
            receipt.get("sourceCommit") == marker["sourceCommit"] and
            receipt.get("branch") == marker["previewBranch"], "Preview staging identity changed")
    require(not (destination / ".git").exists(), "Staging directory is already a repository")
    manifest_path = destination / "deployment-files.json"
    require(digest(manifest_path) == receipt["deploymentFilesSha256"], "Deployment manifest changed")
    require_files(destination, json.loads(manifest_path.read_text()))
    branch = marker["previewBranch"]
    require(re.fullmatch(r"preview/city-rebuild-[0-9a-f]{12}", branch), "Invalid preview branch")
    env = os.environ.copy()
    token = env.pop("GH_TOKEN")
    auth = base64.b64encode(("x-access-token:" + token).encode()).decode()
    env.update(GIT_CONFIG_COUNT="1", GIT_CONFIG_KEY_0="http.https://github.com/.extraheader",
               GIT_CONFIG_VALUE_0="AUTHORIZATION: basic " + auth)
    remote = "https://github.com/" + REPOSITORY + ".git"

    def git(*args, authenticated=False):
        return subprocess.check_output(["git", "-c", "core.hooksPath=/dev/null", *args],
                                       cwd=destination, env=env if authenticated else None,
                                       text=True).strip()

    git("init", "--initial-branch=" + branch)
    git("config", "user.name", "github-actions[bot]")
    git("config", "user.email", "41898282+github-actions[bot]@users.noreply.github.com")
    git("remote", "add", "origin", remote)
    target = "refs/heads/" + branch
    require(not git("ls-remote", "--heads", "origin", target, authenticated=True),
            "This immutable preview branch already exists; refusing to overwrite it")
    git("add", "--all")
    staged = set(git("diff", "--cached", "--name-only", "-z").split("\0")) - {""}
    expected = set(json.loads(manifest_path.read_text())) | {"deployment-files.json"}
    require(staged == expected and not any(p.startswith((".github/", "unity-city/", "dev/")) for p in staged),
            "Unexpected staged files in the artifact-only preview")
    git("commit", "-m", "preview(city): reviewed Unity candidate " + marker["sourceCommit"][:12])
    # Normal create-only push: this root commit has no parent, so any race with
    # another branch creator causes a non-fast-forward rejection. No force push.
    git("push", "origin", "HEAD:" + target, authenticated=True)
    published = git("rev-parse", "HEAD")
    receipt.update(status="branch-created-awaiting-preview-verification", publishedCommit=published)
    write_json(receipt_path, receipt)
    fallback = {
        "requestBody": {"name": "braxhorn-city", "project": PROJECT,
                        "gitSource": {"type": "github", "repoId": REPO_ID,
                                      "ref": branch, "sha": published},
                        "meta": {"citySourceCommit": marker["sourceCommit"],
                                 "cityQaCommit": marker["qaCommit"],
                                 "purpose": "isolated-unity-city-preview"}},
        "note": "Use only if the Git integration did not create this exact preview. Omitted target means preview. Do not add a production target or change project settings.",
        "verifiedProjectOwnerTeam": TEAM,
    }
    write_json(evidence / "vercel-preview-request.json", fallback)
    with open(os.environ["GITHUB_STEP_SUMMARY"], "a") as stream:
        stream.write("## Reviewed Unity artifact saved to an independent test branch\n\n")
        stream.write(f"Branch: `{branch}`\n\nPublication commit: `{published}`\n\n")
        stream.write(f"Unity source: `{marker['sourceCommit']}`; build run: {marker['sourceRun']}; QA run: {marker['qaRun']}.\n\n")
        stream.write("The exact Vercel preview still requires deployment identity, HTTP and live Unity startup verification. Physical phone and final city acceptance remain open.\n")
    print("CITY_PREVIEW_BRANCH_CREATED", branch, published)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("phase", choices=("resolve", "stage", "push"))
    parser.add_argument("--request", type=Path, default=Path(__file__).with_name("preview-request.json"))
    parser.add_argument("--player", type=Path, default=Path("preview-player"))
    parser.add_argument("--qa", type=Path, default=Path("preview-qa"))
    parser.add_argument("--destination", type=Path, default=Path(os.environ.get("RUNNER_TEMP", "/tmp")) / "city-preview-publication")
    parser.add_argument("--evidence", type=Path, default=Path("city-preview-publication-report"))
    args = parser.parse_args()
    marker = read_request(args.request)
    if args.phase == "resolve":
        resolve(marker, args.evidence)
    elif args.phase == "stage":
        stage(marker, args.player, args.qa, args.destination, args.evidence)
    else:
        push(marker, args.destination, args.evidence)


if __name__ == "__main__":
    main()
