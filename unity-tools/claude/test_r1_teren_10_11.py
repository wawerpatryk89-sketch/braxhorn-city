"""MIASTO R1 — teren od siłowni w lewo i w prawo, boiska, morze, klub NOVA (właściciel 10.11 09:21–09:26, zdjęcia i filmy z APK 9.11):
mural siłowni ucięty, migające elewacje obok siłowni / przy boiskach / nad morzem, siatka ogrodzenia boiska, słup na jezdni
ul. Wiaduktowej, latarnie wysięgnikiem nad chodnik, NOVA: stary szyld, szatnia z brył, butelki z kodu, otwarte WC, mural nie na
całą ścianę, matowa kula. Sprawdzenie źródła i geometrii w Pythonie (bez Unity)."""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
E = ROOT / "unity-city/Assets/Editor"
R = E / "Claude/Region"


def src(p):
    return re.sub(r'//[^\n]*', "", p.read_text(encoding="utf-8"))


SILOWNIA = src(R / "BraxSilowniaZewnatrz.cs")
BILL = src(R / "BraxMiastoBillboardy.cs")
PIERZEJE = src(R / "BraxUlicePierzeje.cs")
LATARNIE = src(E / "CityRebuild/BraxCityStreetLighting.cs")
ARCH = src(E / "CityRebuild/BraxCityArchitecture.cs")
COURTS = src(E / "Claude/BraxClaudeCourts.cs")
NOVA = src(R / "BraxMiastoNova.cs")
PRYW = (ROOT / "unity-tools/claude/prywatne.py").read_text(encoding="utf-8")


class Silownia(unittest.TestCase):
    def test_courtyard_brick_slabs_of_original_gym_are_not_drawn(self):
        # Gym_Mesh_38: płyty x −1.6…−1.0 i 14.5…15.1 (oś x odbita) — lica w licach BAR 24 (x −1) i NUTRITION (x 14.5)
        self.assertIn('r.name != "Gym_Mesh_38"', SILOWNIA)
        self.assertIn("r.enabled = false", SILOWNIA)
        self.assertIn("ScianyDziedzinca", SILOWNIA.split("static void Krok")[0])

    def test_nutrition_side_cladding_is_2cm_in_front_of_old_face(self):
        m = re.search(r'kl\.MinMax\(new Vector3\(([\d.]+)f, [\d.]+f, [\d.]+f\), new Vector3\(([\d.]+)f', SILOWNIA)
        x0, x1 = float(m.group(1)), float(m.group(2))
        self.assertLess(x1, 14.5 - .015)          # tył okładziny ≥ 2 cm przed licem oryginału x 14.5
        self.assertGreater(x1 - x0, .05)

    def test_mural_whole_image_inside_courtyard(self):
        self.assertIn("DziedzinecX0 = -1.0f, DziedzinecX1 = 14.5f", BILL)
        self.assertIn("if (x < DziedzinecX0 || x > DziedzinecX1) continue;", BILL)
        # cały obraz: wysokość ściany, szerokość z proporcji; UV 0…1 w obrysie obrazu (bez kadrowania)
        self.assertIn("float imgH = y1 - y0, imgW = imgH * proporcja;", BILL)
        self.assertIn("(ix1 - p.x) / imgW, (p.y - iy0) / imgH", BILL)
        # 3:2 na ścianie 15.5 × 8.5 m → 12.75 m szerokości, mieści się w dziedzińcu
        w, h = 14.5 - (-1.0), 8.5
        self.assertLessEqual(h * 1.5, w)


