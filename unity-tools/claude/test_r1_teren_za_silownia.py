"""MIASTO R1 — teren za siłownią od A do Z (właściciel 9.11 20:10–20:38, zdjęcia: dźwig „stoi w powietrzu”, podwórze z „niebieskim”
= woda pod miastem, „biały klocek z czarnym otworem” = siłownia od zewnątrz, wulkanizacja i warsztaty, trawniki z krawężnikami).
Źródło i geometria liczona w Pythonie (bez Unity i APK)."""
import random
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
R = ROOT / "unity-city/Assets/Editor/Claude/Region"


def src(p):
    return re.sub(r'//[^\n]*', "", p.read_text(encoding="utf-8"))


HARBOR = src(ROOT / "unity-city/Assets/Editor/CityRebuild/BraxHarborBeachBuild.cs")
REGION = src(R / "BraxRegion.cs")
DZIURY = src(R / "BraxRegionDziury.cs")
SILOWNIA = src(R / "BraxSilowniaZewnatrz.cs")
WARSZTATY = src(R / "BraxRegionMiastoBWarsztaty.cs")
MIASTOB = src(R / "BraxRegionMiastoB.cs")
RUCH = src(R / "BraxRegionRuchMiasta.cs")
TYLYB = src(R / "BraxRegionTylyB.cs")

K = 1.294


class Dzwig(unittest.TestCase):
    def test_height_from_exact_chassis_vertices_not_renderer_box(self):
        self.assertNotIn(".17f - BraxRegion.Bounds(crane).min.y", HARBOR)
        self.assertIn("static Bounds Dokladnie(GameObject go, Func<Transform, bool> filtr)", HARBOR)
        self.assertIn("M.MultiplyPoint3x4(v)", HARBOR)
        self.assertIn('p.name.StartsWith("Base", StringComparison.OrdinalIgnoreCase)', HARBOR)

    def test_ground_measured_under_chassis_and_logged(self):
        self.assertIn("Physics.Raycast(new Vector3(px, 6f, pz), Vector3.down", HARBOR)
        self.assertIn("crane.transform.position += Vector3.up * (grunt - przedYmin);", HARBOR)
        self.assertIn("BRAXHORN_DZWIG", HARBOR)
        self.assertIn('"Dźwig: podwozie"', HARBOR)


