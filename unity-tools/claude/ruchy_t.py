"""Install shared role clips from the owner's T archive before Unity imports the cast."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import tempfile
from zipfile import ZipFile

from prywatne import CEL, ROOT

CATALOG = ROOT / "Postacie/ruchy/mixamo.json"


def save(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.is_file() and path.read_bytes() == data:
        return
    with tempfile.NamedTemporaryFile(dir=path.parent, delete=False) as tmp:
        try:
            tmp.write(data)
            tmp.close()
            os.replace(tmp.name, path)
        finally:
            Path(tmp.name).unlink(missing_ok=True)


def install(characters, archive=None, root=CEL, catalog=CATALOG):
    data = json.loads(catalog.read_text())
    plans = {}
    needed = {}
    for character in sorted(set(characters)):
        profile = data["profiles"][character]
        entries = []
        for state, key in profile.items():
            clip = data["clips"][key]
            needed[key] = clip
            entries.append({"state": state, "file": Path(clip["path"]).name})
        plans[character] = entries
    dest = root / "MixamoT"
    with tempfile.TemporaryDirectory() as tmp:
        pending = {}
        for key, clip in needed.items():
            output = dest / Path(clip["path"]).name
            if not output.is_file() or hashlib.sha256(output.read_bytes()).hexdigest() != clip["sha256"]:
                pending[key] = clip
        if pending:
            if archive is None:
                from pobierz_obsade import pobierz
                archive = Path(tmp) / "T.zip"
                pobierz(data["archive"], os.environ["GITHUB_REPOSITORY"], archive)
            if hashlib.sha256(Path(archive).read_bytes()).hexdigest() != data["archive"]["sha256"]:
                raise ValueError("Niepoprawna suma paczki T")
            with ZipFile(archive) as z:
                for key, clip in pending.items():
                    content = z.read(clip["path"])
                    if hashlib.sha256(content).hexdigest() != clip["sha256"]:
                        raise ValueError("Uszkodzony klip " + clip["path"])
                    save(dest / Path(clip["path"]).name, content)
        for character, entries in plans.items():
            save(dest / (character + ".json"), json.dumps({"clips": entries}, indent=2).encode())


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("characters", nargs="+")
    parser.add_argument("--archive", type=Path)
    args = parser.parse_args()
    install(args.characters, args.archive)
