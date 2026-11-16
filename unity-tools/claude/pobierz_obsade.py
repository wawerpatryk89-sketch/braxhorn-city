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
    if plik == PACZKA and any(a["name"] == GOTOWA_PACZKA["name"] and a["state"] == "uploaded"
                              for a in release["assets"]):
        plik = GOTOWA_PACZKA
    matches = [a for a in release["assets"] if a["name"] == plik["name"] and a["state"] == "uploaded"]
    if len(matches) != 1:
        raise FileNotFoundError(f"Brak {plik['name']} w szkicu {plik['tag']}")
    asset = matches[0]
    if asset.get("digest") and asset["digest"] != "sha256:" + plik["sha256"]:
        raise ValueError("Suma archiwum w szkicu różni się od zatwierdzonej")
    with cel.open("wb") as out:
        subprocess.run(["gh", "api", f"{api}/assets/{asset['id']}", "-H", "Accept: application/octet-stream"],
                       stdout=out, check=True)
    return plik


PACZKA = {
    "release_id": 443180649,
    "tag": "postacie-globalne-20261112",
    "name": "Osiedle_19_postaci_20261115.zip",
    "sha256": "94a34ab8d0635bd92dfec7e518185a2fda98f376ba4c9e74a6087fb0deaabd97",
}


GOTOWA_PACZKA = dict(PACZKA, name="BRAXHORN_Osiedle_19_postaci.zip",
                     sha256="408be30891fed4e37ee20ec9af125326f422dfc71845f5470998859a830f4560")


def odczytaj_gotowa(archive, postacie, bar):
    with archive.open("rb") as stream:
        if hashlib.file_digest(stream, "sha256").hexdigest() != GOTOWA_PACZKA["sha256"]:
            raise ValueError("Niepoprawna suma przygotowanej obsady Osiedla")
    with ZipFile(archive) as pack:
        roster = json.loads(pack.read("Obsada.json"))
        if len(roster) != 19 or len({r["id"] for r in roster}) != 19:
            raise ValueError("Niepełna obsada Osiedla")
        for row in roster:
            actor = row["id"]
            if not actor or Path(actor).name != actor or actor in (".", ".."):
                raise ValueError("Niepoprawny identyfikator postaci")
            base = "Assets/Source/ObsadaOsiedla/" + actor + "/"
            profile = json.loads(pack.read(base + "postac.json"))
            if row["model"] != base + actor + ".fbx" or not {"Drink", "Eat"} <= set(profile["states"]):
                raise ValueError("Brak modelu lub klipów postaci " + actor)
            dest = postacie / actor
            dest.mkdir(parents=True)
            names = (actor + ".fbx", "Color.png", "Normal.png", "MetalSmooth.png", "Mimika.png",
                     "ChwytNapoju.json", "ChwytJedzenia.json", "postac.json")
            for name in names:
                with pack.open(base + name) as src, (dest / name).open("xb") as dst:
                    shutil.copyfileobj(src, dst)
                if name != "postac.json":
                    with (dest / name).open("rb") as saved:
                        if hashlib.file_digest(saved, "sha256").hexdigest() != profile["files"].get(name):
                            raise ValueError(f"Niezgodny plik obsady: {actor}/{name}")
        bar.mkdir(parents=True)
        for name in ("Barman.fbx", "patryk.fbx", "gitara.fbx", "Chwyt.json", "ButelkaPrzyBlacie.json"):
            with pack.open("Assets/Source/Prywatne/BarOsiedla/" + name) as src, (bar / name).open("xb") as dst:
                shutil.copyfileobj(src, dst)


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


def popraw_model(model, result, blender):
    script = Path(__file__).with_name("osiedle_ruch.py").resolve()
    subprocess.run([str(blender), "--background", "--factory-startup", "--disable-autoexec",
                    "--python-exit-code", "1", "--python", str(script), "--",
                    str(model.resolve()), str(result.resolve())], check=True)
    if not result.is_file() or result.stat().st_size == 0:
        raise ValueError("Brak poprawionego modelu " + model.stem)
    (result.parent / "Mimika.png").replace(model.parent / "Mimika.png")
    result.replace(model)


def popraw_modele(staged, work, blender):
    work.mkdir()
    models = sorted(staged.glob("*/*.fbx"))
    if len(models) != 19:
        raise ValueError("Oczekiwano 19 modeli do obróbki")
    # Patryk borrows the forward take from the unmodified source FBX of Ewa.
    models.sort(key=lambda model: model.stem != "patryk")
    for model in models:
        popraw_model(model, work / model.name, blender)


