#!/usr/bin/env python3
"""LUDZIE NA OSIEDLU (build 14, zlecenie „ludzie”): wybrany podzbiór paczki Microsoft Rocketbox (licencja MIT,
github.com/microsoft/Microsoft-Rocketbox, zatwierdzony commit 0943055d…, BEZ LFS — zwykłe pliki git).

Pobiera TYLKO potrzebne pliki (git: częściowy klon bez blobów + sparse checkout po ścieżkach, partiami; awaryjnie
pojedyncze pliki z raw.githubusercontent.com tego samego commitu):
  • 24 postacie dorosłych (FBX bez mimiki — szkielet Bip01 z kośćmi twarzy, 81 kości, ok. 7,4 tys. trójkątów),
  • tekstury *_color / *_normal (bez specular i „wrinkle”) — TGA 2048 → PNG: kolor i włosy 1024, normalne 512
    (telefon; Unity i tak kompresuje — BraxRocketboxImport),
  • ok. 60 animacji (chód, bieg, postój, rozmowa z gestami, telefon, picie, siedzenie na krześle / przy stole, doping,
    klaskanie, taniec, praca za ladą).
MASKA UBRAŃ (dyspozytor 01:28 — zróżnicowanie ludzi bez kopii materiałów): w kanale alfa tekstury *_body_color
zapisujemy 1 = ubranie, 0 = skóra. Skóra = piksele o barwie (Cb, Cr) bliskiej barwy twarzy z *_head_color tej samej
postaci (środek twarzy), zamknięcie/otwarcie morfologiczne + margines 3 px, lekkie rozmycie. Shader postaci
(Braxhorn/LudzieUbranie) przebarwia tylko ubrania (odcień z MaterialPropertyBlock, BraxWyglad). Bez numpy → bez maski
(tekstura RGB) → odcień wyłączony dla tej postaci.

Wynik: unity-city/Assets/Source/Rocketbox/{Avatars/<Nazwa>/<Nazwa>.fbx, Avatars/<Nazwa>/Textures/*.png,
Animations/<klip>.fbx, LICENSE.md, CREDITS.md, manifest.json}. Folder jest w .gitignore (pobierany w CI).
Uruchamiane w CI przed autorzy.py (CREDITS.md trafia na ekran AUTORZY) i przed Unity. Błąd = brak ludzi Rocketbox
(generator wraca wtedy do przechodniów z build 13) — nie przerywa builda.
"""
import json, os, shutil, subprocess, sys, time, urllib.request
from pathlib import Path

REPO = "https://github.com/microsoft/Microsoft-Rocketbox.git"
COMMIT = "0943055db6ec570bcef9f2c8b41c9e5467c808f9"
RAW = "https://raw.githubusercontent.com/microsoft/Microsoft-Rocketbox/" + COMMIT + "/"
ROOT = Path(__file__).resolve().parents[2]
DST = ROOT / "unity-city" / "Assets" / "Source" / "Rocketbox"
TMP = Path(os.environ.get("RUNNER_TEMP", "/tmp")) / "rocketbox-src"
WERSJA = 3            # zmiana = ponowne pobranie (manifest.json)
KOLOR, NORMALNA = 1024, 512