class DziuryWGruncie(unittest.TestCase):
    def test_step_after_all_ground_steps(self):
        i_b = REGION.index("BraxRegionMiastoB.OdbiorWyposazeniaUlic(root)")
        i_s = REGION.index("BraxSilowniaZewnatrz.Apply(root)")
        i_d = REGION.index("BraxRegionDziury.Apply(root)")
        i_t = REGION.index("BraxRegionTylyB.Apply(world, root)")
        self.assertLess(i_t, i_b)
        self.assertLess(i_b, i_s)
        self.assertLess(i_s, i_d)
        self.assertLess(i_d, REGION.index("BraxClaudeOsiedle.RuchMiastaB(root)"))

    def test_zones_cover_gym_backs_workshops_and_port(self):
        self.assertIn("Rect.MinMaxRect(-114f, BraxMiastoGranice.Z0, -61.4f, 23.5f)", DZIURY)
        self.assertIn("Rect.MinMaxRect(-61.4f, -68f, 83.55f, 23.5f)", DZIURY)

    def test_water_not_counted_as_ground_and_gap_rules(self):
        ymin = float(re.search(r"YMin = (-?[\d.]+)f", DZIURY).group(1))
        woda = float(re.search(r"WodaDol = (-?[\d.]+)f", DZIURY).group(1))
        odst = float(re.search(r"Odstep = (\.[\d]+)f", DZIURY).group(1))
        self.assertGreater(ymin, -1.7 + .3)       # tafla wody miasta y −1.7 nie jest nawierzchnią
        self.assertLess(woda, -1.7 - .3)
        self.assertGreaterEqual(odst, .025)       # kontrola migotania szuka par < 2 cm
        self.assertIn("float top = href - Odstep;", DZIURY)
        self.assertIn("BRAXHORN_DZIURY", DZIURY)

    def test_row_run_merge_covers_cells_exactly(self):
        # ta sama metoda co w C#: przebiegi w wierszach sklejane z identycznymi przebiegami kolejnych wierszy
        rnd = random.Random(9)
        for _ in range(200):
            cells = {(rnd.randrange(12), rnd.randrange(12)) for _ in range(rnd.randrange(1, 60))}
            wiersze = {}
            for (i, j) in sorted(cells, key=lambda c: (c[1], c[0])):
                l = wiersze.setdefault(j, [])
                if l and l[-1][1] == i - 1:
                    l[-1] = (l[-1][0], i)
                else:
                    l.append((i, i))
            otwarte, gotowe, jpop = {}, [], None
            for j in sorted(wiersze):
                teraz = set(wiersze[j])
                for key in list(otwarte):
                    if jpop is None or j != jpop + 1 or key not in teraz:
                        gotowe.append((key[0], key[1], otwarte.pop(key), jpop))
                for p in wiersze[j]:
                    otwarte.setdefault(p, j)
                jpop = j
            for key, j0 in otwarte.items():
                gotowe.append((key[0], key[1], j0, jpop))
            pokryte = []
            for i0, i1, j0, j1 in gotowe:
                pokryte += [(i, j) for i in range(i0, i1 + 1) for j in range(j0, j1 + 1)]
            self.assertEqual(len(pokryte), len(set(pokryte)), "prostokąty płyt nachodzą na siebie")
            self.assertEqual(set(pokryte), cells, "prostokąty nie pokrywają dokładnie dziury")

    def test_lawns_get_edging(self):
        self.assertIn('"Trawnik: obrzeże betonowe (dołożone) "', DZIURY)
        self.assertIn("BRAXHORN_TRAWNIKI", DZIURY)


class SilowniaZZewnatrz(unittest.TestCase):
    def test_courtyard_edge_contact_with_plaza_and_side_street(self):
        self.assertIn("Rect.MinMaxRect(-61.45f, -6f, X0, Z1)", SILOWNIA)
        self.assertIn('Plyta("Plac: płyty P1a", granit, -61.45f, 66f, -13.25f, -6f, YC, g);', TYLYB)
        self.assertIn('Chodnik("ul. Wiaduktowa wsch.", WX + WH + Kr, -61.45f', MIASTOB)
        self.assertIn("X0 = -18.6f, X1 = 18.6f, Z0 = -16.6f, Z1 = 11.6f", SILOWNIA)

    def test_roof_above_ceiling_and_up_facing_only(self):
        y = float(re.search(r"const float y = ([\d.]+)f;", SILOWNIA).group(1))
        self.assertGreaterEqual(y - 8.5, .025)    # strop sali y 8.5 (od spodu) — dach 3 cm wyżej, bez migotania
        self.assertIn("m.Quad(new Vector3(x0, y, z0), new Vector3(x0, y, z1), new Vector3(x1, y, z1), new Vector3(x1, y, z0));", SILOWNIA)

    def test_cladding_off_the_gym_wall(self):
        self.assertIn("Sciana(sc, P, 0f, L, y0, yA, .03f, .11f, otwory);", SILOWNIA)

    def test_windows_between_pilasters(self):
        for L, lizeny in ((28.2, [7.6, 13.6, 19.6, 25.6]), (37.2, [9.6, 18.6, 27.6]), (28.2, [2.6, 8.6, 14.6, 20.6])):
            granice = [.4] + [v for s in lizeny for v in (s - .35, s + .35)] + [L - .4]
            okna = 0
            for i in range(0, len(granice) - 1, 2):
                a, b = granice[i], granice[i + 1]
                if b - a < 3.2:
                    continue
                w = min(4.0, b - a - 1.6)
                m = (a + b) / 2
                self.assertGreaterEqual(m - w / 2, a + .7)
                self.assertLessEqual(m + w / 2, b - .7)
                okna += 1
            self.assertGreaterEqual(okna, 4)

    def test_inside_untouched(self):
        self.assertNotIn("braxhorn-original-gym", SILOWNIA.replace("braxhorn-original-gym.glb", ""))
        self.assertIn('"BRAXHORN GYM"', SILOWNIA)


