#!/usr/bin/env python3
"""PRYWATNE ZASOBY WŁAŚCICIELA w publicznym repo — tylko w postaci zaszyfrowanej (decyzja właściciela 1.11, 23:54).

Paczki: unity-city/Prywatne/<nazwa>.tar.gz.enc  (tar.gz → openssl enc -aes-256-cbc -pbkdf2 -iter 200000 -md sha256 -salt)
Hasło: sekret repozytorium BRAXHORN_KEYSTORE_PASS (w CI zmienna KLUCZ). Nigdy w repo, w logach ani w raportach.

  patryk             model Patryka (FBX z Tripo, szkielet Mixamo 65 kości; tekstury 4K WBUDOWANE w FBX)
  nyxa               model Nyxy (FBX z Tripo, tekstury wbudowane)
  ruchy_brax         surowe animacje Mixamo z dawnego dev/anims/{brax,extra,extra2,gym} (65 FBX)
  ruchy_wlasciciela  paczki ruchów właściciela: nyxa/, xd/, nina/ (FBX bez skóry)
  nina_1..3          trzy FBX Mixamo „ze skórą” z paczki nina (każdy ~32 MB, osobno — limit GitHuba 100 MB na plik)
  miasto_*           modele ze Sketchfaba do miasta (etap „assety” 2.11; licencje Free Standard/CC-BY — surowych plików nie wolno
                     rozpowszechniać): miasto_centrum (city5 — wieże), miasto_kamienice (city3), miasto_wiezowce (Atlanta, London,
                     Kinnaird), miasto_wyposazenie (TampaJoey), miasto_pojazdy (autobus, tramwaj), miasto_port (statki),
                     miasto_silownia_metro (sprzęt siłowni, bramka metra), miasto_znaki (tarcze znaków 3D, paczka „Traffic Signs —
                     European (UK)” od właściciela 3.11; lico podmieniane na wzór polski w BraxZnakiPL). Już przetworzone (GLB + _tekstury/…), odszyfrowane
                     do Assets/Source/Prywatne/Miasto/<paczka>/. Brak klucza/paczki = miasto bez tych modeli (generator to loguje).

Tryby:
  python3 unity-tools/claude/prywatne.py                       CI przed Unity: odszyfrowuje patryk + ruchy_brax + ruchy_wlasciciela,
                                                               przygotowuje Assets/Source/Prywatne (poza gitem). Bez klucza → Brax.
                                                               Potem paczki miasta (miasto_*) → Assets/Source/Prywatne/Miasto/<paczka>/.
  KLUCZ=… python3 unity-tools/claude/prywatne.py szyfruj NAZWA KATALOG   paczka z zawartości KATALOGU (ścieżki względne)
  KLUCZ=… python3 unity-tools/claude/prywatne.py sprawdz                 odszyfrowanie na próbę + lista plików każdej paczki
  KLUCZ=… python3 unity-tools/claude/prywatne.py rozpakuj NAZWA CEL      odszyfrowanie paczki do katalogu CEL (właściciel)

Build nigdy nie pada przez ten skrypt: każdy błąd = komunikat i gra z Braxem (BRAXHORN_PRYWATNE …).
"""
import hashlib
import io
import os
import pathlib
import shutil
import struct
import subprocess
import sys
import tarfile
import tempfile
import zlib

ROOT = pathlib.Path(__file__).resolve().parents[2]
PACZKI = ROOT / "unity-city" / "Prywatne"
CEL = ROOT / "unity-city" / "Assets" / "Source" / "Prywatne"   # w .gitignore
OPENSSL = ["openssl", "enc", "-aes-256-cbc", "-pbkdf2", "-iter", "200000", "-md", "sha256"]

# Ruchy kopiowane do Unity (reszta zostaje tylko w paczce). MUSI obejmować wszystkie pliki z listy KlipyPatryka
# w unity-city/Assets/Editor/Claude/BraxClaudeOsiedlePatryk.cs (kolejność kandydatów decyduje tam).
RUCHY_DO_UNITY = {
    "brax": ["Idle", "Jab Cross (1)", "Mma Kick (1)", "Jump", "Boxing", "Hook", "Kicking", "Head Hit", "Hit To Body"],
    "extra": ["Walking", "Running", "Dodging", "Center Block"],
    "extra2": ["Walking", "Unarmed Run Forward", "Unarmed Jump Running", "Getting Up"],
    "xd": ["Jab Cross", "Kicking", "Boxing", "Hit To Body", "Center Block", "Blocking"],
    "nyxa": ["Walking", "Mma Kick (1)", "Hook", "Head Hit", "Getting Up", "Dodging", "Right Block"],
}


