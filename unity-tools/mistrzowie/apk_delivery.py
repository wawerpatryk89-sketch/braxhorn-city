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
from pipeline import configuration

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



def android_platform_jar():
    """Use an installed SDK platform, not an arbitrary new download."""
    roots = [os.environ.get("ANDROID_HOME"), os.environ.get("ANDROID_SDK_ROOT")]
    jars = []
    for root in filter(None, roots):
        jars.extend((Path(root) / "platforms").glob("android-*/android.jar"))
    jars = [jar for jar in jars if jar.is_file()]
    if not jars:
        raise RuntimeError("Android SDK platform android.jar missing before Unity build")
    return max(jars, key=lambda jar: int(jar.parent.name.split("-")[-1])
               if jar.parent.name.split("-")[-1].isdigit() else 0)


def parse_identity(badging):
    match = re.search(r"^package: name='([^']+)' versionCode='(\d+)'", badging, re.M)
    if not match:
        raise ValueError("APK package/version metadata missing")
    return match.group(1), int(match.group(2))


def certificate_sha256():
    cert = command("keytool", "-exportcert", "-alias", "braxhorn", "-keystore",
                   str(ROOT / "unity-city/braxhorn.keystore"), "-storepass:file",
                   str(ROOT / "unity-city/braxhorn.keystore.pass"), binary=True)
    return hashlib.sha256(cert).hexdigest()


def preflight():
    """Read/sign a temporary CSR with the existing key; never modify the keystore."""
    versions = {name: command(sdk_tool(name), "version").strip() for name in ("aapt", "apksigner")}
    password = ROOT / "unity-city/braxhorn.keystore.pass"
    if not password.is_file() or not password.read_text().strip():
        raise RuntimeError("Missing existing signing password")
    with tempfile.TemporaryDirectory(prefix="champions-signing-check-") as tmp:
        # Exporting only the certificate would also accept a certificate-only
        # entry. A CSR proves that the private key and its password are usable.
        command("keytool", "-certreq", "-alias", "braxhorn", "-keystore",
                str(ROOT / "unity-city/braxhorn.keystore"), "-storepass:file", str(password),
                "-keypass:file", str(password), "-file", str(Path(tmp)/"check.csr"))
    expected = certificate_sha256()
    observed = signing_smoke_preflight(expected)
    report = dict(status="SIGNING_KEY_AND_APKSIGNER_SMOKE_PASS_APK_NOT_BUILT",
                  certificateSha256=expected, smokeApkCertificateSha256=observed,
                  tools=versions)
    evidence = ROOT / "mistrzowie-evidence"
    evidence.mkdir(exist_ok=True)
    (evidence / "signing-preflight.json").write_text(json.dumps(report, indent=2) + "\n")
    print(json.dumps(report))


def signer_fingerprints(apk):
    """Return actual APK signer certificates; accept all verified SDK label formats.

    Build Tools 37 prints 'V2 Signer:' or 'V3.0 Signer:', not just
    'Signer #1'. Different schemes may repeat the same certificate.
    A successful apksigner verify is required but is not sufficient:
    one actual APK signer must match the EXISTING keystore certificate.
    """
    output = command(sdk_tool("apksigner"), "verify", "--print-certs", str(apk))
    label = re.compile(
        r"^\s*(?:Signer(?:\s*#\s*\d+|\s*\([^)]*\))?"
        r"|V\d+(?:\.\d+)?\s+Signer)\s*:?\s+certificate\s+"
        r"SHA[-_\s]*256\s+digest\s*:\s*([a-fA-F0-9:\s]+)\s*$",
        re.IGNORECASE)
    digests = []
    unknown = []
    for line in output.splitlines():
        match = label.match(line)
        if match:
            digest = re.sub(r"[^a-fA-F0-9]", "", match.group(1)).lower()
            if len(digest) != 64:
                raise RuntimeError("apksigner returned a malformed certificate SHA-256")
            if digest not in digests:
                digests.append(digest)
        elif (re.search(r"certificate\s+SHA[-_\s]*256\s+digest", line, re.IGNORECASE)
              and not re.search(r"Source\s+Stamp\s+Signer", line, re.IGNORECASE)):
            unknown.append(line.strip()[:180])
    if unknown:
        # Fail explicitly rather than turning an unexpected format into NONE.
        raise RuntimeError("Unrecognized apksigner certificate line(s): " + repr(unknown[:4]))
    if not digests:
        print("BRAX_MISTRZOWIE_APKSIGNER_NO_APK_SIGNER; "
              "verify_output_prefix=" + repr(output.splitlines()[:5]))
    return digests


