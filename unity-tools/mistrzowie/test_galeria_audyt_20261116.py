"""Owner 16.11.2026 08:59: "Teraz tak samo galerię sprawdzasz. Co jest do wykończenia, co jest do poprawienia, co jest za
duże, co za małe, skala. I skala wszystkiego ... tam chyba buty były za małe ... wszystko masz sprawdzić przy galerii."

The audit of G1 (every goods model measured from its glTF bounds, every shop rendered with 1.80 m figures) found: the shoes
are their real 25-29 cm but stood at the customer's feet (32 / 58 cm podiums) and one per metre on the SPORT ARENA wall; the
laptops 25-26 cm wide (a netbook); the mall stone, the counters, the "warm" plaster, the market granite and the home
textiles beige; bare street windows; a mirror and open fitting rooms floating in the façade units; an empty office behind a
wall-to-wall counter; thin toy tables; 12 m malls with four benches; open floor at the cinema tills."""
import pathlib
import re
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[2]
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"
TOOLS = ROOT / "unity-tools/mistrzowie"


def read(name):
    return (ED / name).read_text(encoding="utf-8")


class Scale(unittest.TestCase):
    def test_laptops_brought_to_14_and_15_inch(self):
        rs = read("BraxChampionsRealSize.cs")
        for name, k in (("laptop_gamingowy_czerwony", 1.38), ("laptop_srebrny", 1.30), ("laptop_zloty", 1.22)):
            self.assertRegex(rs, r'case "%s": return %.2ff;' % (name, k))
        # measured widths x factor: 31-36 cm
        for w, k in ((.255, 1.38), (.247, 1.30), (.264, 1.22)):
            self.assertTrue(.31 <= w * k <= .36)
        self.assertIn("BraxChampionsRealSize.Towar(name)", read("BraxChampionsG1Sklepy.cs"))

    def test_shoe_islands_at_knee_and_hip_height(self):
        s = read("BraxChampionsG1Sklepy.cs")
        self.assertIn("const float st1=.45f,st2=.80f;", s)
        self.assertNotIn("P(tt+sg*.40f,y+.32f,dd+q)", s)
        self.assertIn('Goods(nm+" | stół z butami"', s)
        self.assertIn('"pufa"', s)

    def test_sport_arena_shoe_wall_is_full(self):
        w = read("BraxChampionsG1Wnetrze.cs")
        self.assertIn("float stp=r==0||r==4?.84f:.42f;", w)
        self.assertNotIn("z<25.6f;z+=1.0f", w)
        self.assertIn('"SPORT ARENA | ściana butów z "', w)


class NoBeige(unittest.TestCase):
    def test_gallery_stone_and_plaster_through_fotoskan_neutral(self):
        g = read("BraxChampionsG1.cs")
        for mid, slug in (("d2_g1_mall_floor_limestone", "marble_01"), ("d2_g1_mall_floor_limestone_light", "marble_01"),
                          ("d2_g1_counter_stone", "marble_01"), ("d2_g1_wall_warm_plaster", "beige_wall_001"),
                          ("d2_g1_market_floor_granite", "granite_tile_04")):
            m = re.search(r'Fotoskan\("%s","%s",[0-9.]+f,new Color\((\d+)f/255f,(\d+)f/255f,(\d+)f/255f\)' % (mid, slug), g)
            self.assertIsNotNone(m, mid)
            r, gg, b = map(int, m.groups())
            self.assertLessEqual(max(r, gg, b) - min(r, gg, b), 3, mid)          # grey, no yellow cast
        self.assertNotIn("new Color(.92f,.86f,.80f)", g)

    def test_home_textiles_and_opal_light(self):
        h = read("BraxChampionsG1Meble.cs")
        for mid in ("d2_g1_home_boucle_white", "d2_g1_home_linen", "d2_g1_home_rug_stone", "d2_g1_home_lamp_linen"):
            self.assertIn('Fotoskan("%s"' % mid, h)
        self.assertNotIn("new Color(1.25f,1.05f,.78f)", h)                      # the golden glow
        self.assertIn('"g1_wall_home_greige",BraxChampionsFoundation.InteriorPaintSlug,2.0f,BraxChampionsFoundation.PaintTint(new Color(.74f,.80f,.78f))', read("BraxChampionsG1Wnetrze.cs"))

    def test_means_measured_and_mirrored_in_the_preview(self):
        f = read("BraxChampionsFoundation.cs")
        stub = (TOOLS / "apartments_preview/RegionStubs.cs").read_text(encoding="utf-8")
        for name in ("MeanCurlyTeddy", "MeanTerlenka", "MeanGraniteTile04"):
            self.assertIn(name, f)
            self.assertIn(name, stub)
        for slug in ("curly_teddy_natural", "terlenka", "granite_tile_04"):
            self.assertIn('{"%s",new Vector3(' % slug, f)
            self.assertIn('{"%s",new Vector3(' % slug, stub)


class Finishing(unittest.TestCase):
    def setUp(self):
        self.s = read("BraxChampionsG1Sklepy.cs")

    def test_street_windows_dressed(self):
        self.assertIn('if(fac&&u.trade!="fashion"&&u.trade!="sport"&&u.trade!="bags")', self.s)
        for t in ('u.trade=="shoes"', 'u.trade=="cosmetics"', 'u.trade=="toys"'):
            self.assertIn(t, self.s[self.s.index("THE STREET WINDOWS"):])
        self.assertIn('nm+" TV witryna"', self.s)

    def test_facade_units_back_the_mirror_and_close_the_fitting_rooms(self):
        self.assertIn("Box(walnut,.36f,1.44f,y,y+2.26f,D-.288f,D-.24f,true)", self.s)
        self.assertIn("Box(walnut,fr-2.85f,fr,y,y+2.4f,D-.13f,D-.07f,true)", self.s)
        self.assertIn("if(fac){Box(walnut,Wd*.5f-1.25f,Wd*.5f+1.25f,y+.06f,y+2.6f,D-.288f,D-.21f,true)", self.s)

    def test_offices_furnished(self):
        self.assertIn('bool office=nm.Contains("BIURO");float cw=office?Wd*.62f:Wd-.5f;', self.s)
        self.assertIn('Goods(nm+" | stanowisko "+dk', self.s)
        self.assertIn('nm+" ekran"', self.s)

    def test_toy_tables_fuller(self):
        part = self.s[self.s.index("five toys on a 1.5 m table"):]
        self.assertGreaterEqual(part[:600].count("BraxKatalog."), 4)

    def test_mall_seating_islands_with_palms(self):
        w = read("BraxChampionsG1Wnetrze.cs")
        self.assertIn("static void G1Planter(float x,float y,float z,int i)", w)
        self.assertIn('Seat(x-1.2f+k*.8f,y+.46f,zs,new Vector3(0,0,s),"ławka przy palmie "+(k+1))', w)
        self.assertIn('BraxMiastoAssety.Model("miasto_palmy","palma")', w)
        self.assertIn("o.transform.localScale*=3.0f*K/ph;", w)

    def test_cinema_queue_lanes_open_at_both_ends(self):
        k = read("BraxChampionsG1Kino.cs")
        self.assertIn("if(outer&&x>xb-1.7f)break;if(!outer&&x<xa+1.7f)continue;", k)
        self.assertIn('"barierka kolejki"', k)


if __name__ == "__main__":
    unittest.main()
