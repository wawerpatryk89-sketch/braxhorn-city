#!/usr/bin/env python3
"""Encrypted Unity build cache. No player content, keystore or source binaries.

Keep the entire Library (including Bee). Restore into staging, validate paths and
profile, then install; source-controlled .meta files always win. A cache is an
optimization, never proof that a player or a scene is correct.
"""
import argparse
import hashlib
import io
import json
import os
from pathlib import Path, PurePosixPath
import shutil
import subprocess
import tarfile
import tempfile
import time

SCHEMA = 2
HEADER = "braxhorn-cache.json"


def project_unity_version(project):
    """Select the actual pinned Editor, never restore a cache from another Unity version."""
    version_file = Path(project) / "ProjectSettings/ProjectVersion.txt"
    if not version_file.is_file():
        raise ValueError("Missing ProjectSettings/ProjectVersion.txt")
    for line in version_file.read_text().splitlines():
        if line.startswith("m_EditorVersion:"):
            version = line.partition(":")[2].strip()
            if not version:
                break
            return version
    raise ValueError("Invalid pinned Unity Editor version")


def digest(path):
    with Path(path).open("rb") as stream:
        return hashlib.file_digest(stream, "sha256").hexdigest()


def profile(project, zone, unity, platform="android", variant="arm64-il2cpp"):
    if zone not in ("miasto", "osiedle", "oboz", "oba"):
        raise ValueError("Unknown zone")
    project = Path(project)
    inputs = {}
    # Package lock may be produced on the first Unity import; use the committed
    # manifest as the stable pre/post-build identity. Unity still validates deps.
    for name in ("Packages/manifest.json", "apk-warianty.txt"):
        p = project / name
        inputs[name] = digest(p) if p.is_file() else None
    data = dict(schema=SCHEMA, zone=zone, unity=unity, platform=platform,
                variant=variant, inputs=inputs)
    identity = hashlib.sha256(json.dumps(data, sort_keys=True).encode()).hexdigest()[:24]
    data["key"] = f"library-android-v2-{zone}-{identity}"
    return data


def output(name, value):
    if os.environ.get("GITHUB_OUTPUT"):
        with open(os.environ["GITHUB_OUTPUT"], "a") as f:
            f.write(f"{name}={value}\n")


def require_key():
    if not os.environ.get("KLUCZ"):
        raise ValueError("Missing KLUCZ environment variable")


def members(project):
    """Only regular Library files and source metadata; never traverse links."""
    for folder in ("Library", "Assets"):
        root = project / folder
        if root.is_symlink():
            raise ValueError(f"Symlink root: {folder}")
        if not root.is_dir():
            continue
        for base, dirs, files in os.walk(root, followlinks=False):
            dirs[:] = sorted(d for d in dirs if not (Path(base) / d).is_symlink())
            for name in sorted(files):
                p = Path(base) / name
                rel = p.relative_to(project).as_posix()
                if p.is_symlink() or not p.is_file():
                    continue
                if folder == "Assets" and (not name.endswith(".meta") or
                        rel.startswith("Assets/PlayableGenerated")):
                    continue
                yield p, rel


def pack(project, archive, expected):
    require_key()
    project, archive = Path(project), Path(archive)
    if not (project / "Library").is_dir():
        raise ValueError("No Library to save")
    archive.parent.mkdir(parents=True, exist_ok=True)
    paths = list(members(project))
    raw_size = sum(p.stat().st_size for p, _ in paths)
    # Compress to another disk file; conservative space check prevents ENOSPC
    # during the post-build step. No deletion of the delivered APK is required.
    if shutil.disk_usage(archive.parent).free < raw_size + 128 * 1024 * 1024:
        raise ValueError("Insufficient disk space for encrypted cache")
    temp = archive.with_name(archive.name + ".partial")
    started = time.monotonic()
    encrypt = compress = None
    try:
        with temp.open("wb") as dest:
            encrypt = subprocess.Popen(["openssl", "enc", "-aes-256-cbc", "-pbkdf2",
                "-salt", "-pass", "env:KLUCZ"], stdin=subprocess.PIPE, stdout=dest)
            compress = subprocess.Popen(["zstd", "-q", "-T2", "-3", "-c"],
                stdin=subprocess.PIPE, stdout=encrypt.stdin)
            encrypt.stdin.close()
            with tarfile.open(fileobj=compress.stdin, mode="w|", dereference=True) as tar:
                metadata = json.dumps(dict(profile=expected, raw_bytes=raw_size,
                    source_sha=os.environ.get("GITHUB_SHA", "local")), sort_keys=True).encode()
                head = tarfile.TarInfo(HEADER)
                head.size, head.mode = len(metadata), 0o600
                tar.addfile(head, io.BytesIO(metadata))
                for p, rel in paths:
                    tar.add(p, arcname=rel, recursive=False)
            compress.stdin.close()
            if compress.wait() != 0 or encrypt.wait() != 0:
                raise ValueError("Cache compression/encryption failed")
        temp.replace(archive)
    finally:
        for proc in (compress, encrypt):
            if proc is not None and proc.poll() is None:
                proc.kill()
                proc.wait()
        temp.unlink(missing_ok=True)
    receipt = dict(profile=expected, bytes=archive.stat().st_size,
        sha256=digest(archive), raw_bytes=raw_size,
        bee_bytes=sum(p.stat().st_size for p, rel in paths if rel.startswith("Library/Bee/")),
        seconds=round(time.monotonic() - started, 2))
    archive.with_name(archive.name + ".json").write_text(json.dumps(receipt, indent=2))
    print("BRAXHORN_CACHE_PACK " + json.dumps(receipt, sort_keys=True))
    output("saved", "true")
    return receipt


