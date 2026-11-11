"""MIASTO R1, 11.11.2026 — fawela odsunięta od miasta; między nimi las, dworzec autobusowo-kolejowy i tory.

Właściciel 15:35: „rozdziel wtedy te fawele od miasta dworcem, torami i lasem. Od strony fawel jeszcze lekko… Bez lokomotywy,
bez wagonu”; 16:19–16:21: „skala, skala i skala”, autobusy muszą wjechać, stanąć i wyjechać; 16:32 (przez właściciela, MIASTO
MISTRZÓW): skład 126.98 m, rozstaw 1.857 m, peron 0.984 m nad główką i 2.233 m od osi, osie torów 5.825 m, sieć 6.86 m.
"""
import json
import os
import math
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
REG = ROOT / "unity-city/Assets/Editor/Claude/Region"
RUN = ROOT / "unity-city/Assets/Claude/Runtime"
LIB = ROOT / "unity-city/Assets/Source/Claude/Library"
K = 1.2944


def code(name, d=REG):
    return (d / name).read_text(encoding="utf-8")


DW = "".join(code(f) for f in ("BraxMiastoDworzec.cs", "BraxMiastoDworzecDrogi.cs", "BraxMiastoDworzecTory.cs", "BraxMiastoDworzecBudynek.cs", "BraxMiastoDworzecLas.cs", "BraxMiastoDworzecWindyWC.cs"))
BEBAS = code("BraxLiteryBebas.cs")
FETCH = (ROOT / "unity-tools/claude/fetch_region.py").read_text(encoding="utf-8")


def stala(nazwa, tekst=DW):
    m = re.search(r"\b" + nazwa + r"\s*=\s*(-?[\d.]+)f", tekst)
    assert m, nazwa
    return float(m.group(1))


class OdsuniecieFaweli(unittest.TestCase):
    def test_shift_constants_agree(self):
        faw = code("BraxRegionFawela.cs")
        self.assertIn("public const float Odsuniecie = 115f;", faw)
        gr = code("BraxMiastoGranice.cs", RUN)
        self.assertIn("public const float FawelaOdsuniecie = 115f;", gr)
        self.assertIn("FawelaZSzczyt = -357f - FawelaOdsuniecie", gr)

    def test_plan_space_inside_generator(self):
        for f in ("BraxRegionFawela.cs", "BraxRegionFawelaDrogi.cs", "BraxRegionFawelaZielen.cs"):
            c = code(f)
            self.assertNotIn("BraxMiastoGranice.WFaweli(", c, f)
            self.assertNotIn("BraxMiastoGranice.FawelaZSzczyt", c, f)

    def test_root_moved_and_vendors_in_world(self):
        faw = code("BraxRegionFawela.cs")
        body = faw[faw.index('Krok("zapis kwadratów", Zapisz);'):faw.index('Debug.Log("BRAXHORN_FAWELA etap 1:')]
        self.assertIn("root.position = new Vector3(0f, 0f, -Odsuniecie);", body)
        self.assertIn("Sprzedawcy[i] = (DoSwiata(Sprzedawcy[i].p)", body)

    def test_streaming_squares_follow_world(self):
        faw = code("BraxRegionFawela.cs")
        self.assertEqual(faw.count("Mathf.FloorToInt((z - Odsuniecie) / Kw)"), 2)
        self.assertIn("(z0 - 2.4f - Odsuniecie) / Kw", faw)

    def test_city_parts_of_old_roads_dropped(self):
        faw = code("BraxRegionFawela.cs")
        self.assertNotIn("new Vector2(-72f, -56f)", faw)           # dawny wjazd przez parking warsztatów
        self.assertNotIn('"wjazd z ul. C1"', faw)                    # tam portal tunelu kolejowego
        self.assertIn("new Vector2(BraxMiastoDworzec.LacznikX, BraxMiastoGranice.Z0)", faw)
        self.assertIn("new Vector2(BraxMiastoDworzec.C0X, BraxMiastoGranice.Z0)", faw)

    def test_tunnel_hill_moved_to_corridor(self):
        faw = code("BraxRegionFawela.cs")
        self.assertNotIn("float garb = 13.65f", faw)
        self.assertIn("float garb = 13.65f - 3.4f * S01(0f, 60f, dz);", DW)
        self.assertIn("if (x > GarbX1 || z < GarbZ1) return 0f;", DW)

    def test_step_order(self):
        reg = code("BraxRegion.cs")
        i_f = reg.index("BraxRegionFawela.Apply(root)")
        i_d = reg.index("BraxMiastoDworzec.Apply(root)")
        self.assertLess(i_f, i_d)
        for dalej in ("BraxRegionDziury.Apply(root)", "BraxClaudeOsiedle.RuchMiastaB(root)", "BraxRegionHoryzont.Apply(world, root)"):
            self.assertLess(i_d, reg.index(dalej), dalej)


