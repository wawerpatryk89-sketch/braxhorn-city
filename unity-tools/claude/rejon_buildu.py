#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Rejon buildu CI i klucz pamięci podręcznej wygenerowanego rejonu (BRAXHORN CITY, build 14).

Właściciel 2.11: „aktualizacja, a nie budowanie całego świata od nowa za każdym razem”; rejony OSOBNO
(osiedle i miasto jako osobne buildy). Ten skrypt:
  1. wybiera rejon buildu: wejście ręcznego uruchomienia (WEJSCIE_REJON) albo plik unity-city/rejon-buildu.txt
     (pierwsze słowo: osiedle | miasto | oba; brak pliku = osiedle; słowo „bez-pamieci” = wymuś pełne generowanie),
  2. liczy KLUCZ świata rejonu = SHA-256 wszystkiego, od czego zależy wynik generatora tego rejonu:
       • cały kod C# i shadery w unity-city/Assets (edytor + gra) OPRÓCZ plików generatora drugiego rejonu
         (listy TYLKO poniżej; plik z tych list, do którego odwołuje się kod wchodzący do klucza, wraca do klucza sam),
       • wszystkie dane w unity-city/Assets (modele, tekstury, dźwięki, .json — także pobrane i odszyfrowane w CI przed
         tym krokiem, łącznie z tym, co kroki przygotowania zapisały w Assets/PlayableGenerated) i pliki .meta obecne
         przed uruchomieniem Unity,
       • unity-city/Packages/manifest.json (+ packages-lock.json), ProjectSettings/ProjectVersion.txt, unity-city/Docs,
       • wersję Unity z workflow, rejon, platformę, ten skrypt i WERSJA (podbicie = unieważnienie wszystkich światów).
     Każda zmiana któregokolwiek z tych plików = nowy klucz = pełne generowanie (nigdy stary świat z nowym kodem).
  3. zapisuje unity-city/rejon-ci.txt (czyta go Assets/Editor/BraxRejonBuildu.cs) i wyjścia kroku do $GITHUB_OUTPUT.

Wywołanie (z katalogu głównego repozytorium, PO wszystkich krokach przygotowujących zasoby, PRZED Unity):
    python3 -I unity-tools/claude/rejon_buildu.py --platforma android --unity 6000.0.58f2
