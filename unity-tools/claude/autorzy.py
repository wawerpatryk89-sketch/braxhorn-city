#!/usr/bin/env python3
"""Ekran „Autorzy” w grze (audyt miasta 31.10, Opus): licencja CC BY 4.0 modeli portu, metra i NOVA wymaga podania
autorów w grze. Zbiera wszystkie CREDITS.md spod unity-city/Assets/Source do zwykłego tekstu
unity-city/Assets/Claude/Resources/braxhorn_autorzy.txt (Resources.Load w BraxTouchControls → MENU → AUTORZY).
Uruchamiane w CI przed Unity, więc nowy model z CREDITS.md sam trafia na ekran."""
import json, re, struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "unity-city" / "Assets" / "Source"
OUT = ROOT / "unity-city" / "Assets" / "Claude" / "Resources" / "braxhorn_autorzy.txt"

HEAD = [
    "BRAXHORN CITY — AUTORZY",
    "",
    "Pomysł, świat, postacie i muzyka: właściciel gry.",
    "Modele oznaczone CC BY 4.0: licencja https://creativecommons.org/licenses/by/4.0/ — zmiany opisane przy modelu.",
    "Tekstury i modele CC0: Poly Haven (polyhaven.com), ambientCG (ambientcg.com) — dziękujemy.",
    "",
]


def plain(md: str):
    out = []
    for line in md.splitlines():
        s = line.strip()
        if not s or re.fullmatch(r"\|?\s*:?-{2,}.*", s):
            continue
        if s.startswith("|"):
            cells = [c.strip() for c in s.strip("|").split("|")]
            if [c.lower() for c in cells[:2]] in (["plik", "autor"],):
                continue
            s = " · ".join(c for c in cells if c)
        s = re.sub(r"^#+\s*", "", s)
        s = s.replace("**", "").replace("`", "")
        out.append(s)
    return out


def glb_meta(path: Path):
    """asset.extras z pliku .glb/.gltf (Sketchfab zapisuje tam autora, licencję i adres)."""
    try:
        raw = path.read_bytes()
        if path.suffix == ".glb":
            n = struct.unpack_from("<I", raw, 12)[0]
            g = json.loads(raw[20:20 + n])
        else:
            g = json.loads(raw.decode("utf-8"))
    except Exception:
        return None
    ex = (g.get("asset") or {}).get("extras") or {}
    return ex if ex.get("author") else None


def main():
    lines = list(HEAD)
    seen = set()
    models = []
    for f in sorted(SRC.rglob("*.gl[tb]*")):
        if f.suffix not in (".glb", ".gltf") or f.name in seen:
            continue
        ex = glb_meta(f)
        if not ex:
            continue
        seen.add(f.name)
        models.append(f"{ex.get('title') or f.stem} — {ex.get('author')} — {ex.get('license', '')} — {ex.get('source', '')}".strip(" —"))
    if models:
        lines.append("— Modele (autor · licencja · źródło, z metadanych plików) —")
        lines.extend(models)
        lines.append("")
    for f in sorted(SRC.rglob("CREDITS.md")) + sorted(SRC.rglob("*.CREDITS.md")):
        body = plain(f.read_text(encoding="utf-8", errors="replace"))
        if not body:
            continue
        lines.append("— " + str(f.parent.relative_to(SRC)).replace("\\", "/") + " —")
        lines.extend(body)
        lines.append("")
    # build 45: autorzy paczek zaszyfrowanych (unity-city/Prywatne/*_CREDITS.md — sam tekst jest jawny; modele odszyfrowuje CI)
    for f in sorted((ROOT / "unity-city" / "Prywatne").glob("*_CREDITS.md")):
        body = plain(f.read_text(encoding="utf-8", errors="replace"))
        if not body:
            continue
        lines.append("— Prywatne/" + f.name.replace("_CREDITS.md", "") + " —")
        lines.extend(body)
        lines.append("")
    OUT.parent.mkdir(parents=True, exist_ok=True)
    OUT.write_text("\n".join(lines).strip() + "\n", encoding="utf-8")
    print(f"ekran Autorzy: {len(lines)} linii → {OUT.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
