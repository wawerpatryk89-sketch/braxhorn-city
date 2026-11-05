#!/usr/bin/env python3
"""Zweryfikowany prywatny import zasobów Obozu BRAXHORN, bez publikacji w Git.

Użycie:
 python3 unity-tools/oboz/owner_assets_intake.py --package /secure/BRAXHORN_OBOZ_WYKONAWCA_ZASOBY_20261105.zip
 python3 unity-tools/oboz/owner_assets_intake.py --package /secure/...zip --apply

Bez --apply: walidacja i suchy przebieg, bez zapisów.
Postaci, animacje, ich tekstury i bieżące pliki w Unity nie są dotykane.
"""
from __future__ import annotations

import argparse
import hashlib
import io
import json
import os
from pathlib import Path, PurePosixPath
import sys
import zipfile

SCRIPT = Path(__file__).resolve()
PROJECT = SCRIPT.parents[2]
MANIFEST = SCRIPT.parent / "OWNER_ASSETS" / "HANDOFF_MANIFEST_20261105.json"
DEFAULT_TARGET = PROJECT / "unity-city" / "Assets" / "Source" / "Prywatne" / "Oboz" / "OwnerAssets"


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha256_file(path: Path, chunk_size: int = 8 * 1024 * 1024) -> str:
    """Stałe zużycie pamięci przy paczce 345 MB, bez read_bytes całego ZIP."""
    digest = hashlib.sha256()
    with path.open("rb") as source:
        while True:
            chunk = source.read(chunk_size)
            if not chunk:
                break
            digest.update(chunk)
    return digest.hexdigest()


def secure_name(name: str) -> PurePosixPath:
    p = PurePosixPath(name)
    if not name or p.is_absolute() or ".." in p.parts or "\\" in name:
        raise ValueError("Niebezpieczna ścieżka: " + name)
    return p


def archive_check(z: zipfile.ZipFile, expected: dict[str, dict]) -> None:
    names = z.namelist()
    if len(names) != len(set(names)) or set(names) != set(expected):
        raise ValueError("Niezgodna lista plików lub zduplikowane nazwy w archiwum")
    for info in z.infolist():
        secure_name(info.filename)
        if info.is_dir() or (info.external_attr >> 16) & 0o170000 == 0o120000:
            raise ValueError("Niedozwolony katalog lub dowiązanie: " + info.filename)
    broken = z.testzip()
    if broken is not None:
        raise ValueError("Błędny CRC: " + broken)


def unpack_plan(raw_zip: zipfile.ZipFile) -> list[tuple[Path, bytes]]:
    output: list[tuple[Path, bytes]] = []
    for member in raw_zip.namelist():
        origin = secure_name(member)
        data = raw_zip.read(member)
        if origin.suffix.lower() in (".glb",):
            output.append((Path(origin.name), data))
        elif origin.suffix.lower() == ".hdr":
            output.append((Path("Panoramy") / origin.name, data))
        elif origin.suffix.lower() in (".jpg", ".jpeg"):
            output.append((Path("Murale") / origin.name, data))
        elif origin.suffix.lower() == ".zip":
            base = origin.name
            if base.endswith(".blend.zip"):
                base = base[: -len(".blend.zip")]
            else:
                base = base[: -len(".zip")]
            with zipfile.ZipFile(io.BytesIO(data)) as nested:
                for f in nested.infolist():
                    secure_name(f.filename)
                    if f.is_dir():
                        continue
                    if (f.external_attr >> 16) & 0o170000 == 0o120000:
                        raise ValueError("Dowiązanie w źródłowym ZIP: " + f.filename)
                    ext = Path(f.filename).suffix.lower()
                    if ext in (".jpg", ".jpeg", ".png", ".exr"):
                        if "Meshy_AI_" in base:
                            rel = Path("Przystanek") / Path(f.filename).name
                        else:
                            rel = Path(base) / "textures" / Path(f.filename).name
                        output.append((rel, nested.read(f.filename)))
                    elif ext == ".fbx":
                        if "Meshy_AI_" in base:
                            output.append((Path("Przystanek") / Path(f.filename).name, nested.read(f.filename)))
                    # BLEND pozostaje w oryginalnym źródłowym ZIP; Unity
                    # nie powinien importować go przypadkiem w CI.
        elif origin.name == "MANIFEST_MATERIALOW.json":
            output.append((Path("Documentation") / origin.name, data))
    if len({str(p) for p, _ in output}) != len(output):
        raise ValueError("Kolizja ścieżek w planie prywatnego importu")
    return output