Zmienne: WEJSCIE_REJON, WEJSCIE_PAMIEC („false” = bez pamięci), PAMIEC_REJONOW (domyślnie „osiedle”), GITHUB_OUTPUT.
Linie BRAXHORN_KLUCZ grupa=… w logu: porównanie dwóch buildów pokazuje, która grupa plików zmieniła klucz.
"""
import argparse
import fnmatch
import hashlib
import os
import pathlib
import re
import sys

WERSJA = '1'
REJONY = ('osiedle', 'miasto', 'oba')

# Pliki generatora WYŁĄCZNIE drugiego rejonu (ścieżki względem unity-city/). Kod gry (Assets/Claude, Assets/City, …),
# pliki wspólne edytora (BraxGymBuild, BraxCityBuild, BraxClaudeSiatki, BraxClaudeBezSwiecenia, BraxClaudeObszary,
# BraxClaudeSufity, BraxClaudeGlosniki, BraxGymPremium, BraxHeroRefinements, postprocesory tekstur …) wchodzą do OBU kluczy.
TYLKO = {
    # generator miasta R1 (siłownia z remontem, port, plaża, wyspa, NOVA, Nutrition, metro, wieżowce) — poza kluczem osiedla
    'miasto': [
        'Assets/Editor/CityRebuild/**',
        'Assets/Editor/Claude/Region/**',
        'Assets/Editor/Claude/BraxClaudeMetro.cs',
        'Assets/Editor/Claude/BraxClaudeNovaClub.cs',
        'Assets/Editor/Claude/BraxClaudeNovaProbe.cs',
        'Assets/Editor/Claude/BraxClaudeTowerTerrace.cs',
        'Assets/Editor/Claude/BraxClaudeVenues.cs',
        'Assets/Editor/Claude/BraxClaudeCourts.cs',
        'Assets/Editor/Claude/BraxClaudeGround.cs',
        'Assets/Editor/Claude/BraxClaudeTrees.cs',
        'Assets/Editor/Claude/BraxClaudeSky.cs',
        'Assets/Editor/BraxNutritionShopBuild.cs',
        'Assets/Editor/BraxNovaMusicBuild.cs',
        'Assets/Editor/BraxGymReferenceRebuild.cs',
        'Assets/Editor/BraxGymReferenceEquipment.cs',
    ],
    # generator osiedla R2 — poza kluczem miasta. BraxClaudeOsiedle.cs (główny) zostaje w kluczu miasta:
    # siłownia miasta woła z niego BraxClaudeOsiedle.StreamAudio (BraxGymBuild).
    'osiedle': [
        'Assets/Editor/Claude/BraxClaudeOsiedle?*.cs',
        'Assets/Editor/BraxEstateR2ApprovedRoofs.cs',
        'Assets/Editor/BraxEstateR2FacadeFinish.cs',
    ],
}
# Znane odwołania z plików wspólnych do kodu drugiego rejonu, które w buildzie danego rejonu się NIE wykonują
# (sprawdzone w kodzie 2.11). Każde inne odwołanie = plik drugiego rejonu wraca do klucza (bezpiecznie: pełne generowanie).
ZNANE = {
    'osiedle': {   # build osiedla: BraxGymBuild pomija miasto i remont siłowni, BraxCityBuild.AttachCity/AddArea("city") się nie wykonuje
        'Assets/Editor/BraxGymBuild.cs': {'BraxNutritionShopBuild', 'BraxGymReferenceRebuild', 'BraxGymReferenceEquipment',
                                          'BraxNovaMusicBuild', 'BraxRegionGym', 'BraxCityRebuild'},
        'Assets/Editor/BraxCityBuild.cs': {'BraxCityRebuild', 'BraxCitySkyAuthoring', 'BraxClaudeSky'},
    },
    'miasto': {    # build miasta: BraxCityBuild.CreateEstate się nie wykonuje
        'Assets/Editor/BraxCityBuild.cs': {'BraxEstateR2ApprovedRoofs', 'BraxEstateR2FacadeFinish', 'BraxClaudeOsiedle.Apply'},
    },
}
KOD = ('.cs', '.shader', '.cginc', '.hlsl', '.compute', '.asmdef', '.asmref')


def log(s):
    print(s, flush=True)


def wybierz_rejon(uc: pathlib.Path):
    wejscie = (os.environ.get('WEJSCIE_REJON') or '').strip().lower()
    pamiec = (os.environ.get('WEJSCIE_PAMIEC') or '').strip().lower() not in ('false', 'nie', '0')
    zrodlo = 'wejście ręcznego uruchomienia'
    rejon = wejscie if wejscie in REJONY else ''
    if not rejon:
        zrodlo, rejon = 'domyślny', 'osiedle'
        plik = uc / 'rejon-buildu.txt'
        if plik.is_file():
            slowa = ' '.join(l.split('#', 1)[0] for l in plik.read_text(encoding='utf-8').splitlines()).lower().split()
            if slowa and slowa[0] in REJONY:
                rejon, zrodlo = slowa[0], 'plik unity-city/rejon-buildu.txt'
            if 'bez-pamieci' in slowa or 'bez-pamięci' in slowa:
                pamiec = False
    return rejon, pamiec, zrodlo


def wzorce(rejon):
    """Pliki poza kluczem danego rejonu = pliki generatora drugiego rejonu."""
    if rejon == 'osiedle':
        return TYLKO['miasto']
    if rejon == 'miasto':
        return TYLKO['osiedle']
    return []


def pasuje(rel, wz):
    for w in wz:
        if w.endswith('/**'):
            if rel.startswith(w[:-2]):
                return True
        elif fnmatch.fnmatchcase(rel, w) or fnmatch.fnmatchcase(rel, w + '.meta'):
            return True
    return False


def czysty(kod: str) -> str:
    """Kod C# bez napisów i komentarzy (odwołania w komentarzach się nie liczą)."""
    kod = re.sub(r'@"(?:""|[^"])*"', '""', kod)
    kod = re.sub(r'"(?:\\.|[^"\\\n])*"', '""', kod)
    kod = re.sub(r"'(?:\\.|[^'\\\n])'", "''", kod)
    kod = re.sub(r'/\*.*?\*/', ' ', kod, flags=re.S)
    return re.sub(r'//[^\n]*', '', kod)