class PociagIPerony(unittest.TestCase):
    def test_colleague_train_numbers(self):
        self.assertEqual(stala("SkladDl"), 126.98)
        self.assertEqual(stala("RozstawSzyn"), 1.857)
        self.assertEqual(stala("SzynaOs"), .975)
        self.assertEqual(stala("OsieTorow"), 5.825)
        self.assertEqual(stala("PeronNadGlowka"), .984)
        self.assertEqual(stala("KrawedzOdOsi"), 2.233)
        self.assertEqual(stala("SiecNadGlowka"), 6.86)

    def test_platforms_fit_train_and_edges(self):
        px0, px1 = stala("PX0"), stala("PX1")
        self.assertGreaterEqual(px1 - px0, 126.98 + 10)
        t1 = stala("T1"); t2 = t1 - 5.825
        self.assertIn("P1Kraw = T1 + KrawedzOdOsi, P2Kraw = T2 - KrawedzOdOsi", DW)
        self.assertGreater(2.233 - 3.728 / 2, .3)                     # prześwit wagon–peron
        self.assertIn("GlowkaY = YP - PeronNadGlowka", DW)
        self.assertLess(stala("KozlyX"), 70)                           # w granicy gry

    def test_wire_and_footbridge_clearance(self):
        yp = stala("YP"); glowka = yp - .984
        self.assertGreater(stala("KladkaSpod"), glowka + 7.5)
        self.assertIn("yJezdny = GlowkaY + SiecNadGlowka", DW)


class SkalaAutobusu(unittest.TestCase):
    def test_bus_turns_and_stands(self):
        r_zew, r_wew, bus = 12.5 * K, 5.3 * K, 12 * K
        self.assertGreaterEqual(stala("PetlaR"), r_zew)
        self.assertLessEqual(stala("PetlaWyspa"), r_wew)
        self.assertAlmostEqual(stala("Pas"), 3.5 * K, delta=.02)
        rl = stala("RL")
        self.assertGreaterEqual(rl + stala("Pas"), r_zew)
        self.assertGreaterEqual(stala("StanowiskoDl"), bus + 2.9)
        self.assertGreaterEqual(stala("BusX1") - stala("BusX0"), 8 + 2 * stala("StanowiskoDl"))

    def test_s_bend_geometry(self):
        self.assertIn("DrogaZ = DworcowaZ - 2f * RL - .03f", DW)
        self.assertIn("Luk2X = Luk1X + 2f * RL", DW)
        # łuk 1 nie zachodzi na narożniki łącznika (R 3.5)
        self.assertLessEqual(stala("LacznikX") + stala("LacznikH") + 3.5, stala("Luk1X"))

    def test_loop_inside_game_boundary(self):
        self.assertLessEqual(stala("PetlaX") + stala("PetlaR") + .25, 70)

    def test_aleja_entry_cut_in_green_belt(self):
        mb = code("BraxRegionMiastoB.cs")
        self.assertIn("zjazdy.Add((1, BraxMiastoDworzec.DworcowaZ - BraxMiastoDworzec.Pas, BraxMiastoDworzec.DworcowaZ + BraxMiastoDworzec.Pas));", mb)
        self.assertIn("Rect.MinMaxRect(BraxMiastoDworzec.LicoAlei, ZboczeZ, -61.4f, BraxMiastoDworzec.PasAleiZ1)", mb)


