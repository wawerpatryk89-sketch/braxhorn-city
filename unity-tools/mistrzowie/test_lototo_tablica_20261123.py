"""LOTOTO results board (owner 23.11, photo from APK #52: balls on "WYNIKI SOBOTA" cut in half, some missing).

Cause: the navy background quad lay 1 mm behind the balls in the same mesh; on the phone the two layers fought over
depth in vertical bands. Fix: the background is its own submesh with a material that writes no depth and is drawn
first, everything else lies in one plane over it; the bezel's face is 2.7 cm behind; the cameras that film the board
get a 24-bit depth buffer. The test also checks that the layout itself never overlaps."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
A = ROOT / "unity-city/Assets"
GRY = (A / "Mistrzowie/BraxChampionsAutomatyGry.cs").read_text(encoding="utf-8")
LOT = (A / "Mistrzowie/BraxChampionsLototo.cs").read_text(encoding="utf-8")
GEN = (A / "Editor/Mistrzowie/BraxChampionsKasynoLototo.cs").read_text(encoding="utf-8")
MON = (A / "Mistrzowie/BraxChampionsMonitoring.cs").read_text(encoding="utf-8")


def tablicuj():
    i = LOT.index("void Tablicuj()")
    return LOT[i:LOT.index("warstwa.Zapisz(tablicaMesh);", i)]


class Warstwa(unittest.TestCase):
    def test_background_is_its_own_submesh(self):
        self.assertIn("public void Tlo(", GRY)
        self.assertIn("m.subMeshCount = 2; m.SetTriangles(tlo, 0); m.SetTriangles(tr, 1);", GRY)
        self.assertIn("tlo.Clear();", GRY)


class Tablica(unittest.TestCase):
    def test_background_via_tlo_and_one_plane(self):
        t = tablicuj()
        self.assertIn("warstwa.Tlo(0f, 0f, W, H,", t)
        # no quad pushed in front of / behind another: every Q call keeps the default plane
        for call in re.findall(r"warstwa\.Q\(([^;]*)\);", t):
            self.assertEqual(call.count(","), 7, "Q with a z argument: " + call)

    def test_materials(self):
        self.assertIn('lotTablicaTlo.SetFloat("_ZWrite", 0f); lotTablicaTlo.renderQueue = 2001; lotTablica.renderQueue = 2002;', GEN)
        self.assertIn("tabR.sharedMaterials = new[] { lotTablicaTlo, lotTablica };", GEN)
        self.assertIn("tabR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; tabR.receiveShadows = false;", GEN)
        self.assertNotIn("sharedMaterial = lotTablica;", GEN)

    def test_bezel_behind_the_screen(self):
        przed = float(re.search(r"const float Przed = ([0-9.]+)f;", (A / "Editor/Mistrzowie/BraxChampionsKasynoSprzet.cs").read_text(encoding="utf-8")).group(1))
        m = re.search(r"b\.Box\(czern, P\(bc\.x, bc\.y, bc\.z - ([0-9.]+)f\), new Vector3\(bw \+ \.08f, bh \+ \.08f, ([0-9.]+)f\)", GEN)
        self.assertIsNotNone(m)
        face = -float(m.group(1)) + float(m.group(2)) / 2
        self.assertGreaterEqual(przed - face, .025, "the bezel's face must be >= 2.5 cm behind the screen")
        hz = 2.2
        wall_front = -hz + .16
        self.assertGreater(-hz + .20 + face, wall_front, "the bezel's face stays in front of the set wall")

    def test_cameras_filming_screens_have_24_bit_depth(self):
        self.assertIn("new RenderTexture(640, 360, 24,", LOT)
        self.assertIn("new RenderTexture(szer, wys, 24,", MON)

    def test_layout_never_overlaps(self):
        # the same numbers as Tablicuj (guarded by the string checks below): 2.4 x 1.35 m, 20 balls of 60 in two rows
        self.assertIn("float kd = dwa ? Mathf.Min(H * .14f, W * .9f / (wRzedzie * 1.12f)) : H * .19f, kx = -kd * 1.12f * (wRzedzie - 1) * .5f;", LOT)
        self.assertIn("float cx = kx + kk * kd * 1.12f, cy = dwa ? H * (.21f - rz * .155f) : H * .12f;", LOT)
        self.assertIn("float yT = dwa ? .39f : .36f, hT = dwa ? .09f : .11f;", LOT)
        self.assertIn("Linia(dwa ? -H * .17f : -H * .14f, H * (dwa ? .08f : .09f), Tablica.Kumul, kum, Tablica.Zl);", LOT)
        self.assertIn("Linia(dwa ? -H * .36f : -H * .34f, H * (dwa ? .065f : .07f), Tablica.Nastepne,", LOT)
        W, H = 2.4, 1.35
        for losowane in (20, 6):                      # LOTOTO BRAX CASH, BRAX SZÓSTKA
            rzedy = (losowane + 9) // 10; w = min(10, losowane); dwa = rzedy > 1
            kd = min(H * .14, W * .9 / (w * 1.12)) if dwa else H * .19
            kx = -kd * 1.12 * (w - 1) * .5
            boxes = []
            for k in range(losowane):
                rz, kk = divmod(k, w)
                cx = kx + kk * kd * 1.12; cy = H * (.21 - rz * .155) if dwa else H * .12
                boxes.append((cx - kd / 2, cx + kd / 2, cy - kd / 2, cy + kd / 2))
            yT, hT = (.39, .09) if dwa else (.36, .11)
            lines = [(H * yT, H * hT), ((-.17 if dwa else -.14) * H, H * (.08 if dwa else .09)), ((-.36 if dwa else -.34) * H, H * (.065 if dwa else .07))]
            for a in boxes:
                self.assertGreaterEqual(a[0], -W / 2); self.assertLessEqual(a[1], W / 2)
                for b in boxes:
                    if a is not b:
                        self.assertFalse(a[0] < b[1] - 1e-9 and b[0] < a[1] - 1e-9 and a[2] < b[3] - 1e-9 and b[2] < a[3] - 1e-9, (losowane, a, b))
                for cy, h in lines:
                    self.assertFalse(a[2] < cy + h / 2 and cy - h / 2 < a[3], ("ball over a line of text", losowane, a, cy, h))
            for cy, h in lines:
                self.assertLessEqual(cy + h / 2, H / 2); self.assertGreaterEqual(cy - h / 2, -H / 2)


if __name__ == "__main__":
    unittest.main()