# postacie: (kategoria/nazwa, tekstury *_color / *_normal — lista do trybu awaryjnego z raw.githubusercontent.com)
POSTACIE = {
    "Adults/Male_Adult_01": ['m002_body_color.tga', 'm002_body_normal.tga', 'm002_head_color.tga', 'm002_head_normal.tga', 'm002_opacity_color.tga'],
    "Adults/Male_Adult_03": ['m004_body_color.tga', 'm004_body_normal.tga', 'm004_head_color.tga', 'm004_head_normal.tga', 'm004_opacity_color.tga'],
    "Adults/Male_Adult_05": ['m009_body_color.tga', 'm009_body_normal.tga', 'm009_head_color.tga', 'm009_head_normal.tga', 'm009_opacity_color.tga'],
    "Adults/Male_Adult_08": ['m014_body_color.tga', 'm014_body_normal.tga', 'm014_head_color.tga', 'm014_head_normal.tga', 'm014_opacity_color.tga'],
    "Adults/Male_Adult_11": ['m001_body_color.tga', 'm001_body_normal.tga', 'm001_head_color.tga', 'm001_head_normal.tga'],
    "Adults/Male_Adult_14": ['m012_body_color.tga', 'm012_body_normal.tga', 'm012_head_color.tga', 'm012_head_normal.tga'],
    "Adults/Female_Adult_01": ['f001_body_color.tga', 'f001_body_normal.tga', 'f001_head_color.tga', 'f001_head_normal.tga', 'f001_opacity_color.tga'],
    "Adults/Female_Adult_04": ['f004_body_color.tga', 'f004_body_normal.tga', 'f004_head_color.tga', 'f004_head_normal.tga', 'f004_opacity_color.tga'],
    "Adults/Female_Adult_07": ['f007_body_color.tga', 'f007_body_normal.tga', 'f007_head_color.tga', 'f007_head_normal.tga', 'f007_opacity_color.tga'],
    "Adults/Female_Adult_10": ['f201_body_color.tga', 'f201_body_normal.tga', 'f201_head_color.tga', 'f201_head_normal.tga', 'f201_opacity_color.tga'],
    "Adults/Female_Adult_13": ['f019_body_color.tga', 'f019_body_normal.tga', 'f019_head_color.tga', 'f019_head_normal.tga', 'f019_opacity_color.tga'],
    "Adults/Female_Party_01": ['f010_body_color.tga', 'f010_body_normal.tga', 'f010_head_color.tga', 'f010_head_normal.tga', 'f010_opacity_color.tga'],
    "Adults/Female_Party_02": ['f022_body_color.tga', 'f022_body_normal.tga', 'f022_head_color.tga', 'f022_head_normal.tga', 'f022_opacity_color.tga'],
    "Professions/Business_Male_01": ['m005_body_color.tga', 'm005_body_normal.tga', 'm005_head_color.tga', 'm005_head_normal.tga', 'm005_opacity_color.tga'],
    "Professions/Business_Female_01": ['f014_body_color.tga', 'f014_body_normal.tga', 'f014_head_color.tga', 'f014_head_normal.tga', 'f014_opacity_color.tga'],
    "Professions/Sports_Male_01": ['m021_body_color.tga', 'm021_body_normal.tga', 'm021_head_color.tga', 'm021_head_normal.tga'],
    "Professions/Sports_Female_01": ['f021_body_color.tga', 'f021_body_normal.tga', 'f021_head_color.tga', 'f021_head_normal.tga'],
    "Professions/Construction_Male_01": ['m111_body_color.tga', 'm111_body_normal.tga', 'm111_head_color.tga', 'm111_head_normal.tga', 'm111_helmet_color.tga', 'm111_helmet_normal.tga', 'm111_opacity_color.tga', 'm111_opacity_normal.tga', 'm111_tools_color.tga', 'm111_tools_normal.tga'],
    "Professions/Police_Male_01": ['m220_body_color.tga', 'm220_body_normal.tga', 'm220_equipment_color.tga', 'm220_equipment_normal.tga', 'm220_head_color.tga', 'm220_head_normal.tga', 'm220_pistol_color.tga', 'm220_pistol_normal.tga'],
    "Professions/Police_Male_04": ['m173_body_color.tga', 'm173_body_normal.tga', 'm173_equipment_color.tga', 'm173_equipment_normal.tga', 'm173_head_color.tga', 'm173_head_normal.tga', 'm173_pistol_color.tga', 'm173_pistol_normal.tga'],
    "Professions/Medical_Male_01": ['m152_body_color.tga', 'm152_body_normal.tga', 'm152_head_color.tga', 'm152_head_normal.tga', 'm152_opacity_color.tga'],
    "Professions/Medical_Female_01": ['f152_body_color.tga', 'f152_body_normal.tga', 'f152_head_color.tga', 'f152_head_normal.tga', 'f152_opacity_color.tga'],
    "Professions/Security_Male_01": ['m171_body_color.tga', 'm171_body_normal.tga', 'm171_equipment_color.tga', 'm171_equipment_normal.tga', 'm171_head_color.tga', 'm171_head_normal.tga'],
    "Professions/Chef_Female_01": ['f110_body_color.tga', 'f110_body_normal.tga', 'f110_head_color_a.tga', 'f110_head_normal_a.tga'],
}
# animacje: folder paczki (static = w miejscu, xy = z przesunięciem w poziomie — chód/bieg, generator robi „w miejscu”)
ANIMACJE = {
    "xy": "m_walk_neutral_01 m_walk_neutral_02 m_walk_stroll_01 f_walk_neutral_01 f_walk_neutral_02 f_walk_stroll_01 "
          "m_run_neutral_01 f_run_neutral_01",
    "static": "m_idle_neutral_01 m_idle_neutral_02 m_idle_neutral_03 m_idle_look_around_01 m_idle_waiting_01 "
              "f_idle_neutral_01 f_idle_neutral_02 f_idle_neutral_03 f_idle_look_around_01 f_idle_waiting_01 "
              "m_idle_scratch_head_01 f_idle_scratch_head_01 m_idle_cough_01 f_idle_cough_01 m_idle_angry_01 f_idle_angry_01 "
              "m_idle_nervous_01 f_idle_nervous_01 "
              "m_gestic_talk_neutral_01 m_gestic_talk_neutral_02 m_gestic_talk_relaxed_01 m_gestic_listen_neutral_01 "
              "m_gestic_listen_neutral_02 m_gestic_laugh_low f_gestic_talk_neutral_01 f_gestic_talk_neutral_02 "
              "f_gestic_talk_relaxed_01 f_gestic_listen_neutral_01 f_gestic_listen_neutral_02 f_gestic_laugh_low "
              "m_cell_phone_talk_01 m_cell_phone_textmessage f_cell_phone_textmessage m_drink_drinking f_drink_drinking "
              "m_sit_chair_breathe_01 m_sit_chair_idle_neutral_02 f_sit_chair_breathe_01 f_sit_chair_idle_neutral_02 "
              "m_sit_table_breathe_01 f_sit_table_breathe_01 m_cheer_01 f_cheer_01 m_claphands_01 f_claphands_01 "
              "m_dancing_cool m_dancing_neutral f_dancing_cool f_dancing_neutral f_dancing_silly m_work_mid f_work_mid "
              "m_documents_idle f_documents_idle",
}


