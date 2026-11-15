"""Przygotuj postacie z prywatnego szkicu wydania przed importem Unity."""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
from zipfile import ZipFile

from postacie_rejonu import zainstaluj
from prywatne import ROOT
from rejon_buildu import wybierz_rejon


def pobierz(plik, repo, cel):
    api = f"repos/{repo}/releases"
    release = json.loads(subprocess.check_output(["gh", "api", f"{api}/{plik['release_id']}"]))
    if release["draft"] is not True or release["tag_name"] != plik["tag"]:
        raise ValueError("Oczekiwano prywatnego szkicu wydania " + plik["tag"])
    matches = [a for a in release["assets"] if a["name"] == plik["name"] and a["state"] == "uploaded"]
    if len(matches) != 1:
        raise FileNotFoundError(f"Brak {plik['name']} w szkicu {plik['tag']}")
    asset = matches[0]
    if asset.get("digest") and asset["digest"] != "sha256:" + plik["sha256"]:
        raise ValueError("Suma archiwum w szkicu różni się od zatwierdzonej")
    with cel.open("wb") as out:
        subprocess.run(["gh", "api", f"{api}/assets/{asset['id']}", "-H", "Accept: application/octet-stream"],
                       stdout=out, check=True)


PACZKA = {
    "release_id": 443180649,
    "tag": "postacie-globalne-20261112",
    "name": "Osiedle_19_postaci_20261115.zip",
    "sha256": "94a34ab8d0635bd92dfec7e518185a2fda98f376ba4c9e74a6087fb0deaabd97",
}


def odczytaj(archive, target):
    with archive.open("rb") as source:
        if hashlib.file_digest(source, "sha256").hexdigest() != PACZKA["sha256"]:
            raise ValueError("Niepoprawna suma paczki Osiedla")
    with ZipFile(archive) as z:
        models = {Path(n).parent.name for n in z.namelist() if n.endswith(".fbx")}
        expected = {f"{k}/{file}" for k in models for file in (k + ".fbx", "Color.png", "Normal.png", "MetalSmooth.png")}
        files = [n for n in z.namelist() if not n.endswith("/")]
        if len(models) != 19 or len(files) != len(expected) or set(files) != expected:
            raise ValueError("Niepełna obsada lub brak map materiałów")
        for n in files:
            out = (target / n).resolve()
            if target.resolve() not in out.parents:
                raise ValueError("Ścieżka poza katalogiem obsady")
            out.parent.mkdir(parents=True, exist_ok=True)
            with z.open(n) as src, out.open("xb") as dst:
                shutil.copyfileobj(src, dst)


def przygotuj():
    if wybierz_rejon(ROOT / "unity-city")[0] not in ("osiedle", "oba"):
        return
    target = ROOT / "unity-city/Assets/Source/ObsadaOsiedla"
    target.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(dir=target.parent) as tmp:
        tmp = Path(tmp)
        archive = tmp / "obsada.zip"
        pobierz(PACZKA, os.environ["GITHUB_REPOSITORY"], archive)
        staged = tmp / "postacie"
        staged.mkdir()
        odczytaj(archive, staged)
        zainstaluj(staged, target)
    print("BRAXHORN_OBSADA: 19 modeli, 57 map materiałów, SHA-256 zgodna")


if __name__ == "__main__":
    przygotuj()
