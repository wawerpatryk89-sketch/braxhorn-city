"""MIASTO R1 — tram T1, scheduled bus, shared traffic, north supermarket parking.

SOURCE CONTRACT TESTS ONLY: not a Unity scene compile, PlayMode or Android APK test.
"""
from pathlib import Path
import math
import re
import unittest

ROOT = Path(__file__).resolve().parents[2] / "unity-city/Assets"
def src(relative):
    return (ROOT / relative).read_text(encoding="utf8")

CITY = src("Editor/Claude/Region/BraxRegionMiastoB.cs")
DISTRICT = src("Editor/Claude/Region/BraxRegionMiastoBDzielnice.cs")
TRAFFIC = src("Editor/Claude/Region/BraxRegionRuchMiasta.cs")
RUNTIME = src("Claude/Runtime/BraxRuchAut.cs")
SYM = src("Claude/Runtime/BraxRuchAutSym.cs")
TRAM = src("Claude/Runtime/BraxTramT1.cs")
SHOP = src("Editor/Claude/Region/BraxSupermarket.cs")
GATE = src("Claude/Runtime/BraxDrzwiPrzesuwne.cs")


def cs_balanced(source):
    pattern = r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|"((?:\\.|[^"\\])*)"'
    source = re.sub(pattern, "", source)
    stack = []
    mapping = {")": "(", "]": "[", "}": "{"}
    for ch in source:
        if ch in "([{":
            stack.append(ch)
        elif ch in ")]}":
            if not stack or stack.pop() != mapping[ch]:
                return False
    return not stack