def klucz():
    k = os.environ.get("KLUCZ", "")
    return k if k.strip() else ""


def szyfruj(nazwa, katalog):
    katalog = pathlib.Path(katalog)
    PACZKI.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory() as tmp:
        tgz = pathlib.Path(tmp) / (nazwa + ".tar.gz")
        with tarfile.open(tgz, "w:gz", compresslevel=9) as tar:
            for p in sorted(katalog.rglob("*")):
                if p.is_file():
                    info = tar.gettarinfo(str(p), arcname=str(p.relative_to(katalog)))
                    info.uid = info.gid = 0
                    info.uname = info.gname = ""
                    info.mode = 0o644
                    with open(p, "rb") as f:
                        tar.addfile(info, f)
        out = PACZKI / (nazwa + ".tar.gz.enc")
        subprocess.run(OPENSSL + ["-salt", "-in", str(tgz), "-out", str(out), "-pass", "env:KLUCZ"], check=True)
    print(f"{out.relative_to(ROOT)}: {out.stat().st_size / 1048576:.1f} MB")


def odszyfruj(nazwa):
    """Zwraca otwarty tarfile albo None (zły klucz / brak paczki). Hasło tylko przez zmienną środowiska."""
    f = PACZKI / (nazwa + ".tar.gz.enc")
    if not f.exists():
        print(f"BRAXHORN_PRYWATNE brak paczki {nazwa}")
        return None
    r = subprocess.run(OPENSSL + ["-d", "-in", str(f), "-pass", "env:KLUCZ"], capture_output=True)
    if r.returncode != 0 or not r.stdout:
        print(f"BRAXHORN_PRYWATNE zly klucz albo uszkodzona paczka: {nazwa}")
        return None
    try:
        return tarfile.open(fileobj=io.BytesIO(r.stdout), mode="r:gz")
    except tarfile.TarError as e:
        print(f"BRAXHORN_PRYWATNE paczka {nazwa} nie jest poprawnym tar.gz: {e}")
        return None


def rozpakuj(tar, cel):
    cel = pathlib.Path(cel).resolve()
    n = 0
    for m in tar.getmembers():
        if not m.isfile():
            continue
        dst = (cel / m.name).resolve()
        if cel not in dst.parents:          # tylko ścieżki wewnątrz celu
            continue
        dst.parent.mkdir(parents=True, exist_ok=True)
        with tar.extractfile(m) as src, open(dst, "wb") as out:
            shutil.copyfileobj(src, out)
        n += 1
    return n


# --- tekstury wbudowane w FBX (Tripo zapisuje 4 obrazy w węzłach Video/Content) ---------------------------------
def fbx_tekstury(path):
    d = open(path, "rb").read()
    if not d.startswith(b"Kaydara FBX Binary"):
        return {}
    ver = struct.unpack_from("<I", d, 23)[0]
    wide = ver >= 7500
    out = {}

    def prop(o):
        t = chr(d[o]); o += 1
        size = {"Y": 2, "C": 1, "I": 4, "F": 4, "D": 8, "L": 8}
        if t in size:
            return None, o + size[t]
        if t in "SR":
            n = struct.unpack_from("<I", d, o)[0]
            return d[o + 4:o + 4 + n], o + 4 + n
        if t in "fdlib":
            _, _, cl = struct.unpack_from("<III", d, o)
            return None, o + 12 + cl
        raise ValueError(t)

    def node(o, rodzic, stan):
        if wide:
            end, npr, pl = struct.unpack_from("<QQQ", d, o); o += 24
        else:
            end, npr, pl = struct.unpack_from("<III", d, o); o += 12
        nl = d[o]; o += 1
        name = d[o:o + nl].decode("latin1"); o += nl
        if end == 0:
            return None
        props = []; po = o
        for _ in range(npr):
            v, po = prop(po); props.append(v)
        o += pl
        if name == "Video":
            stan = {}
        if rodzic == "Video" and name in ("RelativeFilename", "Filename", "Content") and props and props[0]:
            stan[name] = props[0]
        while o < end:
            r = node(o, name, stan)
            if r is None:
                o += 25 if wide else 13
                break
            o = r
        if name == "Video" and stan.get("Content"):
            fn = (stan.get("RelativeFilename") or stan.get("Filename") or b"tekstura.png").decode("latin1", "ignore")
            out[fn.replace("\\", "/").split("/")[-1]] = stan["Content"]
        return end

    off = 27
    while off < len(d) - 200:
        r = node(off, "", {})
        if r is None:
            break
        off = r
    return out


