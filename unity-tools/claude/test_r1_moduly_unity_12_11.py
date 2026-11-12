"""MIASTO R1 (12.11.2026): moduły wbudowane Unity w Packages/manifest.json dla przestrzeni nazw używanych w kodzie.

Bolid 43137235485 (12.11, 03:19) padł na kompilacji skryptów: BraxR1ReklamyTV.cs używa UnityEngine.Video.VideoPlayer, a w
manifeście nie było com.unity.modules.video (CS1069, „type has been forwarded to UnityEngine.VideoModule”). Kontrola kompilacji
poza Unity (kompiluj.sh) ma referencje do WSZYSTKICH modułów, więc tego nie łapie — pilnuje tu test przed godzinnym buildem.
"""
import json
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ASSETS = ROOT / "unity-city/Assets"
MANIFEST = json.loads((ROOT / "unity-city/Packages/manifest.json").read_text(encoding="utf-8"))["dependencies"]

# przestrzeń nazw → moduł wbudowany, który musi być w manifeście (moduły bez zależności przechodnich od gltfast)
WYMAGANE = {
    "UnityEngine.Video": "com.unity.modules.video",
    "UnityEngine.AI": "com.unity.modules.ai",
}


class ModulyUnity(unittest.TestCase):
    def test_przestrzenie_nazw_maja_modul_w_manifescie(self):
        brak = []
        for cs in ASSETS.rglob("*.cs"):
            t = cs.read_text(encoding="utf-8", errors="ignore")
            for ns, modul in WYMAGANE.items():
                if re.search(r"\b" + re.escape(ns) + r"\b(?!\.\w*Module)", t) and modul not in MANIFEST:
                    brak.append(cs.relative_to(ROOT).as_posix() + " → " + modul)
        self.assertEqual(brak, [], "brakuje modułów Unity w Packages/manifest.json")

    def test_wideo_wlaczone_dla_telewizorow_r1(self):
        self.assertIn("com.unity.modules.video", MANIFEST)
        self.assertIn("using UnityEngine.Video;", (ASSETS / "Claude/Runtime/BraxR1ReklamyTV.cs").read_text(encoding="utf-8"))


if __name__ == "__main__":
    unittest.main()
