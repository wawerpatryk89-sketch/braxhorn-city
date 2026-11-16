"""Owner 16.11.2026 01:36 ("gra się źle rysuje, wszystko migota, galeria, basen ... woda w całym basenie, nie pasek"):
the far copy keeps the building's own triangles (no vertex clustering), big batches split their small pieces onto the
culled layers, owner models keep their root scale and the aquapark measures them."""
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"


class FarCopyTests(unittest.TestCase):
    def setUp(self):
        self.src = (ED / "BraxChampionsPodzial.cs").read_text(encoding="utf-8")

    def test_no_vertex_clustering(self):
        for gone in ("static long Klucz(", "KlasaNormalnej", "static Dictionary<Material, Kubel> Grupuj("):
            self.assertNotIn(gone, self.src)

    def test_pieces_whole_by_size_within_budget(self):
        self.assertIn("const float MinKawalek = 2.5f, Smuklosc = 25f, Spaw = .001f;", self.src)
        self.assertIn("OrderByDescending(k => k.wynik / Mathf.Sqrt(Mathf.Max(1f, k.tri.Count / 1500f)))", self.src)
        self.assertIn("suma0 + k.tri.Count > maxTris", self.src)
        # vertices are copied as they are (position, normal, tangent, uv of the source)
        body = self.src[self.src.index("static Dictionary<Material, Kubel> Zloz("):self.src.index("static GameObject ZDaleka(")]
        self.assertIn("b.v.Add(l2w.MultiplyPoint3x4(d.v[i]));", body)
        self.assertIn("b.uv.Add(maUv ? d.uv[i] : Vector2.zero);", body)


class BatchSplitTests(unittest.TestCase):
    def setUp(self):
        self.src = (ED / "BraxChampionsMeshBatch.cs").read_text(encoding="utf-8")

    def test_thresholds_and_layers(self):
        self.assertIn("const int ProgPodzialu = 20000;", self.src)
        self.assertIn("const float Drobny = .6f, Maly = 2f, KomorkaMala = 24f, KomorkaDrobna = 12f;", self.src)
        self.assertIn("BraxChampionsWydajnosc.WarstwaDrobne : BraxChampionsWydajnosc.WarstwaMale", self.src)

    def test_small_batches_left_whole(self):
        # templates (goods, pins, a TV screen, an escalator handrail) are far below the threshold: one mesh per material
        self.assertIn("bool dziel = Klasy && Triangles > ProgPodzialu;", self.src)
        self.assertIn("if (!dziel) { Emit(g, name + \" | \" + m.name, m, p, null, u, shadows, assetFolder, 0); continue; }", self.src)

    def test_runtime_layers_match(self):
        rt = (ROOT / "unity-city/Assets/Mistrzowie/BraxChampionsWydajnosc.cs").read_text(encoding="utf-8")
        self.assertIn("public const int WarstwaMale = 20, WarstwaDrobne = 21;", rt)


class OwnerModelScaleTests(unittest.TestCase):
    def test_every_region_placement_keeps_the_root_scale(self):
        for f in ED.glob("*.cs"):
            if f.name == "BraxChampionsModel.cs":
                continue
            self.assertNotIn("BraxMiastoAssety.Postaw(", f.read_text(encoding="utf-8"), f.name)
        model = (ED / "BraxChampionsModel.cs").read_text(encoding="utf-8")
        self.assertIn("Vector3.Scale(s, o.transform.localScale)", model)
        self.assertTrue((ED / "BraxChampionsModel.cs.meta").exists())

    def test_aquapark_measures_its_models(self):
        w = (ED / "BraxChampionsAquaparkWyposazenie.cs").read_text(encoding="utf-8")
        self.assertIn("Dopasuj(o, plik, at);", w)
        for name in ("sauna_kabina", "prysznic_kabina", "jacuzzi_wanna", "ratownik_krzeslo", "stol_masaz_drewno", "lezanka_masaz_tajski"):
            self.assertRegex(w, r'\{ "%s", \(' % name)
        sauna = re.search(r'\{ "sauna_kabina", \(([^)]*)\)', w).group(1)
        lo, hi, cel, _ = [float(x.strip().rstrip("f")) for x in sauna.split(",")]
        self.assertTrue(lo <= 2.1 <= hi and abs(cel - 2.1) < .01)


if __name__ == "__main__":
    unittest.main()