def ensure_stable_meta(target: Path, relative: Path, folder: bool = False) -> None:
    """Preserve valid existing Unity GUIDs, create deterministic GUID for new inputs.

    Does not touch characters, animation imports, original model bytes or meshes.
    """
    asset = target / relative
    if not asset.exists():
        raise ValueError("Cannot create Unity GUID for missing file: " + str(asset))
    meta = asset.with_name(asset.name + ".meta")
    if meta.exists():
        content = meta.read_text(encoding="utf-8")
        import re
        if not re.search(r"(?m)^guid: [0-9a-f]{32}$", content):
            raise ValueError("Invalid pre-existing Unity GUID: " + str(meta))
        return
    source = relative.as_posix() + ("/" if folder else "")
    guid = hashlib.sha256(("brx-owner-20261105:" + source).encode("utf-8")).hexdigest()[:32]
    data = "fileFormatVersion: 2\n" + "guid: " + guid + "\n"
    if folder:
        data += "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n"
    with meta.open("x", encoding="utf-8") as file:
        file.write(data)


def stamp_unity_metadata(target: Path, files: list[tuple[Path, bytes]]) -> int:
    """Writes only .meta files under OwnerAssets; keeps every existing GUID."""
    if not target.is_dir():
        raise ValueError("OwnerAssets target directory missing")
    # OwnerAssets folder itself has its .meta file next to it, not inside.
    ensure_stable_meta(target.parent, Path(target.name), folder=True)
    folders: set[Path] = set()
    for relative, _ in files:
        current = relative.parent
        while current != Path("."):
            folders.add(current)
            current = current.parent
    for folder in sorted(folders, key=lambda item: (len(item.parts), item.as_posix())):
        ensure_stable_meta(target, folder, folder=True)
    for relative, _ in files:
        ensure_stable_meta(target, relative)
    return 1 + len(folders) + len(files)


def run(package: Path, target: Path, apply: bool) -> dict:
    manifest = json.loads(MANIFEST.read_text("utf-8"))
    if package.stat().st_size != manifest["packageBytes"]:
        raise ValueError("Nieprawidłowy rozmiar paczki")
    if sha256_file(package) != manifest["packageSha256"]:
        raise ValueError("Błędny SHA-256 paczki; nie wolno importować")
    expected = {v["path"]: v for v in manifest["contents"]}
    with zipfile.ZipFile(package) as z:
        archive_check(z, expected)
        for name, row in expected.items():
            buf = z.read(name)
            if len(buf) != row["bytes"] or sha256(buf) != row["sha256"]:
                raise ValueError("Nieprawidłowy SHA-256 źródła: " + name)
        files = unpack_plan(z)
    # Refuse to touch any characters/animations or file outside target.
    if any("character" in str(p).lower() or "animat" in str(p).lower() for p, _ in files):
        raise ValueError("Niedozwolony import do obszaru postaci")
    created = skipped = 0
    for rel, buf in files:
        secure_name(rel.as_posix())
        dst = target / rel
        if dst.exists():
            if sha256(dst.read_bytes()) != sha256(buf):
                raise ValueError("Plik już istnieje i ma inne bajty: " + str(dst))
            skipped += 1
        elif apply:
            dst.parent.mkdir(parents=True, exist_ok=True)
            # Atomic staging; never overwrite an existing asset.
            with dst.open("xb") as handle:
                handle.write(buf)
            created += 1
    meta_count = stamp_unity_metadata(target, files) if apply else 0
    result = {"status": "applied" if apply else "verified_only",
              "source_count": len(expected), "unity_files": len(files),
              "new_files": created, "existing_unchanged": skipped,
              "unity_guids_verified": meta_count,
              "characters_untouched": True,
              "source_originals_unchanged": True}
    return result



# Five independent ZIPs for mobile upload; every source entry is pinned in the
# 31-file owner SHA-256 manifest. Splitting does NOT alter any original model.
SPLIT_PARTS = {
    "BRAXHORN_MODELE_CZESC_01_Z_05.zip": (84984137, "1207ac04b4bd2422a9b5e9870332887b42d110f3628bdf960b5f96174fa4e281"),
    "BRAXHORN_MODELE_CZESC_02_Z_05.zip": (84997165, "01a33b3b93a01604e89bcf0ff0ba3d831d89d32f8ddb52e51051920342d24b8b"),
    "BRAXHORN_MODELE_CZESC_03_Z_05.zip": (84995158, "399abced06451b1dc19a723a0578d7db24d6afb97d992dacd551f22cfcb42321"),
    "BRAXHORN_MODELE_CZESC_04_Z_05.zip": (84985353, "f7a07cd18ef6ad800878d8b2964851ee0262d74ad5098de0b4bf2ca538b14f08"),
    "BRAXHORN_MODELE_CZESC_05_Z_05.zip": (5311312, "59c048af6a84ca4cd5668498f369fbfc746cb2d23c12636e73ced40372bec1c6"),
}


