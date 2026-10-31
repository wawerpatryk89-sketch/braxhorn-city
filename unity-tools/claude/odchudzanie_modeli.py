#!/usr/bin/env python3
"""Odchudzanie modeli pod telefon (audyt miasta 31.10, Opus). Uruchamiane w CI przed Unity, na kopii roboczej.

1) TEKSTURY Z .glb → OSOBNE PLIKI. glTFast tworzy z obrazów wbudowanych w .glb nieskompresowane tekstury RGBA32
   (dźwig portu 133 MB, pociąg metra 96 MB, skutery, jacht… razem ok. 650 MB w pamięci). Obraz z .glb zapisujemy
   jako plik obok modelu (_tekstury_glb/<model>/…), a w .glb zostaje odnośnik (uri). glTFast przy imporcie w edytorze
   bierze wtedy zwykły asset tekstury z projektu (ImageConversionImageLoader: „Use the original texture at Editor
   import”), a ten Unity kompresuje (ETC2 na Androidzie, DXT w przeglądarce) — BraxClaudeGlbTekstury ustawia
   limit 1K i sRGB. Obrazy danych (normalne, chropowatość/metal, okluzja) dostają w nazwie „__lin” (bez sRGB).
2) UPROSZCZENIE CIĘŻKICH SKANÓW (.gltf z Poly Haven: skały 0,7–1,3 mln trójkątów, krzak 156 tys.) narzędziem
   gltfpack (meshoptimizer, MIT) do ~40 tys. trójkątów. Bez gltfpack (brak sieci) krok jest pomijany z ostrzeżeniem.

Oryginały w repo się nie zmieniają (CI pracuje na checkoutcie). Raport: unity-city/Assets/PlayableGenerated/odchudzanie.json
"""
import json, os, re, shutil, struct, subprocess, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ASSETS = ROOT / "unity-city" / "Assets"
GLB_DIRS = [
    "Source/HarborBeach/Models",
    "Source/Claude/Metro",
    "Source/Claude/Library",
    "Source/Claude/Nova",
    "Source/Claude/Osiedle/models/drzewo_verdant",
    "Source/Claude/Osiedle/models/nutrition_kit",
]
GLTF_DIRS = ["Source/Claude/PolyHaven/models", "Source/Claude/Osiedle/models"]
HEAVY = 150_000      # trójkątów: powyżej upraszczamy
TARGET = 40_000

GLB_MAGIC, CH_JSON, CH_BIN = 0x46546C67, 0x4E4F534A, 0x004E4942


def safe(s):
    s = re.sub(r"[^A-Za-z0-9_-]+", "_", s).strip("_")
    return s[:60] or "model"


def read_glb(path):
    data = path.read_bytes()
    magic, _ver, _length = struct.unpack_from("<III", data, 0)
    if magic != GLB_MAGIC:
        raise ValueError("to nie jest .glb")
    off, chunks = 12, []
    while off + 8 <= len(data):
        clen, ctype = struct.unpack_from("<II", data, off)
        chunks.append([ctype, data[off + 8: off + 8 + clen]])
        off += 8 + clen
    js = json.loads(chunks[0][1].decode("utf-8").rstrip(" \x00"))
    return js, chunks


def write_glb(path, js, chunks):
    raw = json.dumps(js, separators=(",", ":"), ensure_ascii=False).encode("utf-8")
    raw += b" " * ((4 - len(raw) % 4) % 4)
    chunks[0][1] = raw
    body = b""
    for ctype, cdata in chunks:
        pad = (4 - len(cdata) % 4) % 4
        cdata = cdata + (b"\x00" * pad)
        body += struct.pack("<II", len(cdata), ctype) + cdata
    path.write_bytes(struct.pack("<III", GLB_MAGIC, 2, 12 + len(body)) + body)


