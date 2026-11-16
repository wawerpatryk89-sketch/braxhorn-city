"""Przygotuj wskazane postacie do pokoju prób — ta sama droga co w buildzie rejonu.

    BRAX_POSTACIE="patryk,ewa,kiler" python3 unity-tools/claude/pokoj-prob/przygotuj.py

Pobiera zatwierdzoną paczkę obsady z prywatnego szkicu wydania, przepuszcza każdą
wskazaną postać przez Blendera (osiedle_ruch.py — chód, pętla spoczynku, twarz,
mimika) i instaluje do Assets/Source/ObsadaOsiedla/<id>/.
Przygotowuje też ścieżki mowy, żeby w próbie było słychać kwestię i ruszały się usta.
"""
import os
import sys
import tempfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

import pobierz_obsade as obsada  # noqa: E402
from osiedle_mowa import prepare  # noqa: E402

KWESTIA = "9a79ffe04cb33bdb_m.ogg"

# Patryk, gitara i barman biorą klip z paczki baru (BraxClaudeOsiedleObsada.DodajKlipBaru).
# Nazwa i suma MUSZĄ być zgodne z pobierz_obsade.dodatki_osiedla — gdy się tam zmienią, zmień tu.
PACZKA_BARU = ("Osiedle_Klipy_Baru.zip",
               "454c273ca563df8de94b25291a39bb9a236a3a5aa1089fdfcd326609f04117c2")
PLIKI_BARU = ["Assets/Source/Prywatne/BarOsiedla/" + n
              for n in ("Barman.fbx", "Chwyt.json", "gitara.fbx", "patryk.fbx")]
POTRZEBUJA_BARU = {"patryk", "gitara", "bramkarz_1c96d333"}


def paczka_baru(praca):
    """Rozpakuj klipy baru do Assets/Source/Prywatne/BarOsiedla (bez tego ModelObsady rzuca)."""
    import hashlib
    import shutil
    from zipfile import ZipFile

    nazwa, suma = PACZKA_BARU
    cel = obsada.ROOT / "unity-city/Assets/Source/Prywatne/BarOsiedla"
    archiwum = praca / nazwa
    obsada.pobierz(dict(release_id=obsada.PACZKA["release_id"], tag=obsada.PACZKA["tag"],
                        name=nazwa, sha256=suma),
                   os.environ["GITHUB_REPOSITORY"], archiwum)
    with archiwum.open("rb") as s:
        if hashlib.file_digest(s, "sha256").hexdigest() != suma:
            raise SystemExit("Uszkodzona paczka baru")
    cel.mkdir(parents=True, exist_ok=True)
    with ZipFile(archiwum) as z:
        if sorted(z.namelist()) != sorted(PLIKI_BARU):
            raise SystemExit("Niepoprawny skład paczki baru")
        for klucz in PLIKI_BARU:
            with z.open(klucz) as src, (cel / Path(klucz).name).open("wb") as dst:
                shutil.copyfileobj(src, dst)
    print("BRAX_POKOJ paczka baru gotowa", flush=True)


def wybrane():
    v = os.environ.get("BRAX_POSTACIE", "patryk")
    out = []
    for czesc in v.replace(";", ",").replace(" ", ",").split(","):
        czesc = czesc.strip()
        if czesc and czesc not in out:
            out.append(czesc)
    return out


def main():
    ids = wybrane()
    cel = obsada.ROOT / "unity-city/Assets/Source/ObsadaOsiedla"
    cel.mkdir(parents=True, exist_ok=True)
    blender = Path(os.environ["RUNNER_TEMP"]) / "blender-4.2.3-linux-x64/blender"

    with tempfile.TemporaryDirectory(dir=cel.parent) as temp:
        praca = Path(temp)
        archiwum = praca / "obsada.zip"
        obsada.pobierz(obsada.PACZKA, os.environ["GITHUB_REPOSITORY"], archiwum)
        staged = praca / "postacie"
        staged.mkdir()
        obsada.odczytaj(archiwum, staged)

        if POTRZEBUJA_BARU.intersection(ids):
            paczka_baru(praca)

        dostepne = sorted(p.name for p in staged.iterdir() if p.is_dir())
        brak = [i for i in ids if i not in dostepne]
        if brak:
            raise SystemExit(
                "Nie ma w paczce: " + ", ".join(brak) + "\nDostępne: " + ", ".join(dostepne))

        # Patryk pożycza ujęcie z nieprzerobionego FBX Ewy — musi iść pierwszy, tak jak w rejonie.
        kolejnosc = sorted(ids, key=lambda i: i != "patryk")
        gotowe = praca / "poprawione"
        gotowe.mkdir()
        for i in kolejnosc:
            model = staged / i / (i + ".fbx")
            print("BRAX_POKOJ przygotowuję", i, flush=True)
            obsada.popraw_model(model, gotowe / model.name, blender)

        for i in ids:
            docelowy = cel / i
            if docelowy.exists():
                import shutil
                shutil.rmtree(docelowy)
            (staged / i).rename(docelowy)
            print("BRAX_POKOJ gotowe", i, "plików:",
                  len(list(docelowy.iterdir())), flush=True)

    zasoby = obsada.ROOT / "unity-city/Assets/Resources"
    prepare(zasoby / "OsiedleGlosy", zasoby / "OsiedleMimika", KWESTIA)
    print("BRAX_POKOJ postacie:", ", ".join(ids), flush=True)


if __name__ == "__main__":
    main()