def log(*a):
    print("BRAXHORN_ROCKETBOX", *a, flush=True)


def git(*args, cwd=None, check=True):
    return subprocess.run(["git", *args], cwd=cwd, check=check, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)


def sciezki_postaci(klucz):
    kat, nazwa = klucz.split("/")
    base = f"Assets/Avatars/{kat}/{nazwa}"
    return [f"/{base}/Export/{nazwa}.fbx", f"/{base}/Textures/*_color*.tga", f"/{base}/Textures/*_normal*.tga",
            f"!/{base}/Textures/*wrinkle*"]


def sciezki_animacji():
    out = []
    for folder, nazwy in ANIMACJE.items():
        for n in nazwy.split():
            out.append(f"/Assets/Animations/all_animations_max_motextr_{folder}/{n}.max.fbx")
    return out


# ---------------- obróbka ----------------
def maska_ubran(body, head):
    """Alfa 255 = ubranie, 0 = skóra (barwa twarzy z tekstury głowy). None, gdy brak numpy."""
    try:
        import numpy as np
        from PIL import Image, ImageFilter
    except Exception as e:
        log("bez maski ubran (brak numpy/Pillow):", e)
        return None
    hd = head.convert("RGB").resize((256, 256))
    hy = np.asarray(hd.convert("YCbCr")).astype(np.float32)[64:144, 90:166]
    hh = np.asarray(hd.convert("HSV")).astype(np.float32)[64:144, 90:166]
    ok = (hh[..., 1] > 30) & (hh[..., 2] > 40)
    if ok.sum() < 200:
        return None
    cb, cr = float(np.median(hy[..., 1][ok])), float(np.median(hy[..., 2][ok]))
    y = np.asarray(body.convert("YCbCr")).astype(np.float32)
    d = np.sqrt((y[..., 1] - cb) ** 2 + (y[..., 2] - cr) ** 2)
    skin = ((d < 11) & (y[..., 0] > 25)).astype(np.uint8) * 255
    sk = Image.fromarray(skin)
    sk = sk.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.MinFilter(9))      # zamknięcie: dziury w skórze
    sk = sk.filter(ImageFilter.MinFilter(5)).filter(ImageFilter.MaxFilter(5))      # otwarcie: plamki na ubraniu
    sk = sk.filter(ImageFilter.MaxFilter(7))                                        # margines od skóry
    return Image.fromarray(255 - np.asarray(sk)).filter(ImageFilter.GaussianBlur(1.2))