def image_roles(js):
    """indeks obrazu → True gdy obraz jest tylko danymi (liniowy), False gdy kolorem (sRGB)."""
    tex = js.get("textures") or []
    color, linear = set(), set()

    def src(info):
        if not isinstance(info, dict):
            return None
        i = info.get("index")
        if i is None or i >= len(tex):
            return None
        t = tex[i]
        s = t.get("source")
        for ext in (t.get("extensions") or {}).values():
            if isinstance(ext, dict) and ext.get("source") is not None:
                s = ext["source"] if s is None else s
        return s

    for m in js.get("materials") or []:
        pbr = m.get("pbrMetallicRoughness") or {}
        for info in (pbr.get("baseColorTexture"), m.get("emissiveTexture")):
            s = src(info)
            if s is not None:
                color.add(s)
        for info in (pbr.get("metallicRoughnessTexture"), m.get("normalTexture"), m.get("occlusionTexture")):
            s = src(info)
            if s is not None:
                linear.add(s)
        sg = (m.get("extensions") or {}).get("KHR_materials_pbrSpecularGlossiness") or {}
        for key in ("diffuseTexture", "specularGlossinessTexture"):
            s = src(sg.get(key))
            if s is not None:
                color.add(s)
    return {i: (i in linear and i not in color) for i in range(len(js.get("images") or []))}


def externalize(glb, rep):
    js, chunks = read_glb(glb)
    images = js.get("images") or []
    binc = next((c for t, c in chunks if t == CH_BIN), None)
    if not images or binc is None:
        return 0
    views = js.get("bufferViews") or []
    roles = image_roles(js)
    stem = safe(glb.stem)
    out = glb.parent / "_tekstury_glb" / stem
    n = 0
    for i, img in enumerate(images):
        bv = img.get("bufferView")
        if bv is None or bv >= len(views):
            continue
        v = views[bv]
        if v.get("buffer", 0) != 0:
            continue
        b = binc[v.get("byteOffset", 0): v.get("byteOffset", 0) + v["byteLength"]]
        mime = img.get("mimeType", "")
        if b[:8] == b"\x89PNG\r\n\x1a\n" or mime == "image/png":
            ext = "png"
        elif b[:3] == b"\xff\xd8\xff" or mime == "image/jpeg":
            ext = "jpg"
        else:
            continue   # webp/ktx2 — zostaje w .glb
        out.mkdir(parents=True, exist_ok=True)
        name = f"{stem}_{i:02d}{'__lin' if roles.get(i) else ''}.{ext}"
        (out / name).write_bytes(b)
        img.pop("bufferView", None)
        img["uri"] = f"_tekstury_glb/{stem}/{name}"
        img["mimeType"] = "image/png" if ext == "png" else "image/jpeg"
        n += 1
    if n:
        write_glb(glb, js, chunks)
        rep["glb"].append({"model": str(glb.relative_to(ASSETS)), "obrazy": n})
    return n


def triangles(gltf_path):
    js = json.loads(gltf_path.read_text(encoding="utf-8"))
    acc = js.get("accessors") or []
    tris = 0
    for mesh in js.get("meshes") or []:
        for p in mesh.get("primitives") or []:
            if p.get("mode", 4) != 4:
                continue
            if p.get("indices") is not None:
                tris += acc[p["indices"]]["count"] // 3
            elif (p.get("attributes") or {}).get("POSITION") is not None:
                tris += acc[p["attributes"]["POSITION"]]["count"] // 3
    return tris


def gltfpack_cmd():
    exe = shutil.which("gltfpack")
    if exe:
        return [exe]
    npx = shutil.which("npx")
    if npx:
        try:
            subprocess.run([npx, "-y", "gltfpack@0.22.0", "-h"], capture_output=True, timeout=240)
            return [npx, "-y", "gltfpack@0.22.0"]
        except Exception:
            return None
    return None


