"""Owner 10.11: "zrób te dwa jachty, modele ... dużo ich zrób ... naklejki na nich narobić, coś, żeby troszeczkę się
różniły" and 12:52 "sprawdzasz globalnie, wszystko w skalę".

* yacht.glb is a real 40.33 m superyacht (8.21 m beam, decks 2.1 / 4.8 / 6.8 / 9.8 m above the waterline); it was
  squeezed to 19.5 m - doors and decks half a person high. It is moored at its real size; the marina has 30 m fingers
  every 11.6 m.
* 15 yachts in 5 hulls (original white + navy, black, emerald, sapphire repainted ONLY on the hull topsides of the atlas,
  build_yacht_liveries.py in CI) and 15 different solid 3D names + home port on the raked transom.
* The second model offered earlier ("Frickies yacht") is NOT used: its licence is unclear (CC BY-NC in the 30.10 asset
  audit; the same name is a GTA V map asset).
"""
import importlib.util
import struct
import json
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
TOOLS = ROOT / "unity-tools/mistrzowie"
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"


def glb_length():
    b = (ROOT / "unity-city/Assets/Source/HarborBeach/Models/yacht.glb").read_bytes()
    js = json.loads(b[20:20 + struct.unpack("<I", b[12:16])[0]])
    a = js["accessors"][js["meshes"][0]["primitives"][0]["attributes"]["POSITION"]]
    return (a["max"][0] - a["min"][0]) / 100.0          # node scale 0.01


class RealSizeYachts(unittest.TestCase):
    def test_length_is_the_model_length(self):
        import port_layout as pl
        self.assertAlmostEqual(glb_length(), 40.33, delta=.01)
        self.assertAlmostEqual(pl.YACHT_LEN, glb_length(), delta=.01)
        h = (ED / "BraxChampionsHarbour.cs").read_text(encoding="utf-8")
        self.assertIn("public const float YachtLength=40.33f;", h)
        self.assertIn('b.kind=="yacht"?YachtLength:', h)
        self.assertNotIn("19.5f", h)
        # helm on the bridge deck, mooring lines to the main deck, half beam 4.1 m
        self.assertIn('(bay.kind=="yacht"?7.25f:.8f)*K', h)
        self.assertIn('b.kind=="yacht"?2.06f:.95f', h)
        self.assertIn('b.kind=="yacht"?4.10f:1.10f', h)

    def test_marina_for_40_m_yachts(self):
        import port_layout as pl
        yachts = [b for b in pl.BERTHS if b["kind"] == "yacht"]
        self.assertEqual(len(yachts), 15)
        self.assertTrue(all(b["length"] == pl.YACHT_LEN for b in yachts))
        fingers = [p for p in pl.PIERS if p["id"].startswith("YACHT_") and "_FINGER_" in p["id"]]
        self.assertEqual(len(fingers), 17)
        self.assertTrue(all(abs(p["w"] - 30.0) < 1e-6 and abs(p["d"] - 2.0) < 1e-6 for p in fingers))
        self.assertTrue(pl.validate(pl.PIERS, pl.BERTHS))


class LiveriesAndNames(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.h = (ED / "BraxChampionsHarbour.cs").read_text(encoding="utf-8")
        cls.src = (TOOLS / "build_yacht_liveries.py").read_text(encoding="utf-8")

    def test_five_hulls_fifteen_names(self):
        self.assertIn('YachtLiveries={"","navy","black","emerald","sapphire"}', self.h)
        names = self.h.split("YachtNames={", 1)[1].split("};", 1)[0]
        names = [n.strip().strip('"') for n in names.replace("\n", " ").split(",") if n.strip()]
        self.assertEqual(len(names), 15)
        self.assertEqual(len(set(names)), 15)
        self.assertTrue(all(n.isascii() for n in names))
        self.assertIn('Line("BRAXHORN",', self.h)
        self.assertIn("Quaternion.FromToRotation(Vector3.back,n)", self.h)
        self.assertIn("BraxChampionsLetters3D.Text(m,face,ret,text,", self.h)

    def test_livery_falls_back_to_white_never_crashes(self):
        self.assertIn("DO POPRAWKI YACHT_LIVERY_MISSING", self.h)
        self.assertIn('Out+"/Materials/harbour_yacht_livery_"+id+".mat"', self.h)

    def test_livery_script_is_pinned_and_generated_in_ci(self):
        self.assertIn('YACHT_GLB_SHA256 = "97b60c22214c00f07a0742a9f640b7f09fe956f9681b41416f74425e3d88416d"', self.src)
        self.assertIn("(lum > .45) & (sat < .12)", self.src)          # only white paint: no windows, teak, antifouling
        wf = (ROOT / ".github/workflows/mistrzowie-apk.yml").read_text(encoding="utf-8")
        self.assertIn("python3 unity-tools/mistrzowie/build_yacht_liveries.py", wf)
        self.assertLess(wf.index("pillow=="), wf.index("build_yacht_liveries.py"))
        import subprocess
        tracked = subprocess.run(["git", "ls-files", "unity-city/Assets/Source/HarborBeach/Liveries"], cwd=ROOT,
                                 capture_output=True, text=True).stdout
        self.assertEqual(tracked.strip(), "")

    def test_livery_repaints_only_the_hull(self):
        try:
            import numpy  # noqa
            from PIL import Image  # noqa
        except ImportError:
            self.skipTest("numpy / Pillow not installed")
        spec = importlib.util.spec_from_file_location("liveries", TOOLS / "build_yacht_liveries.py")
        mod = importlib.util.module_from_spec(spec); spec.loader.exec_module(mod)
        import tempfile
        with tempfile.TemporaryDirectory() as d:
            made = mod.build(d)
            self.assertEqual(sorted(made), ["black", "emerald", "navy", "sapphire"])
            # about 8 % of the 2048 x 2048 atlas is painted hull; never the whole atlas
            self.assertTrue(all(250_000 < v < 450_000 for v in made.values()), made)


class NoUnclearLicence(unittest.TestCase):
    def test_frickies_yacht_not_used(self):
        for p in list(ED.glob("*.cs")) + list(TOOLS.glob("*.py")):
            if p.name == Path(__file__).name:
                continue
            self.assertNotIn("frickies", p.read_text(encoding="utf-8", errors="ignore").lower(), p.name)


if __name__ == "__main__":
    unittest.main()
