"""MIASTO R1 (12.11.2026): wyspa — drzwi działające (zamknięte, na zawiasach) i WC plażowe premium (właściciel 13:51, 15:27).

13:51 „WC stoją na chodnikach… wszystko przejrzyj”; 15:27 „Co z tymi drzwiami do WC — rozpierdolone” (skrzydła stały na sztywno
uchylone pod kątem); 15:29 „skoro gdzieś tam stworzyłeś lustro, to dlaczego go nie przeniesiesz” — lustra, miski, pisuary
i umywalki to ta sama wspólna biblioteka R1 (BraxCeramika / BraxLustro), co w WC dworca.
"""
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
REG = ROOT / "unity-city/Assets/Editor/Claude/Region"
ARENA = (REG / "BraxMiastoWyspaArena.cs").read_text(encoding="utf-8")
DOMKI = (REG / "BraxMiastoWyspaDomki.cs").read_text(encoding="utf-8")
WYSPA = (REG / "BraxMiastoWyspa.cs").read_text(encoding="utf-8")


def fn(src, name, nxt):
    i = src.index(name)
    return src[i:src.index(nxt, i)]


class DrzwiDzialajace(unittest.TestCase):
    D = fn(ARENA, "internal static void DrzwiObrotowe(", "static void ApSufitPom(")

    def test_skrzydlo_na_zawiasie_z_braxcitydoor(self):
        self.assertIn("AddComponent<BraxCityDoor>()", self.D)
        self.assertIn("d.leaves = new[] { z }; d.sides = new[] { dir };", self.D)
        self.assertIn("AddComponent<BoxCollider>()", self.D)          # kolizja jedzie ze skrzydłem
        self.assertIn('d.id = "wyspa_drzwi_" + BraxUliceKit.Bezpieczna(label)', self.D)

    def test_klamki_po_obu_stronach(self):
        self.assertIn("foreach (float sz in new[] { -1f, 1f })", self.D)
        self.assertIn("kl.Rura(", self.D)

    def test_skrzydla_areny_zamkniete_w_osi_sciany(self):
        sk = fn(ARENA, "static void ApSkrzydlo(", "static void ApSufitPom(")
        self.assertIn("DrzwiObrotowe(", sk)
        self.assertNotIn("Quaternion.Euler(0, kier", sk)                # dawne skrzydło uchylone na sztywno

    def test_kabiny_wc_areny(self):
        t = fn(ARENA, "static void ApToalety()", "// TUNEL ZAWODNIKÓW")
        self.assertIn('DrzwiObrotowe(nz + " kabina " + (k + 1)', t)


class WcPlazowe(unittest.TestCase):
    W = fn(DOMKI, "// WC PLAŻOWE PREMIUM", "// WYPOŻYCZALNIA")

    def test_pawilon_za_deptakiem_z_dojsciem(self):
        self.assertIn("var c = Zaplecze(ang, 50f)", self.W)
        self.assertIn('"Dojście do WC plażowego"', self.W)

    def test_ceramika_i_lustro_wspolne_r1(self):
        for s in ("BraxCeramika.MiskaWiszaca(", "BraxCeramika.Pisuar(", "BraxCeramika.ZestawUmywalkowy("):
            self.assertIn(s, self.W)

    def test_materialy(self):
        for slug in ('"long_white_tiles"', '"large_floor_tiles_02"', '"teak_veneer"', '"granite_tile"'):
            self.assertIn(slug, self.W)
        self.assertNotIn("zlot", self.W.lower())

    def test_drzwi_dzialajace(self):
        self.assertEqual(self.W.count("DrzwiObrotowe("), 2)             # wejście (×2 w pętli) i kabina (×2 w pętli)

    def test_zero_migotania(self):
        self.assertIn("Bx(posW, -hw2 + tw, hw2 - tw, -.03f, .025f, -hd2 + tw, hd2 + .03f);", self.W)   # posadzka zatopiona w cokole
        self.assertIn("hd2 + .6f, hd2 + .63f);", self.W)                                              # opaska 3 cm przed płytą dachu

    def test_tynk_naprawde_bialy(self):
        self.assertIn('"white_stucco", "white_plaster_02");', self.W)                                  # white_plaster_02 wychodził szary

    def test_plytki_do_sufitu(self):
        self.assertIn("float yT = h - .03f, tT = .025f;", self.W)


class DeptakIWidownia(unittest.TestCase):
    def test_latarnia_nie_stoi_w_dojsciu_do_wc(self):
        self.assertIn("if (Mathf.Abs(lp.x - CX) < 3.0f * K + .6f && lp.z < CZ) continue;", DOMKI)
        self.assertIn("foreach (float przes in new[] { 0f, 6f, -6f })", DOMKI)

    def test_kotwa_krzeselka_ponad_2_cm(self):
        self.assertIn("me.Walec(new Vector3(0, 0, -.14f), .085f, .022f, 6);", ARENA)
        self.assertIn("me.Rura(new Vector3(0, .022f, -.14f)", ARENA)


class SzatniePremium(unittest.TestCase):
    S = fn(ARENA, "static void ApSzatnie()", "static void ApRozgrzewka(")

    def test_boksy_zawodnikow_zamiast_plaskich_szafek(self):
        self.assertIn("// BOKSY ZAWODNIKÓW", self.S)
        self.assertNotIn('"Arena: szafki szatni (stal lakierowana)"', ARENA)
        self.assertNotIn("lakD.OBox(dc, Vector3.right, Vector3.up, ws, lw - .006f, hl - .12f, .018f);", ARENA)
        for s in ('"brown_leather"', "BraxUliceKit.Napis3D(nrBoksu.ToString()", '"Arena premium: LED boksów szatni"'):
            self.assertIn(s, self.S)

    def test_zaokraglone_bryly_nie_klocki(self):
        self.assertIn("q.Poducha(Vector3.Min(A, B), Vector3.Max(A, B), r, puf, 3);", self.S)
        self.assertGreaterEqual(self.S.count("Pd("), 10)

    def test_sciany_debowe_i_posadzka_gumowa(self):
        self.assertIn('"Arena premium: ściany szatni (panele dębowe)"', self.S)
        self.assertIn('"rubber_tiles"', ARENA)

    def test_siedzisko_gracza_w_boksie(self):
        self.assertIn('BraxMiastoA.Siedzisko(root, "ŁAWKA — " + kto, siad, ws);', self.S)

    def test_gasnica_gladka(self):
        self.assertIn("new Vector2(0, .5f * s) }, b, 28);", ARENA)


class KrawedzPlacuBezZabkow(unittest.TestCase):
    def test_wierzcholki_dosuwane_na_granice(self):
        self.assertIn("Vector2 Dosun(float xx, float zz, bool wnetrze)", WYSPA)
        self.assertIn("float thr = step / fine * .505f;", WYSPA)
        self.assertIn("List<int> Klasa(int p0, int p1, int p2)", WYSPA)       # klasyfikacja po środku trójkąta po dosunięciu

    def test_przekatna_po_granicy(self):
        self.assertIn("if (dosuniete.Contains(bb) && dosuniete.Contains(cc) && !(dosuniete.Contains(aa) && dosuniete.Contains(d1)))", WYSPA)


if __name__ == "__main__":
    unittest.main()