def extract(stream, stage, expected, legacy=False):
    """Validate every entry before installing any of it into the project."""
    header = None
    seen = set()
    with tarfile.open(fileobj=stream, mode="r|") as tar:
        for entry in tar:
            path = PurePosixPath(entry.name)
            if path.is_absolute() or ".." in path.parts or "\\" in entry.name:
                raise ValueError("Unsafe cache path")
            if entry.name in seen:
                raise ValueError("Duplicate cache entry")
            seen.add(entry.name)
            if not (entry.isfile() or entry.isdir()):
                raise ValueError("Links and special files are forbidden in cache")
            if entry.name == HEADER:
                if entry.size > 16384:
                    raise ValueError("Oversized cache header")
                header = json.load(tar.extractfile(entry))
                if header.get("profile") != expected:
                    raise ValueError("Cache profile mismatch")
                continue
            if not header and not legacy:
                raise ValueError("Cache header must be first")
            if not path.parts:
                raise ValueError("Empty cache path")
            if path.parts[0] == "Assets":
                if not entry.isdir() and (path.suffix != ".meta" or
                        str(path).startswith("Assets/PlayableGenerated")):
                    raise ValueError("Cache contains source data instead of metadata")
            elif path.parts[0] != "Library":
                raise ValueError("Unexpected root in cache")
            destination = stage.joinpath(*path.parts)
            if entry.isdir():
                destination.mkdir(parents=True, exist_ok=True)
            else:
                destination.parent.mkdir(parents=True, exist_ok=True)
                free = shutil.disk_usage(stage).free
                if entry.size > free - 64 * 1024 * 1024:
                    raise ValueError("Insufficient disk space during restore")
                with tar.extractfile(entry) as src, destination.open("wb") as dst:
                    shutil.copyfileobj(src, dst, length=1024 * 1024)
                destination.chmod(entry.mode & 0o777)
                os.utime(destination, (entry.mtime, entry.mtime))
    if not header and not legacy:
        raise ValueError("Missing cache header")
    if not (stage / "Library").is_dir():
        raise ValueError("Missing Library in cache")


def install(stage, project):
    # Never merge an incomplete restore into an existing Library.
    library = project / "Library"
    if library.exists():
        raise ValueError("Refusing to overwrite existing Library")
    metadata = []
    for source in (stage / "Assets").rglob("*.meta") if (stage / "Assets").exists() else []:
        target = project / source.relative_to(stage)
        # A pre-existing source path may itself contain a symlink. Reject escape.
        if not target.resolve().is_relative_to(project.resolve()):
            raise ValueError("Metadata destination escapes project")
        if not target.exists():
            metadata.append((source, target))
    for source, target in metadata:
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)
    shutil.move(str(stage / "Library"), str(library))
    return len(metadata)


def restore(project, archive, expected, legacy=False, sha256=None):
    require_key()
    project, archive = Path(project).resolve(), Path(archive).resolve()
    if sha256 and digest(archive) != sha256:
        raise ValueError("Encrypted cache checksum mismatch")
    started = time.monotonic()
    decrypt = expand = None
    try:
        with tempfile.TemporaryDirectory(prefix=".unity-cache-", dir=project.parent) as tmp:
            decrypt = subprocess.Popen(["openssl", "enc", "-d", "-aes-256-cbc", "-pbkdf2",
                "-in", str(archive), "-pass", "env:KLUCZ"], stdout=subprocess.PIPE)
            expand = subprocess.Popen(["zstd", "-q", "-d", "-c"],
                stdin=decrypt.stdout, stdout=subprocess.PIPE)
            decrypt.stdout.close()
            extract(expand.stdout, Path(tmp), expected, legacy)
            # Consume any remaining padding, then verify both process exit codes.
            while expand.stdout.read(1024 * 1024):
                pass
            expand.stdout.close()
            if expand.wait() != 0 or decrypt.wait() != 0:
                raise ValueError("Cache decryption/decompression failed")
            count = install(Path(tmp), project)
    finally:
        for proc in (expand, decrypt):
            if proc is not None and proc.poll() is None:
                proc.kill()
                proc.wait()
            if proc is not None and proc.stdout is not None:
                proc.stdout.close()
    result = dict(profile=expected["key"], source="legacy" if legacy else "v2",
        meta_restored=count, bee=(project / "Library/Bee").is_dir(),
        seconds=round(time.monotonic() - started, 2))
    print("BRAXHORN_CACHE_RESTORE " + json.dumps(result, sort_keys=True))
    output("restored", "true")
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("profile", "pack", "restore"))
    parser.add_argument("--project", type=Path, default=Path("unity-city"))
    parser.add_argument("--zone", required=True)
    parser.add_argument("--unity", default=None)
    parser.add_argument("--variant", default="arm64-il2cpp")
    parser.add_argument("--archive", type=Path)
    parser.add_argument("--legacy", action="store_true")
    args = parser.parse_args()
    expected = profile(args.project, args.zone, args.unity or project_unity_version(args.project), variant=args.variant)
    if args.command == "profile":
        print(json.dumps(expected, sort_keys=True))
        output("prefix", expected["key"] + "-")
    elif not args.archive:
        parser.error("--archive is required")
    elif args.command == "pack":
        pack(args.project, args.archive, expected)
    else:
        restore(args.project, args.archive, expected, args.legacy)


if __name__ == "__main__":
    main()