def przygotuj_patryka(staging):
    from PIL import Image  # Pillow instalowany w kroku „Prepare unchanged player…”
    fbx = sorted((staging / "patryk").glob("*.fbx"))
    if not fbx:
        print("BRAXHORN_PRYWATNE paczka patryk bez FBX")
        return False
    dst = CEL / "patryk"
    dst.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(fbx[0], dst / "Patryk.fbx")
    obrazy = fbx_tekstury(fbx[0])
    print("BRAXHORN_PRYWATNE tekstury wbudowane w FBX Patryka:", ", ".join(f"{k} {len(v) / 1048576:.1f} MB" for k, v in obrazy.items()))

    def znajdz(*slowa):
        for k, v in obrazy.items():
            if any(s in k.lower() for s in slowa):
                return Image.open(io.BytesIO(v))
        return None

    def do2k(im):
        if max(im.size) > 2048:
            im = im.resize((2048, 2048) if im.size[0] == im.size[1] else (min(2048, im.size[0]), min(2048, im.size[1])), Image.Resampling.LANCZOS)
        return im

    kolor = znajdz("basecolor", "color", "diffuse", "albedo")
    if kolor is None:
        print("BRAXHORN_PRYWATNE brak tekstury koloru Patryka — Brax")
        shutil.rmtree(dst, ignore_errors=True)
        return False
    do2k(kolor.convert("RGB")).save(dst / "Patryk_BaseColor.png", optimize=False)
    normal = znajdz("normal")
    if normal is not None:
        do2k(normal.convert("RGB")).save(dst / "Patryk_Normal.png")
    # ORM jak u Braxa (shader Braxhorn/HeroPremium): R = okluzja (brak mapy → 1), G = chropowatość, B = metaliczność
    rough = znajdz("rough")
    metal = znajdz("metal")
    rozmiar = (2048, 2048)
    g = do2k(rough.convert("L")).resize(rozmiar, Image.Resampling.LANCZOS) if rough is not None else Image.new("L", rozmiar, 214)
    b = do2k(metal.convert("L")).resize(rozmiar, Image.Resampling.LANCZOS) if metal is not None else Image.new("L", rozmiar, 0)
    Image.merge("RGB", (Image.new("L", rozmiar, 255), g, b)).save(dst / "Patryk_ORM.png")
    print(f"BRAXHORN_PRYWATNE Patryk: FBX {fbx[0].stat().st_size / 1048576:.1f} MB, tekstury 2K: kolor, "
          f"normal={'tak' if normal is not None else 'brak'}, ORM (chropowatość={'mapa' if rough is not None else 'stała'}, "
          f"metal={'mapa' if metal is not None else '0'})")
    return True


# Paczki modeli miasta (etap „assety”): każda osobno (< 90 MB po kompresji — limit GitHuba 100 MB na plik).
PACZKI_MIASTA = ("miasto_centrum", "miasto_kamienice", "miasto_wiezowce", "miasto_wyposazenie", "miasto_pojazdy",
                 "miasto_port", "miasto_silownia_metro", "miasto_znaki")


def miasto():
    """Paczki modeli miasta → Assets/Source/Prywatne/Miasto/<paczka>/. Każda osobno; błąd jednej nie rusza pozostałych
    ani Patryka. Nigdy nie wywraca buildu (miasto buduje się wtedy bez modelu i loguje BRAXHORN_MIASTO_ASSETY brak …)."""
    if not klucz():
        print("BRAXHORN_PRYWATNE miasto: brak klucza — miasto bez modeli Sketchfab (drogi i kwartały i tak powstają)")
        return
    ok, brak = [], []
    for nazwa in PACZKI_MIASTA:
        cel = CEL / "Miasto" / nazwa
        try:
            tar = odszyfruj(nazwa)
            if tar is None:
                brak.append(nazwa)
                continue
            with tar:
                n = rozpakuj(tar, cel)
            mb = sum(p.stat().st_size for p in cel.rglob("*") if p.is_file()) / 1048576
            glb = len(list(cel.glob("*.glb")))
            ok.append(f"{nazwa}({n} plików, {glb} glb, {mb:.1f} MB)")
            if glb == 0:
                print(f"BRAXHORN_PRYWATNE miasto: {nazwa} bez .glb — DO POPRAWKI")
        except Exception as e:   # build idzie dalej bez tej paczki
            print(f"BRAXHORN_PRYWATNE miasto: {nazwa} DO POPRAWKI: {type(e).__name__}: {e}")
            shutil.rmtree(cel, ignore_errors=True)
            brak.append(nazwa)
    print(f"BRAXHORN_PRYWATNE miasto: odszyfrowane={len(ok)}/{len(PACZKI_MIASTA)} [{', '.join(ok)}]"
          + (f" brak=[{', '.join(brak)}]" if brak else ""))


