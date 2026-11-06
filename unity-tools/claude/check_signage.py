#!/usr/bin/env python3
"""Wymagane, oryginalne oznakowanie Braxhorn City (tylko miasto).

Kontrola jest uruchamiana po odszyfrowaniu paczek, PRZED buildem Unity 6.
Podstawianie tekstowych atrap i błędnych logotypów nie jest zaliczeniem.
Wymagane wyłącznie grafiki już powiązane z istniejącymi punktami miasta.
"""
from pathlib import Path
import json
import sys

ROOT = Path("unity-city/Assets/Source/Prywatne")
DIRS = [
    ROOT / "Grafiki" / "szyldy",
    ROOT / "Grafiki",
    ROOT / "Miasto" / "miasto_sklepy" / "szyldy",
    ROOT / "Miasto" / "miasto_sklepy" / "grafiki",
    ROOT / "Miasto" / "miasto_sklepy",
    ROOT / "Miasto" / "miasto_sklepy" / "billboardy",
]
REQUIRED = {
    "SOBCZAK KAWIARNIA": "sobczak_kawiarnia",
    "SOBCZAK APTEKA": "sobczak_apteka",
    "SOBCZAK PIEKARNIA": "sobczak_piekarnia",
    "SOBCZAK CUKIERNIA": "sobczak_cukiernia",
    "SOBCZAK DELIKATESY": "sobczak_delikatesy",
    "SOBCZAK WARZYWA OWOCE": "sobczak_warzywa",
    "WAWER SUPERMARKET": "wawer_supermarket",
    "MURAL ZA DJ — NOVA CLUB": "mural_nova",
    "WAWER — PLAKAT W SUPERMARKECIE 1": "wawer_plakat_1",
    "WAWER — PLAKAT W SUPERMARKECIE 2": "wawer_plakat_2",
}
EXTS = {".jpg", ".jpeg", ".png"}


def find_exact(stem):
    for directory in DIRS:
        if not directory.is_dir():
            continue
        for file in sorted(directory.iterdir()):
            if (file.is_file() and file.suffix.lower() in EXTS
                    and file.stem.casefold() == stem.casefold()
                    and file.stat().st_size > 1_000):
                return file
    return None


def main():
    result = {}
    for title, stem in REQUIRED.items():
        f = find_exact(stem)
        result[title] = {
            "oczekiwany_id": stem,
            "jest": bool(f),
            "sciezka": str(f) if f else None,
        }
        if f:
            print("BRAXHORN_SZYLD_OK", title, "->", f)
        else:
            print("::error::BRAXHORN_SZYLD_NIEGOTOWY", title, "— brak oryginalnej grafiki", stem)
    # NOVA: użytkownik wymaga oryginalnego szyldu z paczki, nie tekstowej
    # atrapy. Import do Unity działa na osobnym GLB szyld_nova.glb albo
    # dedykowanej grafice JPG/PNG/JPEG — sprawdzamy przed drogim buildem.
    nazwy_nova = ("club_nova", "nova_club", "nova")
    model_nova = ROOT / "Miasto" / "miasto_sklepy" / "szyld_nova.glb"
    grafika_nova = next((p for stem in nazwy_nova
        for p in [
            ROOT / "Grafiki" / "szyldy" / (stem + ext)
            for ext in (".jpg", ".png", ".jpeg")
        ] + [
            ROOT / "Grafiki" / (stem + ext)
            for ext in (".jpg", ".png", ".jpeg")
        ] + [
            ROOT / "Miasto" / "miasto_sklepy" / "szyldy" / (stem + ext)
            for ext in (".jpg", ".png", ".jpeg")
        ] + [
            ROOT / "Miasto" / "miasto_sklepy" / "grafiki" / (stem + ext)
            for ext in (".jpg", ".png", ".jpeg")
        ] if p.is_file() and p.stat().st_size > 1000), None)
    oryginal_nova = (model_nova if model_nova.is_file() and model_nova.stat().st_size > 1000
                     else grafika_nova)
    result["NOVA CLUB — oryginalny szyld właściciela"] = {
        "jest": bool(oryginal_nova), "sciezka": str(oryginal_nova) if oryginal_nova else None
    }
    if oryginal_nova:
        print("BRAXHORN_SZYLD_OK NOVA CLUB — oryginal z paczki ->", oryginal_nova)
    else:
        print("::error::BRAXHORN_NOVA_SZYLD_BRAK_ORYGINALU — wymagana paczka miasto_sklepy/szyld_nova.glb lub osobna grafika club_nova/nova_club/nova")
    
    # Mural właściciela: numer 19 jest dedykowany tylko BRAXHORN GYM.
    gyms = [
        ROOT / "Grafiki" / "billboardy" / "19.jpg",
        ROOT / "Grafiki" / "billboardy" / "19.png",
        ROOT / "Miasto" / "miasto_sklepy" / "billboardy" / "19.jpg",
        ROOT / "Miasto" / "miasto_sklepy" / "billboardy" / "19.png",
    ]
    has_mural = any(f.is_file() and f.stat().st_size > 1000 for f in gyms)
    result["BRAXHORN GYM — mural Brax i Nyxa"] = {"jest": has_mural}
    if not has_mural:
        print("::error::BRAXHORN_GYM_MURAL_BRAK oryginału nr 19")
    destination = Path("unity-city/Assets/Source/Claude/signage-check.json")
    destination.parent.mkdir(parents=True, exist_ok=True)
    destination.write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding="utf-8")
    ok = sum(1 for item in result.values() if item["jest"])
    print("BRAXHORN_SZYLDY_WYNIK", ok, "/", len(result))
    return 0 if ok == len(result) else 3


if __name__ == "__main__":
    sys.exit(main())