def png(src: Path, dst: Path, rozmiar: int, alfa: bool, maska=None):
    dst.parent.mkdir(parents=True, exist_ok=True)
    try:
        from PIL import Image
        im = Image.open(src)
        im = im.convert("RGBA" if alfa else "RGB")
        if im.size[0] > rozmiar:
            im = im.resize((rozmiar, rozmiar), Image.LANCZOS)
        if maska is not None and not alfa:
            im = im.convert("RGBA"); im.putalpha(maska.resize(im.size))
        im.save(dst, optimize=False)
        return True
    except ImportError:
        pass
    # bez Pillow: ImageMagick (jest na ubuntu-latest), bez maski
    r = subprocess.run(["convert", str(src), "-resize", f"{rozmiar}x{rozmiar}>", str(dst)])
    return r.returncode == 0


def obrob_postac(klucz, zrodlo: Path):
    kat, nazwa = klucz.split("/")
    base = zrodlo / "Assets" / "Avatars" / kat / nazwa
    fbx = base / "Export" / f"{nazwa}.fbx"
    if not fbx.exists():
        log("BRAK", fbx); return None
    cel = DST / "Avatars" / nazwa
    (cel / "Textures").mkdir(parents=True, exist_ok=True)
    shutil.copy2(fbx, cel / f"{nazwa}.fbx")
    tex = sorted((base / "Textures").glob("*.tga")) if (base / "Textures").exists() else []
    tex = [t for t in tex if ("_color" in t.name or "_normal" in t.name) and "wrinkle" not in t.name]
    head = next((t for t in tex if "_head_color" in t.name), None)
    maski = 0
    for t in tex:
        n = t.stem.lower()
        normal = "_normal" in n
        alfa = "opacity" in n and not normal
        maska = None
        if "_body_color" in n and head is not None:
            try:
                from PIL import Image
                maska = maska_ubran(Image.open(t).convert("RGB").resize((KOLOR, KOLOR)), Image.open(head))
            except Exception as e:
                log("maska", t.name, e)
            if maska is not None: maski += 1
        png(t, cel / "Textures" / (t.stem + ".png"), NORMALNA if normal else KOLOR, alfa, maska)
    return {"nazwa": nazwa, "kategoria": kat, "tekstury": len(tex), "maska_ubran": maski > 0}


def obrob_animacje(zrodlo: Path):
    n = 0
    for folder, nazwy in ANIMACJE.items():
        for a in nazwy.split():
            src = zrodlo / "Assets" / "Animations" / f"all_animations_max_motextr_{folder}" / f"{a}.max.fbx"
            if not src.exists():
                log("BRAK animacji", a); continue
            (DST / "Animations").mkdir(parents=True, exist_ok=True)
            shutil.copy2(src, DST / "Animations" / f"{a}.fbx"); n += 1
    return n


# ---------------- pobieranie ----------------
def klon():
    if TMP.exists(): shutil.rmtree(TMP)
    TMP.mkdir(parents=True)
    git("init", "-q", cwd=TMP)
    git("remote", "add", "origin", REPO, cwd=TMP)
    git("config", "core.sparseCheckout", "true", cwd=TMP)
    git("sparse-checkout", "init", "--no-cone", cwd=TMP)
    git("sparse-checkout", "set", "--no-cone", "/LICENSE.md", cwd=TMP)
    # sam commit i drzewa (bez zawartości plików); bloby dociąga checkout tylko dla wybranych ścieżek
    git("fetch", "-q", "--depth", "1", "--filter=blob:none", "origin", COMMIT, cwd=TMP)
    git("checkout", "-q", COMMIT, cwd=TMP)


def partia(wzorce):
    git("sparse-checkout", "set", "--no-cone", "/LICENSE.md", *wzorce, cwd=TMP)


