#!/usr/bin/env python3
"""BRAXHORN — the owner's shop models for EVERY region's build (owner 11.11.2026 22:44: "tworzę ciuchy, torebki, buty i robimy
to globalnie na całą grę do sklepów ... żeby każdy z tego korzystał"; rule docs/zasady/ZASADA_SKLEP_MODELE_GLOBALNE_20261111.md).

Run before Unity in any APK workflow, in the step that has the KLUCZ secret:

    python3 unity-tools/sklep/install_sklep_modele.py

Every pack unity-city/Prywatne/braxhorn-sklep-*/ (encrypted, the repository is public) is opened, checked against its
manifest.json and its "target" folder (the cut products: one .glb per product in real metres + the shared _tekstury/ +
katalog.json) is staged to unity-city/Assets/Source/Prywatne/Miasto/<target>/ - where BraxMiastoAssety.Jest / .Model
(Assets/Editor/Claude/Region) find the owner's packs. No KLUCZ / no pack = the build goes on (the shops use their
fallbacks). Never starts Unity, a build or a workflow.
"""
from pathlib import Path
import json
import os
import shutil
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[2]
PACKS = ROOT / "unity-city/Prywatne"
OUT = ROOT / "unity-city/Assets/Source/Prywatne/Miasto"
sys.path.insert(0, str(ROOT / "unity-tools/wideo"))
import install_braxhorn_wideo as wideo   # noqa: E402  (the same CMS pack format and checks)


def install(packs=PACKS, out=OUT):
    found = sorted(p for p in packs.glob("braxhorn-sklep-*") if (p / "manifest.json").exists())
    if not found:
        print("BRAX_SKLEP_SKIPPED: no packs"); return 0
    if not os.environ.get("KLUCZ", "").strip():
        print("BRAX_SKLEP_SKIPPED: no KLUCZ (the shops use their fallbacks)"); return 0
    for pack in found:
        target = json.loads((pack / "manifest.json").read_text()).get("target", "")
        if not target:
            print("BRAX_SKLEP_BAD_MANIFEST " + pack.name); continue
        with tempfile.TemporaryDirectory() as tmp:
            lib = wideo.open_pack(pack, tmp)
            if not lib or not (lib / target).is_dir():
                print("BRAX_SKLEP_FAILED " + pack.name); continue
            dst = out / target
            if dst.exists(): shutil.rmtree(dst)
            shutil.copytree(lib / target, dst)
            n = len(list(dst.glob("*.glb")))
            print("BRAX_SKLEP_OK " + pack.name + " -> " + str(dst) + " (" + str(n) + " models)")
    return 0


if __name__ == "__main__":
    sys.exit(install())