class LicaZrodla(unittest.TestCase):
    def test_slab_cut_only_original_city_meshes(self):
        self.assertIn('mf.gameObject.name.StartsWith("CityMesh_", StringComparison.Ordinal)', PIERZEJE)
        self.assertIn("BraxCityMeshCut.Subtract(mf.transform, plyta", PIERZEJE)

    def test_all_street_building_sides_and_extensions(self):
        self.assertIn('ZrodloWLicuX(b.x0, b.z0, b.z1, .18f, yG, "bok_zach_"', PIERZEJE)
        self.assertIn('ZrodloWLicuX(b.x1, b.z0, b.z1, .18f, yG, "bok_wsch_"', PIERZEJE)
        for s in ('"_zach"', '"_wsch"', '"_tyl"'):
            self.assertIn(s, PIERZEJE)

    def test_slab_straddles_face_both_sides(self):
        # cięcie zostawia trójkąty leżące NA granicy bryły → płyta ±4 cm obejmuje lico z obu stron
        self.assertIn("new Vector3(x - .04f, y0, z0 - .02f), new Vector3(x + .04f, y1, z1 + .02f)", PIERZEJE)


class UlicaILatarnie(unittest.TestCase):
    def test_arm_over_carriageway(self):
        self.assertIn("Vector3 Z(float along,float y)=>new Vector3(0,y,along*facing);", LATARNIE)
        self.assertIn("frame.Tube(Z(.18f,6.04f),Z(1.22f,6.04f)", LATARNIE)
        self.assertNotIn("new Vector3(1.22f*facing,6.04f,0)", LATARNIE)
        # bliższa strona z 22.90 → +z (oś jezdni 28.5), dalsza 33.32 → −z
        self.assertIn("float z=far?33.32f:22.90f;", LATARNIE)
        self.assertIn("float facing=far?-1f:1f;", LATARNIE)
        for z, f in ((22.90, 1), (33.32, -1)):
            self.assertLess(abs(z + f * 1.54 - 28.5), abs(z - 28.5))

    def test_no_post_in_wiaduktowa_mouth(self):
        self.assertIn("if(Mathf.Abs(x-(-66f))<.01f)pominiete++;", LATARNIE)
        # jezdnia Wiaduktowej x −69.2…−63.2: słup −66 był w środku
        self.assertTrue(-69.2 < -66 < -63.2)


class BoiskaIWieze(unittest.TestCase):
    def test_fence_mesh_cell_is_oczko(self):
        self.assertIn('m.SetTextureScale("_MainTex", Vector2.one / (oczko * 8f));', COURTS)
        cell = .125 * 8 / 8   # kafel 0.125 × 8 = 1 m ma 8 oczek
        self.assertAlmostEqual(cell, .125)

    def test_spandrel_between_mullions(self):
        self.assertIn("opaque.Box(new Vector3(x+.09f+(bay-.09f)*.5f,y+.28f,.0f),new Vector3(bay-.09f,.56f,.14f));", ARCH)
        self.assertNotIn("opaque.Box(new Vector3(x+bay*.5f,y+.28f,.0f),new Vector3(bay,.56f,.14f));", ARCH)


