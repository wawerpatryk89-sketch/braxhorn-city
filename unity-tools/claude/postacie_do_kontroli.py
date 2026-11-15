"""Restore only the models, textures and shared clips needed for the Editor review."""
import argparse
import json
import os
from pathlib import Path
from tempfile import TemporaryDirectory
from zipfile import ZipFile
from hashlib import sha256

from pobierz_obsade import pobierz
from prywatne import CEL, ROOT
from ruchy_t import install, save


def prepare(selected=None):
    actors = []
    for path in sorted((ROOT / "Postacie").rglob("karta.json")):
        card = json.loads(path.read_text())
        cid = card["id"]
        if selected and cid not in selected:
            continue
        actors.append(cid)
        packaged = card["pliki"]["unity"]
        descriptor = {"release_id": card["wydanie"]["id"], "tag": card["wydanie"]["tag"],
                      "name": packaged["nazwa"], "sha256": packaged["sha256"]}
        with TemporaryDirectory() as tmp:
            archive = Path(tmp) / "character.zip"
            pobierz(descriptor, os.environ["GITHUB_REPOSITORY"], archive)
            if sha256(archive.read_bytes()).hexdigest() != descriptor["sha256"]:
                raise ValueError(cid)
            with ZipFile(archive) as z:
                prefix = f"unity-city/Assets/Source/Prywatne/Postacie/{cid}/"
                if "postac.json" in z.namelist():
                    prefix = ""
                manifest = z.read(prefix + "postac.json")
                if sha256(manifest).hexdigest() != card["manifest_sha256"]:
                    raise ValueError(cid + " manifest")
                data = json.loads(manifest)
                for name, digest in data["files"].items():
                    if "/" in name or Path(name).suffix.lower() not in (".fbx", ".png"):
                        continue
                    content = z.read(prefix + name)
                    if sha256(content).hexdigest() != digest:
                        raise ValueError(cid + "/" + name)
                    target = name
                    if not prefix:
                        target = {"Color.png": cid + "_BaseColor.png", "Normal.png": cid + "_Normal.png",
                                  "MetalSmooth.png": cid + "_MetallicSmoothness.png"}.get(name, name)
                    save(CEL / "Postacie" / cid / target, content)
    if selected and set(actors) != set(selected):
        raise ValueError("Nieznany identyfikator postaci")
    install(actors)
    from gym import przygotuj_gym
    przygotuj_gym(actors)


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("postacie", nargs="*")
    prepare(parser.parse_args().postacie)