def signing_smoke_preflight(expected_cert):
    """Sign and verify a real minimal APK with the existing repository key.

    No key creation, no changes to the owner keystore, no release or dispatch.
    An APK-signing regression fails here BEFORE a 50-minute Unity build.
    """
    aapt = sdk_tool("aapt")
    apksigner = sdk_tool("apksigner")
    jar = android_platform_jar()
    keystore = ROOT / "unity-city/braxhorn.keystore"
    password = ROOT / "unity-city/braxhorn.keystore.pass"
    with tempfile.TemporaryDirectory(prefix="champions-real-signing-preflight-") as directory:
        work = Path(directory)
        manifest = work / "AndroidManifest.xml"
        unsigned = work / "preflight-unsigned.apk"
        signed = work / "preflight-signed.apk"
        resigned = work / "preflight-resigned.apk"
        manifest.write_text(
            '<manifest xmlns:android="http://schemas.android.com/apk/res/android" '
            'package="com.braxhorn.preflight" android:versionCode="1" android:versionName="1.0">'
            '<uses-sdk android:minSdkVersion="24" android:targetSdkVersion="28"/>'
            '<application android:hasCode="false" android:label="Preflight"/></manifest>',
            encoding="utf-8")
        command(aapt, "package", "-f", "-M", str(manifest), "-I", str(jar),
                "-F", str(unsigned))
        if not unsigned.is_file() or unsigned.stat().st_size < 256:
            raise RuntimeError("aapt failed to generate a real signing test APK")
        # Single use of the one-line password file; no second --key-pass read.
        command(apksigner, "sign", "--ks", str(keystore), "--ks-key-alias", "braxhorn",
                "--ks-pass", "file:" + str(password), "--min-sdk-version", "24",
                "--out", str(signed), str(unsigned))
        actual = signer_fingerprints(signed)
        print("BRAX_MISTRZOWIE_REAL_SIGNING_SMOKE expected_cert=" + expected_cert
              + " actual_signers=" + (",".join(actual) or "NONE"))
        if actual != [expected_cert]:
            raise RuntimeError("Real apksigner smoke certificate mismatch before Unity build")
        # Production stage re-signs an already packaged APK. Exercise that
        # exact operation now using only the existing keystore as well.
        command(apksigner, "sign", "--ks", str(keystore), "--ks-key-alias", "braxhorn",
                "--ks-pass", "file:" + str(password), "--min-sdk-version", "24",
                "--out", str(resigned), str(signed))
        actual_resigned = signer_fingerprints(resigned)
        print("BRAX_MISTRZOWIE_RE_SIGN_SMOKE expected_cert=" + expected_cert
              + " actual_signers=" + (",".join(actual_resigned) or "NONE"))
        if actual_resigned != [expected_cert]:
            raise RuntimeError("Real apksigner re-sign certificate mismatch before Unity build")
        identity = parse_identity(command(aapt, "dump", "badging", str(resigned)))
        if identity != ("com.braxhorn.preflight", 1):
            raise RuntimeError("Real apksigner smoke changed APK identity")
        return actual[0]


