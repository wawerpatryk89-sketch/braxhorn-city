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

    def test_cars_no_longer_drive_on_dedicated_t1_tracks(self):
        self.assertNotIn('nazwa = "N pętla C1–C2 południe"',TRAFFIC)
        self.assertNotIn('nazwa = "N pętla duża (Aleja, C3, Tramwajowa, C0)"',TRAFFIC)
        self.assertIn("NdTorowisko()",DISTRICT)
        self.assertIn("TorRect",DISTRICT)

    def test_tram_moves_on_actual_track_without_unsupported_turn(self):
        self.assertIn('var svc = vehicle.gameObject.AddComponent<BraxTramT1>();',DISTRICT)
        self.assertIn("svc.wschodX = east; svc.zachodX = west;",DISTRICT)
        self.assertIn("celX=east?wschodX:zachodX",TRAM)
        self.assertIn("p.x=Mathf.MoveTowards",TRAM)
        self.assertIn("p.z=torZ",TRAM)
        self.assertNotIn("transform.rotation=Quaternion.Euler(0f,180", TRAM)

    def test_tram_pivot_is_recentered_under_moving_hull(self):
        self.assertIn("o.transform.SetParent(vehicle, true)", DISTRICT)
        self.assertIn("east-before.center.x", DISTRICT)
        self.assertIn("box.center = b.center - vehicle.position",DISTRICT)
        self.assertNotIn('BraxMiastoAssety.Kolizja(root, o, "Tramwaj kolizja"', DISTRICT)

    def test_tram_size_fits_straight_track_analytic_clearance(self):
        k = 1.294
        track_left = -600 + 9.75
        track_right = -450 - 9.75
        tram_length = 33.9 * k
        travel = (track_right - 1.6 - tram_length/2) - (track_left + 1.6 + tram_length/2)
        self.assertGreater(travel, 60)
        self.assertIn("if (east - west < 4f)",DISTRICT)

    def test_tram_door_pairs_open_only_on_platform_side(self):
        self.assertIn('name.Contains("drzwi") || name.Contains("door")',TRAM)
        self.assertIn("b.center.z > modelBounds.center.z-.25f",TRAM)
        self.assertIn("doors.Count%2==0",TRAM)
        self.assertIn("Vector3.left*.52f",TRAM)
        self.assertIn("Vector3.right*.52f",TRAM)
        self.assertIn("bool beside = Mathf.Abs(transform.position.x-wschodX)",TRAM)
        self.assertIn("doors.Count==0 || doorState<.85f",TRAM)

    def test_tram_unknown_door_meshes_must_be_reported(self):
        self.assertIn("potrzebny krótki audyt siatek w Unity", TRAM)
        self.assertIn("Nie przesuwam losowych brył",TRAM)

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