def dodatki_osiedla(parent, blender, *, z_flota=True, gotowy_bar=None):
    base = {"release_id": PACZKA["release_id"], "tag": PACZKA["tag"]}
    vehicle_names = ("Policja_Kombi", "Policja_Kombi", "SPKP", "Ambulans", "Taxi", "Autobus", "GT6_Coupe",
                     "S2_Spider", "Roadster", "Roadster_Metalik", "L8", "Shooting_Brake", "Coupe", "Graphite_Coupe")
    vehicles = [("R5_" if i == 3 else "") + f"Braxhorn_{name}_BX{10000+i}.glb"
                for i, name in enumerate(vehicle_names, 1)]
    bar = ["Assets/Source/Prywatne/BarOsiedla/" + n for n in ("Barman.fbx", "Chwyt.json", "gitara.fbx", "patryk.fbx")]
    packages = [
        ("Osiedle_Klipy_Baru.zip", "454c273ca563df8de94b25291a39bb9a236a3a5aa1089fdfcd326609f04117c2", bar, "BarOsiedla")]
    if z_flota:
        packages.append(("Osiedle_14_pojazdow_z_tablicami.zip", "70c78d9bfffd4f2617be29504993edb9dd7b83a5b8570f1e2fa264392f4aae65", vehicles, "FlotaOsiedla"))
    parent.mkdir(parents=True, exist_ok=True)
    if gotowy_bar is not None:
        zainstaluj(gotowy_bar, parent / "BarOsiedla")
        packages = [p for p in packages if p[3] != "BarOsiedla"]
    for name, digest, names, directory in packages:
        with tempfile.TemporaryDirectory(dir=parent) as temporary:
            tmp = Path(temporary)
            archive = tmp / name
            pobierz(dict(base, name=name, sha256=digest), os.environ["GITHUB_REPOSITORY"], archive)
            with archive.open("rb") as stream:
                if hashlib.file_digest(stream, "sha256").hexdigest() != digest:
                    raise ValueError("Uszkodzone archiwum " + name)
            staged = tmp / "source"
            staged.mkdir()
            with ZipFile(archive) as pack:
                if sorted(pack.namelist()) != sorted(names):
                    raise ValueError("Niepoprawny skład " + name)
                for key in names:
                    with pack.open(key) as src, (staged / Path(key).name).open("xb") as dst:
                        shutil.copyfileobj(src, dst)
            if directory == "FlotaOsiedla":
                prepared = tmp / "rig"
                subprocess.run([str(blender), "--background", "--factory-startup", "--disable-autoexec",
                                "--python-exit-code", "1", "--python", str(Path(__file__).with_name("osiedle_flota.py")),
                                "--", str(staged), str(prepared)], check=True)
                staged = prepared
            zainstaluj(staged, parent / directory)


def przygotuj():
    if wybierz_rejon(ROOT / "unity-city")[0] not in ("osiedle", "oba"):
        return
    target = ROOT / "unity-city/Assets/Source/ObsadaOsiedla"
    target.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(dir=target.parent) as tmp:
        tmp = Path(tmp)
        archive = tmp / "obsada.zip"
        wybrana = pobierz(PACZKA, os.environ["GITHUB_REPOSITORY"], archive)
        staged = tmp / "postacie"
        staged.mkdir()
        blender = Path(os.environ["RUNNER_TEMP"]) / "blender-4.2.3-linux-x64/blender"
        if wybrana == GOTOWA_PACZKA:
            bar = tmp / "bar" / "source"
            odczytaj_gotowa(archive, staged, bar)
            dodatki_osiedla(target.parent / "Prywatne", blender, gotowy_bar=bar)
        else:
            odczytaj(archive, staged)
            dodatki_osiedla(target.parent / "Prywatne", blender)
            popraw_modele(staged, tmp / "poprawione", blender)
        zainstaluj(staged, target)
    from osiedle_mowa import prepare
    resources = ROOT / "unity-city/Assets/Resources"
    prepare(resources / "OsiedleGlosy", resources / "OsiedleMimika")
    print("BRAXHORN_OBSADA: 19 modeli, 57 map materiałów, SHA-256 zgodna")


if __name__ == "__main__":
    przygotuj()