TYP = re.compile(r'^(?:\[[^\]\n]*\]\s*)*(?:(?:public|internal|static|sealed|abstract|partial)\s+)*(?:class|struct|enum|interface)\s+(\w+)', re.M)
CZESCIOWY = re.compile(r'^(?:\[[^\]\n]*\]\s*)*(?:(?:public|internal|static|sealed|abstract)\s+)*partial\s+(?:class|struct)\s+(\w+)', re.M)
CZLONEK = [re.compile(r'\b(?:static|const)\b[^;={}()]*?\b(\w+)\s*(?:\(|=|;|\{)'),
           re.compile(r'\b(?:static)\b[^;={}()]*?\b(\w+)\s*<[^<>()]*>\s*\('),
           re.compile(r'\b(?:class|struct|enum|interface)\s+(\w+)')]


def eksport(kod: str):
    """Nazwy, przez które inny plik może użyć kodu z tego pliku: typy z kolumny 0; dla klas częściowych — Typ.Członek."""
    proste, czesciowe = set(TYP.findall(kod)), set(CZESCIOWY.findall(kod))
    nazwy = {n for n in proste if n not in czesciowe}
    for t in czesciowe:
        for wz in CZLONEK:
            nazwy.update(t + '.' + m for m in wz.findall(kod) if m != t)
    return nazwy


def odwolanie(nazwa: str, kod: str) -> bool:
    if '.' in nazwa:
        t, m = nazwa.split('.', 1)
        return re.search(r'\b' + re.escape(t) + r'\s*\.\s*' + re.escape(m) + r'\b', kod) is not None
    return re.search(r'\b' + re.escape(nazwa) + r'\b', kod) is not None


def pliki(uc: pathlib.Path):
    out = []
    for baza in ('Assets', 'Docs'):
        for root, dirs, files in os.walk(uc / baza):
            rr = pathlib.Path(root).relative_to(uc).as_posix()
            # katalogi i pliki ignorowane przez Unity („~”, „.” na początku) nie są wejściem. Assets/PlayableGenerated
            # WCHODZI: klucz liczony jest przed Unity i przed przywróceniem pamięci, więc jest tam tylko to, co zapisały
            # kroki przygotowania (np. odchudzanie.json z odchudzanie_modeli.py) — to też wejście generatora.
            dirs[:] = sorted(d for d in dirs if not d.endswith('~') and not d.startswith('.'))
            for f in sorted(files):
                if f.startswith('.') or f.endswith('~'):
                    continue
                out.append(rr + '/' + f)
    for f in ('Packages/manifest.json', 'Packages/packages-lock.json', 'ProjectSettings/ProjectVersion.txt'):
        if (uc / f).is_file():
            out.append(f)
    return out


def sha(p: pathlib.Path) -> str:
    h = hashlib.sha256()
    if p.is_symlink():
        h.update(os.readlink(p).encode())
        return h.hexdigest()
    with open(p, 'rb') as fh:
        for blok in iter(lambda: fh.read(1 << 20), b''):
            h.update(blok)
    return h.hexdigest()


def grupa(rel: str) -> str:
    if rel.endswith('.meta'):
        return 'meta'
    if rel.endswith(KOD):
        return 'kod-edytora' if '/Editor/' in '/' + rel else 'kod-gry'
    cz = rel.split('/')
    return 'dane/' + '/'.join(cz[1:3] if len(cz) > 3 else cz[1:-1] or cz[:1])


