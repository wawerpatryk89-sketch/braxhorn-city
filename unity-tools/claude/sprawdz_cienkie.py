"""BRAMKA CIENKICH ELEMENTOW — co bedzie migotac, zanim ktokolwiek zbuduje APK.

    python3 -I narzedzia/sprawdz_cienkie.py <katalog-repo> [--rejon osiedle|miasto|mistrzowie|pokoj|wszystko]
                                            [--prog 3.0] [--limit 40]

Wlasciciel 16.11, 13:58: „kazdy pasek cienki, czysto swiatlo czy co, wszystko migota”.
I 13:59-14:00: nie chce powtarzac tego samego sto razy i nie chce zgadywania.
Wiec to narzedzie NIE ZGADUJE: czyta deklaracje funkcji w tym samym pliku, w ktorym stoi
wywolanie, i dopiero wtedy wie, ktory argument jest rozmiarem, ktory promieniem, a ktory
gladkoscia. Pierwsza wersja brala „najwieksza liczbe w linii” i zglosila 60 falszywych
odblyskow (podawala gladkosc 1.00 dla poscieli, bo lapala kanaly koloru). To bylo zle i jest
poprawione.

DLACZEGO 3 cm. Telefon w pionie: ok. 1080 px szerokosci, FOV 55 st. w pionie, proporcja ok. 0,46
→ poziomo +/-13,4 st. Na 50 m od kamery widac 23,8 m szerokosci, czyli 2,2 cm NA PIKSEL. Element
wezszy nie ma jak sie narysowac: piksel albo go trafi, albo nie, wiec gasnie i zapala sie przy
kazdym kroku. 3 cm to ten prog z zapasem. Prog mozna zmienic przez --prog.

CZTERY PRZYCZYNY, nie jedna:
 1. CIENKA GEOMETRIA — bryla, rurka albo walec ponizej progu.
 2. JASNY PASEK — cienki element w jasnym albo swiecacym materiale; migocze najmocniej,
    bo kontrast jest najwiekszy.
 3. ODBLYSK — cienki element z gladkoscia materialu powyzej progu; odblask jest wezszy od
    piksela i strzela przy ruchu.
 4. NAKLADKA — dwie powierzchnie w jednym miejscu: stale wysokosci rozniace sie o mniej niz 5 cm.

Czyta tylko pliki. Niczego nie zmienia. Zglasza plik, linie i nazwe obiektu.
"""
import os
import re
import sys
from collections import defaultdict

PROG_CM = 3.0
PROG_NAKLADKA_CM = 5.0
PROG_GLADKOSC = 0.55
PROG_JASNOSC = 0.80
K = 2.33 / 1.80

REJONY = {
    "osiedle": ("BraxClaudeOsiedle",),
    "miasto": ("BraxCity", "BraxClaudeObszary", "BraxClaudeSky", "BraxClaudeMiasto"),
    "mistrzowie": ("BraxMistrz", "BraxGaleria", "BraxHotel"),
    "pokoj": ("BraxPokojProb", "BraxProba"),
}

LICZBA = r"-?(?:\d+\.?\d*|\.\d+)f?"
# deklaracja: static <zwrot> Nazwa(parametry)
DEKLARACJA = re.compile(
    r"static\s+[A-Za-z_][\w<>\[\],\s]*?\s+([A-Z][A-Za-z0-9_]*)\s*\(([^)]*)\)")
PARAM = re.compile(r"(?:^|\s)(?:ref\s+|out\s+|params\s+)?"
                   r"([A-Za-z_][\w<>\[\]?]*)\s+([A-Za-z_]\w*)\s*(?:=[^,]*)?$")
STALA = re.compile(r"const\s+float\s+([A-Za-z_]\w*)\s*=\s*(" + LICZBA + r")")
VEC3 = re.compile(r"new\s+Vector3\s*\(")
KOLOR = re.compile(r"new\s+Color\s*\(\s*(" + LICZBA + r")\s*,\s*(" + LICZBA +
                   r")\s*,\s*(" + LICZBA + r")")
EMISJA = re.compile(r"emisja|_Emission|_EMISSION", re.I)

# jak nazywaja sie parametry, ktore nas interesuja
NAZWY_ROZMIARU = {"size", "s", "rozmiar", "skala", "scale", "wymiar"}
NAZWY_PROMIENIA = {"r", "promien", "radius", "grubosc", "gr"}
NAZWY_GLADKOSCI = {"smooth", "gloss", "gladkosc", "smoothness"}
NAZWY_NAZWY = {"name", "n", "nazwa", "key"}


def liczba(txt):
    txt = txt.strip().rstrip("f")
    try:
        return float(txt)
    except ValueError:
        return None