class ZasadyWlasciciela(unittest.TestCase):
    def test_signs_3d_and_blue(self):
        self.assertNotIn("TextMesh", DW)
        self.assertIn('"WC"', DW)
        self.assertIn('"INFORMACJA"', DW)
        self.assertIn('niebieski = T("tablice informacyjne — niebieskie"', DW)

    def test_only_poly_haven_materials(self):
        fetch = code("fetch_region.py", ROOT / "unity-tools/claude") + code("fetch_polyhaven.py", ROOT / "unity-tools/claude")
        mat = DW[DW.index("static void Materialy()"):DW.index("// ============================================================================ NARZĘDZIA SIATKI")]
        for slug in re.findall(r'\.\d+f, \.?\d*f?, "([a-z0-9_]+)"', mat):
            self.assertIn('"' + slug + '"', fetch, slug)

    def test_working_clock_doors_seats(self):
        self.assertTrue((RUN / "BraxZegarDworca.cs").exists())
        self.assertIn("AddComponent<BraxZegarDworca>()", DW)
        self.assertGreaterEqual(DW.count("BraxMiastoA.DrzwiPrzesuwne("), 4)   # hala: 3 wejścia + 2 na perony (pętle), skrzydła: wejścia, KOLEJ: na peron
        self.assertIn("BraxMiastoA.Siedzisko(root", DW)

    def test_staff_for_ticket_office(self):
        self.assertIn("BraxMiastoDworzec.Obsluga", code("BraxRegionRuchMiasta.cs"))


