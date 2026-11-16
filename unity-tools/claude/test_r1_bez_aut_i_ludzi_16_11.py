"""MIASTO R1 16.11.2026 14:56 — właściciel: „Usuwasz wszystkie pojazdy (samochody) z gry i ludzi! Wszystkie te w powietrzu, na
drzewach, w lesie też!”; autobus: „mamy nowy ze środkiem… go bierzemy, nie tego MAN-a”. Źródło, bez Unity i APK."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor"


def src(p):
    return re.sub(r"//[^\n]*", "", Path(p).read_text(encoding="utf-8"))


BEZ = src(ED / "Claude/Region/BraxBezAutILudzi.cs")
REGION = src(ED / "Claude/Region/BraxRegion.cs")
GYM = src(ED / "BraxGymBuild.cs")
ACC = src(ED / "Claude/BraxR1SceneAcceptance.cs")


class BezAutILudzi(unittest.TestCase):
    def test_switch_on(self):
        self.assertIn("public static readonly bool Wlaczone = true;", BEZ)

    def test_traffic_and_people_steps_not_run(self):
        self.assertIn("if (!BraxBezAutILudzi.Wlaczone) BraxClaudeOsiedle.RuchMiastaB(root);", REGION)
        self.assertIn("if (!BraxBezAutILudzi.Wlaczone) BraxClaudeOsiedle.LudzieMiastaB(root);", REGION)

    def test_scene_swept_twice(self):
        i_l = REGION.index("BraxClaudeOsiedle.LudzieMiastaB(root)")
        i_s = REGION.index('BraxBezAutILudzi.Usun(root.gameObject.scene, "krok rejonu")')
        i_z = REGION.index("BraxKontrolaZakamarkow.Wykonaj(world, root)")
        self.assertLess(i_l, i_s); self.assertLess(i_s, i_z)
        self.assertLess(GYM.index("BraxSupermarket.Wykonaj(scene,2)"), GYM.index('BraxBezAutILudzi.Usun(scene,"po supermarkecie")'))

    def test_sweep_targets_and_keeps(self):
        for co in ("GetComponentsInChildren<BraxAuto>", "GetComponentsInChildren<BraxRuchAut>", "mb is IBraxOsoba", "an.avatar.isHuman"):
            self.assertIn(co, BEZ)
        for chroniony in ("BraxGymPlayer", "BraxArenaWalka", "BraxArenaPokaz", '"manekin"'):
            self.assertIn(chroniony, BEZ)

    def test_acceptance_expects_no_cars(self):
        self.assertIn('if(BraxBezAutILudzi.Wlaczone){ if(cars.Length>0)bad.Add("BEZ AUT: zostało aut "+cars.Length); }', ACC)


if __name__ == "__main__":
    unittest.main()
