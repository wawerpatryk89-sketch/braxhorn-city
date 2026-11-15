"""Install shared role clips from the owner's T archive before Unity imports the cast."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import tempfile
from paczki_ruchow_t import open_clips

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
        model_takes = set(data.get("model_take_states", {}).get(character, []))
        entries = []
        for state, key in profile.items():
            if state in model_takes:
                entries.append({"state": state, "modelTake": True})
                continue
            clip = data["clips"][key]
            needed[key] = clip
            entries.append({"state": state, "file": Path(clip["path"]).name})
        plans[character] = entries
    dest = root / "MixamoT"
    with open_clips(data, archive) as read:
        for key, clip in needed.items():
            output = dest / Path(clip["path"]).name
            if not output.is_file() or hashlib.sha256(output.read_bytes()).hexdigest() != clip["sha256"]:
                save(output, read(clip))
        for character, entries in plans.items():
            profile = {"clips": entries}
            if character in data.get("android_texture_size", {}):
                profile["androidTextureSize"] = data["android_texture_size"][character]
            save(dest / (character + ".json"), json.dumps(profile, indent=2).encode())


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("characters", nargs="+")
    parser.add_argument("--archive", type=Path)
    args = parser.parse_args()
    install(args.characters, args.archive)