class WindyIWC(unittest.TestCase):
    """Właściciel 18:12–18:13: „Drzwi do WC to trzeba zrobić… Winda przy kładce… zrób, dokończ to”."""

    def test_two_working_lifts_at_footbridge(self):
        self.assertEqual(DW.count('Winda("KŁADKA'), 2)
        self.assertIn("AddComponent<BraxWinda>()", DW)
        self.assertIn("new[] { Y0, y }", DW)                                   # przystanki: peron/plac i pomost kładki
        self.assertIn("wd.przyciskAktywny = przyciskOn", DW)
        self.assertIn("KabW = 1.6f * 1.2944f, KabD = 1.4f * 1.2944f, WDrzW = 1.1f * 1.2944f, WDrzH = 2.1f * 1.2944f", DW)

    def test_lift_shafts_glass_with_black_frames(self):
        body = DW[DW.index("static void Winda(string nazwa"):DW.index("static Transform Skrzydlo(")]
        self.assertIn("BL(czarnyAlu", body)
        self.assertIn("BL(szklo", body)

    def test_footbridge_openings_for_lifts(self):
        # otwory w parapecie: drzwi windy + miejsce na słupek przywołania na dźwigarze
        self.assertIn("(WindaA.y - 1.3f, zN)", DW)
        self.assertIn("(WindaB.y - 1.3f, WindaB.y + 2.0f)", DW)

    def test_lift_doors_fit_inside_shaft_and_call_post_outside_door_path(self):
        # skrzydła otwierane centralnie chowają się w obrysie szybu: x maks. = WDrzW/2 + przesuw ≤ wnętrze szybu
        wdrz = 1.1 * K; przesuw = wdrz * .5 - .04; kab = 1.6 * K * .5 + .1
        szyb = max(wdrz - .02, kab)
        self.assertLessEqual(wdrz * .5 + przesuw, szyb)
        self.assertIn("SzybWew = WDrzW - .02f > KabW * .5f + .1f", DW)
        self.assertIn("float px = stronaSlupka * (hw + t + .3f)", DW)          # słupek poza drogą skrzydeł
        self.assertIn('new[] { "0 PERON 2", "1 KŁADKA" }, -1f)', DW)

    def test_wc_doors_hinged_and_rooms_built(self):
        self.assertIn("AddComponent<BraxCityDoor>()", DW)
        self.assertGreaterEqual(DW.count('DrzwiZawiasy("WC '), 2)
        self.assertIn('"PANIE", "PANOWIE"', DW)
        self.assertIn("if (!kolej) WC();", DW)
        for (a, b) in [(-138.05, -136.75), (-135.55, -134.25)]:
            self.assertGreaterEqual(b - a, .9 * K)
        self.assertIn("WcPrzejscia.Any(q =>", DW)                             # przejścia przez pylon hali

    def test_urinals_real_ceramic_not_pillows(self):
        # właściciel 19:39: „te takie dwie… pisuary, czy co to jest?” — pisuar z kodu: powłoka z misą, warga, odpływ, zawór chrom
        self.assertNotIn("S(ceramika).Poducha(V(xp", DW)
        self.assertEqual(DW.count("BraxCeramika.Pisuar(new Vector3(xp, Y0, zb), Vector3.right, Vector3.back)"), 1)
        CER = code("BraxCeramika.cs")
        body = CER[CER.index("public static void Pisuar("):CER.index("public static void Umywalka(")]
        for s in ["Warga(", "Obrys(", "sitko", "S(chrom)", "S(ceramika)", "* K"]:
            self.assertIn(s, body)
        self.assertIn("/ e;", body)                                            # pochodne dzielone przez krok (Normalize w Unity)
        self.assertIn("WC: przegroda pisuarów", DW)
        self.assertIn('chrom = BraxCityMaterials.Surface("armatura — chrom polerowany"', CER)
        self.assertIn("yD = .525f, zD = .175f", CER)                          # odpływ w najniższym punkcie misy (właściciel 19:52–19:59)

    def test_sinks_are_basins_with_drain_not_plates(self):
        # właściciel 19:53: „czy mają dziurę… odpływ, czy są jak talerze” — misa toczona z wnętrzem, odpływem i sitkiem
        self.assertNotIn("S(ceramika).PoduchaOkragla(V(xu", DW)
        self.assertIn("BraxCeramika.Umywalka(V(xu, Y(hb), zs + sg * gl * .5f));", DW)
        CER = code("BraxCeramika.cs")
        body = CER[CER.index("public static void Umywalka("):CER.index("public static void Toczony(")]
        for s in ["yIn = .045f, ySit = .025f", "Toczony(s, p, pr, seg)", "Pierscien(", "S(b % 2 == 0 ? chrom : grafit)"]:
            self.assertIn(s, body)
        self.assertIn("perlator", CER)                                         # bateria z perlatorem i strumieniem (BraxCeramika.Bateria)
        self.assertIn("internal static class BraxCeramika", CER)                # wspólna ceramika całego miasta (właściciel 19:59)


class Las(unittest.TestCase):
    def test_new_species_from_poly_haven(self):
        for n in ("las_jodla_a", "las_jodla_b", "las_jodla_c", "las_sosna_a", "las_sosna_b", "las_sosna_c", "las_drzewko", "las_kloda"):
            p = LIB / (n + ".glb")
            self.assertTrue(p.exists(), n)
            self.assertLess(p.stat().st_size, 2_500_000, n)
        rap = json.loads((LIB / "las_drzewa.json").read_text(encoding="utf-8"))
        for n, st in rap.items():
            if "liscie_0_trojkatow" in st:
                self.assertLess(st["liscie_0_trojkatow"] + st["drewno_trojkatow"], 6000, n)
        self.assertIn("Poly Haven", (LIB / "las_drzewa.CREDITS.md").read_text(encoding="utf-8"))

    def test_dense_city_side_light_favela_side(self):
        self.assertIn("if (z > T1 + 6f) return 1f;", DW)
        self.assertIn("return .36f *", DW)


if __name__ == "__main__":
    unittest.main()