class Nova(unittest.TestCase):
    def test_old_club_nova_sign_gone(self):
        self.assertNotIn('Napis3D("CLUB NOVA"', NOVA)
        self.assertNotIn("new Vector3(38.4f, 3.62f, pz)", NOVA)

    def test_cloakroom_coats_from_osiedle_pack(self):
        self.assertIn('BraxMiastoAssety.Model("osiedle_wnetrza", "szatnia_plaszcz_3decraft")', NOVA)
        self.assertIn('BraxMiastoAssety.Model("osiedle_wnetrza", "szatnia_wieszak_roh3d")', NOVA)

    def test_bar_bottles_from_osiedle_pack(self):
        self.assertIn('"bar_polki_butelki_windupbird"', NOVA)
        self.assertIn('"bar_butelki_helyeouka"', NOVA)
        self.assertIn("for (int sh = 0; sh < 3 && !polkiZModelu; sh++)", NOVA)

    def test_wc_doors_closed_cover_both_openings(self):
        self.assertIn('"WC drzwi zamknięte"', NOVA)
        for xd, hc in ((46.7, 46.75), (49.9, 49.85)):
            da, db = min(xd, hc) - .6, max(xd, hc) + .6
            self.assertLessEqual(da, xd - .6); self.assertGreaterEqual(db, xd + .6)
            self.assertLessEqual(da, hc - .6); self.assertGreaterEqual(db, hc + .6)

    def test_mural_whole_wall(self):
        self.assertIn("MuralNaCalaSciane(HX0 + .01f, ZX0 - .21f, F + .01f, C - .02f, HZ1 - .012f", NOVA)
        w, h = (47.4 - .21) - (32.6 + .01), (4.55 - .02) - (.15 + .01)
        self.assertGreater(w, 14.0); self.assertGreater(h, 4.0)

    def test_ball_mirror_facets_glow_and_rotate(self):
        self.assertIn("KulaZLusterek(piv, d * .5f);", NOVA)
        self.assertIn('lustra.SetTexture("_EmissionMap", paleta)', NOVA)
        self.assertIn("ef.obrotyNaMinute = 12f;", NOVA)
        self.assertNotIn('LoadGlb("Assets/Source/Claude/Nova/disco_ball.glb")', NOVA)

    def test_pack_decrypted_for_city(self):
        self.assertIn('"osiedle_wnetrza")', PRYW.split("PACZKI_MIASTA = ")[1].split("\n\n")[0])
        self.assertTrue((ROOT / "unity-city/Prywatne/osiedle_wnetrza.tar.gz.enc").stat().st_size > 1_000_000)
        cred = (ROOT / "unity-city/Assets/Source/Claude/Nova/CREDITS.md").read_text(encoding="utf-8")
        for a in ("windupbird", "Helyeouka", "3DECraft", "tedpermana"):
            self.assertIn(a, cred)


TOWAR = src(R / "BraxSklepyTowar.cs")


class NovaPremium(unittest.TestCase):
    """Właściciel 10.11 10:06–10:08: „jeden wielki kloc, piksel” — skóra pikowana, orzech, mosiądz, marmur jak u kolegi."""

    def test_quilted_leather_texture_in_repo(self):
        d = ROOT / "unity-city/Assets/Source/Claude/Nova/tekstury/nova_skora_pikowana"
        self.assertTrue((d / "diffuse.jpg").stat().st_size > 50_000)
        self.assertTrue((d / "normal.png").exists())
        self.assertIn('"nova_skora_pikowana", .64f', NOVA)

    def test_walls_bar_and_booths_upholstered(self):
        self.assertIn("static void Okladzina(", NOVA)
        for s in ('SkoraPik("front baru', 'SkoraPik("oparcia loży', 'SkoraPik("front konsolety DJ', "MarmurCzarny", "Mosiadz"):
            self.assertIn(s, NOVA)


