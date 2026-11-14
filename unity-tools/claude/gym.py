"""Install selected, checksum-verified Mixamo exercises from the shared gym archive."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import tempfile
from zipfile import ZipFile

from prywatne import CEL, ROOT

CATALOG = ROOT / "Postacie" / "silownia" / "gym.json"


def digest(data):
    return hashlib.sha256(data).hexdigest()


def przygotuj_gym(characters, archive=None, root=CEL, catalog=CATALOG):
    data = json.loads(catalog.read_text(encoding="utf-8"))
    clips = {clip["id"]: clip for clip in data["clips"]}
    selected = set(characters)
    profiles = [profile for profile in data["profiles"] if profile["id"] in selected]
    expected = {}
    for profile in profiles:
        ids = profile["exercises"]
        if len(ids) != 3 or len(set(ids)) != 3:
            raise ValueError(f"Oczekiwano trzech różnych ćwiczeń: {profile['id']}")
        for exercise in ids:
            if not re.fullmatch(r"[A-Za-z][A-Za-z0-9]*", exercise):
                raise ValueError("Niepoprawny identyfikator ćwiczenia")
            group = "GymPatryk" if profile["id"] == "patryk" else "Gym"
            expected[root / group / (exercise + ".fbx")] = clips[exercise]
    pending = {p: clip for p, clip in expected.items()
               if not p.is_file() or digest(p.read_bytes()) != clip["sha256"]}
    if not pending:
        return

    with tempfile.TemporaryDirectory() as temp:
        if archive is None:
            from pobierz_obsade import pobierz
            archive = Path(temp) / "gym.zip"
            pobierz(data["archive"], os.environ["GITHUB_REPOSITORY"], archive)
        if digest(Path(archive).read_bytes()) != data["archive"]["sha256"]:
            raise ValueError("Niezgodna suma SHA-256 paczki siłowni")
        # Validate the entire selection before replacing any FBX. Keep Unity .meta files.
        staged = {}
        with ZipFile(archive) as source:
            for path, clip in pending.items():
                content = source.read(clip["entry"])
                if digest(content) != clip["sha256"]:
                    raise ValueError("Niepoprawny klip siłowni: " + clip["entry"])
                staged[path] = content
        for path, content in staged.items():
            path.parent.mkdir(parents=True, exist_ok=True)
            tmp = tempfile.NamedTemporaryFile(dir=path.parent, delete=False)
            try:
                with tmp:
                    tmp.write(content)
                os.replace(tmp.name, path)
            finally:
                Path(tmp.name).unlink(missing_ok=True)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("archive", type=Path)
    parser.add_argument("characters", nargs="+", help="Identyfikatory postaci, np. aneta patryk")
    args = parser.parse_args()
    available = {profile["id"] for profile in json.loads(CATALOG.read_text())["profiles"]}
    unknown = set(args.characters) - available
    if unknown:
        parser.error("Brak profilu gym: " + ", ".join(sorted(unknown)))
    przygotuj_gym(args.characters, args.archive)
