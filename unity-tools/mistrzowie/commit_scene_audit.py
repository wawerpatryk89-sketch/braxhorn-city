"""Commit genuine, generated scene-only findings as ONE Git commit (no APK).

Invoked only by an explicitly dispatched Unity scene QA job. Uses the official
GitHub REST Git Database API; does not alter generated scenes or assets.
"""
import json
import os
from pathlib import Path
from urllib.error import HTTPError
from urllib.parse import quote
from urllib.request import Request, urlopen

REPO = "wawerpatryk89-sketch/braxhorn-city"
ALLOWED_BRANCHES = {
    "gpt/miasto-mistrzow-kasyno-t11-20261105",
    "gpt/miasto-mistrzow-hotel-sobczak-t03-20261108",
}
NAMES = ("audit/mistrzowie-scena.json", "audit/mistrzowie-scena.md")
BASE = "https://api.github.com/repos/" + REPO
ROOT = Path(__file__).resolve().parents[2]
TOKEN = os.environ.get("GH_TOKEN", "")
SOURCE = os.environ.get("AUDIT_SOURCE_SHA", "")


def request(method, endpoint, data=None):
    body = json.dumps(data).encode("utf-8") if data is not None else None
    req = Request(
        BASE + endpoint,
        data=body,
        method=method,
        headers={
            "Accept": "application/vnd.github+json",
            "Authorization": "Bearer " + TOKEN,
            "X-GitHub-Api-Version": "2022-11-28",
            "Content-Type": "application/json",
        },
    )
    try:
        with urlopen(req, timeout=40) as response:
            return json.load(response)
    except HTTPError as exc:
        problem = exc.read(1300).decode("utf-8", errors="replace")
        raise RuntimeError(
            "GitHub HTTP " + str(exc.code) + ": " + endpoint + " " + problem
        ) from exc


def main():
    if not TOKEN or len(SOURCE) != 40:
        raise SystemExit("Token and exact source SHA are required")
    if os.environ.get("GITHUB_REPOSITORY") != REPO:
        raise SystemExit("Wrong repository; no write permitted")
    ref_name = os.environ.get("GITHUB_REF", "")
    prefix = "refs/heads/"
    if not ref_name.startswith(prefix):
        raise SystemExit("Not a branch ref; refusing to write")
    branch = ref_name[len(prefix):]
    if branch not in ALLOWED_BRANCHES:
        raise SystemExit("Wrong branch; refusing to write: " + branch)
    content = {}
    for name in NAMES:
        file = ROOT / name
        if not file.is_file() or file.stat().st_size == 0:
            raise SystemExit("Real Unity scene report absent: " + name)
        content[name] = file.read_text(encoding="utf-8")
    data = json.loads(content[NAMES[0]])
    if data.get("sourceCommit") != SOURCE or data.get("status") != "SCENES_ONLY_NO_APK":
        raise SystemExit("Report has wrong source SHA or execution mode")
    if len(content[NAMES[1]].splitlines()) > 150:
        raise SystemExit("Summary must be no longer than 150 lines")

    # Never force push; a concurrent author may advance this same branch.
    ref = "/git/ref/heads/" + quote(branch, safe="/")
    old = request("GET", ref)["object"]["sha"]
    # Never attach a stale scene audit to newer code. If anyone advanced the
    # branch while Unity was running, the scene must be regenerated.
    if old != SOURCE:
        raise SystemExit(
            "Branch advanced during Unity scene validation; stale audit refused. "
            + "source=" + SOURCE + " current=" + old
        )
    parent = request("GET", "/git/commits/" + old)
    entries = []
    for name, text in content.items():
        blob = request("POST", "/git/blobs", {"content": text, "encoding": "utf-8"})
        entries.append({"path": name, "mode": "100644", "type": "blob", "sha": blob["sha"]})
    tree = request("POST", "/git/trees",
                   {"base_tree": parent["tree"]["sha"], "tree": entries})
    commit = request("POST", "/git/commits", {
        "message": "audit(mistrzowie): scene-only measurements for " + SOURCE[:12] + " (NO APK)",
        "tree": tree["sha"],
        "parents": [old],
    })
    request("PATCH", "/git/refs/heads/" + quote(branch, safe="/"),
            {"sha": commit["sha"], "force": False})
    print(commit["sha"])


if __name__ == "__main__":
    main()