def klucz(uc: pathlib.Path, rejon: str, platforma: str, unity: str):
    wszystkie = pliki(uc)
    wz = wzorce(rejon)
    poza = [r for r in wszystkie if pasuje(r, wz)]
    zbior_poza = set(poza)
    w_kluczu = [r for r in wszystkie if r not in zbior_poza]
    # Bezpiecznik: plik drugiego rejonu, do którego odwołuje się kod z klucza (poza ZNANE), wraca do klucza.
    # (Nie wykrywa wywołań bez nazwy typu między plikami jednej klasy częściowej — tylko Typ.Członek i nazwy typów.)
    teksty = {r: czysty((uc / r).read_text(encoding='utf-8', errors='replace')) for r in wszystkie if r.endswith('.cs')}
    tokeny = {r: set(re.findall(r'\w+', t)) for r, t in teksty.items()}
    znane = ZNANE.get(rejon, {})

    def kto_uzywa(r):
        kod_klucza = [k for k in w_kluczu if k.endswith('.cs')]
        for nazwa in sorted(eksport(teksty[r])):
            czesci = nazwa.split('.')
            for k in kod_klucza:
                if nazwa in znane.get(k, ()) or not all(c in tokeny[k] for c in czesci):
                    continue
                if odwolanie(nazwa, teksty[k]):
                    return k, nazwa
        return None

    zmiana = True
    while zmiana:
        zmiana = False
        for r in [p for p in poza if p.endswith('.cs')]:
            uzycie = kto_uzywa(r)
            if uzycie:
                log(f'BRAXHORN_KLUCZ UWAGA: {r} (kod drugiego rejonu) użyty w {uzycie[0]} ({uzycie[1]}) — wraca do klucza rejonu {rejon}')
                poza = [p for p in poza if p not in (r, r + '.meta')]
                w_kluczu += [p for p in (r, r + '.meta') if p in wszystkie and p not in w_kluczu]
                zmiana = True
    w_kluczu.sort()
    calosc, grupy = hashlib.sha256(), {}
    for s in (f'wersja={WERSJA}', f'rejon={rejon}', f'platforma={platforma}', f'unity={unity}'):
        calosc.update((s + '\n').encode())
    skrypt = pathlib.Path(__file__).resolve()
    calosc.update(('skrypt=' + sha(skrypt) + '\n').encode())
    for r in w_kluczu:
        p = uc / r
        s = sha(p)
        calosc.update(f'{r}\0{s}\n'.encode())
        g = grupy.setdefault(grupa(r), [hashlib.sha256(), 0, 0])
        g[0].update(f'{r}\0{s}\n'.encode())
        g[1] += 1
        g[2] += p.stat().st_size if not p.is_symlink() else 0
    for nazwa, (h, n, b) in sorted(grupy.items()):
        log(f'BRAXHORN_KLUCZ grupa={nazwa} plikow={n} MB={b / 1048576:.1f} sha={h.hexdigest()[:12]}')
    log(f'BRAXHORN_KLUCZ poza kluczem (generator drugiego rejonu): {len([p for p in poza if p.endswith(".cs")])} plików .cs')
    return f'braxhorn-rejon-v{WERSJA}-{rejon}-{platforma}-{calosc.hexdigest()[:40]}'


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument('--platforma', required=True, choices=('android', 'webgl'))
    ap.add_argument('--unity', required=True)
    a = ap.parse_args()
    uc = pathlib.Path('unity-city').resolve()
    if not (uc / 'Assets').is_dir():
        log('BRAXHORN_KLUCZ BŁĄD: brak unity-city/Assets (uruchom z katalogu głównego repozytorium)')
        return 1
    rejon, pamiec, zrodlo = wybierz_rejon(uc)
    z_pamiecia = (os.environ.get('PAMIEC_REJONOW') or 'osiedle').lower().split()
    k = klucz(uc, rejon, a.platforma, a.unity) if rejon != 'oba' else ''
    uzyj = 'tak' if rejon != 'oba' and pamiec and rejon in z_pamiecia else 'nie'
    (uc / 'rejon-ci.txt').write_text(f'rejon={rejon}\npamiec={uzyj}\nklucz={k}\n', encoding='utf-8')
    log(f'BRAXHORN_REJON {rejon} (źródło: {zrodlo}), platforma {a.platforma}, pamięć rejonu: {uzyj}'
        + ('' if rejon in z_pamiecia or rejon == 'oba' else f' (rejon spoza PAMIEC_REJONOW={" ".join(z_pamiecia)})')
        + ('' if pamiec else ' (wymuszone pełne generowanie)'))
    if k:
        log(f'BRAXHORN_KLUCZ rejon={rejon} platforma={a.platforma} klucz={k}')
    wy = os.environ.get('GITHUB_OUTPUT')
    if wy:
        with open(wy, 'a', encoding='utf-8') as f:
            f.write(f'rejon={rejon}\npamiec={uzyj}\nklucz={k or "brak"}\n')
    return 0


if __name__ == '__main__':
    sys.exit(main())