def resign_with_existing_key(source, identity, expected_cert):
    """Use only the existing keystore; never publish without verification.

    The original compiled APK remains untouched if re-signing fails.
    """
    # Re-sign only the runner-owned staged copy. Unity's original APK may
    # belong to root (game-ci Docker) and must never be modified in place.
    if source.parent.resolve() != OUT.resolve():
        raise RuntimeError("Re-signing requires the writable staged APK, not Unity output")
    keystore = ROOT / "unity-city/braxhorn.keystore"
    password = ROOT / "unity-city/braxhorn.keystore.pass"
    with tempfile.TemporaryDirectory(prefix="mistrzowie-resign-", dir=OUT) as temporary:
        signed = Path(temporary) / "signed.apk"
        # The existing key has the same key/store password (checked in
        # preflight). apksigner consumes each file: password on a new line,
        # so reading this one-line password file twice causes EOF on key-pass.
        # Omitting key-pass makes apksigner reuse the verified store password.
        command(sdk_tool("apksigner"), "sign",
                "--ks", str(keystore), "--ks-key-alias", "braxhorn",
                "--ks-pass", "file:" + str(password),
                "--out", str(signed), str(source))
        if not signed.is_file() or signed.stat().st_size < 1024:
            raise RuntimeError("Existing-key signer did not produce an APK")
        signed_fingerprints = signer_fingerprints(signed)
        print("BRAX_MISTRZOWIE_RE_SIGN_CERT expected=" + expected_cert
              + " actual=" + (",".join(signed_fingerprints) or "NONE"))
        if signed_fingerprints != [expected_cert]:
            raise RuntimeError("Re-signed APK still fails repository certificate check: "
                               + "expected=" + expected_cert
                               + " actual=" + (",".join(signed_fingerprints) or "NONE"))
        signed_identity = parse_identity(command(sdk_tool("aapt"), "dump", "badging", str(signed)))
        if signed_identity != identity:
            raise RuntimeError("Re-signed APK changed applicationId/versionCode")
        os.replace(signed, source)
    print("BRAX_MISTRZOWIE_APK_RESIGNED_WITH_EXISTING_KEY_AND_VERIFIED")


def stage():
    source = ROOT / "build/Android/braxhorn-mistrzowie.apk"
    if not source.is_file() or source.stat().st_size < 1024:
        raise RuntimeError("No completed exploration APK")
    app_id, version = parse_identity(command(sdk_tool("aapt"), "dump", "badging", str(source)))
    expected_version = int((ROOT / "unity-city/braxhorn.versioncode").read_text().strip())
    if app_id != APP_ID or version != expected_version:
        raise RuntimeError("APK applicationId/versionCode mismatch")
    expected_cert = certificate_sha256()
    # The Actions entry may live on main while the game is checked out at a
    # separate, explicitly selected Champions commit. Keep both identities.
    workflow_sha = os.environ["GITHUB_SHA"]
    source_receipt = ROOT / "unity-city/mistrzowie.source-commit"
    sha = source_receipt.read_text().strip() if source_receipt.is_file() else workflow_sha
    if not all(re.fullmatch(r"[0-9a-f]{40}", value) for value in (sha, workflow_sha)):
        raise RuntimeError("Invalid source/workflow commit receipt")
    run, attempt = os.environ["GITHUB_RUN_ID"], os.environ["GITHUB_RUN_ATTEMPT"]
    name = f"braxhorn-mistrzowie-{run}-{attempt}-{sha[:7]}.apk"
    # game-ci Unity can leave build/Android owned by root. Never create a
    # signing directory there: copy the completed APK to runner-owned staging.
    OUT.mkdir(exist_ok=True)
    target = OUT / name
    shutil.copyfile(source, target)
    try:
        try:
            signers = signer_fingerprints(target)
        except subprocess.CalledProcessError:
            signers = []
        if signers != [expected_cert]:
            print("BRAX_MISTRZOWIE_APK_SIGNATURE_MISMATCH expected=" + expected_cert
                  + " actual=" + (",".join(signers) or "NONE")
                  + "; attempting existing-key correction")
            resign_with_existing_key(target, (app_id, version), expected_cert)
        if signer_fingerprints(target) != [expected_cert]:
            raise RuntimeError("APK is not signed with the existing repository certificate")
    except Exception:
        target.unlink(missing_ok=True)
        raise
    manifest = dict(file=name, sha256=digest(target), bytes=target.stat().st_size,
                    applicationId=app_id, versionCode=version, certificateSha256=expected_cert,
                    commit=sha, workflowCommit=workflow_sha, runId=run, attempt=attempt, variant="mistrzowie-apk-arm64-gles3",
                    unity=configuration(ROOT)['unity'], runtimeTested=False, visualAccepted=False)
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
            # API size_in_bytes measures ZIP bytes, not the decompressed APK.
            # A valid compressed artifact can be smaller than its APK.
            if artifact["expired"] or artifact["size_in_bytes"] <= 0:
                raise RuntimeError("Artifact expired or empty")
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
    if sys.argv[1:] == ["preflight"]:
        preflight()
    elif sys.argv[1:] == ["stage"]:
        stage()
    elif sys.argv[1:] == ["verify"]:
        verify()
    else:
        raise SystemExit("Use preflight, stage or verify")

