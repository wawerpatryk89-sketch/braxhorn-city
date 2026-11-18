"""MIASTO R1 — szyldy (właściciel 18.11 10:40: „Szyldy, wawer nieruchomości, co to za gówno, zwykły napis… Każdy jeden”).
Litery miasta były kreskami stałej grubości (krój „z patyków”). Teraz każdy napis przestrzenny i każdy napis w teksturze idzie
krojem Bebas Neue (BraxLiteryBebas, zasada napisów premium 10.11, jak zatwierdzony szyld BRAXHORN GYM), szyldy lokali kamienic
to nośnik z ramą + litery dystansowe ze stali + profil LED pod nośnikiem, szyldy lokali wieżowców dostają profil LED. Test pilnuje źródła."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def code(rel):
    return re.sub(r'//[^\n]*|/\*[\s\S]*?\*/', "", (ROOT / rel).read_text(encoding="utf-8"))


KIT = code("unity-city/Assets/Editor/Claude/Region/BraxUliceKit.cs")
BEBAS = code("unity-city/Assets/Editor/Claude/Region/BraxLiteryBebas.cs")
PIERZ = code("unity-city/Assets/Editor/Claude/Region/BraxUlicePierzeje.cs")
WIEZE = code("unity-city/Assets/Editor/Claude/Region/BraxRegionFawelaWiezeLokale.cs")


def blok(s, start, koniec):
    a = s.index(start); return s[a:s.index(koniec, a + len(start))]


class R1SzyldyBebas(unittest.TestCase):
    def test_every_3d_text_is_bebas(self):
        n = blok(KIT, "public static float Napis3D(", "public static void NapisWTeksturze(")
        self.assertIn("if (CalyBebas(tekst))", n)
        self.assertIn("BraxLiteryBebas.Napis(tekst, o + up * ((cap - c2) * .5f), nn, c2, glebokosc, lico, boki);", n)
        self.assertIn("Mathf.Min(cap * BebasMaxWyzej, cap * stara / b)", KIT)   # układ szyldów bez zmian
        self.assertIn("public const float BebasMaxWyzej = 1.15f;", KIT)

    def test_every_texture_text_is_bebas(self):
        n = blok(KIT, "public static void NapisWTeksturze(", "static void OdcinekWTeksturze(")
        self.assertIn("BraxLiteryBebas.WTeksturze(px, W, H, tekst, cx, by + (cap - c2) * .5f, c2, kolor);", n)
        self.assertIn("public static void WTeksturze(Color[] px, int W, int H, string tekst", BEBAS)
        self.assertIn("for (int sy = 0; sy < 4; sy++) for (int sx = 0; sx < 4; sx++)", BEBAS)   # gładkie krawędzie

    def test_no_floating_halo_outline(self):
        # płaski obrys „halo” 3 cm przed nośnikiem z boku wyglądał jak biały cień liter — zamiast niego profil LED pod nośnikiem
        self.assertNotIn("Halo(", BEBAS)
        self.assertNotIn("NapisHalo(", KIT)

    def test_shop_sign_inside_wall_band_not_on_glass(self):
        # 12:35 (właściciel: „na szybie wisi szyld”): cały szyld między nadprożem a gzymsem (3.50); od 14:21 nadproże drzwi
        # = nadproże witryn 3.05 (szyld nad drzwiami w pełnym pasie)
        s = blok(PIERZ, "static void Szyldy(Fasada f)", "poSzyldzie:")
        self.assertIn("const float yNad = DrzwiGora, yGzyms = 3.50f, ramaW = .025f, mBok = .2f, dN0 = .02f, dN1 = .08f;", s)
        self.assertIn("const float WitrynaGora = 3.05f, DrzwiGora = WitrynaGora;", PIERZ)
        self.assertIn("float y0 = yNad + .005f + ramaW, y1 = yGzyms - .005f - ramaW, yS = (y0 + y1) * .5f;", s)
        self.assertIn("float hG = y1 - y0 - .02f, wG = Mathf.Min(maxW, hG * 5f);", s)            # grafiki SOBCZAK w pasie
        self.assertIn('BraxUliceKit.Mat("szyld: litery — lico (stal nierdzewna szczotkowana)"', s)
        self.assertIn("OBox(f, listwa, a, b, y0, y1, dN0, dN1, false);", s)
        self.assertIn("OBox(f, ramaN, a - ramaW, b + ramaW, y1, y1 + ramaW, dN0, dN1 + .01f);", s)
        self.assertNotIn("3.33f", s)                                                               # stary środek grafiki 52 cm
        self.assertNotIn("3.02", s)

    def test_tower_shop_signs_led(self):
        self.assertIn('BraxMiastoA.Swieci("szyld: profil LED pod nośnikiem (ciepła biel)"', WIEZE)
        self.assertIn(".Box(F.P(sa + .1f, yc - .36f - .025f, -.22f), F.P(sb - .1f, yc - .36f, -.25f));", WIEZE)


if __name__ == "__main__":
    unittest.main()
