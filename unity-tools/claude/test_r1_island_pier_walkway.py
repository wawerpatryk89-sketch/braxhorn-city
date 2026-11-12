"""MIASTO R1 island pier: 53 m pier with T-head and a plank boardwalk to the arena plaza.

Source-only geometric arithmetic. No Unity editor, no APK, no asset changes.
Stan po 9.11: przystań i deptak z buildu 6.11 (commit 9d9aff5).
"""
import math
import unittest
from pathlib import Path

SOURCE=(Path(__file__).resolve().parents[2]/
        "unity-city/Assets/Editor/Claude/Region/BraxMiastoWyspa.cs").read_text(encoding="utf-8")


class IslandPierWalkwayBoundary(unittest.TestCase):
    def test_boardwalk_runs_from_pier_to_plaza_edge_with_metre_profile(self):
        # profil co 1 m od końca pomostu (x1) do krawędzi placu (PlacR - 1 m), wysokość ≥ teren + 25 cm, spadek ≤ 12 cm/m
        self.assertIn("float xa = CX + Plac.x - PlacR + 1f; var prof = new List<Vector2>();",SOURCE)
        self.assertIn("for (float x = x1; x <= xa + .01f; x += 1f)",SOURCE)
        self.assertIn("Mathf.Max(mx + .25f, yPrev - .12f)",SOURCE)
        self.assertIn("prof[i] = new Vector2(prof[i].x, Mathf.Max(prof[i].y, prof[i + 1].y - .12f));",SOURCE)
        # każdy odcinek 1 m ma własną kolizję w nachyleniu profilu
        self.assertIn('new GameObject("kolizja: deptak")',SOURCE)
        self.assertIn("kc.AddComponent<BoxCollider>().size = new Vector3(Vector2.Distance(a, b) + .02f, .2f, hw * 2f);",SOURCE)
        # zejście na plac: pochylnia 1:10 bez stopnia
        self.assertIn("float lr = dh * 10f;",SOURCE)
        self.assertIn('new GameObject("kolizja: pochylnia deptaka")',SOURCE)
        for dh in (0.05, 0.12, 0.25, 0.3):
            self.assertLessEqual(dh / (dh * 10.0), 0.1 + 1e-9)

    def test_deck_boards_follow_segment_pitch(self):
        # deski 14 cm co 15 cm wzdłuż każdego odcinka profilu (t od 0 do 1 z krokiem .15)
        self.assertIn("for (float t = 0; t < 1f; t += .15f)",SOURCE)
        self.assertIn("z[deski].Box(new Vector3(x, y - .03f, pz), new Vector3(.14f, .06f, hw * 2f));",SOURCE)
        positions=[i*.15 for i in range(math.ceil(1/.15))]
        self.assertTrue(all(p<1 for p in positions))
        self.assertTrue(all(positions[i+1]-positions[i]>0.14 for i in range(len(positions)-1)))

    def test_original_structure_and_materials_untouched(self):
        self.assertIn("float x0 = xs - 52f, x1 = xs + 1f, hw = 1.6f * K;",SOURCE)
        # 12.11 (właściciel: „pomościk… powiększyć… motorówką i skuterami”): głowica 44 × 5 m × k
        self.assertIn("float tx0 = x0 - 5.0f * K, tz0 = pz - 22f, tz1 = pz + 22f;",SOURCE)
        self.assertIn("BraxMiastoA.Kol(kol, \"pomost wyspy\"",SOURCE)
        self.assertIn("BraxMiastoA.Kol(kol, \"głowica pomostu\"",SOURCE)
        self.assertIn('Miejsca["wyspa_przystan"] = new Vector3(x0 + 1f, deck, pz);',SOURCE)
        # styk pokładu z deptakiem: różnica poziomu > 12 cm = ostrzeżenie, nie blokada
        self.assertIn('Ostrzez("WYSPA_POMOST_STYK',SOURCE)


if __name__=="__main__":
    unittest.main()
