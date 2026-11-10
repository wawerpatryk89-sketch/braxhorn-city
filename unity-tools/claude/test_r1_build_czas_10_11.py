"""MIASTO R1 10.11 — build 42810573029 zabity po 4 h 50 min (kod 137, brak pamięci).

Z logu: 12 492 cięcia „migotanie_pierzeje_i_lokale_ulicy” po jednym pudle (każde = nowa kopia siatki + import),
13 212 × AssetDatabase.Refresh() w BraxCityMeshCut.Subtract, 1 838 ponownych importów tych samych tekstur Poly Haven
(teak_veneer 163 ×), 558 ponownych importów modeli, których import już się nie udał. Testy pilnują, żeby to nie wróciło.
"""
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor"
REG = ED / "Claude/Region"


def czytaj(p):
    return pathlib.Path(p).read_text(encoding="utf-8")


def metoda(src, sygn):
    i = src.index(sygn)
    j = src.index("{", i)
    d = 0
    for k in range(j, len(src)):
        if src[k] == "{":
            d += 1
        elif src[k] == "}":
            d -= 1
            if d == 0:
                return src[i:k + 1]
    raise AssertionError(sygn)


class CieciaHurtem(unittest.TestCase):
    def test_subtract_bez_refresh_przy_kazdym_cieciu(self):
        cut = czytaj(ED / "CityRebuild/BraxCityMeshCut.cs")
        sub = metoda(cut, "public static int Subtract(")
        self.assertNotIn("AssetDatabase.Refresh()", sub)
        self.assertIn("Folder(outputDirectory);", sub)
        folder = metoda(cut, "static void Folder(")
        self.assertIn("if (AssetDatabase.IsValidFolder(outputDirectory)) return;", folder)

    def test_wiele_pudel_jeden_zapis_na_siatke(self):
        cut = czytaj(ED / "CityRebuild/BraxCityMeshCut.cs")
        many = metoda(cut, "public static int SubtractMany(")
        self.assertEqual(many.count("BraxZapisAssetu.Utworz("), 1)
        self.assertIn("foreach (var volume in mine)", many)       # wszystkie pudła na tych samych trójkątach w pamięci
        self.assertNotIn("Subtract(", many.replace("SubtractMany(", ""))

    def test_kontrola_migotania_tnie_hurtem(self):
        a = czytaj(REG / "BraxMiastoA.cs")
        k = metoda(a, "static int KontrolaMigotania(") if "static int KontrolaMigotania(" in a else a
        self.assertIn("BraxCityMeshCut.SubtractMany(g.Key.transform, lista,", k)
        self.assertNotIn("BraxCityMeshCut.Subtract(g.Key.transform, bb,", a)


class TeksturyRaz(unittest.TestCase):
    def test_tekstura_z_bazy_przed_importem(self):
        r = czytaj(REG / "BraxRegion.cs")
        t = metoda(r, "public static Texture2D TeksturaRaz(")
        self.assertLess(t.index("LoadAssetAtPath<Texture2D>(path)"), t.index("AssetDatabase.ImportAsset("))
        self.assertIn("if (t) return t;", t)
        self.assertIn("static Texture2D Tex(string path) => TeksturaRaz(path);", r)

    def test_wszystkie_loadery_tekstur_regionu(self):
        kit = czytaj(REG / "BraxUliceKit.cs")
        self.assertIn("BraxRegion.TeksturaRaz(p)", metoda(kit, "public static Material TexPlik("))
        self.assertNotIn("AssetDatabase.ImportAsset(p); var t", kit)
        self.assertIn("static Texture2D Load(string p) => BraxRegion.TeksturaRaz(p);", czytaj(REG / "BraxRegionMeble.cs"))
        self.assertIn("BraxRegion.TeksturaRaz(d + f)", czytaj(ED / "Claude/BraxClaudeVenues.cs"))

    def test_nieudany_model_bez_ponownego_importu(self):
        a = czytaj(REG / "BraxMiastoAssety.cs")
        m = metoda(a, "public static GameObject Model(")
        self.assertLess(m.index("if (nieudane.Contains(p)) return null;"), m.index("AssetDatabase.ImportAsset("))
        self.assertIn("nieudane.Add(p);", m)


if __name__ == "__main__":
    unittest.main()
