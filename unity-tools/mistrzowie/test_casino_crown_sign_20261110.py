"""Owner 10.11 16:36-16:45 (casino T11): "To czarne coś to ma zniknąć, te jajo, na czym jest szyld, to wszystko psuje",
"To na czym jest znak 3d to coś zaokrąglone znika i te 3d napis lepszy i czytelniejszy !!", "Premium premium !!!!!!",
and of the two reference pictures he sent: "Ten drugi lepszy Casino WAWER" (heavy gold word in a red outline, the four
card suits behind its top, gold ribbons with a red stripe at both ends, sparkles).
KASYNO WAWER is a layered 3D relief on the front parapet between the pylons; the rotunda ends in a thin cream coping."""
import re
import unittest
from pathlib import Path

import build_casino_sign as sign

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"


def method(src, name):
    i = src.index("static void " + name + "(")
    j = src.index("\n    }\n", i)
    return src[i:j]


class CasinoCrownSign(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.crown = sign.roof_crown()
        cls.casino = (ED / "BraxChampionsCasinoT11.cs").read_text(encoding="utf-8")
        cls.sign = (ED / "BraxChampionsCasinoT11Sign.cs").read_text(encoding="utf-8")

    def bounds(self, role=None):
        import numpy as np
        ps = [np.array(m["positions"]).reshape(-1, 3) / sign.K for m in self.crown["meshes"] if role in (None, m["role"])]
        p = np.concatenate(ps)
        return p.min(0), p.max(0)

    def test_composition_like_the_owners_reference(self):
        roles = {m["role"] for m in self.crown["meshes"]}
        self.assertEqual(roles, {"gold", "accent", "red", "black", "light"})
        self.assertEqual(sign.CROWN_TEXT, "KASYNO WAWER")
        self.assertGreaterEqual(sign.CROWN_CAP, 1.7)                       # readable from the street
        src = (Path(__file__).parent / "build_casino_sign.py").read_text(encoding="utf-8")
        for part in ("club, ", "heart, ", "spade, ", "diamond, ", "def ribbon(", "star4(", "MONTSERRAT"):
            self.assertIn(part, src)
        # layers stand apart in depth (gold letters in front of the red outline in front of the gold rim)
        self.assertIn("g.solid(word, .20, .50,", src)
        self.assertIn("g.solid(red, .12, .36,", src)
        self.assertIn("g.solid(rim, .06, .26,", src)

    def test_fits_between_the_pylons_and_sits_on_the_parapet(self):
        lo, hi = self.bounds()
        # pylons at x +-18.25, 2.75 wide: inner faces +-16.875
        self.assertLess(hi[0], 16.875 - 1.5)
        self.assertGreater(lo[0], -16.875 + 1.5)
        z = float(re.search(r"CrownZ=(-?[\d.]+)f", self.casino).group(1))
        y = float(re.search(r"CrownBaseY=(-?[\d.]+)f", self.casino).group(1))
        bottom = y + lo[1]
        self.assertGreater(bottom, 15.60)                                    # parapet top
        self.assertLess(bottom, 15.60 + .10)
        # front of the letters a little proud of the parapet face (-28.125), the back still over the parapet
        self.assertLess(z - hi[2], -28.125)
        self.assertGreater(z - lo[2], -28.125)

    def test_name_no_longer_on_the_rotunda(self):
        name = method(self.casino, "CasinoName")
        self.assertIn('Place3DSign("KASYNO_WAWER_crown",signRoot,', name)
        self.assertNotIn("letter rail", name)
        self.assertNotIn("BraxChampionsLetters3D.Text", name)
        cap = method(self.casino, "RotundaRoofCap")
        self.assertIn("sharedMaterial=cream;", cap)
        self.assertNotIn("sharedMaterial=roof;", cap)
        self.assertNotIn("rotunda roof gold edge line", self.casino)
        # one front parapet run (no notch under the sign)
        self.assertEqual(self.casino.count('"Parapet front cream limestone"'), 1)
        self.assertIn('B(trim,"Parapet front cream limestone",cream,0,14.9f,-27.8f,75f,1.4f,.65f);', self.casino)

    def test_crown_materials(self):
        self.assertIn('signRed=Surface("sign_red_lacquered_aluminium"', self.sign)
        self.assertIn('signBlack=Surface("sign_piano_black_lacquer"', self.sign)
        self.assertIn('signSparkle=Surface("sign_sparkle_white_glow"', self.sign)


if __name__ == "__main__":
    unittest.main()
