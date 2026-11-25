#!/usr/bin/env python3
"""MIASTO MISTRZÓW - obrazy z plików .glb jako osobne tekstury, zanim ruszy Unity (tak jak Miasto R1).

Właściciel 25.11.2026 17:19: „Czego nie ruszyłeś sposobu budowania? Jak ja ci pokazałem, jak twój kolega robi … Wszystko
jest w kompresji w grze?” - nie było. glTFast 6.20 robi z obrazu zaszytego w .glb teksturę BEZ kompresji (RGBA32): w
#52 250 takich obrazów, ~1,7 GB (łodzie portu, statki, metro, basen, ludzie, leki w sklepach). Krok w Unity, który miał je
podmienić (BraxChampionsTeksturyGlb), nie wczytał ani jednego obrazu w #52 i #57, a dwa buildy (#54, #57) zabił przy nim
brak pamięci (exit 137).

Miasto R1 robi to inaczej i to działa (unity-tools/claude/odchudzanie_modeli.py): w CI, na kopii roboczej, PRZED Unity
obraz z bloku BIN pliku .glb (bajt w bajt) zapisuje się jako plik obok modelu, a w .glb zostaje odnośnik (uri). glTFast przy
imporcie w edytorze bierze wtedy zwykły asset tekstury z projektu, a ten Unity kompresuje przy pierwszym imporcie - bez
żadnej pamięci ponad zwykły import i bez kroku po wygenerowaniu sceny. Ten skrypt robi to samo dla Miasta Mistrzów:
  - folder _tekstury_glb_m/<model>/ (inny niż _tekstury_glb R1 - własne ustawienia importu, BraxChampionsGlbOsobnoImport:
    ASTC 6x6, mipmapy, bez kopii do odczytu, na telefonie najwyżej 2048 - ZASADA-1: obraz modelu z biblioteki 1-2K);
  - „__lin” = obraz danych (normalne, metal/chropowatość, okluzja - glTFast czyta je liniowo), „__clamp” / „__mirror” =
    zawijanie z samplera glTF (drzewo Verdant ma clamp);
  - obrazy WebP/KTX2 i obrazy już zewnętrzne zostają jak są; oryginały Tripo z paczki klasyków (…/oryginal/) też (nie są w
    grze);
  - oryginały w repozytorium się nie zmieniają (CI pracuje na kopii); raport: unity-city/mistrzowie-tekstury-glb.json
    (BraxChampionsGlbOsobno sprawdza po imporcie, że materiały modeli mają teksturę z projektu, a nie z .glb).
"""
import json
import re
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "unity-city" / "Assets"
RAPORT = ROOT / "unity-city" / "mistrzowie-tekstury-glb.json"
FOLDER = "_tekstury_glb_m"
# every folder whose .glb the Champions scene uses (#57: 45 models, 296 images); the whole tree of each
KATALOGI = [
    "Source/HarborBeach/Models",
    "Source/Claude/Metro",
    "Source/Claude/Library",
    "Source/CityRebuild/landscape",
    "Source/Mistrzowie",
    "Source/Prywatne/Miasto",
]
POMIN = ("/oryginal/", "/" + FOLDER + "/", "/_tekstury_glb/")
GLB_MAGIC, CH_JSON, CH_BIN = 0x46546C67, 0x4E4F534A, 0x004E4942
CLAMP, MIRROR = 33071, 33648


def bezpieczna(s):
    s = re.sub(r"[^A-Za-z0-9_-]+", "_", s).strip("_")
    return s[:60] or "model"


def czytaj(path):
    data = path.read_bytes()
    if len(data) < 20:
        raise ValueError("za krótki plik")
    magic, _ver, _len = struct.unpack_from("<III", data, 0)
    if magic != GLB_MAGIC:
        raise ValueError("to nie jest .glb")
    off, chunks = 12, []
    while off + 8 <= len(data):
        clen, ctype = struct.unpack_from("<II", data, off)
        chunks.append([ctype, data[off + 8: off + 8 + clen]])
        off += 8 + clen
    if not chunks or chunks[0][0] != CH_JSON:
        raise ValueError("brak bloku JSON")
    return json.loads(chunks[0][1].decode("utf-8").rstrip(" \x00")), chunks


def zapisz(path, js, chunks):
    raw = json.dumps(js, separators=(",", ":"), ensure_ascii=False).encode("utf-8")
    raw += b" " * ((4 - len(raw) % 4) % 4)
    chunks[0][1] = raw
    body = b""
    for ctype, cdata in chunks:
        cdata = cdata + (b"\x00" * ((4 - len(cdata) % 4) % 4))
        body += struct.pack("<II", len(cdata), ctype) + cdata
    tmp = path.with_name(path.name + ".tmp")
    tmp.write_bytes(struct.pack("<III", GLB_MAGIC, 2, 12 + len(body)) + body)
    tmp.replace(path)


