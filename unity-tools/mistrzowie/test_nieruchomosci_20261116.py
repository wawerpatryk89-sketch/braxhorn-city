"""Owner 16.11.2026 23:12-23:13: WAWER NIERUCHOMOŚCI - "To masz do Biura Nieruchomości" with his poster "Poznaj Monikę -
Prowadzę biuro nieruchomości w Mieście Mistrzów", "U ciebie też biura i tylko wille kozaki zrobimy na sprzedaż 3", "W mieście
mistrzów tylko zrobimy 3 wille na sprzedaż ... naprawdę bogate na jakimś wzgórzu".

The office is the west wing of the T12 tower's ground floor with its own street door: Monika's desk, the model of the three
villas on the hill, her poster (his art 1:1, from the encrypted pack only) in the window and framed on the wall."""
import pathlib
import subprocess
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
TOOLS = ROOT / "unity-tools/mistrzowie"
KIT = (ED / "BraxChampionsKasynoBiuro.cs").read_text(encoding="utf-8")
TOWER = (ED / "BraxChampionsWiezowce.cs").read_text(encoding="utf-8")


class Office(unittest.TestCase):
    def test_in_the_t12_ground_floor(self):
        st = TOWER[TOWER.index("static float Stacked(Tower t)"):TOWER.index("static void Terraces(")]
        self.assertIn("GroundFloor(A,-13f,new[]{0f,-12f},core);", st)            # its own street door at x -12
        self.assertIn("LobbyFitOut(core,new Vector2(10f,-8f),new Vector2(12.5f,4.5f),false,4,OfficeX);", st)
        self.assertIn("Nieruchomosci(A);", st)
        self.assertIn("BraxChampionsKasynoSprzet.Biuro(inside,kat,x0+.2f,OfficeX-.30f,z0+.15f,z1-.15f,y0,-12f);", st)
        self.assertIn('BraxChampionsLetters3D.Text(letters,ledWhite,black,"WAWER NIERUCHOMOŚCI"', st)

    def test_monika_her_desk_and_the_villas(self):
        self.assertIn('W.Obsada("Monika - WAWER NIERUCHOMOŚCI"', KIT)
        self.assertIn('W.Akcja("nieruchomosci_wille", "WAWER NIERUCHOMOŚCI · ZAPYTAJ O WILLĘ"', KIT)
        self.assertIn('W.Akcja("nieruchomosci_makieta", "MAKIETA · TRZY WILLE NA WZGÓRZU"', KIT)
        wille = next(l for l in KIT.splitlines() if "var wille = new[]" in l)
        self.assertEqual(wille.count("(new Vector3("), 3)                           # three villas, no more

    def test_her_poster_only_from_the_pack(self):
        self.assertIn('Wczytaj(NieruchomosciWlasciciel + "/plakaty/wawer_nieruchomosci_monika.png"', KIT)
        self.assertIn("Wklej(pp, 1024, 1536, src, sw, sh, 0, 0, sw, sh, 0, 0, 1024, 1536);", KIT)   # 1:1, no lettering of ours
        self.assertIn("DO POPRAWKI NIERUCHOMOSCI_PLAKAT", KIT)
        tracked = subprocess.run(["git", "ls-files", "unity-city/Assets/Source/Mistrzowie/NieruchomosciWlasciciel"], cwd=ROOT,
                                 capture_output=True, text=True).stdout
        self.assertEqual(tracked.strip(), "")
        self.assertIn("NieruchomosciWlasciciel", (ROOT / ".gitignore").read_text(encoding="utf-8"))

    def test_preview_compiles_it(self):
        self.assertIn("BraxChampionsKasynoBiuro.cs", (TOOLS / "wiezowce_preview/wiezowce_preview.csproj").read_text(encoding="utf-8"))
        self.assertTrue((ED / "BraxChampionsKasynoBiuro.cs.meta").exists())


if __name__ == "__main__":
    unittest.main()
