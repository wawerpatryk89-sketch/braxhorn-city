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

import obsada_surowa  # noqa: E402
import pobierz_obsade as obsada  # noqa: E402
from osiedle_mowa import prepare  # noqa: E402

KWESTIA = "9a79ffe04cb33bdb_m.ogg"
ZLECENIE = "unity-city/pokoj-prob.txt"
# Wlasciciel 16.11, 12:25: „Max 5 nie wiecej”. Pokoj bierze NAJWYZEJ piec postaci na jedna probe —
# dzieki temu APK jest lekkie, wszystkie piec widac w kadrze i proba konczy sie w rozsadnym czasie.
MAX_POSTACI = 5

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


WSZYSTKIE = {"wszystkie", "wszyscy", "all", "*"}
# Wlasciciel 16.11, 21:30: „nie dales Moniki. Monika jest jedna. Ta w sukience takiej dlugiej.
# Z Patrykiem, co prowadzi.” Para Patryk i Monika prowadzi gre (zasada PATRYK-I-MONIKA),
# wiec oboje sa w staliej piatce. Dziecko ustapilo miejsca — wroci w nastepnej piatce.
PIERWSZA_PIATKA = ["patryk", "monika", "kapitan_02d85d24",
                   "imprezowiczka_0dbe3922", "bramkarz_1c96d333"]


def wybrane():
    v = os.environ.get("BRAX_POSTACIE", "").strip()
    if not v or v.lower() in WSZYSTKIE:
        return list(PIERWSZA_PIATKA)
    out = []
    for czesc in v.replace(";", ",").replace(" ", ",").split(","):
        czesc = czesc.strip()
        if czesc and czesc not in out:
            out.append(czesc)
    if not out:
        return list(PIERWSZA_PIATKA)
    if len(out) > MAX_POSTACI:
        print("BRAX_POKOJ limit", MAX_POSTACI, "— biore pierwsze:", ", ".join(out[:MAX_POSTACI]),
              "| odlozone:", ", ".join(out[MAX_POSTACI:]), flush=True)
        out = out[:MAX_POSTACI]
    return out


def zapisz_zlecenie(ids):
    """Zlecenie dla Unity. unity-builder nie przepuszcza wlasnych zmiennych do kontenera,
    a lista musi byc ROZWIAZANA (slowo „wszystkie” Unity nic nie mowi)."""
    plik = obsada.ROOT / ZLECENIE
    plik.write_text(",".join(ids) + "\n" + os.environ.get("BRAX_TRYB", "apk") + "\n",
                    encoding="utf-8")
    print("BRAX_POKOJ zlecenie:", plik.read_text(encoding="utf-8").replace("\n", " | "),
          flush=True)


def gotowa(katalog, ident):
    """Postać przerobiona we wcześniejszym przebiegu (pamięć podręczna) — nie liczymy jej od nowa."""
    return all((katalog / n).is_file() for n in
               (ident + ".fbx", "Color.png", "Normal.png", "MetalSmooth.png", "Mimika.png"))


def rownolegle(zadania, blender, workers):
    """Blender na kazdej postaci osobno — niezalezne procesy, wiec ida obok siebie.

    Wlasciciel 16.11, 09:03: „mamy bardzo duzo postaci, wiec musimy miec do tego
    srodowisko odpowiednie na szybkie testy”. Kazde zadanie ma WLASNY katalog wyjsciowy,
    bo popraw_model odklada Mimika.png obok wyniku — przy wspolnym katalogu dwa procesy
    nadpisalyby sobie twarze.
    """
    from concurrent.futures import ThreadPoolExecutor

    bledy = []

    def jeden(i, model, wyjscie):
        wyjscie.mkdir(parents=True, exist_ok=True)
        try:
            obsada.popraw_model(model, wyjscie / model.name, blender)
            print("BRAX_POKOJ przerobiona", i, flush=True)
            return None
        except Exception as e:  # noqa: BLE001 — jedna zla postac nie moze zabic calej proby
            print("BRAX_POKOJ BLAD", i, "-", e, flush=True)
            return (i, str(e))

    with ThreadPoolExecutor(max_workers=workers) as pula:
        for wynik in pula.map(lambda z: jeden(*z), zadania):
            if wynik:
                bledy.append(wynik)
    return bledy


# POSTACIE TESTOWE Z RUCHAMI ROKOKO (wlasciciel 23.11, 01:54: „dodasz kazdej postaci po pare
# ruchow z Rokoko i wpuscisz na pokoj. I zobaczymy, co z tego wyjdzie.”).
# Leza gotowe w repozytorium: ruch przeniesiony narzedziem rokoko/rk_przenies.py, potem zwykla
# droga osiedle_ruch (mimika, petla spoczynku) — i OBEJRZANE na zdjeciach przed wyslaniem.
# Maja wlasne identyfikatory z koncowka _rk, wiec nie nadpisuja zatwierdzonych postaci
# ani ich pamieci podrecznej. Wystarczy podac je w polu „postacie”.
PACZKA_TESTOWA = Path(__file__).resolve().parents[1] / "rokoko" / "paczka-testowa"


def postacie_testowe(ids, cel):
    import shutil

    for i in ids:
        zrodlo = PACZKA_TESTOWA / i
        if not zrodlo.is_dir():
            continue
        docelowy = cel / i
        if docelowy.exists():
            shutil.rmtree(docelowy)
        shutil.copytree(zrodlo, docelowy)
        print("BRAX_POKOJ postac testowa Rokoko:", i, flush=True)