def role(js):
    """image index -> (linear, wrap): linear only when every use is data (normal, metal/roughness, occlusion)"""
    tex = js.get("textures") or []
    samplers = js.get("samplers") or []
    kolor, dane, wrap = set(), set(), {}

    def zrodlo(info):
        if not isinstance(info, dict):
            return None
        i = info.get("index")
        if not isinstance(i, int) or i < 0 or i >= len(tex):
            return None
        t = tex[i]
        s = t.get("source")
        for ext in (t.get("extensions") or {}).values():
            if isinstance(ext, dict) and ext.get("source") is not None and s is None:
                s = ext["source"]
        if s is not None and s not in wrap:
            sm = t.get("sampler")
            w = samplers[sm].get("wrapS", 10497) if isinstance(sm, int) and 0 <= sm < len(samplers) else 10497
            wrap[s] = "__clamp" if w == CLAMP else "__mirror" if w == MIRROR else ""
        return s

    for m in js.get("materials") or []:
        pbr = m.get("pbrMetallicRoughness") or {}
        for info in (pbr.get("baseColorTexture"), m.get("emissiveTexture")):
            s = zrodlo(info)
            if s is not None:
                kolor.add(s)
        for info in (pbr.get("metallicRoughnessTexture"), m.get("normalTexture"), m.get("occlusionTexture")):
            s = zrodlo(info)
            if s is not None:
                dane.add(s)
        for ext in (m.get("extensions") or {}).values():
            if not isinstance(ext, dict):
                continue
            for key, info in ext.items():
                if key.endswith("Texture"):
                    s = zrodlo(info)
                    if s is not None:
                        (kolor if key in ("diffuseTexture", "specularGlossinessTexture", "sheenColorTexture") else dane).add(s)
    n = len(js.get("images") or [])
    return {i: (i in dane and i not in kolor, wrap.get(i, "")) for i in range(n)}


def osobno(glb, rap, assets=ASSETS):
    js, chunks = czytaj(glb)
    images = js.get("images") or []
    binc = next((c for t, c in chunks if t == CH_BIN), None)
    if not images or binc is None:
        return 0
    views = js.get("bufferViews") or []
    rola = role(js)
    stem = bezpieczna(glb.stem)
    out = glb.parent / FOLDER / stem
    n = 0
    for i, img in enumerate(images):
        bv = img.get("bufferView")
        if not isinstance(bv, int) or bv < 0 or bv >= len(views) or img.get("uri"):
            continue
        v = views[bv]
        if v.get("buffer", 0) != 0:
            continue
        a = v.get("byteOffset", 0)
        b = binc[a: a + v["byteLength"]]
        if len(b) != v["byteLength"]:
            continue
        if b[:8] == b"\x89PNG\r\n\x1a\n":
            ext = "png"
        elif b[:3] == b"\xff\xd8\xff":
            ext = "jpg"
        else:
            continue  # WebP / KTX2 / unknown: stays in the .glb
        lin, wrap = rola.get(i, (False, ""))
        out.mkdir(parents=True, exist_ok=True)
        name = "%s_%02d%s%s.%s" % (stem, i, "__lin" if lin else "", wrap, ext)
        (out / name).write_bytes(b)
        img.pop("bufferView", None)
        img["uri"] = "%s/%s/%s" % (FOLDER, stem, name)
        img["mimeType"] = "image/png" if ext == "png" else "image/jpeg"
        n += 1
    if n:
        zapisz(glb, js, chunks)
        rap["glb"].append({"model": glb.relative_to(assets).as_posix(), "obrazy": n})
    return n


def main(argv):
    assets = Path(argv[1]).resolve() if len(argv) > 1 else ASSETS
    raport = Path(argv[2]).resolve() if len(argv) > 2 else RAPORT
    rap = {"glb": [], "bledy": []}
    razem = 0
    for d in KATALOGI:
        p = assets / d
        if not p.is_dir():
            continue
        for glb in sorted(p.rglob("*.glb")):
            rel = "/" + glb.relative_to(assets).as_posix()
            if any(x in rel for x in POMIN) or glb.stem.endswith("__lekki"):
                continue
            try:
                razem += osobno(glb, rap, assets)
            except Exception as e:  # one bad file never stops the build: it stays as glTFast imports it
                rap["bledy"].append("%s: %s" % (rel, e))
    print("BRAX_GLB_OSOBNO obrazy=%d modele=%d bledy=%d" % (razem, len(rap["glb"]), len(rap["bledy"])))
    for b in rap["bledy"]:
        print("DO POPRAWKI BRAX_GLB_OSOBNO", b)
    raport.write_text(json.dumps(rap, ensure_ascii=False, indent=1), encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