def run_parts(parts: list[Path], target: Path, apply: bool) -> dict:
    """Verify all five small ZIPs, then install original contents and stable GUIDs.

    Each ZIP can be opened separately on iPhone. No re-zipping, re-compression,
    resizing or change to original GLB/FBX/EXR/JPEG bytes is performed.
    """
    manifest = json.loads(MANIFEST.read_text("utf-8"))
    expected = {entry["path"]: entry for entry in manifest["contents"]}
    if len(expected) != len(manifest["contents"]) or len(parts) != 5:
        raise ValueError("Five parts and unique source manifest records required")
    by_name = {item.name: item for item in parts}
    if len(by_name) != len(parts) or set(by_name) != set(SPLIT_PARTS):
        raise ValueError("All five BRAXHORN_MODELE_CZESC_0x_Z_05.zip files are required")
    contents: dict[str, bytes] = {}
    for name, (size, checksum) in SPLIT_PARTS.items():
        path = by_name[name]
        if path.stat().st_size != size or sha256_file(path) != checksum:
            raise ValueError("Damaged or substituted mobile ZIP part: " + name)
        with zipfile.ZipFile(path) as z:
            broken = z.testzip()
            if broken is not None:
                raise ValueError("Wrong CRC in " + name + ": " + broken)
            names = z.namelist()
            if len(names) != len(set(names)):
                raise ValueError("Duplicated source within ZIP: " + name)
            for info in z.infolist():
                filename = str(secure_name(info.filename))
                if info.is_dir() or (info.external_attr >> 16) & 0o170000 == 0o120000:
                    raise ValueError("Unexpected directory or symlink in: " + name)
                if filename not in expected or filename in contents:
                    raise ValueError("Unknown/duplicated owner asset: " + filename)
                item = expected[filename]
                if info.file_size != item["bytes"]:
                    raise ValueError("Wrong source byte length: " + filename)
                raw = z.read(info.filename)
                if len(raw) != item["bytes"] or sha256(raw) != item["sha256"]:
                    raise ValueError("Original model source checksum mismatch: " + filename)
                contents[filename] = raw
    missing = sorted(set(expected) - set(contents))
    if missing:
        raise ValueError("Incomplete five-part mobile transfer: " + repr(missing))
    class VerifiedArchive:
        def namelist(self):
            return list(contents)
        def read(self, name):
            return contents[name]
    # Same mapping of unaltered originals as the big-archive importer.
    files = unpack_plan(VerifiedArchive())
    if any("character" in str(p).lower() or "animat" in str(p).lower() for p, _ in files):
        raise ValueError("A source attempts to modify locked characters")
    created = unchanged = 0
    for rel, raw in files:
        secure_name(rel.as_posix())
        path = target / rel
        if path.exists():
            if sha256_file(path) != sha256(raw):
                raise ValueError("Cannot overwrite different pre-existing source: " + str(path))
            unchanged += 1
        elif apply:
            path.parent.mkdir(parents=True, exist_ok=True)
            with path.open("xb") as output:
                output.write(raw)
            created += 1
    metadata = stamp_unity_metadata(target, files) if apply else 0
    return {
        "status": "applied" if apply else "verified_only",
        "source_count": len(expected), "parts_verified": 5,
        "unity_files": len(files), "new_files": created,
        "existing_unchanged": unchanged, "unity_guids_verified": metadata,
        "characters_untouched": True, "source_originals_unchanged": True,
    }


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--package", type=Path,
                        help="Original 345MB ZIP, OR use --parts")
    parser.add_argument("--parts", type=Path, nargs=5,
                        help="Five unchanged small ZIPs downloaded on mobile")
    parser.add_argument("--target", type=Path, default=DEFAULT_TARGET)
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    try:
        if bool(args.package) == bool(args.parts):
            raise ValueError("Provide exactly one input: --package OR --parts 01 02 03 04 05")
        if args.parts:
            result = run_parts([p.resolve() for p in args.parts],
                               args.target.resolve(), args.apply)
        else:
            result = run(args.package.resolve(), args.target.resolve(), args.apply)
    except (OSError, ValueError, zipfile.BadZipFile, KeyError) as error:
        print("BRAXHORN_OWNER_IMPORT_FAIL:", str(error), file=sys.stderr)
        return 2
    print(json.dumps(result, ensure_ascii=False, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
