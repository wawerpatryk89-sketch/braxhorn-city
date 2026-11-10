#!/usr/bin/env python3
"""MIASTO R1 10.11 — Driving.fbx właściciela dla Braxa (skuter, motorówka, jacht: „ręce na kierownicy — jest animacja driver”).

Plik NIE jest w repozytorium (repo publiczne). Pobieramy go w CI z zatwierdzonego wydania właściciela (ta sama paczka, z której
Miasto Mistrzów bierze 3 animacje Braxa do auta), sprawdzamy SHA-256 na każdym poziomie i kopiujemy tylko Driving.fbx do
unity-city/Assets/Source/Claude/Brax/Ruchy/. Każdy błąd = komunikat i wyjście 0: gra buduje się dalej z pozą z kodu.
"""
import hashlib
import io
import json
import os
import pathlib
import subprocess
import sys
import zipfile

ROOT = pathlib.Path(__file__).resolve().parents[2]
CEL = ROOT / "unity-city/Assets/Source/Claude/Brax/Ruchy/Driving.fbx"
WYDANIE = 435656828
PACZKA = "BRAXHORN_MISTRZOWIE_A1_T11_ORIGINALY_DO_BUILD.zip"
PACZKA_SHA = "43ea47f02a1f2e9e4f707bb75d16ec0a3fe79ec02762929ca591e317defa1088"
ANIM = "BRAXHORN_BRAX_3_ANIMACJE_MIXAMO_ORYGINALY.zip"
ANIM_SHA = "509a6379fad404edec798e2999f3cdff37776c5399e92839a16ef05687f65bd3"
FBX_W_PACZCE = "Assets/Source/Mistrzowie/Brax/CarAnimations/Driving.fbx"
FBX_SHA = "74ccdfa8557ecf510be804b973b09606a2cc5a5b0048a4c0e5832a30451ed7e9"


def sha(b):
    return hashlib.sha256(b).hexdigest()


def gh(*args, binary=False):
    return subprocess.run(["gh", *args], check=True, capture_output=True, text=not binary).stdout


def main():
    if CEL.exists() and sha(CEL.read_bytes()) == FBX_SHA:
        print("BRAXHORN_BRAX_JAZDA Driving.fbx już jest (SHA zgodne)")
        return 0
    repo = os.environ.get("GITHUB_REPOSITORY", "wawerpatryk89-sketch/braxhorn-city")
    rel = json.loads(gh("api", f"repos/{repo}/releases/{WYDANIE}"))
    ids = [a["id"] for a in rel.get("assets", []) if a.get("name") == PACZKA]
    if len(ids) != 1:
        raise SystemExit("brak paczki " + PACZKA + " w wydaniu " + str(WYDANIE))
    dane = gh("api", "-H", "Accept: application/octet-stream", f"repos/{repo}/releases/assets/{ids[0]}", binary=True)
    if sha(dane) != PACZKA_SHA:
        raise SystemExit("SHA paczki właściciela niezgodne — nie używamy")
    with zipfile.ZipFile(io.BytesIO(dane)) as z:
        wew = z.read(ANIM)
    if sha(wew) != ANIM_SHA:
        raise SystemExit("SHA paczki animacji niezgodne — nie używamy")
    with zipfile.ZipFile(io.BytesIO(wew)) as z:
        fbx = z.read(FBX_W_PACZCE)
    if sha(fbx) != FBX_SHA or not fbx.startswith(b"Kaydara FBX Binary") or b"mixamorig:Hips" not in fbx:
        raise SystemExit("Driving.fbx zmieniony albo nie Mixamo — nie używamy")
    CEL.parent.mkdir(parents=True, exist_ok=True)
    CEL.write_bytes(fbx)
    print("BRAXHORN_BRAX_JAZDA Driving.fbx właściciela →", CEL.relative_to(ROOT), len(fbx), "B, SHA", FBX_SHA[:12])
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception as e:  # build idzie dalej: poza z kodu
        print("::warning::BRAXHORN_BRAX_JAZDA DO POPRAWKI — bez animacji Driving (poza z kodu):", e)
        sys.exit(0)