def raw(sciezka: str, cel: Path):
    cel.parent.mkdir(parents=True, exist_ok=True)
    for proba in range(4):
        try:
            with urllib.request.urlopen(RAW + sciezka, timeout=120) as r, open(cel, "wb") as f:
                shutil.copyfileobj(r, f, 1 << 20)
            return True
        except Exception as e:
            log("raw", sciezka, "próba", proba + 1, e); time.sleep(3 * (proba + 1))
    return False


def main():
    t0 = time.time()
    man = DST / "manifest.json"
    if man.exists():
        try:
            if json.loads(man.read_text()).get("wersja") == WERSJA:
                log("już pobrane (manifest)"); return 0
        except Exception:
            pass
    if DST.exists(): shutil.rmtree(DST)
    DST.mkdir(parents=True)
    postacie, tryb = [], "git"
    klucze = list(POSTACIE.keys())
    try:
        klon()
        # partie po 6 postaci: mało miejsca na dysku naraz (TGA 2048 ≈ 12–16 MB sztuka)
        for i in range(0, len(klucze), 6):
            grupa = klucze[i:i + 6]
            partia([w for k in grupa for w in sciezki_postaci(k)])
            for k in grupa:
                p = obrob_postac(k, TMP)
                if p: postacie.append(p)
        partia(sciezki_animacji())
        anim = obrob_animacje(TMP)
        shutil.copy2(TMP / "LICENSE.md", DST / "LICENSE.md")
    except Exception as e:
        out = getattr(e, "output", "") or ""
        log("git sparse nieudany — pobieram pojedyncze pliki:", e, out[-400:])
        tryb = "raw"; postacie = []
        if TMP.exists(): shutil.rmtree(TMP)
        TMP.mkdir(parents=True)
        for k, tex in POSTACIE.items():
            kat, nazwa = k.split("/")
            base = f"Assets/Avatars/{kat}/{nazwa}"
            if not raw(f"{base}/Export/{nazwa}.fbx", TMP / base / "Export" / f"{nazwa}.fbx"): continue
            for t in tex: raw(f"{base}/Textures/{t}", TMP / base / "Textures" / t)
            p = obrob_postac(k, TMP)
            if p: postacie.append(p)
            shutil.rmtree(TMP / base, ignore_errors=True)
        for folder, nazwy in ANIMACJE.items():
            for a in nazwy.split():
                s = f"Assets/Animations/all_animations_max_motextr_{folder}/{a}.max.fbx"; raw(s, TMP / s)
        anim = obrob_animacje(TMP)
        if not raw("LICENSE.md", DST / "LICENSE.md"):
            (DST / "LICENSE.md").write_text("MIT License\n\nCopyright (c) 2020 Microsoft\n", encoding="utf-8")
    finally:
        shutil.rmtree(TMP, ignore_errors=True)
    (DST / "CREDITS.md").write_text(
        "# Ludzie — Microsoft Rocketbox\n\n"
        "Postacie i animacje: Microsoft Rocketbox Avatar Library — Microsoft, licencja MIT "
        "(https://github.com/microsoft/Microsoft-Rocketbox, commit " + COMMIT[:12] + "). Pełny tekst licencji: LICENSE.md.\n"
        "Zmiany: tekstury zmniejszone (kolor 1024 px, normalne 512 px), w kanale alfa tekstur ciała maska ubrań "
        "(przebarwianie ubrań w grze); animacje chodu i biegu w grze „w miejscu” (ruch postaci prowadzi skrypt).\n",
        encoding="utf-8")
    man.write_text(json.dumps({"wersja": WERSJA, "commit": COMMIT, "tryb": tryb, "postacie": postacie,
                               "animacje": anim}, ensure_ascii=False, indent=1), encoding="utf-8")
    mb = sum(f.stat().st_size for f in DST.rglob("*") if f.is_file()) / 1e6
    log(f"postacie={len(postacie)}/{len(POSTACIE)} animacje={anim} maski_ubran={sum(1 for p in postacie if p['maska_ubran'])} "
        f"tryb={tryb} rozmiar={mb:.0f}MB czas={time.time() - t0:.0f}s")
    return 0 if postacie else 1


if __name__ == "__main__":
    sys.exit(main())
