"""Validate/signature-check an APK, then verify its immediate GitHub delivery.

No dispatch, retries, deletion, key generation, or overwriting published assets.
"""
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "mistrzowie-apk"
APP_ID = "com.braxhorn.mistrzowie"


def digest(path):
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1024 * 1024), b""):
            h.update(block)
    return h.hexdigest()


def command(*args, binary=False):
    return subprocess.check_output(args, text=not binary)


def sdk_tool(name):
    for key in ("ANDROID_HOME", "ANDROID_SDK_ROOT"):
        root = os.environ.get(key)
        if not root:
            continue
        candidates = sorted((Path(root) / "build-tools").glob("*/" + name))
        if candidates:
            return str(candidates[-1])
    found = shutil.which(name)
    if found:
        return found
    raise RuntimeError("Android SDK tool unavailable: " + name)


def parse_identity(badging):
    match = re.search(r"^package: name='([^']+)' versionCode='(\d+)'", badging, re.M)
    if not match:
        raise ValueError("APK package/version metadata missing")
    return match.group(1), int(match.group(2))


def stage():
    source = ROOT / "build/Android/braxhorn-mistrzowie.apk"
    if not source.is_file() or source.stat().st_size < 1024:
        raise RuntimeError("No completed exploration APK")
    app_id, version = parse_identity(command(sdk_tool("aapt"), "dump", "badging", str(source)))
    expected_version = int((ROOT / "unity-city/braxhorn.versioncode").read_text().strip())
    if app_id != APP_ID or version != expected_version:
        raise RuntimeError("APK applicationId/versionCode mismatch")
    cert = command("keytool", "-exportcert", "-alias", "braxhorn", "-keystore",
                   str(ROOT / "unity-city/braxhorn.keystore"), "-storepass:file",
                   str(ROOT / "unity-city/braxhorn.keystore.pass"), binary=True)
    expected_cert = hashlib.sha256(cert).hexdigest()
    verification = command(sdk_tool("apksigner"), "verify", "--print-certs", str(source))
    signers = re.findall(r"Signer #\d+ certificate SHA-256 digest:\s*([a-fA-F0-9]+)", verification)
    if [s.lower() for s in signers] != [expected_cert]:
        raise RuntimeError("APK is not signed with the existing repository certificate")
    # The Actions entry may live on main while the game is checked out at a
    # separate, explicitly selected Champions commit. Keep both identities.
    workflow_sha = os.environ["GITHUB_SHA"]
    source_receipt = ROOT / "unity-city/mistrzowie.source-commit"
    sha = source_receipt.read_text().strip() if source_receipt.is_file() else workflow_sha
    if not all(re.fullmatch(r"[0-9a-f]{40}", value) for value in (sha, workflow_sha)):
        raise RuntimeError("Invalid source/workflow commit receipt")
    run, attempt = os.environ["GITHUB_RUN_ID"], os.environ["GITHUB_RUN_ATTEMPT"]
    name = f"braxhorn-mistrzowie-{run}-{attempt}-{sha[:7]}.apk"
    OUT.mkdir(exist_ok=True)
    target = OUT / name
    shutil.copyfile(source, target)
    manifest = dict(file=name, sha256=digest(target), bytes=target.stat().st_size,
                    applicationId=app_id, versionCode=version, certificateSha256=expected_cert,
                    commit=sha, workflowCommit=workflow_sha, runId=run, attempt=attempt, variant="mistrzowie-apk-arm64-gles3",
                    unity="6000.0.58f2", runtimeTested=False, visualAccepted=False)
    (OUT / "manifest.json").write_text(json.dumps(manifest, indent=2) + "\n")
    print(json.dumps(manifest))


def api(path):
    return json.loads(command("gh", "api", path))


def verify_artifact_contents(repo, artifact_id, manifest):
    # Stream the ZIP to disk: the APK can be too large to hold twice in RAM.
    with tempfile.TemporaryDirectory(prefix="mistrzowie-artifact-") as tmp:
        archive = Path(tmp) / "artifact.zip"
        with archive.open("wb") as output:
            subprocess.run(["gh", "api", f"repos/{repo}/actions/artifacts/{artifact_id}/zip"],
                           stdout=output, check=True)
        with zipfile.ZipFile(archive) as bundle:
            matches = [entry for entry in bundle.infolist() if entry.filename == manifest["file"]]
            if len(matches) != 1 or matches[0].file_size != manifest["bytes"]:
                raise RuntimeError("Actions artifact APK missing, duplicated or wrong size")
            h = hashlib.sha256()
            with bundle.open(matches[0]) as apk:
                for block in iter(lambda: apk.read(1024 * 1024), b""):
                    h.update(block)
            if h.hexdigest() != manifest["sha256"]:
                raise RuntimeError("Actions artifact APK SHA-256 differs")


