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
        self.assertIn("if (kolT) kolT.localRotation *= Quaternion.AngleAxis(obrot, Vector3.right);", VESSEL)
        self.assertIn("v.kolP = piastaP; v.kolT = piastaT; v.rozstaw = g.rozstaw; v.rKola = (g.kp.R + g.kt.R) * .5f;", KOLA)

    def test_every_owner_motorcycle_gets_wheels(self):
        a = MOTORY.index("public static BraxVessel Motor(")
        b = MOTORY.index("Lod(v.gameObject, .012f);", a)
        self.assertIn("bool kola = BraxKolaMotoru.Wydziel(v, o, L, plik, out string opisKol);", MOTORY[a:b])
        self.assertIn('"DO POPRAWKI: BRAXHORN_MOTOR_KOLA "', MOTORY[a:b])                   # bez kół = jak dotąd, z wpisem

    def test_detection_rules(self):
        self.assertIn("for (float R = .10f * L; R <= .20f * L; R += .004f * L)", KOLA)
        self.assertIn("float s = n * (pok / (float)Wycinkow) * (pok / (float)Wycinkow);", KOLA)   # pełny okrąg opony
        self.assertIn("float krok = .002f * L, cap = (przednie ? .040f : .058f) * L;", KOLA)        # lagi / wahacz poza kołem
        self.assertIn("if (rozstaw < .55f * L || rozstaw > .78f * L)", KOLA)
        self.assertIn("if (nP < 50 || nT < 50)", KOLA)

    def test_sport_bike_at_gym(self):
        self.assertIn('BraxMiastoMotory.PodSilownia(gj, miejscaM, Vector3.forward)', RUCH)
        self.assertIn('var v = Motor(rodzic, 0, p, yaw);', MOTORY)                          # nr 0 = motor_sportowy_czerwony
        self.assertIn('"motor_sportowy_czerwony"', MOTORY)
        self.assertIn("Physics.CheckBox(p + Vector3.up * 1.0f, new Vector3(.55f, .8f, 1.35f * K), Quaternion.Euler(0f, yaw, 0f)", MOTORY)

    @unittest.skipUnless(shutil.which("dotnet"), "brak dotnet")
    def test_split_on_test_sport_bike(self):
        b = subprocess.run(["dotnet", "build", "-nologo", "-v", "q"], cwd=TEST, capture_output=True, text=True, timeout=600)
        self.assertEqual(b.returncode, 0, b.stdout[-2000:])
        r = subprocess.run(["dotnet", "bin/Debug/net8.0/kola_motoru.dll"], cwd=TEST, capture_output=True, text=True, timeout=300)
        self.assertIn("BRAXHORN_TEST_KOLA osie znalezione, trójkątów w złej grupie: 0", r.stdout, r.stdout)
        self.assertEqual(r.returncode, 0)


if __name__ == "__main__":
    unittest.main()