class ZeroMigotania(unittest.TestCase):
    """Zasada 10.11 „zero migotania, zakaz nakładania” w kodzie dworca (kontrola geometrii: unity-tools/claude/kontrola_geometrii)."""

    def test_paint_in_asphalt_cutouts_not_on_top(self):
        self.assertNotIn("YJ + .02f", DW)                                       # zebra i napisy nie leżą 2 cm nad asfaltem
        self.assertIn("static void WytnijFarbe()", DW)
        self.assertIn("NapisNaJezdni(", DW)
        self.assertIn("Farba(farba, o.p[i] - l0", DW)                           # oś przerywana w wycięciu
        self.assertIn("public static List<Vector3[]> Trojkaty(", BEBAS)

    def test_plaza_strips_inlaid_and_curb_corners_mitred(self):
        self.assertNotIn("YP + .02f", DW)
        self.assertIn("Plyta(granitC, x - .15f, sz0, x + .15f, sz1, YP)", DW)
        self.assertIn("var tyl = new Vector2[pkt.Count]", DW)

    def test_no_terrain_under_pavement(self):
        self.assertIn("KomorkaPokryta(xs[i], zs[j], xs[i + 1], zs[j + 1])", DW)
        self.assertIn("Pokryj(x0, z0, x1, z1);", DW)

    def test_platform_and_floors_cut_not_layered(self):
        self.assertIn("PlytaBez(m, PX0, Zd(d0), PX1, Zd(d1), YP, .05f, OtworySzybow)", DW)
        self.assertNotIn("Y0 + .02f", DW)                                       # posadzka WC w poziomie posadzki skrzydła
        self.assertIn("Rect.MinMaxRect(WcX0, WcZ0, WcX1 + .15f, WcZ1 + .15f)", DW)

    def test_wing_pilasters_front_only_and_doors_clear(self):
        self.assertIn("Y0, zf - .2f, Mathf.Min(R, x + .32f), Y(h - 1.6f), zf + .25f)", DW)
        self.assertNotIn("B(kamien, L, Y0 - .3f, zt, R, Y(.85f), zf)", DW)         # cokół nie wypełnia poczekalni
        self.assertIn('V(xDrzwi, Y0, zf - .25f)', DW)                              # skrzydła po stronie poczekalni
        self.assertIn("srodkowy ? Y(3.15f) : Y(0f)", DW)                           # słupek środkowy nad drzwiami hali
        self.assertIn('V(xd, Y0, HZ0 - .05f)', DW)

    def test_door_thresholds_in_wall_thickness(self):
        self.assertIn("Plyta(posadzka, -22.4f, BudZ1, -13.6f, HZ1, Y0, .3f)", DW)
        self.assertIn("Plyta(posadzka, xDrzwi - 1.4f, BudZ1, xDrzwi + 1.4f, zt + .3f, Y0, .3f)", DW)

    def test_details_have_real_thickness(self):
        self.assertIn("n * .105f", DW)                                           # ekran tablicy 2.5 cm przed ramą
        self.assertIn("Lustro(nazwyWc[k]", DW); self.assertIn("AddComponent<BraxLustro>()", DW)   # lustro z odbiciem
        self.assertNotIn("Plyta(sciolka, x - 1.6f, -123.2f, x + 1.6f, -121.4f, Y(.62f)", DW)       # ziemia w donicy, nie na niej
        self.assertIn("S(tarcza).Rura(c + n * .12f, c + n * .15f", DW)

    def test_premium_materials(self):
        self.assertIn('"blue_metal_plate", "metal_plate");', DW)                  # czarne okucia bez ryflowania
        self.assertIn('"floor_tiles_02", "floor_tiles_06"', DW)
        self.assertIn('"floor_tiles_02"', FETCH)

    def test_geometry_check_tool_present(self):
        d = os.path.join(os.path.dirname(__file__), "kontrola_geometrii")
        for f in ("Program.cs", "harness.csproj", "build.sh", os.path.join("fake", "UnityEngine.cs"), os.path.join("fake", "Stubs.cs")):
            self.assertTrue(os.path.exists(os.path.join(d, f)), f)
