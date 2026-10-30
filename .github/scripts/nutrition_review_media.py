"""Export genuine Nutrition Unity captures without changing game or production.

The source workflow runs the real Unity player. This helper only verifies its
identity, retains the QA result and transfers original screenshots plus a short
unretouched video excerpt through the authenticated review connector.
"""
from __future__ import annotations

import argparse
import base64
import hashlib
import json
import os
from pathlib import Path
import subprocess
import time
import urllib.request

REPOSITORY = "wawerpatryk89-sketch/braxhorn-city"
SOURCE = os.environ.get("REVIEW_SOURCE", "7ee178ab5f688748e0f17bfd3b69cd2a6b937783")
RUN = int(os.environ.get("REVIEW_RUN", "41098711225"))
ARTIFACT = "gym-city-renovation-review-" + SOURCE
ROOT = Path("actual-evidence")
OUTPUT = Path("nutrition-phone-review")


def api(path: str) -> dict:
    request = urllib.request.Request(
        "https://api.github.com/repos/" + REPOSITORY + path,
        headers={
            "Authorization": "Bearer " + os.environ["GH_TOKEN"],
            "Accept": "application/vnd.github+json",
            "X-GitHub-Api-Version": "2022-11-28",
            "User-Agent": "BRAXHORN-actual-Nutrition-review",
        },
    )
    with urllib.request.urlopen(request, timeout=30) as response:
        return json.load(response)


def wait_for_artifact() -> None:
    deadline = time.monotonic() + 125 * 60
    while time.monotonic() < deadline:
        run = api(f"/actions/runs/{RUN}")
        if run["head_sha"] != SOURCE:
            raise RuntimeError("Review source differs from the actual Unity run")
        artifacts = api(f"/actions/runs/{RUN}/artifacts?per_page=100")["artifacts"]
        found = next((a for a in artifacts if a["name"] == ARTIFACT and not a["expired"]), None)
        if found:
            print("ACTUAL_REVIEW_ARTIFACT_AVAILABLE " + json.dumps({
                "run": RUN, "source": SOURCE, "artifact": found["id"],
                "runStatus": run["status"], "runConclusion": run["conclusion"],
                "artifactAvailabilityIsNotQAAcceptance": True,
            }), flush=True)
            return
        if run["status"] == "completed":
            raise RuntimeError("Unity run ended without its actual review artifact: " + str(run["conclusion"]))
        print("WAITING_FOR_ACTUAL_UNITY_REVIEW " + json.dumps({
            "run": RUN, "source": SOURCE, "status": run["status"],
        }), flush=True)
        time.sleep(45)
    raise TimeoutError("Actual Unity evidence did not become available within the review window")


def review_report() -> dict:
    report_path = ROOT / "nutrition-review/report.json"
    if not report_path.is_file():
        raise RuntimeError("The Nutrition player test produced no report; no completed shop is asserted")
    report = json.loads(report_path.read_text())
    if report.get("sourceCommit") != SOURCE:
        raise RuntimeError("Nutrition runtime evidence identifies another source")
    OUTPUT.mkdir(parents=True, exist_ok=True)
    (OUTPUT / "actual-runtime-report.json").write_text(json.dumps(report, indent=2) + "\n")
    playable = ROOT / "build/WebGL/playable.json"
    if playable.is_file():
        identity = json.loads(playable.read_text())
        if identity.get("commit") != SOURCE or identity.get("unity") != "6000.0.58f2":
            raise RuntimeError("Captured player is not the expected Unity 6 source")
    print("BRAX_NUTRITION_ACTUAL_REPORT_BEGIN", flush=True)
    print(json.dumps(report, indent=2), flush=True)
    print("BRAX_NUTRITION_ACTUAL_REPORT_END", flush=True)
    return report


def emit(path: Path, mime: str) -> dict:
    data = path.read_bytes()
    if len(data) > 3_000_000:
        raise RuntimeError("Review transfer file exceeds the explicit size budget: " + path.name)
    metadata = {
        "name": path.name, "mime": mime, "bytes": len(data),
        "sha256": hashlib.sha256(data).hexdigest(), "sourceCommit": SOURCE,
    }
    print("BRAX_REVIEW_FILE_BEGIN " + json.dumps(metadata), flush=True)
    encoded = base64.b64encode(data).decode("ascii")
    for start in range(0, len(encoded), 240):
        print(encoded[start:start + 240])
    print("BRAX_REVIEW_FILE_END " + path.name, flush=True)
    return metadata


def photos() -> int:
    report = review_report()
    names = [
        "00-kerb-and-frontage.png", "02-shop-entrance-and-interior.png",
        "04-counter-after-regeneration.png", "05-products-and-retail-finish.png",
    ]
    source = ROOT / "nutrition-review"
    if not report.get("passed"):
        names.append("failure.png")
    transferred = []
    for name in names:
        path = source / name
        if path.is_file():
            destination = OUTPUT / name
            destination.write_bytes(path.read_bytes())
            transferred.append(emit(destination, "image/png"))
    if not transferred:
        raise RuntimeError("No genuine Nutrition Unity screenshots were produced")
    (OUTPUT / "photos-manifest.json").write_text(json.dumps({
        "sourceCommit": SOURCE, "sourceRun": RUN, "actualUnityScreenshots": True,
        "imageRetouching": False, "ownerAccepted": False,
        "automatedRuntimePassed": report.get("passed", False), "files": transferred,
    }, indent=2) + "\n")
    return 0 if report.get("passed") else 1


def video() -> int:
    report = review_report()
    source = ROOT / "nutrition-review/actual-unity-shop-and-regeneration.mp4"
    if not source.is_file():
        raise RuntimeError("No genuine Nutrition gameplay MP4 was captured")
    destination = OUTPUT / "Nutrition-actual-Unity-counter-45s.mp4"
    subprocess.run([
        "ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-sseof", "-45",
        "-i", str(source), "-t", "45", "-an", "-vf", "scale=960:-2,fps=15",
        "-c:v", "libx264", "-preset", "fast", "-b:v", "300k", "-maxrate", "350k",
        "-bufsize", "700k", "-pix_fmt", "yuv420p", "-movflags", "+faststart", str(destination),
    ], check=True, timeout=120)
    metadata = emit(destination, "video/mp4")
    (OUTPUT / "video-manifest.json").write_text(json.dumps({
        "sourceCommit": SOURCE, "sourceRun": RUN, "actualUnityCapture": True,
        "excerpt": "Final up to 45 seconds of the recorded shop entrance/counter route",
        "playbackSpeed": 1, "audio": False, "diagnosticPositionFixtures": True,
        "physicalMovementWithinNamedLegs": True,
        "gamePerformanceOnPhysicalPhoneMeasured": False, "ownerAccepted": False,
        "automatedRuntimePassed": report.get("passed", False), "file": metadata,
    }, indent=2) + "\n")
    return 0 if report.get("passed") else 1


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("mode", choices=["wait", "photos", "video"])
    mode = parser.parse_args().mode
    if mode == "wait":
        wait_for_artifact()
        return 0
    return photos() if mode == "photos" else video()


if __name__ == "__main__":
    raise SystemExit(main())
