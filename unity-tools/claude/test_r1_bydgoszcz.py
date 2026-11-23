"""MIASTO R1 — osiedle bydgoskie (23.11.2026): kontrola pliku geometrii i jego wpięcia. Bez Unity.

Właściciel 23.11: „nie chcę lipy”, „zdjęcia do wglądu, zanim cokolwiek zaakceptuję” — test pilnuje, żeby plik, który gra wczytuje,
był dokładnie tym, co liczy osiedle_gen.py, mieścił się w budżecie (ZASADY 10.2) i żeby krok był wyłączony do czasu przesunięcia granicy.
"""
import gzip, json, re, struct, unittest
from pathlib import Path

REPO = Path(__file__).resolve().parents[2]
B = REPO / "unity-tools/claude/bydgoszcz"
CS = (REPO / "unity-city/Assets/Editor/Claude/Region/BraxBydgoszcz.cs").read_text(encoding="utf-8")


def wczytaj():
    d = gzip.decompress((B / "osiedle.bin").read_bytes()); p = [4]
    def i32(): p[0] += 4; return struct.unpack_from("<i", d, p[0] - 4)[0]
    def txt(): n = i32(); p[0] += n; return d[p[0] - n:p[0]].decode("utf-8")
    assert d[:4] == b"BYD1"
    maty = []
    for _ in range(i32()): maty.append((txt(), txt())); p[0] += 12
    siatki = []
    for _ in range(i32()):
        g = txt(); mi = i32(); p[0] += 1; nv = i32(); ni = i32(); p[0] += nv * 32
        idx = struct.unpack_from("<%di" % ni, d, p[0]); p[0] += ni * 4
        siatki.append((g, maty[mi][0], nv, ni, max(idx) if idx else -1))
    boksy = i32(); p[0] += boksy * 28
    napisy = []
    for _ in range(i32()): napisy.append(txt()); p[0] += 40
    return maty, siatki, boksy, napisy, p[0] == len(d)


class Bydgoszcz(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.maty, cls.siatki, cls.boksy, cls.napisy, cls.caly = wczytaj()
        cls.opis = json.loads((B / "osiedle_opis.json").read_text(encoding="utf-8"))

    def test_plik_czyta_sie_do_konca_i_zgadza_z_opisem(self):
        self.assertTrue(self.caly)
        self.assertEqual(sum(s[3] for s in self.siatki) // 3, self.opis["trojkatow_razem"])
        self.assertEqual(self.boksy, self.opis["budynkow_zbudowanych"])

    def test_indeksy_w_zakresie_i_siatka_do_65_tysiecy_wierzcholkow(self):
        for g, m, nv, ni, mx in self.siatki:
            self.assertLess(mx, nv, g); self.assertLessEqual(nv, 65000, g); self.assertEqual(ni % 3, 0, g)

    def test_budzet_zasady_10_2(self):
        na_bud = {}
        for g, m, nv, ni, mx in self.siatki:
            if g.startswith("bud_"): na_bud[g] = na_bud.get(g, 0) + ni // 3
        self.assertLessEqual(max(na_bud.values()), 60000)                 # budynek ≤ 60 tys.
        self.assertLessEqual(self.opis["trojkatow_razem"], 800000)        # całe osiedle mieści się w budżecie jednego kadru

    def test_kazdy_rodzaj_materialu_ma_obsluge_w_grze(self):
        for nazwa, rodzaj in self.maty:
            if rodzaj != "gladki": self.assertIn('case "%s":' % rodzaj, CS, nazwa)

    def test_krok_wylaczony_i_wpiety_jednym_wywolaniem(self):
        self.assertRegex(CS, r"public static readonly bool Wlaczone = false;")
        region = (REPO / "unity-city/Assets/Editor/Claude/Region/BraxRegion.cs").read_text(encoding="utf-8")
        self.assertEqual(len(re.findall(r"BraxBydgoszcz\.Apply\(", region)), 1)
        self.assertIn("if (BraxBydgoszcz.Wlaczone) Step(", region)

    def test_budynki_bez_zdjecia_sa_na_liscie_brakow_a_nie_w_grze(self):
        self.assertGreater(len(self.opis["braki_bez_zdjecia"]), 0)
        zbud = {z["osm"] for z in self.opis["zbudowane"]}
        self.assertFalse(zbud & {b["osm"] for b in self.opis["braki_bez_zdjecia"]})

    def test_napisy_to_tylko_adresy_i_bar(self):
        for n in self.napisy:
            self.assertRegex(n, r"^(M\. SKŁODOWSKIEJ|-CURIE( \d+)?|\d+a|BAR MLECZNY)$")

    def test_tekstury_z_listy_wlasciciela_sa_pobierane(self):
        fetch = (REPO / "unity-tools/claude/fetch_region.py").read_text(encoding="utf-8")
        for slug in ("white_stucco_02", "brushed_concrete_04", "brick_wall_02", "concrete_pavers", "box_profile_metal_sheet"):
            self.assertIn('"%s"' % slug, fetch); self.assertIn('"%s"' % slug, CS)


if __name__ == "__main__":
    unittest.main()
