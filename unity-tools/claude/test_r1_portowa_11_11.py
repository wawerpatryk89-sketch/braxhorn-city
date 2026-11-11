"""MIASTO R1, 11.11.2026 — Portowa posprzątana, mural 19 na całą ścianę, szyld BRAXHORN GYM w 3D.

Właściciel 14:14: „mural masz zrobić w końcu prawidłowo; z całej ulicy usuwasz drzewa i odblokowujesz każde wejście …
całą ulicę sprzątasz porządnie — Portowa”; 14:43: „napis BRAXHORN GYM ma zrobić ładny w 3D. Tam to usuń”; 14:50: „na ulicy
Portowej nie musi być przystanków”; 15:04: „B.” (daszek nad drzwiami); 15:08: latarnie „tylko i wyłącznie nowe”.
"""
import hashlib
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
REG = ROOT / "unity-city/Assets/Editor/Claude/Region"


def code(name):
    return (REG / name).read_text(encoding="utf-8")


class R1Portowa(unittest.TestCase):
    def test_no_trees_on_portowa(self):
        c = code("BraxMiastoUlica.cs")
        body = c[c.index("public static void Apply(Transform cityRoot)"):c.index("static bool Drzewo(")]
        self.assertNotIn("Drzewo(cityRoot", body)

    def test_every_door_kept_clear(self):
        c = code("BraxMiastoUlica.cs")
        self.assertIn("const float DrzwiZapas = 2.3f;", c)
        self.assertIn("ZbierzDrzwi(cityRoot);", c)
        self.assertIn("Mathf.Abs(x - d.x) < DrzwiZapas", c)
        p = code("BraxUlicePierzeje.cs")
        self.assertIn("const float WolneOdDrzwi = 2.3f;", p)

    def test_bus_stop_removed_not_moved(self):
        c = code("BraxMiastoUlica.cs")
        self.assertIn("static string UsunPrzystanek(Transform cityRoot)", c)
        self.assertIn('"Finished BRAXHORN bus shelter"', c)
        self.assertNotIn("WiataOdDrzwi", c)

    def test_only_original_invisible_colliders_disabled(self):
        c = code("BraxMiastoUlica.cs")
        body = c[c.index("static int UsunNiewidzialneKolizje()"):c.index("static string UsunPrzystanek(")]
        self.assertIn('StartsWith("Original round collider"', body)
        self.assertIn("if (!oryginal) continue;", body)
        self.assertIn("if (Widoczna(b)) continue;", body)          # obrys bez zapasu (gzymsy elewacji nie liczą się jako bryła)


class R1Mural19AndGymSign(unittest.TestCase):
    def test_mural_covers_whole_wall(self):
        self.assertIn("const bool MuralCalaSciana = true;", code("BraxMiastoBillboardy.cs"))

    def test_flat_sign_off_before_mural_and_3d_sign_after(self):
        c = code("BraxMiastoBillboardy.cs")
        body = c[c.index("static void MuralSilowni(Transform cityRoot, Transform rodzic)"):c.index("static void MuralSilowniWlasciwy(")]
        self.assertLess(body.index("BraxSzyldGym3D.ZdejmijPlaski()"), body.index("MuralSilowniWlasciwy(cityRoot, rodzic, usuniete)"))
        self.assertIn("BraxSzyldGym3D.Postaw(rodzic, licoSciany)", body)

    def test_owner_chose_canopy_b(self):
        s = code("BraxSzyldGym3D.cs")
        self.assertRegex(s, r"public const char Wariant = 'B';")
        self.assertIn("BraxLiteryBebas.Napis(Tekst", s)
        self.assertNotIn("TextMesh", s)

    def test_font_licensed_in_repo_and_data_matches_font(self):
        font = ROOT / "unity-tools/claude/fonts/BebasNeue-Regular.ttf"
        self.assertTrue((ROOT / "unity-tools/claude/fonts/BebasNeue-OFL.txt").read_text(encoding="utf-8").find("SIL Open Font License") >= 0)
        sha = hashlib.sha256(font.read_bytes()).hexdigest()
        dane = code("BraxLiteryBebasDane.cs")
        self.assertIn(f'FontSha256 = "{sha}"', dane)
        for ch in "BRAXHORNGYM":
            self.assertIn(f"['{ch}'] = (", dane)


if __name__ == "__main__":
    unittest.main()
