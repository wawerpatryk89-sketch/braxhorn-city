"""MIASTO R1 18.11 — ścigacz pod siłownią z kołami, które się kręcą (14:49: „Zrób mi motor, w którym się kręcą koła. Podstaw go
pod siłownię, żebym mógł jeździć”; „A mamy tam ścigacze gdzieś są”). Koła wycinane z bryły modelu Tripo przy buildzie
(BraxKolaMotoru), test na ścigaczu testowym z prawdziwymi wymiarami (unity-tools/claude/kola_motoru)."""
import shutil
import subprocess
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
REG = ROOT / "unity-city/Assets/Editor/Claude/Region"
KOLA = (REG / "BraxKolaMotoru.cs").read_text(encoding="utf-8")
MOTORY = (REG / "BraxMiastoMotory.cs").read_text(encoding="utf-8")
RUCH = (REG / "BraxRegionRuchMiasta.cs").read_text(encoding="utf-8")
VESSEL = (ROOT / "unity-city/Assets/Claude/BraxVessel.cs").read_text(encoding="utf-8")
TEST = ROOT / "unity-tools/claude/kola_motoru"


class Kola(unittest.TestCase):
    def test_wheels_spin_in_vessel(self):
        self.assertIn("if (kolP) kolP.localRotation *= Quaternion.AngleAxis(obrot, Vector3.right);", VESSEL)
        self.assertIn("v.kolP = piastaP; v.kolT = piastaT; v.rozstaw = g.rozstaw; v.rKola = (g.kp.R + g.kt.R) * .5f;", KOLA)

    def test_owner_bike_wheels_are_model_nodes(self):
        # 18.11 16:xx: koła ścigacza właściciela = węzły modelu (przygotowane poza Unity na jego modelu); wycinanie przy buildzie
        # nie jest używane (opony Tripo spłaszczone u dołu i zrośnięte z wahaczem → zła oś)
        a = MOTORY.index("static BraxVessel Motor(Transform rodzic, string paczka, string plik")
        b = MOTORY.index("Lod(v.gameObject, .012f);", a)
        blok = MOTORY[a:b]
        self.assertNotIn("BraxKolaMotoru.Wydziel(", MOTORY)
        self.assertIn('var kp = BraxMiastoAssety.Znajdz(o.transform, "kolo_przednie"); var kt = BraxMiastoAssety.Znajdz(o.transform, "kolo_tylne");', blok)
        self.assertIn("v.rozstaw = lp.z - lt.z; v.rKola = (Promien(kp) + Promien(kt)) * .5f; KolaKreca++;", blok)
        self.assertIn('PaczkaWlasciciela = "pojazdy_wlasciciela_01", ScigaczWlasciciela = "motor_scigacz_czerwony", AutoWlasciciela = "auto_braxhorn_czarne"', MOTORY)

    def test_detection_rules(self):
        self.assertIn("for (float R = .10f * L; R <= .20f * L; R += .004f * L)", KOLA)
        self.assertIn("float s = n * (pok / (float)Wycinkow) * (pok / (float)Wycinkow);", KOLA)   # pełny okrąg opony
        self.assertIn("float krok = .002f * L, cap = (przednie ? .040f : .058f) * L;", KOLA)        # lagi / wahacz poza kołem
        self.assertIn("if (rozstaw < .55f * L || rozstaw > .78f * L)", KOLA)
        self.assertIn("if (nP < 50 || nT < 50)", KOLA)

    def test_sport_bike_at_gym(self):
        self.assertIn('BraxMiastoMotory.PodSilownia(gj, miejscaM, Vector3.forward)', RUCH)
        self.assertIn('var v = wl ? Motor(rodzic, PaczkaWlasciciela, ScigaczWlasciciela, p, yaw) : Motor(rodzic, 0, p, yaw);', MOTORY)
        self.assertIn('"motor_sportowy_czerwony"', MOTORY)
        self.assertIn("Physics.CheckBox(p + Vector3.up * 1.0f, new Vector3(.55f, .8f, 1.35f * K), Quaternion.Euler(0f, yaw, 0f)", MOTORY)

    @unittest.skipUnless(shutil.which("dotnet"), "brak dotnet")
    def test_split_on_test_sport_bike(self):
        b = subprocess.run(["dotnet", "build", "-nologo", "-v", "q"], cwd=TEST, capture_output=True, text=True, timeout=600)
        self.assertEqual(b.returncode, 0, b.stdout[-2000:])
        r = subprocess.run(["dotnet", "bin/Debug/net8.0/kola_motoru.dll"], cwd=TEST, capture_output=True, text=True, timeout=300)
        self.assertIn("BRAXHORN_TEST_KOLA osie znalezione, trójkątów w złej grupie: 0", r.stdout, r.stdout)
        self.assertEqual(r.returncode, 0)


