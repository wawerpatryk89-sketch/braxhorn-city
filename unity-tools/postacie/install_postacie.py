#!/usr/bin/env python3
"""BRAXHORN — global characters for EVERY region's build (owner 12.11.2026 16:57: "Postacie będą musiały i tak być globalnie
zrobione, te same pod całą grę").

Run before Unity in any APK workflow, in a step that has the KLUCZ secret:

    python3 unity-tools/postacie/install_postacie.py

Every pack unity-city/Prywatne/braxhorn-postacie-*/ (encrypted, the repository is public) is opened, checked against its
manifest.json and the CONTENT of its "target" folder (one folder per character: <id>/<id>.fbx + textures, the shared
_ruchy_T/ clips, katalog.json) is staged to unity-city/Assets/Source/Prywatne/postacie/ - where BraxPostacie
(Assets/Editor/Claude/Region/BraxPostacieGlobalne.cs) finds it. Several packs merge into the same folder. No KLUCZ / no pack =
the build goes on without the owner's characters (the arena cage stays empty). Never starts Unity, a build or a workflow.
"""
from pathlib import Path
import json
import os
import shutil
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[2]
PACKS = ROOT / "unity-city/Prywatne"
OUT = ROOT / "unity-city/Assets/Source/Prywatne/postacie"
sys.path.insert(0, str(ROOT / "unity-tools/wideo"))
import install_braxhorn_wideo as wideo   # noqa: E402  (the same CMS pack format and checks)


def install(packs=PACKS, out=OUT):
    found = sorted(p for p in packs.glob("braxhorn-postacie-*") if (p / "manifest.json").exists())
    if not found:
        print("BRAX_POSTACIE_SKIPPED: no packs"); return 0
    if not os.environ.get("KLUCZ", "").strip():
        print("BRAX_POSTACIE_SKIPPED: no KLUCZ (the arena cage stays without fighters)"); return 0
    for pack in found:
        try:
            target = json.loads((pack / "manifest.json").read_text()).get("target", "")
            if not target:
                print("BRAX_POSTACIE_BAD_MANIFEST " + pack.name); continue
            with tempfile.TemporaryDirectory() as tmp:
                lib = wideo.open_pack(pack, tmp)
                if not lib or not (lib / target).is_dir():
                    print("BRAX_POSTACIE_FAILED " + pack.name); continue
                n = 0
                for src in sorted((lib / target).iterdir()):
                    dst = out / src.name
                    if src.is_dir():
                        if dst.exists(): shutil.rmtree(dst)
                        shutil.copytree(src, dst); n += 1
                    else:
                        out.mkdir(parents=True, exist_ok=True); shutil.copy(src, out / (pack.name + "_" + src.name))
                print("BRAX_POSTACIE_OK " + pack.name + " -> " + str(out) + " (" + str(n) + " folders)")
        except Exception as e:   # never fail the build because of the owner's characters
            print("BRAX_POSTACIE_FAILED " + pack.name + ": " + repr(e)[:200])
    return 0


if __name__ == "__main__":
    sys.exit(install())