class SklepyKazdy(unittest.TestCase):
    """Właściciel 10.11 10:12–10:13: kwiaciarnia z samymi kwiatami na półkach — przejrzeć KAŻDY sklep."""

    def test_center_of_floor_fixtures_per_type(self):
        self.assertIn('BraxRegion.Note("pierzeje: środek sali "', PIERZEJE)
        for typ in ('case "market": case "piekarnia":', 'case "warzywniak":', 'case "cukiernia": case "optyk":', 'case "ksiegarnia":', 'case "kwiaty":', 'case "poczta":'):
            self.assertIn(typ, PIERZEJE)

    def test_florist_cut_flowers_in_buckets(self):
        self.assertIn("void Bukiet(Vector3 p, Color32 kwiat, int n)", PIERZEJE)
        self.assertIn("float sBok = regalLewy ? sIn1 - .26f : sIn0 + .26f;", PIERZEJE)

    def test_post_office_parcels_not_books(self):
        poczta = TOWAR.split('"poczta" =>')[1].split("\n")[0]
        self.assertIn("cardboard_box_01", poczta)
        self.assertNotIn("ksiazk", poczta)

    def test_bar24_booths_upholstered_not_blocks(self):
        bar = PIERZEJE.split('if (t == "bar")')[1].split('if (t == "bistro" || t == "pizzeria" || t == "bar")')[0]
        self.assertIn('"nova_skora_pikowana"', bar)
        self.assertIn('"bar: orzech ciemny (fornir)"', bar)
        self.assertIn('"bar: mosiądz szczotkowany"', bar)
        self.assertNotIn('BraxUliceKit.Mat("bar: skóra loży (bordo)"', bar)
        # tablica zakładów nad lożami na ścianie bocznej, wiersze co 21 cm przy wersalikach 7.5 cm (nie nachodzą)
        self.assertIn("Vector3 nT = drzwiPrawo ? f.R : -f.R;", bar)
        self.assertLess(.11 + .02, .21)

    def test_bar_shelves_without_flowery_jugs(self):
        for typ in ('"bar" or "bistro" =>', '"pizzeria" =>'):
            self.assertNotIn("jug_01", TOWAR.split(typ)[1].split("\n")[0])

    def test_kantor_bench_instead_of_garden_chairs(self):
        self.assertNotIn('"plastic_monobloc_chair_01", "Kantor: krzesło"', PIERZEJE)
        self.assertIn('"kolizja: ławka kantoru"', PIERZEJE)

    def test_workshop_bike_on_repair_stand_and_floors(self):
        self.assertIn('"kolizja: rower na stojaku"', PIERZEJE)
        for typ in ('case "kwiaty": pod = new[] { "concrete_floor_worn_001"', 'case "warsztat": pod = new[] { "concrete_floor_worn_001"'):
            self.assertIn(typ, PIERZEJE)
        self.assertIn('"concrete_floor_worn_001"', (ROOT / "unity-tools/claude/fetch_region.py").read_text(encoding="utf-8"))


KIT = src(R / "BraxUliceKit.cs")


class MiekkiePoduchy(unittest.TestCase):
    """Właściciel 10.11 10:29: „kolega w kodzie zrobił miękkie, okrągłe poduszki”."""

    def test_rounded_puffed_cushion_mesh(self):
        self.assertIn("public void Poducha(Vector3 a, Vector3 b, float r, float puf = .012f, int N = 10)", KIT)
        self.assertIn("var p = sr + wn + nr * r + fn * (pf * B);", KIT)
        self.assertIn("float pf = fn.y < -.5f ? 0f", KIT)          # spód płaski — leży na cokole

    def test_cushion_math_rounds_corners(self):
        import math
        h, r = (.3, .2, .25), .06
        hin = [x - r for x in h]
        for su in (-1.0, -.5, 0.0, 1.0):
            for sw in (-1.0, 1.0):
                q = (h[0], su * h[1], sw * h[2])                    # ściana +x
                wn = [max(-a, min(a, x)) for x, a in zip(q, hin)]
                d = [x - y for x, y in zip(q, wn)]
                L = math.sqrt(sum(x * x for x in d))
                p = [y + x / L * r for x, y in zip(d, wn)]
                for x, a in zip(p, h):
                    self.assertLessEqual(abs(x), a + 1e-9)       # w obrysie min/max
                if abs(su) == 1:
                    self.assertGreater(L, r * 1.01)                # naroże ścięte łukiem (punkt sześcianu poza kulą)

    def test_used_on_booths_and_bench(self):
        self.assertGreaterEqual(NOVA.count("Poducha("), 6)
        bar = PIERZEJE.split('if (t == "bar")')[1].split('if (t == "bistro" || t == "pizzeria" || t == "bar")')[0]
        self.assertIn("gladka.Poducha(", bar); self.assertIn("pik.Poducha(", bar)
        self.assertIn("skG.Poducha(", PIERZEJE)


class NovaBezKolumn(unittest.TestCase):
    def test_hanging_line_arrays_removed(self):
        # właściciel 10:39: dwie czarne „kolumny jak schody” wiszące koło DJ-a — usunięte (zasłaniały mural)
        self.assertNotIn("foreach (float x in new[] { RX0 - .3f, RX1 + .3f })", NOVA)
        self.assertNotIn("float y = C - .4f - i * .3f, tilt = i * 2.2f;", NOVA)


