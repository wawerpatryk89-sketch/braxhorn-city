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
        # 15:14: zamiast wszystkich aut — sam nowy autobus BX 10006 (tryb TylkoAutobus)
        self.assertIn("if (!BraxBezAutILudzi.Wlaczone) { BraxClaudeOsiedle.RuchMiastaB(root); return; }", REGION)
        self.assertIn("BraxClaudeOsiedle.TylkoAutobus = true;", REGION)
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
        self.assertIn('int inne=cars.Count(x=>!x.name.Contains("Autobus KURSOWY")); if(inne>0)bad.Add("BEZ AUT: zostało aut "+inne);', ACC)
        self.assertIn('if(buses.Length!=1)bad.Add("AUTOBUS: oczekiwany jeden kursowy (BX 10006), jest "+buses.Length);', ACC)


RUCH = src(ED / "Claude/Region/BraxRegionRuchMiasta.cs")
SM = src(ED / "Claude/Region/BraxSupermarket.cs")
GRANICE = src(ROOT / "unity-city/Assets/Claude/Runtime/BraxMiastoGranice.cs")
PACZKA = ROOT / "unity-city/Prywatne/braxhorn-sklep-pojazdy-01"


class NowyAutobus(unittest.TestCase):
    def test_bus_only_mode_and_model(self):
        self.assertIn("public static bool TylkoAutobus;", RUCH)
        self.assertIn("TylkoAutobus || r == busRoute ? 0", RUCH)
        self.assertIn("static GameObject NowyAutobus(GameObject source", RUCH)
        self.assertIn('t.name.StartsWith("Driver seat", StringComparison.Ordinal)', RUCH)   # przód z fotela kierowcy
        self.assertIn('t.name.StartsWith("Wheel ", StringComparison.Ordinal)', RUCH)       # koła na osiach ramy
        self.assertIn("a.kola = kolaBus; a.skret = skretBus;", RUCH)

    def test_encrypted_pack_only(self):
        import json
        m = json.loads((PACZKA / "manifest.json").read_text(encoding="utf-8"))
        self.assertEqual(m["target"], "pojazdy_globalne_01")
        self.assertIn("pojazdy_globalne_01/Braxhorn_Autobus_BX10006.glb", m["files"])
        self.assertTrue((PACZKA / "part-000.cms").exists())
        self.assertFalse(list(PACZKA.glob("*.glb")))                       # nic jawnego w publicznym repo
        self.assertNotEqual((PACZKA / "part-000.cms").read_bytes()[:4], b"glTF")

    def test_sweep_keeps_the_bus(self):
        self.assertIn('a.name.StartsWith("Autobus KURSOWY", StringComparison.Ordinal)', BEZ)


class WejscieSupermarketuGranica(unittest.TestCase):
    """15:13 — „wejście do supermarketu nie istnieje”: sklep stał za granicą miasta (wycięcie lasu z ≥ 126)."""

    def test_store_lot_is_playable(self):
        self.assertIn("DzialkaSupermarketu = Rect.MinMaxRect(-152f, 126f, -100f, 162f)", GRANICE)
        self.assertNotIn("Rect.MinMaxRect(-262f, 126f, -40f, Z1)", GRANICE)
        self.assertIn("Rect.MinMaxRect(-262f, 126f, -152f, Z1), Rect.MinMaxRect(-100f, 126f, -40f, Z1), Rect.MinMaxRect(-152f, 162f, -100f, Z1)", GRANICE)

    def test_corridor_cleared_of_any_collider_and_probed_finely(self):
        self.assertIn("static void KorytarzWejscia(", SM)
        self.assertIn("static bool PrzytnijPudlo(BoxCollider c, Bounds v, Transform rodzic)", SM)
        self.assertIn("for (float z = -14f; z <= 3.01f; z += .2f)", SM)
        self.assertIn("static void Dzialka(", SM)
        i_d = SM.index("Dzialka(scena, root.transform, t)"); i_w = SM.index("OczyscWejscie(scena, root.transform, t, lustro, i + 1);")
        i_k = SM.index("KorytarzWejscia(scena, root.transform, t, WejscieX(lustro, i + 1), i + 1)")
        self.assertLess(i_d, i_w); self.assertLess(i_w, i_k)
        self.assertIn("Mathf.Lerp(-14f,3f,k/68f)", ACC)


if __name__ == "__main__":
    unittest.main()
