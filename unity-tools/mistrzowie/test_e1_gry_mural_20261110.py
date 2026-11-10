"""E1 Centrum Rozrywki, owner 10.11 20:45-21:31: the games are PLAYED (bowling with real pins and scoring, billiards with
real pockets), every screen shows something live, the furniture is rounded and dressed in real materials, and the owner's
picture is the mural on the wall at the west corner (upscaled, mounted flat) - the name sign E1 stays a 3D sign."""
import re
import struct
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
RT = ROOT / "unity-city/Assets/Mistrzowie"


def code(path):
    return re.sub(r"//.*", "", path.read_text(encoding="utf-8"))


class E1Games(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.e1 = code(ED / "BraxChampionsE1.cs") + code(ED / "BraxChampionsE1Premium.cs")
        cls.kregle = code(RT / "BraxChampionsKregle.cs")
        cls.bilard = code(RT / "BraxChampionsBilard.cs")
        cls.piksele = code(RT / "BraxChampionsPiksele.cs")
        cls.ekrany = code(RT / "BraxChampionsE1Ekrany.cs")

    def test_runtime_scripts_have_metas_and_unity6_guards(self):
        for n in ("BraxChampionsGry", "BraxChampionsGryHud", "BraxChampionsKregle", "BraxChampionsBilard", "BraxChampionsPiksele", "BraxChampionsE1Ekrany"):
            self.assertTrue((RT / (n + ".cs.meta")).exists(), n)
        for src in (self.kregle, self.bilard):
            self.assertIn("#if UNITY_6000_0_OR_NEWER", src)
            self.assertEqual(src.count(".velocity"), 2)                     # only in Predkosc's #else (linearVelocity in Unity 6)
        self.assertNotIn(".angularDamping =", self.bilard.split("static class BraxChampionsRigidbodyCompat")[0])

    def test_bowling_is_played(self):
        self.assertIn("kg.kregle=pins;kg.kula=ball;kg.ekrany=screens;kg.konsole=consoles;", self.e1)
        self.assertIn("rb[i].centerOfMass = new Vector3(0f, srodekCiezkosci, 0f);", self.kregle)
        for c in ('"rynna"', '"tor"', '"dół za torami"', '"przegroda"'):            # real lane / gutter / pit colliders
            self.assertIn(c, self.e1)
        for f in ("Ramki", "Pozycja", "ZnakiRamki", "Suma", "TabelaToru"):
            self.assertIn("public static", self.piksele.split(f + "(")[0][-120:])
        self.assertIn("for (int l = 0; l < faul.Length; l++) { gry[l] = new Gra(); Rysuj(l); }", self.kregle)   # idle scorecards

    def test_billiards_is_played(self):
        self.assertIn("var bil=game.gameObject.AddComponent<BraxChampionsBilard>();", self.e1)
        self.assertIn("PocketCasting(cx,cz,P,R,poly,top-.03f,castTop,top+.006f);", self.e1)
        self.assertEqual(self.e1.count('"banda"'), 2)
        self.assertIn("ZaStol(k, i)", self.bilard)
        # WPA 9 ft: field 2.54 x 1.27, ball 57.2 mm, cloth 79 cm over the floor
        for k, v in (("PoolHX", .635), ("PoolHZ", 1.27), ("PoolBall", .0286), ("PoolCloth", .79)):
            self.assertAlmostEqual(float(re.search(k + r"=([0-9.]+)f", self.e1).group(1)), v)

    def test_every_screen_shows_something(self):
        self.assertIn("ek.tablice=boardR.ToArray();ek.kioski=kioskR.ToArray();ek.automaty=arcadeR.ToArray();ek.cymbergaj=hockeyR.ToArray();", self.e1)
        self.assertNotRegex(self.e1, r"\bB\(screen,")                      # no dead blue boxes: every screen has a picture
        for f in ("Tablica", "Kiosk", "Automat", "Cymbergaj"):
            self.assertIn("BraxChampionsPiksele." + f + "(", self.ekrany)

    def test_furniture_rounded_and_real(self):
        for f in ("static void Sofa(", "static void Stool(", "static void Chair(", "static void Bench(", "static void Toilet(",
                  "static void Arcade(", "static void AirHockey(", "static void VRPod(", "static void ClawMachine(", "static void Desk("):
            self.assertIn(f, self.e1)
        self.assertNotIn("BenchFramed(m,cols,5f", self.e1)               # hall benches are leather, not the street bench
        self.assertNotIn("whiteLit,cx-.19f", self.e1)                    # no lit boxes for WC pans
        self.assertIn('velvet=Inside(Tex("settee_plum_velvet","velour_velvet"', self.e1)

    def test_mural_is_the_owners_picture_on_the_wall(self):
        png = ROOT / "unity-city" / "Assets/Mistrzowie/Textures/E1/e1_mural_owner_4096.png"
        self.assertIn('MuralPath="Assets/Mistrzowie/Textures/E1/e1_mural_owner_4096.png"', self.e1)
        head = png.read_bytes()[:24]
        self.assertEqual(struct.unpack(">II", head[16:24]), (4096, 1536))
        self.assertTrue(Path(str(png) + ".meta").exists())
        self.assertTrue((ROOT / "docs/zlecenia/plansze/MISTRZOWIE_E1_MURAL_WLASCICIEL_20261110.png").exists())
        mural = self.e1.split("static void Mural()")[1].split("static void FrontSigns()")[0]
        self.assertNotIn("Pin(", mural)                                   # flat, no giant 3D pins
        self.assertIn('TexQuad(root,"E1 mural owner print",muralArt', mural)
        self.assertIn("x0=-60.40f", mural)                                # at the west corner
        self.assertIn('"E1",new Vector3(5f,10.4f,zp+.01f),180f,3.6f,.90f', self.e1)   # the name sign stays 3D


if __name__ == "__main__":
    unittest.main()