EFEKTY = src(ROOT / "unity-city/Assets/Claude/Runtime/BraxNovaEfekty.cs")


class NovaPoludnie(unittest.TestCase):
    """Właściciel 10.11 10:41–10:45: mural na całą ścianę koło DJ-a, przerwa nad fasadą NOVA, światełka mrugające (lasery tylko mrugają)."""

    def test_mural_wraps_onto_backroom_wall(self):
        self.assertIn("MuralNaCalaSciane(HX0 + .01f, ZX0 - .21f, F + .01f, C - .02f, HZ1 - .012f, ZX0 - .212f, ZZ0 - .19f)", NOVA)
        self.assertIn("v1 = 1f - (1f - vis) * .15f; v0 = v1 - vis;", NOVA)     # logo u góry zostaje
        # rozwinięcie: 14.58 + 4.38 m przy 4.38 m wysokości (obraz 3:1) — widać 69 % wysokości obrazu
        W = (47.4 - .21) - (32.6 + .01) + ((61.4 - .012) - (57.2 - .19)); H = (4.55 - .02) - (.15 + .01)
        self.assertGreater(H * 3 / W, .6)

    def test_upper_facade_saved_in_world_space(self):
        self.assertIn('uchwyt.SetParent(gNova, false); uchwyt.position = Vector3.zero;', PIERZEJE)
        self.assertIn('BraxUliceKit.Zapisz(uchwyt, "pierzeje_nova_gora")', PIERZEJE)
        self.assertNotIn('BraxUliceKit.Zapisz(gNova, "pierzeje_nova_gora")', PIERZEJE)
        self.assertIn("parent ? parent.InverseTransformPoint((lo + hi) * .5f) : (lo + hi) * .5f", PIERZEJE)
        arch = (E / "CityRebuild/BraxCityArchitecture.cs").read_text(encoding="utf-8")
        self.assertIn('Group(id+" preserved interior upper storeys",parent,new Vector3(0,baseY,0)', arch)   # grupa na y = baseY

    def test_lasers_only_short_flashes_and_blinking_par_lights(self):
        self.assertIn("bool on = takt % 3 == 2 && (ud + i) % 2 == 0 && fb < .12f;", EFEKTY)
        self.assertNotIn("foreach (var x in lasery) strob.Dodaj(", EFEKTY)
        self.assertIn("public Transform[] migacze", EFEKTY)
        self.assertIn('"soczewka świecąca"', EFEKTY)
        self.assertIn("ef.migacze = migacze.ToArray();", NOVA)
        self.assertIn('"stroboskop", .13f', NOVA)


class Hokery(unittest.TestCase):
    """Właściciel 10.11 12:19: „krzesła — siedziska za płaskie, zrób to normalnie”."""

    def test_round_soft_cushion_lathe(self):
        self.assertIn("public void PoduchaOkragla(Vector3 p, float r, float h, float rr, float puf = .015f, int seg = 28)", KIT)
        self.assertIn("prof.Add(new Vector2(x, h + puf * (1f - t * t)));", KIT)

    def test_nova_and_bar24_stools_thick_seats(self):
        self.assertIn("BraxUliceKit.S(skora).PoduchaOkragla(p + Vector3.up * (.71f * K), .21f * K, .11f * K, .045f * K, .018f);", NOVA)
        self.assertNotIn("BraxUliceKit.S(skora).Walec(p + Vector3.up * .74f * K, .2f * K, .06f, 18);", NOVA)
        self.assertIn("s.PoduchaOkragla(new Vector3(0, .69f * k, 0), .2f * k, .1f * k, .042f * k, .016f)", PIERZEJE)
        self.assertGreaterEqual(.11, .06 * 1.8)   # siedzisko ≥ 1.8 × grubsze niż dawny krążek