def argumenty(tekst, start):
    """Dzieli argumenty wywolania zaczynajacego sie nawiasem na pozycji start.

    Zwraca (lista_argumentow, pozycja_po_zamykajacym_nawiasie) albo (None, start).
    Liczy zagniezdzenie nawiasow, klamr i cudzyslowow, zeby new Vector3(1,2,3) byl JEDNYM
    argumentem, a nie trzema.
    """
    if start >= len(tekst) or tekst[start] != "(":
        return None, start
    glebia, i, biezacy, out = 0, start, "", []
    w_tekscie = w_znaku = False
    while i < len(tekst):
        z = tekst[i]
        if w_tekscie:
            if z == "\\":
                biezacy += z + (tekst[i + 1] if i + 1 < len(tekst) else "")
                i += 2
                continue
            if z == '"':
                w_tekscie = False
        elif w_znaku:
            if z == "'":
                w_znaku = False
        elif z == '"':
            w_tekscie = True
        elif z == "'":
            w_znaku = True
        elif z in "([{":
            glebia += 1
            if glebia == 1:
                i += 1
                continue
        elif z in ")]}":
            glebia -= 1
            if glebia == 0:
                out.append(biezacy)
                return [a.strip() for a in out], i + 1
        elif z == "," and glebia == 1:
            out.append(biezacy)
            biezacy = ""
            i += 1
            continue
        biezacy += z
        i += 1
    return None, start


def skladowe_vector3(arg):
    """[x, y, z] w metrach dla new Vector3(...) o policzalnych czlonach; None tam, gdzie zmienna."""
    m = VEC3.search(arg)
    if not m:
        return None
    czesci, _ = argumenty(arg, arg.index("(", m.start()))
    if not czesci or len(czesci) != 3:
        return None
    out = []
    for c in czesci:
        c = c.strip()
        mk = re.fullmatch(r"(" + LICZBA + r")\s*(\*\s*K)?", c)
        if not mk:
            out.append(None)
            continue
        v = liczba(mk.group(1))
        out.append(None if v is None else abs(v) * (K if mk.group(2) else 1.0))
    return out


def deklaracje(tekst):
    """Nazwa funkcji -> lista nazw parametrow, z deklaracji w TYM pliku."""
    out = {}
    for m in DEKLARACJA.finditer(tekst):
        nazwa, surowe = m.group(1), m.group(2)
        if not surowe.strip():
            continue
        czesci, _ = argumenty("(" + surowe + ")", 0)
        if not czesci:
            continue
        param = []
        for c in czesci:
            mp = PARAM.search(" " + c.split("=")[0].rstrip())
            param.append(mp.group(2) if mp else "?")
        # przy przecizeniach bierzemy to, ktore ma najwiecej parametrow —
        # i tak sprawdzamy tylko po NAZWIE parametru, a nie po pozycji
        if nazwa not in out or len(param) > len(out[nazwa]):
            out[nazwa] = param
    return out


def wywolania(tekst, nazwa):
    """(pozycja, [argumenty]) dla kazdego wywolania funkcji o tej nazwie."""
    for m in re.finditer(r"(?<![\w.])" + re.escape(nazwa) + r"\s*\(", tekst):
        poz = tekst.index("(", m.start())
        args, _ = argumenty(tekst, poz)
        if args:
            yield m.start(), args


def nr_linii(tekst, poz):
    return tekst.count("\n", 0, poz) + 1


def nazwa_z_argow(args, param):
    for p, a in zip(param, args):
        if p in NAZWY_NAZWY:
            m = re.search(r'"([^"]*)"', a)
            if m:
                return m.group(1)
    return "(bez nazwy)"


