"""Przygotuj postacie z prywatnego szkicu wydania przed importem Unity."""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
from zipfile import ZipFile

from postacie_rejonu import REGIONS, valid, zainstaluj
from prywatne import CEL, PACZKI, ROOT
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


def odczytaj(zip_path, selection, target):
    with zip_path.open("rb") as source:
        if hashlib.file_digest(source, "sha256").hexdigest() != selection["draft_asset"]["sha256"]:
            raise ValueError("Niepoprawna suma ZIP-a " + selection["id"])
    prefix = f"unity-city/Assets/Source/Prywatne/Postacie/{selection['id']}/"
    with ZipFile(zip_path) as z:
        for item in z.infolist():
            if not item.filename.startswith(prefix):
                raise ValueError("Plik spoza katalogu postaci")
            out = target / item.filename[len(prefix):]
            if item.is_dir() or target.resolve() not in out.resolve().parents:
                raise ValueError("Niepoprawna ścieżka w archiwum postaci")
            out.parent.mkdir(parents=True, exist_ok=True)
            with z.open(item) as src, out.open("xb") as dst:
                shutil.copyfileobj(src, dst)
    if not valid(target, selection):
        raise ValueError("Model lub klipy nie odpowiadają obsadzie rejonu")


def przygotuj():
    region = wybierz_rejon(ROOT / "unity-city")[0]
    if region not in ("osiedle", "oba"):
        return
    for selection in json.loads((REGIONS / "osiedle.json").read_text())["characters"]:
        target = CEL / "Postacie" / selection["id"]
        if valid(target, selection) or (PACZKI / (selection["package"] + ".tar.gz.enc")).is_file():
            continue
        target.parent.mkdir(parents=True, exist_ok=True)
        with tempfile.TemporaryDirectory(dir=target.parent) as tmp:
            tmp = Path(tmp)
            archive = tmp / "source.zip"
            pobierz(selection["draft_asset"], os.environ["GITHUB_REPOSITORY"], archive)
            staged = tmp / "postac"
            staged.mkdir()
            odczytaj(archive, selection, staged)
            zainstaluj(staged, target)
            print(f"Sprawdzono model i animacje: {selection['id']} {selection['version']}")


if __name__ == "__main__":
    przygotuj()