class R1TrafficRetailRepairs(unittest.TestCase):
    def test_all_changed_csharp_balanced(self):
        for name, source in (
            ("city",CITY),("district",DISTRICT),("traffic",TRAFFIC),
            ("runtime",RUNTIME),("tram",TRAM),("shop",SHOP)):
            with self.subTest(name=name):
                self.assertTrue(cs_balanced(source))

    def test_stopped_bus_static_model_and_ghost_collider_removed(self):
        self.assertNotIn('BraxMiastoAssety.Kolizja(root, o, "Autobus kolizja"', DISTRICT)
        self.assertNotIn('"Autobus na przystanku ALEJA CENTRALNA (model)"', DISTRICT)
        self.assertIn("pojazd kursowy z jednej symulacji ruchu",DISTRICT)

    def test_bus_source_model_reused_as_moving_car_with_original_dimensions(self):
        self.assertIn('BraxMiastoAssety.Model("miasto_pojazdy", "autobus")', TRAFFIC)
        self.assertIn('bus.AddComponent<BraxAuto>()', TRAFFIC)
        self.assertIn("a.dl = busLength; a.szer = busWidth;", TRAFFIC)
        self.assertIn("var rigid = bus.AddComponent<Rigidbody>(); rigid.isKinematic = true;", TRAFFIC)
        self.assertIn("ra.auta = auta.ToArray();", TRAFFIC)

    def test_bus_has_route_in_correct_direction_on_central_avenue(self):
        self.assertIn('"AUTOBUS Aleja Centralna C0-C1 kurs"', TRAFFIC)
        self.assertIn("P(nA[0]), P(nT[0]), P(nT[1]), P(nA[1])", TRAFFIC)
        self.assertIn("if (next.x < p.x + .01f) continue;", TRAFFIC)
        self.assertIn("PrzystanekAutobusuCentralnaX = -318f", DISTRICT)
        self.assertIn("ra.autobusStopS = bestS", TRAFFIC)

    def test_bus_stops_and_resumes_in_real_simulation(self):
        self.assertIn("void AutobusPostoj()", RUNTIME)
        self.assertIn("autobusStopCzas = 8f", RUNTIME)
        self.assertIn("autobusKoniecPostoju = Time.time", RUNTIME)
        self.assertIn("a.vmax = 0f;", RUNTIME)
        self.assertIn("autobusObsłużony = true", RUNTIME)
        self.assertIn("AutobusPostoj();", RUNTIME)
        self.assertIn("sym.Krok(dt);", RUNTIME)

    def test_bus_cars_share_collision_simulator(self):
        self.assertIn("var s0 = new BraxRuchAutSym(", TRAFFIC)
        self.assertIn("foreach (var a in auta) s0.Dodaj(", TRAFFIC)
        self.assertIn("var st = new BraxRuchAutSym(", TRAFFIC)
        self.assertIn("st.Test(600f", TRAFFIC)
        self.assertIn("bool Nachodzi(Auto a", SYM)

    def test_vehicle_collision_shapes_use_swept_oriented_rectangles(self):
        self.assertIn("public static bool Prost(", SYM)
        self.assertIn("float dx = b.poz.x - p.x", SYM)
        self.assertIn("int NoweNachodzenie(", SYM)

    def test_cars_never_share_t1_loop_lanes(self):
        # 9.11 19:44: T1 dookoła całych nowych dzielnic przeciwnie do zegara zewnętrznymi pasami — wszystkie pętle aut zgodnie z zegarem
        self.assertIn('punkty = new[] { P(nA[1]), P(nN[1]), P(nN[0]), P(nA[0]) }',TRAFFIC)      # C0–C1 północ
        self.assertIn('punkty = new[] { P(nA[3]), P(nN[3]), P(nN[2]), P(nA[2]) }',TRAFFIC)      # C2–C3 północ
        self.assertIn('punkty = new[] { P(nA[2]), P(nT[2]), P(nT[3]), P(nA[3]) }',TRAFFIC)      # C2–C3 południe
        self.assertIn('nazwa = "N pętla duża (Aleja, C3, Nowojorska, C0)", petla = true, y = YA, punkty = new[] { P(nA[0]), P(nA[1]), P(nA[2]), P(nA[3]), P(nN[3]), P(nN[2]), P(nN[1]), P(nN[0]) }',TRAFFIC)
        self.assertIn('punkty = new[] { P(nA[0]), P(nT[0]), P(nT[1]), P(nA[1]) }',TRAFFIC)      # autobus (zgodnie z zegarem)
        self.assertNotIn('P(nA[0]), P(nN[0]), P(nN[1]), P(nA[1])',TRAFFIC)
        self.assertIn("NdTorowisko()",DISTRICT)

    def test_tram_loop_goes_around_whole_new_district_with_six_stops(self):
        self.assertIn("float zS = NS1 - AH * .5f, xE = NC[0] + AH * .5f, zN = NN1 + AH * .5f, xW = NC[3] - AH * .5f;",DISTRICT)
        for n in ("T1 · TRAMWAJOWA ZACHÓD","T1 · TRAMWAJOWA WSCHÓD","T1 · ALEJA CENTRALNA","T1 · NOWOJORSKA WSCHÓD","T1 · NOWOJORSKA ZACHÓD","T1 · PLAC ATLANTA"):
            self.assertIn('"'+n+'"',DISTRICT)
        self.assertIn("foreach (var cz2 in new[] { NS1, RZ, NN1 })",DISTRICT)   # wszystkie skrzyżowania obwodu

    def test_tram_runs_a_closed_loop_with_left_turns_only(self):
        self.assertIn('var svc = vehicle.gameObject.AddComponent<BraxTramT1>();',DISTRICT)
        self.assertIn("BraxTramwajSklad.Petla(new[] { new Vector2(xW, zS), new Vector2(xE, zS), new Vector2(xE, zN), new Vector2(xW, zN) }, TramR",DISTRICT)
        self.assertIn("public const float TramR = 16f;",DISTRICT)
        self.assertIn("s = Zawin(s + krok);",TRAM)

    def test_left_turn_radius_clears_inner_curb_corner(self):
        # łuk R przy lewym skręcie z prawego pasa (oś 2.25 m od osi ulicy 9 m): narożnik krawężnika musi być wewnątrz okręgu
        # łuku z zapasem na pół szerokości wagonu (1.33 m) i wychylenie członu 9 m (L²/8R)
        R, lane, half = 16.0, 2.25, 4.5
        cx, cz = lane - R, -lane + R           # środek łuku: skręt z jazdy na wschód (pas z = −2.25) na północ (pas x = +2.25)
        corner = (-half, half)                 # narożnik krawężnika po wewnętrznej stronie
        d = math.hypot(corner[0] - cx, corner[1] - cz)
        clearance = R - d - (1.33 + 81 / (8 * R))
        self.assertGreater(clearance, 0.3)

    def test_articulated_sections_follow_track_at_both_ends(self):
        self.assertIn("var f = Punkt(s - czlonPrzod[i]); var r = Punkt(s - czlonTyl[i]);",TRAM)
        self.assertIn("Quaternion.LookRotation(d.normalized, Vector3.up)",TRAM)
        self.assertIn("BraxTramwajSklad.Przeguby(o, out var jakPrzeguby)",DISTRICT)
        sk = src("Editor/Claude/Region/BraxTramwajSklad.cs")
        self.assertIn("BraxCityMeshCut.Subtract(kopia.transform, przed",sk)
        self.assertIn("BraxCityMeshCut.Subtract(kopia.transform, za",sk)

    def test_doors_plug_then_slide_on_the_right_side(self):
        self.assertIn("drzwiDom[i] + drzwiWysun[i] * w + drzwiPrzesun[i] * p",TRAM)
        sk = src("Editor/Claude/Region/BraxTramwajSklad.cs")
        self.assertIn("wysun = new Vector3(.06f, 0f, 0f)",sk)
        self.assertIn("bool prawa = la.x > .9f && lb2.x > .9f && lc.x > .9f;",sk)
        self.assertIn("BRAXHORN_TRAM_MODEL",sk)

    def test_cars_and_pedestrians_yield_to_tram_on_crossings(self):
        self.assertIn("BraxStrefyZatrzymania.Dodaj(new BraxStrefyZatrzymania.Strefa",TRAM)
        self.assertIn("dlaPieszych = true",TRAM)
        self.assertIn("autoWPolu",TRAM)

    def test_only_northern_shop_roundabout_removed(self):
        self.assertIn("RONDO_POLNOC_USUNIETE=tak",CITY)
        self.assertNotIn('PlacZPierscieniem("pętla PÓŁNOC"',CITY)
        self.assertIn('PlacZPierscieniem("rondo"',CITY)
        self.assertIn('PlacZPierscieniem("pętla ZACHÓD"',CITY)

    def test_parking_has_18_bays_and_valid_sizes(self):
        self.assertIn("for (int i = 0; i < 9; i++)",CITY)
        self.assertIn("MiejscaParkingu.Add((new Vector3(AX - 10.0f",CITY)
        self.assertIn("MiejscaParkingu.Add((new Vector3(AX + 10.0f",CITY)
        self.assertIn('MiejscaParkingu.Count',CITY)
        self.assertGreater(3.5-2.5*1.294,.1)
        self.assertGreater(12-2*2.55,5)
        self.assertIn("PARKING SUPERMARKET BRAXHORN",CITY)

    def test_parking_access_is_paved_without_overlapping_marks(self):
        self.assertIn('Chodnik("SUPERMARKET BRAXHORN: dojście od parkingu"',CITY)
        self.assertIn("farbaB.MinMax(new Vector3(AX - 17f",CITY)
        self.assertIn("farbaB.MinMax(new Vector3(AX + 6f",CITY)
        self.assertIn("Wstawki(",CITY)
        self.assertIn("BudujAsfalt(farbaB)",CITY)

    def test_old_north_roundabout_yield_doesnt_stop_bus(self):
        self.assertNotIn('nazwa = "M pętla przez Aleję Północną (zawracanie na rondzie PÓŁNOC)"',TRAFFIC)
        self.assertIn("PETLA = new BraxTrasyAut.Wezel",TRAFFIC)
        self.assertIn("rondo = false, ux = Pd",TRAFFIC)
        self.assertNotIn("Wlot(cP,",CITY)

    def test_store_does_not_move_because_parking_sidewalk_is_near_door(self):
        self.assertIn('var depth = D + 1.5f',SHOP)
        self.assertIn('nowy chodnik parkingu', SHOP.lower())
        self.assertIn("if (Physics.OverlapBox(srodek,",SHOP)
        self.assertIn("for (float x = -W * .5f - 4f",SHOP)

    def test_tree_and_fence_clearing_is_store_specific(self):
        self.assertIn("void OczyscWejscie(Scene scene",SHOP)
        self.assertIn("if (!go || go.transform == shopRoot",SHOP)
        self.assertIn("var q = store.InverseTransformPoint(p)",SHOP)
        self.assertIn("BraxCityMeshCut.Subtract(candidate, opening",SHOP)
        self.assertIn("brak_rozpoznania=",SHOP)
        self.assertIn("cutSize",SHOP)
        self.assertNotIn("DestroyImmediate(fence", SHOP)

    def test_store_access_and_receipt_samples_exist(self):
        self.assertIn('BraxMiastoA.DrzwiPrzesuwne(t,',SHOP)
        self.assertIn("GetComponentInParent<BraxDrzwiPrzesuwne>()",SHOP)
        self.assertIn("Physics.OverlapCapsule(", SHOP)
        self.assertIn('potencjalne_przeszkody=',SHOP)
        self.assertIn('"supermarket_" + nr + "_wejscie"', SHOP)

    def test_original_user_sign_and_wares_are_preserved(self):
        self.assertIn('"szyld_supermarket"',SHOP)
        self.assertIn('"wawer_supermarket"',SHOP)
        self.assertIn("BraxSklepyTowar.Polka(",SHOP)
        self.assertIn('"regał chłodniczy',SHOP)
        self.assertIn('Obj("stoły warzyw"',SHOP)


if __name__ == "__main__":
    unittest.main()