def simplify(gltf_path, tris, cmd, rep):
    ratio = max(.01, min(1.0, TARGET / max(tris, 1)))
    tmp = gltf_path.with_name(gltf_path.stem + "__lekki.gltf")
    best = None
    for extra in ([], ["-sa"]):
        args = cmd + ["-i", str(gltf_path), "-o", str(tmp), "-si", f"{ratio:.4f}", "-noq", "-kn", "-km", "-ke"] + extra
        r = subprocess.run(args, capture_output=True, text=True, timeout=600)
        if r.returncode != 0 or not tmp.exists():
            rep["bledy"].append(f"{gltf_path.name}: gltfpack {r.returncode} {r.stderr[-300:]}")
            return
        after = triangles(tmp)
        best = after
        if after <= TARGET * 2:
            break
    # gltfpack zapisuje <stem>__lekki.gltf + <stem>__lekki.bin; podmieniamy oryginał (te same ścieżki tekstur).
    tmp_bin = tmp.with_suffix(".bin")
    if not tmp_bin.exists():
        rep["bledy"].append(f"{gltf_path.name}: gltfpack nie zapisał {tmp_bin.name}")
        tmp.unlink()
        return
    js = json.loads(tmp.read_text(encoding="utf-8"))
    src = json.loads(gltf_path.read_text(encoding="utf-8"))
    old_bins = [b.get("uri") for b in (src.get("buffers") or []) if b.get("uri") and not b["uri"].startswith("data:")]
    new_bin = old_bins[0] if old_bins else gltf_path.with_suffix(".bin").name
    for b in js.get("buffers") or []:
        if b.get("uri") == tmp_bin.name:
            b["uri"] = new_bin
    for u in old_bins:   # tylko bufory, które wskazywał ten model (Poly Haven: jeden .bin na model)
        f = gltf_path.parent / u
        if f.exists() and f != tmp_bin:
            f.unlink()
    tmp_bin.replace(gltf_path.parent / new_bin)
    gltf_path.write_text(json.dumps(js), encoding="utf-8")
    tmp.unlink()
    rep["uproszczone"].append({"model": str(gltf_path.relative_to(ASSETS)), "przed": tris, "po": best})


def main():
    rep = {"glb": [], "uproszczone": [], "pominiete": [], "bledy": []}
    total_img = 0
    for d in GLB_DIRS:
        p = ASSETS / d
        if not p.is_dir():
            continue
        for glb in sorted(p.rglob("*.glb")):
            try:
                total_img += externalize(glb, rep)
            except Exception as e:
                rep["bledy"].append(f"{glb.name}: {e}")
    print(f"tekstury z .glb jako osobne pliki: {total_img} obrazów z {len(rep['glb'])} modeli")

    heavy = []
    for d in GLTF_DIRS:
        p = ASSETS / d
        if not p.is_dir():
            continue
        for g in sorted(p.rglob("*.gltf")):
            if g.stem.endswith("__lekki"):
                continue
            try:
                t = triangles(g)
            except Exception as e:
                rep["bledy"].append(f"{g.name}: {e}")
                continue
            if t > HEAVY:
                heavy.append((g, t))
    if heavy:
        cmd = gltfpack_cmd()
        if not cmd:
            rep["pominiete"] = [f"{g.name} ({t})" for g, t in heavy]
            print("DO POPRAWKI: brak gltfpack — ciężkie skany zostają bez uproszczenia:", ", ".join(rep["pominiete"]))
        else:
            for g, t in heavy:
                try:
                    simplify(g, t, cmd, rep)
                except Exception as e:
                    rep["bledy"].append(f"{g.name}: {e}")
    for u in rep["uproszczone"]:
        print(f"uproszczony skan: {u['model']}: {u['przed']} → {u['po']} trójkątów")
    for b in rep["bledy"]:
        print("DO POPRAWKI:", b)
    out = ASSETS / "PlayableGenerated"
    out.mkdir(parents=True, exist_ok=True)
    (out / "odchudzanie.json").write_text(json.dumps(rep, ensure_ascii=False, indent=1), encoding="utf-8")


if __name__ == "__main__":
    main()
