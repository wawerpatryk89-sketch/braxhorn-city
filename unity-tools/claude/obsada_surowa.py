"""Surowe modele z biblioteki -> paczka w kształcie, jakiego oczekuje potok obsady.

Właściciel 18.11, 11:27: „Zrób teraz test pokoju z Braxem z Nyxą."

Brax i Nyxa leżą w bibliotece globalnej jako surowe konwersje z Tripo. Mają pełny
szkielet (65 kości Mixamo, nazwy zgodne CO DO JEDNEJ z resztą obsady — sprawdzone),
ale zero ujęć ruchu, a mapy materiału są w osobnych plikach zamiast w zestawie.

Ten moduł robi z nich paczkę taką samą jak te 46 przerobionych, więc dalej idą
normalną drogą: osiedle_ruch, wygładzanie wag, mimika, import do Unity.

Wszystko bierze się z wydań, które już są — nic nie trzeba nigdzie wgrywać.
"""
import hashlib
import json
import os
import shutil
import subprocess
import tempfile
from pathlib import Path
from zipfile import ZipFile

BIBLIOTEKA_RUCHOW = {
    "asset_id": 719031450,
    "name": "animacje.pozycja.A.zip",
    "sha256": "bea2c8fdd7f2fec7241593f1cc923eaeef3041c63caa97d6afd04934f69cb31e",
}

# PRZESUNIĘCIE RZUTU TWARZY. Rzut mimiki jest wyśrodkowany pod ludzkie głowy
# (0,48 rozpiętości od czubka siatki). Rogi i włosy spychają twarz niżej — u Nyxy
# usta wypadały CAŁKIEM POZA KADR, u Braxa stały na dolnej krawędzi. Liczby niżej
# zostały odczytane z rzutu, nie zgadnięte: przy nich cała twarz mieści się w kadrze.
SUROWI = {
    "brax": {"asset_id": 740160034, "name": "brax.zip", "przesuniecie": 0.14,
             "sha256": "04b00ca8911ff78a55d5aeffd90e5b9e9457641539949761b142a4f27324c25d"},
    "nyxa": {"asset_id": 740148294, "name": "nyxa1.zip", "przesuniecie": 0.30,
             "sha256": "0e38be74b194a629f8e8ae34ce83b5201c5828b5d20b4808f3b284cbb0ff0f5c"},
}


def pobierz_asset(asset_id, repo, cel, suma):
    with cel.open("wb") as out:
        subprocess.run(["gh", "api", f"repos/{repo}/releases/assets/{asset_id}",
                        "-H", "Accept: application/octet-stream"], stdout=out, check=True)
    with cel.open("rb") as s:
        mam = hashlib.file_digest(s, "sha256").hexdigest()
    if mam != suma:
        raise SystemExit(f"{cel.name}: suma {mam} zamiast {suma}")


def biblioteka_ruchow(repo, praca):
    """Rozpakuj globalną bibliotekę ruchów (pozycja A) — raz na przebieg."""
    cel = praca / "ruchy"
    if cel.is_dir():
        return cel
    arch = praca / BIBLIOTEKA_RUCHOW["name"]
    pobierz_asset(BIBLIOTEKA_RUCHOW["asset_id"], repo, arch, BIBLIOTEKA_RUCHOW["sha256"])
    cel.mkdir()
    with ZipFile(arch) as z:
        for n in z.namelist():
            if n.lower().endswith(".fbx"):
                (cel / Path(n).name).write_bytes(z.read(n))
    print("BRAX_SUROWA biblioteka ruchow:", len(list(cel.glob('*.fbx'))), "ujec", flush=True)
    return cel


def metalsmooth(fbm, cel):
    """R = metaliczność, A = gładkość (1 − szorstkość) — tak czyta shader obsady."""
    from PIL import Image, ImageChops

    pliki = list(Path(fbm).iterdir())

    def znajdz(*klucze):
        for p in pliki:
            if any(k in p.name.lower() for k in klucze):
                return p
        return None

    szor = znajdz("roughness", "rough")
    if not szor:
        raise SystemExit("brak mapy szorstkości w " + str(fbm))
    met = znajdz("metallic", "metalness")
    s = Image.open(szor).convert("L")
    m = Image.open(met).convert("L").resize(s.size) if met else Image.new("L", s.size, 0)
    puste = Image.new("L", s.size, 0)
    Image.merge("RGBA", (m, puste, puste, ImageChops.invert(s))).save(cel)


def zloz(ident, repo, praca, staged, blender):
    """Pobierz surowy model, doklej ujęcia, złóż mapy. Zwraca katalog gotowej paczki."""
    opis = SUROWI[ident]
    arch = praca / opis["name"]
    pobierz_asset(opis["asset_id"], repo, arch, opis["sha256"])
    rozpak = praca / ("surowy_" + ident)
    rozpak.mkdir(parents=True, exist_ok=True)
    with ZipFile(arch) as z:
        z.extractall(rozpak)
    model = next(rozpak.rglob("*.fbx"))
    ruchy = biblioteka_ruchow(repo, praca)

    cel = staged / ident
    cel.mkdir(parents=True, exist_ok=True)
    skrypt = Path(__file__).with_name("zrob_paczke_postaci.py")
    subprocess.run([str(blender), "-b", "-P", str(skrypt), "--",
                    str(model), str(ruchy), str(cel), ident], check=True)

    fbm = next((p for p in model.parent.glob("*.fbm") if p.is_dir()), None)
    if fbm is None:
        raise SystemExit("brak katalogu .fbm obok " + str(model))
    for wzor, nazwa in (("basecolor", "Color.png"), ("color", "Color.png"),
                        ("normal", "Normal.png")):
        if (cel / nazwa).exists():
            continue
        zrod = next((p for p in fbm.iterdir() if wzor in p.name.lower()), None)
        if zrod:
            shutil.copy(zrod, cel / nazwa)
    metalsmooth(fbm, cel / "MetalSmooth.png")
    print("BRAX_SUROWA %s gotowa: %s" % (ident, ", ".join(sorted(p.name for p in cel.iterdir()))),
          flush=True)
    return cel
