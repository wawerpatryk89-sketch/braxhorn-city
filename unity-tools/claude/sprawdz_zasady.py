"""Sprawdzacz ZASAD — przejeżdża gałąź i wypisuje każde odstępstwo.

Właściciel 18.11, 10:01: „Ty miałeś ustalić konkretne zasady, które będą wszyscy
stosować. A z jakiej racji jedno stosuje co innego, a drugie co innego?"

Miał rację i powód jest prosty: zasady były DOKUMENTAMI. Jeden z nich (ZASADA-0
z 16.11) powoływał się nawet na sprawdzacz `sprawdz_zasade_0.py` jako „jest, działa" —
a tego pliku nigdy nie było w repozytorium. Dokument nie egzekwuje niczego.

Ten skrypt sprawdza tylko to, co da się sprawdzić JEDNOZNACZNIE, bez zgadywania.
Lepiej sześć pewnych reguł niż dwadzieścia, z których połowa krzyczy po próżnicy.

    python3 -I sprawdz_zasady.py <katalog_repo>

Kod wyjścia 1, gdy jest choć jeden BŁĄD. UWAGI nie przerywają.
"""
import re
import sys
from pathlib import Path

# Pliki, którym wolno robić rzeczy zakazane dla reszty — z powodem.
WYJATKI = {
    "BraxGrafika.cs": "jedyne miejsce, które wolno ustawiać QualitySettings",
    "BraxObrazBuild.cs": "wypala standard obrazu w buildzie",
    "BraxObsadaImport.cs": "importer obsady — klasa TwarzBohatera i mimika",
    "sprawdz_zasady.py": "ten plik",
}

BLAD, UWAGA = "BŁĄD", "uwaga"


def zrodla(repo):
    for p in sorted(Path(repo).rglob("*.cs")):
        if "/Library/" in str(p) or "/obj/" in str(p):
            continue
        yield p


def linie(p):
    try:
        return p.read_text(encoding="utf-8", errors="replace").splitlines()
    except OSError:
        return []


def main(repo):
    znaleziska = []

    def dodaj(waga, plik, nr, tresc, regula):
        znaleziska.append((waga, regula, "%s:%d" % (plik, nr), tresc.strip()[:110]))

    for p in zrodla(repo):
        nazwa = p.name
        wzgl = str(p.relative_to(repo))
        for nr, l in enumerate(linie(p), 1):
            goly = l.split("//")[0]

            # 1. Rozdzielczość tekstur: sufit 2048. Wyżej tylko w importerze obsady.
            m = re.search(r"maxTextureSize\s*=\s*(\d+)", goly)
            if m and int(m.group(1)) > 2048 and nazwa not in WYJATKI:
                dodaj(BLAD, wzgl, nr, l, "T1 tekstura ponad 2048")

            # 2. Brak kompresji w buildzie Androida.
            if re.search(r"TextureImporterCompression\.Uncompressed|TextureImporterFormat\.RGB24", goly):
                dodaj(BLAD, wzgl, nr, l, "T2 tekstura bez kompresji")

            # 3. ASTC 4x4 tylko dla twarzy bohaterów (w importerze obsady).
            #    Liczy się PRZYPISANIE. Porównanie (`format != ASTC_4x4`) to kontrola,
            #    nie ustawienie — zgłaszanie go byłoby krzykiem po próżnicy.
            if re.search(r"format\s*=\s*TextureImporterFormat\.ASTC_4x4", goly) and nazwa not in WYJATKI:
                dodaj(BLAD, wzgl, nr, l, "T3 ASTC 4x4 poza twarzami")

            # 4. Mipmapy i streaming — wyłączanie jest błędem.
            if re.search(r"mipmapEnabled\s*=\s*false|streamingMipmaps\s*=\s*false", goly):
                dodaj(BLAD, wzgl, nr, l, "T4 mipmapy lub streaming wyłączone")

            # 5. QualitySettings tylko w BraxGrafika.
            if "QualitySettings." in goly and "=" in goly and nazwa not in WYJATKI:
                dodaj(BLAD, wzgl, nr, l, "J1 QualitySettings poza BraxGrafika")

            # 6. Przestrzeń barw: tylko Linear.
            if re.search(r"colorSpace\s*=\s*ColorSpace\.Gamma", goly):
                dodaj(BLAD, wzgl, nr, l, "O1 przestrzeń barw Gamma")

            # 7. Cienie wyłączane na stałe.
            if re.search(r"shadows\s*=\s*LightShadows\.None", goly):
                dodaj(UWAGA, wzgl, nr, l, "O2 cienie wyłączone — czy na pewno?")

            # 8. Siatki do odczytu w buildzie.
            if re.search(r"isReadable\s*=\s*true", goly):
                dodaj(UWAGA, wzgl, nr, l, "S1 siatka do odczytu — podwaja pamięć")

    bledy = [z for z in znaleziska if z[0] == BLAD]
    uwagi = [z for z in znaleziska if z[0] == UWAGA]

    print("SPRAWDZACZ ZASAD — %s" % repo)
    print("przejrzane pliki .cs: %d\n" % sum(1 for _ in zrodla(repo)))
    for tytul, lista in (("BŁĘDY (przerywają odbiór)", bledy), ("UWAGI", uwagi)):
        print("== %s: %d ==" % (tytul, len(lista)))
        reg = {}
        for _, r, gdzie, tresc in lista:
            reg.setdefault(r, []).append((gdzie, tresc))
        for r in sorted(reg):
            print("  %s — %d" % (r, len(reg[r])))
            for gdzie, tresc in reg[r][:6]:
                print("      %s   %s" % (gdzie, tresc))
            if len(reg[r]) > 6:
                print("      … i jeszcze %d" % (len(reg[r]) - 6))
        print()

    print("Reguły: ZASADY.md w korzeniu repozytorium.")
    return 1 if bledy else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1] if len(sys.argv) > 1 else "."))