class Warsztaty(unittest.TestCase):
    def test_old_code_replaced(self):
        self.assertNotIn("static void Warsztaty()", MIASTOB)
        self.assertNotIn("static void Podnosnik(", MIASTOB)
        self.assertIn("static void Warsztaty()", WARSZTATY)
        self.assertIn('Krok("2. budynki: warsztaty (jeden otwarty) i plac ustawek", Warsztaty);', MIASTOB)

    def test_no_gap_under_doors(self):
        self.assertIn("pos.MinMax(new Vector3(a + (i == 0 ? t : 0f), YC - .25f, zb + t), new Vector3(b - t, YC + .05f, zf));", WARSZTATY)
        self.assertIn("korpus.MinMax(new Vector3(x0, yp - .33f, z0), new Vector3(x1, yp - .03f, z1 + .35f));", WARSZTATY)

    def test_facade_openings_do_not_collide(self):
        bw = 3.6 * K
        dw = 1.0 * K
        for i in range(4):
            a = -112 + 10 * i
            b = a + 10
            m = (a + b) / 2
            otw = [(m - bw / 2, m + bw / 2, .15, .15 + bw), (a + .75, a + .75 + dw, .15, .15 + 2.2 * K),
                   (b - 2.25, b - .65, 1.15, 3.05), (a + .6, a + 2.2, 3.6, 5.15), (b - 2.25, b - .65, 3.6, 5.15)]
            for p in range(len(otw)):
                for q in range(p + 1, len(otw)):
                    A, B = otw[p], otw[q]
                    sx = min(A[1], B[1]) - max(A[0], B[0])
                    sy = min(A[3], B[3]) - max(A[2], B[2])
                    self.assertFalse(sx > 0 and sy > 0, f"lokal {i}: otwory {p} i {q} nachodzą")
            for o in otw:                        # lizeny ±0.35 na granicach lokali
                self.assertGreaterEqual(o[0], a + .35)
                self.assertLessEqual(o[1], b - .35)
            # szyld m ± 2.7 nad bramą — nie wchodzi na okna górne
            self.assertGreater(m - 2.7, a + 2.2)
            self.assertLess(m + 2.7, b - 2.25)

    def test_open_tire_shop_and_service_with_wheels_and_cars(self):
        self.assertIn('"WULKANIZACJA", "AUTO NAPRAWA", "GARAŻ 12", "MAGAZYN"', WARSZTATY)
        self.assertIn("bool otwarty = i <= 1;", WARSZTATY)
        self.assertIn("AutaWarsztatow.Add(", WARSZTATY)
        self.assertIn("KolaWarsztatow.Add(", WARSZTATY)
        self.assertIn("WarsztatyMiastaB(szablony)", RUCH)
        self.assertIn("BraxRegionMiastoB.KolaWarsztatow", RUCH)
        self.assertIn("BraxRegionMiastoB.AutaWarsztatow", RUCH)
        self.assertIn("BraxRegionMiastoB.MechanikWulkanizacja", RUCH)
        self.assertIn('t.parent.name.StartsWith("koło", StringComparison.Ordinal)', RUCH)

    def test_roof_hidden_behind_parapet(self):
        self.assertIn("yA = yT + .6f", WARSZTATY)
        self.assertIn('dach.Obiekt("Warsztaty: dach (papa, krawężniki świetlików)"', WARSZTATY)


if __name__ == "__main__":
    unittest.main()