def grafiki():
    """Grafiki właściciela (ekran startowy, billboardy; obrazy z postaciami — tylko zaszyfrowane) → Assets/Source/Prywatne/
    Grafiki/ (Resources/BraxStart.jpg = ekran startowy). Nigdy nie wywraca buildu: bez paczki gra startuje bez ekranu."""
    if not klucz():
        print("BRAXHORN_PRYWATNE grafiki: brak klucza — gra bez ekranu startowego")
        return
    cel = CEL / "Grafiki"
    try:
        tar = odszyfruj("grafiki")
        if tar is None:
            return
        with tar:
            n = rozpakuj(tar, cel)
        start = (cel / "Resources" / "BraxStart.jpg").exists()
        print(f"BRAXHORN_PRYWATNE grafiki: {n} plików, ekran startowy={'tak' if start else 'NIE — DO POPRAWKI'}")
    except Exception as e:   # build idzie dalej bez grafik
        print(f"BRAXHORN_PRYWATNE grafiki DO POPRAWKI: {type(e).__name__}: {e}")
        shutil.rmtree(cel, ignore_errors=True)


def ci():
    try:
        patryk_i_ruchy()
    finally:
        try:
            miasto()
        finally:
            grafiki()


def patryk_i_ruchy():
    if not klucz():
        print("BRAXHORN_PRYWATNE brak klucza (sekret BRAXHORN_KEYSTORE_PASS) — build z Braxem")
        return
    staging = pathlib.Path(tempfile.mkdtemp(prefix="braxhorn-prywatne-", dir=os.environ.get("RUNNER_TEMP") or None))
    ok = []
    try:
        for nazwa in ("patryk", "ruchy_brax", "ruchy_wlasciciela"):
            tar = odszyfruj(nazwa)
            if tar is None:
                continue
            with tar:
                n = rozpakuj(tar, staging)
            ok.append(f"{nazwa}({n})")
        patryk = "patryk" in " ".join(ok) and przygotuj_patryka(staging)
        ruchy = 0
        if patryk:
            for grupa, pliki in RUCHY_DO_UNITY.items():
                for p in pliki:
                    src = staging / grupa / (p + ".fbx")
                    if src.exists():
                        d = CEL / "ruchy" / grupa
                        d.mkdir(parents=True, exist_ok=True)
                        shutil.copyfile(src, d / src.name)
                        ruchy += 1
            if ruchy == 0:
                print("BRAXHORN_PRYWATNE brak ruchów — Patryk bez animacji = Brax")
                for sub in ("patryk", "ruchy"):
                    shutil.rmtree(CEL / sub, ignore_errors=True)
                patryk = False
        print(f"BRAXHORN_PRYWATNE odszyfrowane={len(ok)}/3 [{', '.join(ok)}] patryk={'tak' if patryk else 'nie'} ruchy_do_unity={ruchy}")
    except Exception as e:   # build idzie dalej z Braxem
        print(f"BRAXHORN_PRYWATNE DO POPRAWKI: {type(e).__name__}: {e} — build z Braxem")
        for sub in ("patryk", "ruchy"):
            shutil.rmtree(CEL / sub, ignore_errors=True)
    finally:
        shutil.rmtree(staging, ignore_errors=True)


def sprawdz():
    for f in sorted(PACZKI.glob("*.tar.gz.enc")):
        nazwa = f.name[:-len(".tar.gz.enc")]
        tar = odszyfruj(nazwa)
        if tar is None:
            continue
        with tar:
            pliki = [m for m in tar.getmembers() if m.isfile()]
            h = hashlib.sha256()
            for m in pliki:
                h.update(m.name.encode()); h.update(tar.extractfile(m).read())
        print(f"{nazwa}: {f.stat().st_size / 1048576:.1f} MB zaszyfrowane, {len(pliki)} plików, "
              f"{sum(m.size for m in pliki) / 1048576:.1f} MB, sha256(treść)={h.hexdigest()[:16]}")


if __name__ == "__main__":
    a = sys.argv[1:]
    if not a:
        ci()
    elif a[0] in ("szyfruj", "sprawdz", "rozpakuj") and not klucz():
        sys.exit("brak hasła w zmiennej KLUCZ")
    elif a[0] == "szyfruj" and len(a) == 3:
        szyfruj(a[1], a[2])
    elif a[0] == "sprawdz":
        sprawdz()
    elif a[0] == "rozpakuj" and len(a) == 3:
        t = odszyfruj(a[1])
        if t is not None:
            with t:
                print(rozpakuj(t, a[2]), "plików →", a[2])
    else:
        sys.exit(__doc__)