MEBLE = src(R / "BraxRegionMeble.cs")
SPORT = src(R / "BraxMiastoSport.cs")


class WszystkoObite(unittest.TestCase):
    """Właściciel 10.11 12:25: „czarną skórę… najlepiej pikowaną”, „każde jedno krzesło ma być obite… sofa, wszystko… nic sztucznego”."""

    def test_nova_seats_black_quilted(self):
        self.assertIn('var skora = SkoraPik("siedziska hokerów — skóra pikowana czarna", Czarna);', NOVA)
        self.assertIn('var welur = SkoraPik("siedziska loży — skóra pikowana czarna", Czarna);', NOVA)
        self.assertIn('var welurC = SkoraPik("oparcia loży — skóra pikowana czarna", Czarna);', NOVA)
        self.assertNotIn("koniak", NOVA.split("static void Bar()")[1].split("static void")[0] if "static void Bar()" in NOVA else "")

    def test_upholstered_chair_everywhere(self):
        self.assertIn("internal static GameObject KrzesloTapicerowane(string nazwa, Transform bg, Vector3 at, float yaw)", PIERZEJE)
        self.assertNotIn('"outdoor_table_chair_set_01"', PIERZEJE)        # ogrodowy komplet z gołymi siedziskami
        self.assertNotIn('"SchoolChair_01"', PIERZEJE)
        self.assertIn('KrzesloTapicerowane("Krzesło kawiarniane (tapicerowane)"', PIERZEJE)
        self.assertIn('BraxUlicePierzeje.KrzesloTapicerowane("Krzesło (tapicerowane)", parent, p, yaw);', MEBLE)
        self.assertNotIn("drewno.Box(L(0, .45f, 0)", MEBLE)
        self.assertIn("u.Poducha(", MEBLE)                                 # krzesło biurowe: miękkie poduchy
        self.assertIn('"Hoker barowy: siedzisko (skóra pikowana)"', PIERZEJE)
        self.assertEqual(SPORT.count("BraxUliceKit.S(SkoraLawki).Poducha("), 2)
        wyspa = src(R / "BraxMiastoWyspa.cs")
        self.assertEqual(wyspa.count("BraxUlicePierzeje.KrzesloTapicerowane("), 2)     # krzesło trenera, krzesła stołu sędziów


TYLYB = src(R / "BraxRegionTylyB.cs")
MIASTOB = src(R / "BraxRegionMiastoB.cs")


class MigotanieSkaner(unittest.TestCase):
    """Właściciel 10.11 12:34–12:38: migotania od wyjścia z siłowni do portu i budynek od boisk; pary ze skanera APK 9.11."""

    def test_rear_premises_6cm_inside_rear_facade(self):
        self.assertIn("float rearIn=(b.far?b.z1:b.z0)-behind*.06f;", ARCH)
        self.assertIn("new Vector3(width-.3f,b.Height,Mathf.Abs(rearIn-interiorEnd))", ARCH)
        self.assertNotIn("(interiorEnd+(b.far?b.z1:b.z0))*.5f", ARCH)

    def test_tower_pilasters_off_glass_and_spandrel_planes(self):
        self.assertIn("-.18f * K + .03f, .14f * K);", TYLYB)
        self.assertIn("Box(lizenaM, a0, a0 + .45f * K, YC, top, -.33f, .2f * K);", TYLYB)
        self.assertIn("b0 = Mathf.Max(b0, a0 + .45f * K + .003f); b1 = Mathf.Min(b1, a1 - .45f * K - .003f);", TYLYB)

    def test_plaza_curb_above_paving(self):
        self.assertEqual(MIASTOB.count("YC + .025f); }"), 2)
        self.assertIn("KlinGora(kraw, c + u0 * R, c + u1 * R, c + u1 * Ro, c + u0 * Ro, YC + .025f);", MIASTOB)


if __name__ == "__main__":
    unittest.main()
