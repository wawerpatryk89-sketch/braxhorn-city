"""The region's heavy potted-plant scans made light (owner 23.11: the island villa at 15-19 frames a second inside; its potted
plants were Poly Haven scans of 70 000 and 176 000 triangles each, the same plants stand in ~60 places of the region).

BraxChampionsUproszczenie: quadric error edge collapse, half-edge (every kept vertex keeps its own normal and UV), seam-aware
(a seam point moves only along its seam, both sides together), outlines held, folds refused, each point held at its place
(a twig's tip never slides down the twig). lzejsze_sim runs the real file on a real scan when the scan and dotnet are here."""
import os
import re
import shutil
import subprocess
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SRC = (ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsUproszczenie.cs").read_text(encoding="utf-8")
SIM = ROOT / "unity-tools/mistrzowie/lzejsze_sim"


class Uproszczenie(unittest.TestCase):
    def test_meta_and_pure(self):
        self.assertTrue((ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsUproszczenie.cs.meta").exists())
        self.assertNotIn("using UnityEditor", SRC)                                     # plain arithmetic: the sim compiles it
        self.assertIn("public static int[][] Uprosc(Vector3[][] v, int[][] t, int cel)", SRC)
        self.assertIn("public static int[] Uprosc(Vector3[] v, int[] t, int cel, int[] czesc = null)", SRC)

    def test_the_faults_found_on_the_real_scans(self):
        # Unity's .normalized is zero under 1e-5: a scan's millimetre twigs refused every collapse, the leaves were eaten instead
        self.assertNotIn(".normalized", SRC.split("public static class BraxChampionsUproszczenie")[1].replace("// Unity's .normalized", ""))
        self.assertIn("static Vector3 Jedn(Vector3 n)", SRC)
        # the top twig of potted_plant_01 slid 57 mm down: every point held at its own place
        self.assertIn("public static double WagaPunktu = .1;", SRC)
        self.assertIn("Dodaj(q, i * 10, 1, 0, 0, -x.x, wPkt);", SRC)
        # the seams: each vertex of the moving point to the target's vertex on its own side, or no collapse
        self.assertIn("if (!zle) foreach (int f in przy[a]) if (zywy[f] && !Ma(f, b) && !mapa.ContainsKey(Wierz(f, a))) { zle = true; break; }", SRC)
        # two parts' vertices at one place stay two points
        self.assertIn("var k = (v[i].x, v[i].y, v[i].z, czesc == null ? 0 : czesc[i]);", SRC)

    def test_the_sim_on_a_real_scan(self):
        scan = os.environ.get("LZEJSZE_SCAN")
        if not scan or not Path(scan).exists() or not shutil.which("dotnet"):
            self.skipTest("set LZEJSZE_SCAN to a potted_plant_0x_1k.gltf (and have dotnet) to run the real simplifier on it")
        r = subprocess.run(["dotnet", "run", "-c", "Release", "--", scan, "5000"], cwd=SIM, capture_output=True, text=True, timeout=600)
        self.assertIn("LZEJSZE_SIM OK", r.stdout, r.stdout[-2000:] + r.stderr[-2000:])
        kept = int(re.search(r"-> (\d+) triangles \(budget 5000\)", r.stdout).group(1))
        self.assertLessEqual(kept, 5000)


if __name__ == "__main__":
    unittest.main()
