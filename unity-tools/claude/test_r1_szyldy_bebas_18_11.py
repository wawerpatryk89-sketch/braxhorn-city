"""MIASTO R1 — szyldy (właściciel 18.11 10:40: „Szyldy, wawer nieruchomości, co to za gówno, zwykły napis… Każdy jeden”).
Litery miasta były kreskami stałej grubości (krój „z patyków”). Teraz każdy napis przestrzenny i każdy napis w teksturze idzie
krojem Bebas Neue (BraxLiteryBebas, zasada napisów premium 10.11, jak zatwierdzony szyld BRAXHORN GYM), szyldy lokali kamienic
to nośnik z ramą + litery dystansowe ze stali + halo LED, szyldy lokali wieżowców dostają halo. Test pilnuje źródła."""
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
        n = blok(KIT, "public static float Napis3D(", "public static int NapisHalo(")
        self.assertIn("if (CalyBebas(tekst))", n)
        self.assertIn("BraxLiteryBebas.Napis(tekst, o + up * ((cap - c2) * .5f), nn, c2, glebokosc, lico, boki);", n)
        self.assertIn("Mathf.Min(cap * BebasMaxWyzej, cap * stara / b)", KIT)   # układ szyldów bez zmian
        self.assertIn("public const float BebasMaxWyzej = 1.15f;", KIT)

    def test_every_texture_text_is_bebas(self):
        n = blok(KIT, "public static void NapisWTeksturze(", "static void OdcinekWTeksturze(")
        self.assertIn("BraxLiteryBebas.WTeksturze(px, W, H, tekst, cx, by + (cap - c2) * .5f, c2, kolor);", n)
        self.assertIn("public static void WTeksturze(Color[] px, int W, int H, string tekst", BEBAS)
        self.assertIn("for (int sy = 0; sy < 4; sy++) for (int sx = 0; sx < 4; sx++)", BEBAS)   # gładkie krawędzie

    def test_halo_more_than_2cm_off_carrier(self):
        self.assertIn("public static int Halo(UlSiatka sh, string tekst", BEBAS)
        self.assertIn("nn.normalized * .025f, nn, c2, obrys);", KIT)

    def test_shop_sign_carrier_frame_letters_halo(self):
        s = blok(PIERZ, "static void Szyldy(Fasada f)", "poSzyldzie:")
        self.assertIn('BraxUliceKit.Mat("szyld: litery — lico (stal nierdzewna szczotkowana)"', s)
        self.assertIn('BraxMiastoA.Swieci("szyld: podświetlenie halo LED (ciepła biel)"', s)
        self.assertIn("const float yS = 3.235f, hN = .43f, mBok = .25f, dN0 = .02f, dN1 = .08f;", s)
        self.assertIn("OBox(f, listwa, a, b, y0, y1, dN0, dN1, false);", s)                    # nośnik
        self.assertIn("OBox(f, ramaN, a - .025f, b + .025f, y1, y1 + .025f, dN0, dN1 + .01f);", s)   # rama dookoła, nie na licu
        self.assertIn("BraxLiteryBebas.Halo(BraxUliceKit.S(halo), lok.szyld, f.P(sc, yb, dN1 + .03f), f.N, cap, .015f);", s)
        self.assertIn("BraxLiteryBebas.Napis(lok.szyld, f.P(sc, yb, dN1), f.N, cap, .05f, stalLit, boki, .05f, .04f);", s)
        self.assertNotIn("BraxUliceKit.Napis3D(lok.szyld, f.P(sc, 3.215f", s)                  # stara listwa 14 cm z kreskami

    def test_tower_shop_signs_halo(self):
        self.assertIn("BraxUliceKit.NapisHalo(l.szyld, F.P(sm, yc - cap * .5f, -.26f), F.N, cap,", WIEZE)


if __name__ == "__main__":
    unittest.main()