class AutoWlasciciela(unittest.TestCase):
    """15:13: „ten samochód wstawisz tak samo… do jeżdżenia po mieście” — auto z paczki pojazdy_wlasciciela_01 pod siłownią."""

    def test_car_drives_on_land(self):
        for n in ("public bool auto;", "public Transform[] kolaAuta = new Transform[0], skretAuta = new Transform[0];",
                  "foreach (var k in kolaAuta) if (k) k.localRotation *= Quaternion.AngleAxis(obrot, Vector3.right);",
                  "foreach (var t in skretAuta) if (t) t.localRotation = Quaternion.AngleAxis(skretS, Vector3.up);",
                  "Physics.BoxCastNonAlloc(pos + Vector3.up * .65f - dir * .2f, new Vector3(width * .45f, .3f, .1f)",
                  'pose.Set(label == "SKUTER" || lad && !auto ? BraxPose.Mode.Ride : (jazda ? BraxPose.Mode.Kierowca : BraxPose.Mode.Ster), transform);'):
            self.assertIn(n, VESSEL)

    def test_car_built_and_placed(self):
        a = MOTORY.index("public static BraxVessel Auto("); blok = MOTORY[a:MOTORY.index("public static BraxVessel AutoPodSilownia(")]
        for n in ('string[] kola = { "kolo_lp", "kolo_pp", "kolo_lt", "kolo_pt" };', 'BraxMiastoAssety.Znajdz(o.transform, "skret_lp")',
                  "v.auto = true;", "v.wejscie = new Vector3(-(W * .5f + .5f * K), 0f, .05f * K);", "v.seat = new Vector3(-.405f * K, .68f * K, -.155f * K);"):
            self.assertIn(n, blok)
        self.assertIn("BraxMiastoMotory.AutoPodSilownia(gj, miejscaA, Vector3.forward);", RUCH)
        self.assertIn("Physics.CheckBox(p + Vector3.up * (.85f * K), new Vector3(1.2f * K, .75f * K, 2.6f * K), q", MOTORY)

    def test_pack_encrypted_with_both_models(self):
        import json
        m = json.loads((ROOT / "unity-city/Prywatne/braxhorn-sklep-pojazdy-wlasciciela-01/manifest.json").read_text())
        self.assertEqual(m["target"], "pojazdy_wlasciciela_01")
        for n in ("pojazdy_wlasciciela_01/motor_scigacz_czerwony.glb", "pojazdy_wlasciciela_01/auto_braxhorn_czarne.glb",
                  "pojazdy_wlasciciela_01/_tekstury/auto_braxhorn_czarne/naklejki.png", "pojazdy_wlasciciela_01/_tekstury/motor_scigacz_czerwony/kolor.jpg",
                  "oryginal/pojazd_auto_braxhorn.py", "oryginal/pojazd_motor_scigacz.py", "pojazdy_wlasciciela_01/katalog.json"):
            self.assertIn(n, m["files"])
        # w gicie nic jawnego od właściciela (jego pliki tylko w zaszyfrowanej paczce)
        tracked = [str(p.relative_to(ROOT)) for p in (ROOT / "unity-city").rglob("*tripo_convert_840fea80*")] + \
                  [str(p.relative_to(ROOT)) for p in (ROOT / "unity-city").rglob("*auto_braxhorn_czarne*")]
        self.assertEqual(tracked, [])


if __name__ == "__main__":
    unittest.main()
