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

23.11 — PŁYNNOŚĆ I KAMERA (ZASADY, rozdział 10). Właściciel, 01:13: „Minimum 30 FPS,
minimum"; 01:27: „kamera w każdym kolejnym buildzie już ma być bliżej postaci".
Doszły reguły F1, K1, W0, W1, W2, G1, Z1. Zasada ta sama co wyżej: tylko to, co da się
stwierdzić z kodu bez zgadywania. Liczby klatek i trójkątów tego skryptu nie obchodzą —
mierzy je licznik w grze (BraxPlynnosc) i `statystyki.txt` przy zdjęciach z bolidu.
"""
import hashlib
import re
import sys
from pathlib import Path

# Pliki, którym wolno robić rzeczy zakazane dla reszty — z powodem.
WYJATKI = {
    "BraxGrafika.cs": "tryby jakości w czasie gry (ZASADY 4.5)",
    "BraxObrazBuild.cs": "wypala standard obrazu w buildzie (ZASADY 4.5)",
    "BraxObrazGlobalny.cs": "pilnuje standardu obrazu po wczytaniu sceny (ZASADY 4.5)",
    "BraxObsadaImport.cs": "importer obsady — klasa TwarzBohatera i mimika",
    "sprawdz_zasady.py": "ten plik",
}

BLAD, UWAGA = "BŁĄD", "uwaga"

# Pliki standardu płynności (ZASADY 10.9). Muszą być na każdej gałęzi — także na main.
STANDARD_PLYNNOSCI = (
    "unity-city/Assets/Claude/BraxKameraStandard.cs",
    "unity-city/Assets/Claude/Runtime/BraxPlynnosc.cs",
    "unity-city/Assets/Editor/Claude/BraxZasiegSortowanie.cs",
)

# Skrót SHA-1 wspólnego BraxGrafika.cs. None = wspólna wersja jeszcze nie ustalona
# (ZASADY 10.3 i 9.4): wtedy G1 tylko WYPISUJE wersję każdego rejonu. Gdy wspólna wersja
# trafi na main, wpisuje się tu jej skrót i każda inna staje się BŁĘDEM.
GRAFIKA_WSPOLNA = None

# Najniższy dozwolony limit klatek (ZASADY 10.1).
KLATKI_MIN = 30


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

            # 5. QualitySettings tylko w trzech plikach standardu obrazu (ZASADY 4.5).
            #    Liczy się USTAWIENIE — przypisanie albo zmiana poziomu. Sam odczyt
            #    (`float z = QualitySettings.shadowDistance;`, nazwa trybu do logu) niczego
            #    nie psuje. Do 23.11 reguła łapała każdą linię z „QualitySettings." i znakiem
            #    „=", czyli także odczyty: to był krzyk po próżnicy, którego ten plik miał
            #    nie robić (patrz reguła 3).
            if nazwa not in WYJATKI and re.search(
                    r"QualitySettings\.\w+\s*[-+*/]?=(?!=)|"
                    r"QualitySettings\.(SetQualityLevel|IncreaseLevel|DecreaseLevel)\s*\(", goly):
                dodaj(BLAD, wzgl, nr, l, "J1 QualitySettings poza standardem obrazu")

            # 6. Przestrzeń barw: tylko Linear.
            if re.search(r"colorSpace\s*=\s*ColorSpace\.Gamma", goly):
                dodaj(BLAD, wzgl, nr, l, "O1 przestrzeń barw Gamma")

            # 7. Cienie wyłączane na stałe.
            if re.search(r"shadows\s*=\s*LightShadows\.None", goly):
                dodaj(UWAGA, wzgl, nr, l, "O2 cienie wyłączone — czy na pewno?")

            # 8. Siatki do odczytu w buildzie.
            if re.search(r"isReadable\s*=\s*true", goly):
                dodaj(UWAGA, wzgl, nr, l, "S1 siatka do odczytu — podwaja pamięć")

            # F1. Limit klatek poniżej 30 (ZASADY 10.1). Tylko liczba wpisana wprost;
            #     `targetFrameRate = fps` zależy od tego, kto woła — patrz `Klatki(…)` niżej.
            m = re.search(r"targetFrameRate\s*=\s*(\d+)\b", goly)
            if m and 0 < int(m.group(1)) < KLATKI_MIN:
                dodaj(BLAD, wzgl, nr, l, "F1 limit klatek poniżej 30")
            if nazwa == "BraxGrafika.cs":
                for m in re.finditer(r"\bKlatki\(\s*(?:\w+\s*\?\s*)?(\d+)(?:\s*:\s*(\d+))?\s*\)", goly):
                    if any(g and 0 < int(g) < KLATKI_MIN for g in m.groups()):
                        dodaj(BLAD, wzgl, nr, l, "F1 limit klatek poniżej 30")

            # K1. Odległość kamery wpisana liczbą w pliku gracza (ZASADY 10.8).
            #     Liczby kamery są w BraxKameraStandard i tylko tam.
            if nazwa == "BraxGymPlayer.cs":
                if re.search(r"\bdistance\s*=\s*\d*\.?\d+f?\s*[;,]", goly):
                    dodaj(BLAD, wzgl, nr, l, "K1 kamera: odległość wpisana liczbą zamiast BraxKameraStandard")
                if re.search(r"\bviewDistance\s*=.*Mathf\.Lerp\(\s*\d", goly):
                    dodaj(BLAD, wzgl, nr, l, "K1 kamera: odległość wpisana liczbą zamiast BraxKameraStandard")

            # W2. Własne liczby zasięgu warstw w pliku rejonu (ZASADY 10.4: „rejon nie
            #     wpisuje własnych warstw ani własnych zasięgów"). Uwaga, nie błąd: rejon
            #     Mistrzów ma dziś te same liczby co standard — do przepięcia, nie do gaszenia.
            if nazwa != "BraxPlynnosc.cs" and re.search(
                    r"const\s+(?:float|int)\b[^;]*\b(?:Zasieg|Warstwa)(?:Male|Drobne)\s*=", goly):
                dodaj(UWAGA, wzgl, nr, l, "W2 własne liczby warstw zasięgu — mają pochodzić z BraxPlynnosc")

    # P1. SHADER OBSADY MUSI MIEĆ POKRĘTŁA STANDARDU POSTACI.
    # 18.11: na gałęzi osiedla leżała STARA kopia Braxhorn/Obsada Osiedla — bez _Gladkosc,
    # _MetalMnoznik i _SilaNormalnych. BraxPostacGlobalna przechodziło przez wszystkie 19
    # postaci, nie miało czego ustawić, NIE ROBIŁO NIC i meldowało sukces w logu. Właściciel
    # dostałby build z postaciami takimi jak przed poprawką i zapewnienie, że to wariant 6.
    # To jest dokładnie to, czego zakazuje punkt 5.0a (rejon nie trzyma własnej kopii potoku
    # obsady), tylko że nikt tego nie sprawdzał. Teraz sprawdza.
    shader = Path(repo) / "unity-city/Assets/Claude/BraxObsadaOsiedla.shader"
    if shader.exists():
        tresc = shader.read_text(encoding="utf-8", errors="replace")
        for pokretlo, uzycie in (("_Gladkosc", "_Gladkosc"),
                                 ("_MetalMnoznik", "_MetalMnoznik"),
                                 ("_SilaNormalnych", "_SilaNormalnych")):
            # musi być i zadeklarowane, i użyte w liczeniu światła — sama właściwość nic nie daje
            if tresc.count(uzycie) < 3:
                dodaj(BLAD, "unity-city/Assets/Claude/BraxObsadaOsiedla.shader", 1,
                      "brak albo nieużyte %s — standard postaci nie ma czego ustawić" % pokretlo,
                      "P1 shader obsady bez pokrętła standardu")
    else:
        dodaj(BLAD, "unity-city/Assets/Claude", 1,
              "brak BraxObsadaOsiedla.shader — rejon bez shadera obsady",
              "P1 shader obsady bez pokrętła standardu")

    # ---- PŁYNNOŚĆ I KAMERA (ZASADY, rozdział 10) -------------------------------------------
    korzen = Path(repo)

    # W0. Pliki standardu płynności muszą być na gałęzi. Bez nich rejon nie ma licznika,
    #     nie ma skąd wziąć liczb kamery ani zasięgów — i każdy wpisze własne.
    for sciezka in STANDARD_PLYNNOSCI:
        if not (korzen / sciezka).exists():
            dodaj(BLAD, sciezka, 1, "brak pliku — gałąź nie ma aktualnego main",
                  "W0 brak pliku standardu płynności")

    # Reguły K1, W1 i Z1 dotyczą gałęzi z GRĄ (jest plik gracza). Sam main trzyma tylko
    # standard i nie ma czego sortować ani czym chodzić.
    gracze = [p for p in zrodla(repo) if p.name == "BraxGymPlayer.cs"]
    for p in gracze:
        tresc = p.read_text(encoding="utf-8", errors="replace")
        wzgl = str(p.relative_to(repo))
        if "BraxKameraStandard.Pieszo" not in tresc:
            dodaj(BLAD, wzgl, 1, "kamera pieszo nie czyta BraxKameraStandard.Pieszo",
                  "K1 kamera: odległość wpisana liczbą zamiast BraxKameraStandard")
        if "autoGracza" in tresc and "BraxKameraStandard.Auto" not in tresc:
            dodaj(BLAD, wzgl, 1, "kamera za autem nie czyta BraxKameraStandard.Auto(prędkość)",
                  "K1 kamera: odległość wpisana liczbą zamiast BraxKameraStandard")

    if gracze:
        # W1. Rejon musi sortować obiekty na warstwy zasięgu przy buildzie (ZASADY 10.4).
        #     Liczy się wywołanie wspólnego sortowania albo przypisanie warstwy MAŁE/DROBNE.
        sortuje = False
        zdjecia = False
        for p in zrodla(repo):
            if p.name in ("BraxZasiegSortowanie.cs", "BraxPlynnosc.cs"):
                continue
            tresc = p.read_text(encoding="utf-8", errors="replace")
            if re.search(r"BraxZasiegSortowanie\.Posortuj\s*\(|"
                         r"\.layer\s*=[^;]*Warstwa(?:Male|Drobne)", tresc):
                sortuje = True
            if "statystyki.txt" in tresc:
                zdjecia = True
        if not sortuje:
            dodaj(BLAD, "unity-city/Assets", 1,
                  "nikt nie woła BraxZasiegSortowanie.Posortuj(scena) — drobiazgi rysują się do pełnego zasięgu",
                  "W1 rejon nie sortuje obiektów na warstwy zasięgu")

        # Z1. Przebieg zdjęć z liczbami kadru (ZASADY 8.8 i 10.2). Uwaga, nie błąd: rejon
        #     może pisać statystyki poza C# (skrypt zdjęć), a tego stąd nie widać na pewno.
        if not zdjecia:
            for wzor in ("*.mjs", "*.py", "*.yml"):
                for p in korzen.rglob(wzor):
                    if p.name == "sprawdz_zasady.py" or "/Library/" in str(p):
                        continue
                    try:
                        if "statystyki.txt" in p.read_text(encoding="utf-8", errors="replace"):
                            zdjecia = True
                            break
                    except OSError:
                        pass
                if zdjecia:
                    break
        if not zdjecia:
            dodaj(UWAGA, "unity-city/Assets", 1,
                  "nie znaleziono przebiegu zdjęć piszącego statystyki.txt",
                  "Z1 brak zdjęć z liczbami kadru przy bolidzie")

    # G1. Jeden plik trybów jakości (ZASADY 10.3). Wersja i liczby każdego rejonu na wierzch,
    #     żeby rozjazd było widać w raporcie, a nie trzeba go było szukać po gałęziach.
    for p in zrodla(repo):
        if p.name not in ("BraxGrafika.cs", "BraxZasiegDrobnych.cs"):
            continue
        surowe = p.read_bytes()
        skrot = hashlib.sha1(surowe).hexdigest()[:7]
        tresc = surowe.decode("utf-8", errors="replace")
        wzgl = str(p.relative_to(repo))
        if p.name == "BraxZasiegDrobnych.cs":
            # Podłoga drobiazgów (ZASADY 10.4 b): dwie wersje pliku na gałęziach, jedna ma być.
            cele = "/".join(re.findall(r"return\s+(\d+)f\s*;", tresc))
            dodaj(UWAGA, wzgl, 1, "wersja %s  znikanie od %s j." % (skrot, cele),
                  "G1 zasięg drobiazgów (LOD) — wspólna wersja jeszcze nie ustalona (ZASADY 10.4)")
            continue
        zasiegi = "/".join(re.findall(r"\bZasieg\(\s*(\d+)", tresc))
        progi = "/".join(re.findall(r"lodBias\s*=\s*([\d.]+)f", tresc))
        bez_cieni = "TAK (wbrew 4.6)" if "ShadowQuality.Disable" in tresc else "nie"
        opis = "wersja %s  zasięg %s  lodBias %s  cienie wyłączane: %s" % (skrot, zasiegi, progi, bez_cieni)
        if GRAFIKA_WSPOLNA is None:
            dodaj(UWAGA, wzgl, 1, opis, "G1 tryby jakości — wspólna wersja jeszcze nie ustalona (ZASADY 10.3)")
        elif skrot != GRAFIKA_WSPOLNA[:7]:
            dodaj(BLAD, wzgl, 1, opis, "G1 tryby jakości inne niż wspólne z main")

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
