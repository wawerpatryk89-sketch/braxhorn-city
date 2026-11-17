"""Owner 16.11.2026 23:41 / 23:44 and 17.11: KASYNO WAWER - "prywatne pomieszczenie, do którego ... wejście będzie nikomu
nieznane ... przez szafę", "dyskretne pomieszczenie, biuro chcę mieć dla siebie", inside: "Monitoring sali na żywo, Sejf ze
złotem". The office block in the hall's rear east corner (off the side windows, against the windowless rear wall), its door
a wardrobe on a hinge, six hall cameras live on its screens, an open safe with London Good Delivery bars and his coins."""
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
RT = ROOT / "unity-city/Assets/Mistrzowie"
HALL = (ED / "BraxChampionsCasinoT11Hall.cs").read_text(encoding="utf-8")
KIT = (ED / "BraxChampionsKasynoBiuroWlasciciela.cs").read_text(encoding="utf-8")
SZAFA = (RT / "BraxChampionsSzafa.cs").read_text(encoding="utf-8")
MON = (RT / "BraxChampionsMonitoring.cs").read_text(encoding="utf-8")


class Office(unittest.TestCase):
    def test_block_off_the_windows(self):
        self.assertIn("const float OfX0=26.0f,OfX1=33.0f,OfZ0=19.0f,OfZ1=27.35f", HALL)
        x1 = float(re.search(r"OfX1=([\d.]+)f", HALL).group(1))
        self.assertLess(x1, 37.8 - 4.0)                 # the side windows (x 38.3) keep looking into the hall
        self.assertIn('Optional("T11 ukryte biuro właściciela",()=>BiuroWlasciciela(g,marble,wall,ceiling,gold));', HALL)

    def test_door_is_a_wardrobe(self):
        self.assertIn('Szafa(sb, new Vector3(sW * .5f, 0f, -sD * .5f), sW, sD, sH);', KIT)
        self.assertIn('var szafa = sb.Build(zawias,', KIT)          # built in the hinge's frame: turns with it
        self.assertIn("drzwi.zawias = zawias; drzwi.katOtwarcia = -90f;", KIT)
        self.assertIn('public string napis = "SZAFA"', SZAFA)        # the label tells nobody it is a door
        self.assertIn("public Vector3 os = Vector3.up;", SZAFA)                 # the wardrobe turns about its up axis
        self.assertIn("zawias.localRotation = spoczynek * Quaternion.AngleAxis(kat, os);", SZAFA)
        self.assertIn("BraxInterakcje.Dodaj(this);", SZAFA)

    def test_live_monitoring_never_a_film(self):
        self.assertEqual(len(re.findall(r'new BraxChampionsKasynoSprzet\.KameraHali\{nazwa="', HALL)), 6)
        self.assertIn("var cam = co.AddComponent<Camera>(); cam.enabled = false;", KIT)
        self.assertIn("kamery[nastepna].Render(); break;", MON)        # one camera a frame, only in the office
        self.assertIn("if (rt.Length == 0 || !WBiurze()) return;", MON)
        self.assertNotIn("VideoPlayer", KIT + MON)

    def test_safe_with_real_gold_and_his_coins(self):
        self.assertIn("const float BL = .250f, BW = .070f, BH = .035f;", KIT)     # LBMA Good Delivery: 250 x 70 x 35 mm
        self.assertIn("Mathf.Tan(15f * Mathf.Deg2Rad)", KIT)                       # undercut within 5-25 degrees
        self.assertIn("szyldyArk[twarz == 1 ? ArkMonetaA : ArkMonetaR]", KIT)      # his coin art 1:1
        self.assertIn("Obraz(b, szyldyArk[ArkLogo]", KIT)

    def test_screen_cameras_are_not_a_second_player(self):
        # the LOTOTO studio camera and the monitoring cameras are disabled (they render on demand): the explorer's guard
        # against a duplicated player camera counts only enabled cameras (build 45 stopped on it otherwise)
        exp = (ED / "BraxChampionsExplorer.cs").read_text(encoding="utf-8")
        self.assertIn("All<Camera>(scene).Count(c => c.enabled) != 0", exp)
        self.assertIn("All<Camera>(scene).Count(c=>c.enabled)!=1", exp)
        self.assertNotIn("All<Camera>(scene).Length", exp)

    def test_metas_and_preview(self):
        for f in (ED / "BraxChampionsKasynoBiuroWlasciciela.cs.meta", RT / "BraxChampionsSzafa.cs.meta", RT / "BraxChampionsMonitoring.cs.meta"):
            self.assertTrue(f.exists(), f)
        proj = (ROOT / "unity-tools/mistrzowie/casino_preview/casino_preview.csproj").read_text(encoding="utf-8")
        for f in ("BraxChampionsKasynoBiuroWlasciciela.cs", "BraxChampionsSzafa.cs", "BraxChampionsMonitoring.cs"):
            self.assertIn(f, proj)


if __name__ == "__main__":
    unittest.main()
