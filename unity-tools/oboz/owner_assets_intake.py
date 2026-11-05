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


def run(package: Path, target: Path, apply: bool) -> dict:
    manifest = json.loads(MANIFEST.read_text("utf-8"))
    if package.stat().st_size != manifest["packageBytes"]:
        raise ValueError("Nieprawidłowy rozmiar paczki")
    if sha256(package.read_bytes()) != manifest["packageSha256"]:
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
    result = {"status": "applied" if apply else "verified_only",
              "source_count": len(expected), "unity_files": len(files),
              "new_files": created, "existing_unchanged": skipped,
              "characters_untouched": True,
              "source_originals_unchanged": True}
    return result


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--package", required=True, type=Path)
    parser.add_argument("--target", type=Path, default=DEFAULT_TARGET)
    parser.add_argument("--apply", action="store_true")
    args = parser.parse_args()
    try:
        result = run(args.package.resolve(), args.target.resolve(), args.apply)
    except (OSError, ValueError, zipfile.BadZipFile, KeyError) as error:
        print("BRAXHORN_OWNER_IMPORT_FAIL:", str(error), file=sys.stderr)
        return 2
    print(json.dumps(result, ensure_ascii=False, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