def main():
    ids = wybrane()
    cel = obsada.ROOT / "unity-city/Assets/Source/ObsadaOsiedla"
    cel.mkdir(parents=True, exist_ok=True)
    postacie_testowe(ids, cel)

    z_pamieci = [i for i in ids if gotowa(cel / i, i)]
    ids = [i for i in ids if i not in z_pamieci]
    if z_pamieci:
        print("BRAX_POKOJ z pamieci podrecznej:", ", ".join(z_pamieci), flush=True)
    if not ids:
        print("BRAX_POKOJ cala piatka gotowa — Blender pominiety", flush=True)
        zasoby = obsada.ROOT / "unity-city/Assets/Resources"
        # WSZYSTKIE nagrania, nie jedno. Trzeci argument prepare to WZORZEC, nie nazwa pliku —
        # przy KWESTIA sciezka mimiki powstawala tylko dla jednego nagrania, a pokaz puszcza
        # ich kilka pod rzad, wiec reszta szla bez sciezki i usta ledwo chodzily
        # (wlasciciel 16.11, 21:06: „nie widac jak ustami ruszaja w ogole”).
        prepare(zasoby / "OsiedleGlosy", zasoby / "OsiedleMimika", "*.ogg")
        zapisz_zlecenie(z_pamieci)
        print("BRAX_POKOJ postacie:", ",".join(z_pamieci), flush=True)
        return

    blender = Path(os.environ["RUNNER_TEMP"]) / "blender-4.2.3-linux-x64/blender"
    workers = max(1, min(MAX_POSTACI, len(os.sched_getaffinity(0))))
    bledy = []
    wstawione = []

    with tempfile.TemporaryDirectory(dir=cel.parent) as temp:
        praca = Path(temp)
        archiwum = praca / "obsada.zip"
        obsada.pobierz(obsada.PACZKA, os.environ["GITHUB_REPOSITORY"], archiwum)
        staged = praca / "postacie"
        staged.mkdir()
        obsada.odczytaj(archiwum, staged)

        # SUROWE MODELE Z BIBLIOTEKI (Brax, Nyxa). Nie ma ich w zatwierdzonej paczce,
        # bo nikt ich jeszcze nie przerabiał — składam paczkę w locie z surowej
        # konwersji Tripo i globalnej biblioteki ruchów, a dalej idą normalną drogą.
        # WYŁĄCZONE. Właściciel 18.11, 15:16: „weź ty nie rób żadnych animacji".
        # Składanie postaci z surowego modelu wymaga dobrania jej ruchów, a tego już
        # nie robię — dwa razy wysłałem zdeformowaną postać i dwa razy nie zauważyłem.
        # Kod zostaje w repozytorium, nic nie jest kasowane, ale sam z siebie nie ruszy.
        surowi = [x for x in ids if x in obsada_surowa.SUROWI]
        if surowi and os.environ.get("BRAX_SKLADAJ_SUROWE") != "1":
            raise SystemExit(
                "Postacie %s wymagaja dobrania ruchow — wylaczone na polecenie wlasciciela "
                "(18.11). Wlasciciel dostarcza model z wlasnymi animacjami." % ", ".join(surowi))
        for i in surowi:
            obsada_surowa.zloz(i, os.environ["GITHUB_REPOSITORY"], praca, staged, blender)

        dostepne = sorted(p.name for p in staged.iterdir() if p.is_dir())
        brak = [i for i in ids if i not in dostepne]
        if brak:
            raise SystemExit(
                "Nie ma w paczce: " + ", ".join(brak) + "\nDostepne: " + ", ".join(dostepne))

        if POTRZEBUJA_BARU.intersection(ids) or POTRZEBUJA_BARU.intersection(z_pamieci):
            paczka_baru(praca)

        # Patryk pozycza ujecie z nieprzerobionego FBX Ewy — idzie pierwszy, tak jak w rejonie.
        kolejnosc = sorted(ids, key=lambda i: i != "patryk")
        gotowe = praca / "poprawione"
        gotowe.mkdir()
        print("BRAX_POKOJ Blender:", len(kolejnosc), "postaci na", workers, "watkach", flush=True)
        zadania = [(i, staged / i / (i + ".fbx"), gotowe / i) for i in kolejnosc]
        bledy = rownolegle(zadania, blender, workers)
        zle = {i for i, _ in bledy}

        for i in ids:
            if i in zle:
                continue
            docelowy = cel / i
            if docelowy.exists():
                import shutil
                shutil.rmtree(docelowy)
            (staged / i).rename(docelowy)
            wstawione.append(i)

    razem = [i for i in wybrane() if i in set(z_pamieci) | set(wstawione)]
    if not razem:
        raise SystemExit("Zadna postac sie nie przygotowala")
    zasoby = obsada.ROOT / "unity-city/Assets/Resources"
    prepare(zasoby / "OsiedleGlosy", zasoby / "OsiedleMimika", "*.ogg")
    zapisz_zlecenie(razem)
    if bledy:
        print("BRAX_POKOJ NIEUDANE:", "; ".join(f"{i}: {e}" for i, e in bledy), flush=True)
    print("BRAX_POKOJ postacie:", ",".join(razem), flush=True)


if __name__ == "__main__":
    main()