def verify():
    manifest = json.loads((OUT / "manifest.json").read_text())
    apk = OUT / manifest["file"]
    if digest(apk) != manifest["sha256"] or apk.stat().st_size != manifest["bytes"]:
        raise RuntimeError("APK changed after validation")
    repo = os.environ["GITHUB_REPOSITORY"]
    delivery, problems = {}, []
    artifact_id = os.environ.get("APK_ARTIFACT_ID", "")
    if artifact_id.isdigit():
        try:
            artifact = api(f"repos/{repo}/actions/artifacts/{artifact_id}")
            if artifact["expired"] or artifact["size_in_bytes"] < manifest["bytes"]:
                raise RuntimeError("Artifact expired or incomplete")
            if artifact["workflow_run"]["head_sha"] != manifest.get("workflowCommit", manifest["commit"]):
                raise RuntimeError("Artifact belongs to a different commit")
            if str(artifact["workflow_run"]["id"]) != manifest["runId"]:
                raise RuntimeError("Artifact belongs to a different workflow run")
            verify_artifact_contents(repo, artifact_id, manifest)
            delivery["artifactUrl"] = os.environ["APK_ARTIFACT_URL"]
            delivery["artifactId"] = artifact["id"]
        except Exception as e:
            problems.append("Actions delivery: " + str(e))
    else:
        problems.append("Actions artifact upload unavailable/failed")
    try:
        tag = os.environ["APK_RELEASE_TAG"]
        release = api(f"repos/{repo}/releases/tags/{tag}")
        asset = next(a for a in release["assets"] if a["name"] == manifest["file"])
        if asset["size"] != manifest["bytes"]:
            raise RuntimeError("Release APK size differs")
        remote_digest = asset.get("digest")
        if remote_digest:
            if remote_digest != "sha256:" + manifest["sha256"]:
                raise RuntimeError("Release APK SHA-256 differs")
        else:
            with tempfile.TemporaryDirectory(prefix="mistrzowie-delivery-") as tmp:
                command("gh", "release", "download", tag, "--repo", repo,
                        "--pattern", manifest["file"], "--dir", tmp)
                if digest(Path(tmp) / manifest["file"]) != manifest["sha256"]:
                    raise RuntimeError("Downloaded release APK SHA-256 differs")
        delivery["apkUrl"] = asset["browser_download_url"]
        delivery["releaseUrl"] = release["html_url"]
    except Exception as e:
        problems.append("Release delivery: " + str(e))
    if not delivery:
        raise RuntimeError("APK delivery failed: " + "; ".join(problems))
    report = dict(manifest, status="APK_DELIVERED_RUNTIME_REVIEW_PENDING", delivery=delivery, warnings=problems)
    evidence = ROOT / "mistrzowie-evidence"
    evidence.mkdir(exist_ok=True)
    (evidence / "apk-delivery.json").write_text(json.dumps(report, indent=2) + "\n")
    with open(os.environ["GITHUB_STEP_SUMMARY"], "a") as f:
        f.write("## Miasto Mistrzów — APK zapisane\n\n")
        for label, key in (("Pobierz APK", "apkUrl"), ("Actions artifact", "artifactUrl")):
            if key in delivery:
                f.write(f"[{label}]({delivery[key]})\n\n")
        f.write(f"Commit `{manifest['commit']}`, versionCode `{manifest['versionCode']}`. "
                f"Rozmiar {manifest['bytes']} B; SHA-256 `{manifest['sha256']}`.\n\n"
                "Test sterowania, wyglądu i biegu na urządzeniu: oczekuje na sprawdzenie.\n")
    for problem in problems:
        print("::warning::" + problem)
    print(json.dumps(report))


if __name__ == "__main__":
    if sys.argv[1:] == ["stage"]:
        stage()
    elif sys.argv[1:] == ["verify"]:
        verify()
    else:
        raise SystemExit("Use stage or verify")