def skan(sciezka):
    try:
        tekst = open(sciezka, encoding="utf-8", errors="replace").read()
    except OSError:
        return []
    zglos = []
    dekl = deklaracje(tekst)

    # --- gladkosc materialow: po NAZWIE parametru, nie po najwiekszej liczbie -----
    gladkie = {}      # nazwa materialu -> (gladkosc, linia)
    jasne = set()     # nazwy materialow jasnych albo swiecacych
    for fun, param in dekl.items():
        if not ({p for p in param} & NAZWY_GLADKOSCI):
            continue
        for poz, args in wywolania(tekst, fun):
            if len(args) < len(param):
                param_uzyte = param[:len(args)]
            else:
                param_uzyte = param
            nazwa = nazwa_z_argow(args, param_uzyte)
            for p, a in zip(param_uzyte, args):
                if p in NAZWY_GLADKOSCI:
                    v = liczba(a)
                    if v is not None:
                        gladkie[nazwa] = (v, nr_linii(tekst, poz))
            kol = KOLOR.search(" ".join(args))
            if kol:
                kanaly = [liczba(kol.group(i)) or 0 for i in (1, 2, 3)]
                if sum(kanaly) / 3 >= PROG_JASNOSC:
                    jasne.add(nazwa)
            if EMISJA.search(" ".join(args)):
                jasne.add(nazwa)

    # --- cienka geometria: rozmiar bryly i promien rurki/walca -------------------
    for fun, param in dekl.items():
        ma_rozmiar = {p for p in param} & NAZWY_ROZMIARU
        ma_promien = {p for p in param} & NAZWY_PROMIENIA
        if not (ma_rozmiar or ma_promien):
            continue
        for poz, args in wywolania(tekst, fun):
            param_uzyte = param[:len(args)] if len(args) < len(param) else param
            male = []
            for p, a in zip(param_uzyte, args):
                if p in NAZWY_ROZMIARU:
                    sk = skladowe_vector3(a)
                    if sk:
                        male += [(os_, v * 100) for os_, v in zip("xyz", sk)
                                 if v is not None and 0 < v * 100 < PROG_CM]
                elif p in NAZWY_PROMIENIA:
                    v = liczba(a)
                    if v is None:
                        mk = re.fullmatch(r"(" + LICZBA + r")\s*\*\s*K", a.strip())
                        v = liczba(mk.group(1)) * K if mk else None
                    if v is not None and 0 < v * 2 * 100 < PROG_CM:
                        male.append(("srednica", v * 2 * 100))
            if not male:
                continue
            nazwa = nazwa_z_argow(args, param_uzyte)
            mat = next((re.sub(r"\W", "", a) for p, a in zip(param_uzyte, args)
                        if p in ("m", "mat", "material", "lico")), "")
            opis_mat = next((n for n in gladkie if mat and n.replace(" ", "") in mat), None)
            wym = ", ".join(f"{o}={v:.1f} cm" for o, v in male)
            rodzaj = "CIENKA"
            dodatek = ""
            if opis_mat and opis_mat in jasne:
                rodzaj = "JASNY PASEK"
                dodatek = f"; material „{opis_mat}” jasny/swiecacy — migocze najmocniej"
            elif opis_mat and gladkie[opis_mat][0] > PROG_GLADKOSC:
                rodzaj = "ODBLYSK"
                dodatek = f"; material „{opis_mat}” gladkosc {gladkie[opis_mat][0]:.2f}"
            zglos.append((rodzaj, nr_linii(tekst, poz), f"{fun} „{nazwa}” {wym}{dodatek}"))

    # --- nakladki: stale wysokosci blisko siebie ---------------------------------
    wys = {}
    for nr, linia in enumerate(tekst.split("\n"), 1):
        for m in STALA.finditer(linia):
            n, v = m.group(1), liczba(m.group(2))
            if v is None or not (0 < v < 2.0):
                continue
            if re.match(r"^(Y|y)[A-Z_]|.*(Wys|wys|Poziom|poziom)", n):
                wys[n] = (v, nr)
    kl = sorted(wys)
    for i in range(len(kl)):
        for j in range(i + 1, len(kl)):
            a, na = wys[kl[i]]
            b, nb = wys[kl[j]]
            d = abs(a - b) * 100
            if 0 < d < PROG_NAKLADKA_CM:
                zglos.append(("NAKLADKA", min(na, nb),
                              f"{kl[i]}={a:.4f} i {kl[j]}={b:.4f} — odstep {d:.1f} cm: "
                              f"dwie powierzchnie beda walczyc o glebie"))
    return zglos


def pliki_rejonu(katalog, rejon):
    przed = None
    if rejon != "wszystko":
        przed = REJONY.get(rejon)
        if not przed:
            raise SystemExit("Nieznany rejon: " + rejon + " (znam: " +
                             ", ".join(sorted(REJONY)) + ", wszystko)")
    for korzen, _, nazwy in os.walk(os.path.join(katalog, "unity-city", "Assets")):
        for n in sorted(nazwy):
            if n.endswith(".cs") and (not przed or any(n.startswith(p) for p in przed)):
                yield os.path.join(korzen, n)


def main(katalog, rejon, limit):
    razem = defaultdict(list)
    plikow = 0
    for sciezka in pliki_rejonu(katalog, rejon):
        plikow += 1
        for rodzaj, nr, opis in skan(sciezka):
            razem[rodzaj].append((os.path.relpath(sciezka, katalog), nr, opis))

    print(f"BRAMKA CIENKICH ELEMENTOW — rejon: {rejon}, przeczytanych plikow: {plikow}")
    print(f"progi: szerokosc {PROG_CM:.1f} cm, odstep powierzchni {PROG_NAKLADKA_CM:.0f} cm, "
          f"gladkosc {PROG_GLADKOSC:.2f}, jasnosc {PROG_JASNOSC:.2f}\n")
    suma = 0
    for rodzaj in ("JASNY PASEK", "ODBLYSK", "CIENKA", "NAKLADKA"):
        wpisy = sorted(razem.get(rodzaj, []))
        if not wpisy:
            continue
        suma += len(wpisy)
        print(f"=== {rodzaj}: {len(wpisy)} ===")
        for plik, nr, opis in wpisy[:limit]:
            print(f"  {plik}:{nr}  {opis}")
        if len(wpisy) > limit:
            print(f"  … i {len(wpisy) - limit} wiecej")
        print()
    print("CZYSTO — nic ponizej progow." if suma == 0 else f"RAZEM DO POPRAWY: {suma}")
    return 1 if suma else 0


if __name__ == "__main__":
    if len(sys.argv) < 2:
        raise SystemExit(__doc__)
    a = sys.argv
    r = a[a.index("--rejon") + 1] if "--rejon" in a else "wszystko"
    if "--prog" in a:
        PROG_CM = float(a[a.index("--prog") + 1])
    lim = int(a[a.index("--limit") + 1]) if "--limit" in a else 40
    sys.exit(main(a[1], r, lim))
