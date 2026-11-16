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
        # 09:57 owner: the walnut screen in the perfumery window was a box in the room - a façade unit has no mirror at all
        self.assertIn("if(!fac){Box(glassDark,Wd*.5f-1.0f,Wd*.5f+1.0f,y+1.2f,y+2.0f,D-.32f,D-.30f)", self.s)
        self.assertNotIn("D-.288f,D-.21f,true", self.s)

    def test_offices_furnished(self):
        self.assertIn('bool office=nm.Contains("BIURO");float cw=office?Wd*.62f:Wd-.5f;', self.s)
        self.assertIn('Goods(nm+" | stanowisko "+dk', self.s)
        self.assertIn('nm+" ekran "+(k+2)', self.s)

    def test_toys_on_round_display_towers(self):
        self.assertIn('"wieża z zabawkami"', self.s)
        self.assertIn("float[] th={.42f,.68f,.92f},tr2={.70f,.50f,.28f};", self.s)
        self.assertIn("int n=ring==0?8:5;", self.s)
        self.assertNotIn("Table(gOak,tr-.75f,tr+.75f,d-.45f,d+.45f,.75f)", self.s)

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


class RealWorldFurniture(unittest.TestCase):
    """Owner 16.11 09:57-10:00: "co to za stoliki ... jak w biurze, w sklepie, pod zabawki", "buty w powietrzu wiszą",
    "ma wyglądać jak w realnym życiu sklepy"."""

    def setUp(self):
        self.s = read("BraxChampionsG1Sklepy.cs")
        self.w = read("BraxChampionsG1Wnetrze.cs")

    def test_no_sled_legged_tables_left(self):
        t = self.s[self.s.index("void Table(Material top"):self.s.index("GameObject Ph(")]
        self.assertNotIn("Box(frame", t)                                    # no steel sled legs
        self.assertIn("Box(walnut,tl,tl+.05f,y+.012f,y+h-.035f,d0+.02f,d1-.02f,true)", t)
        self.assertNotIn("foreach(float xl in new[]{x+.15f,x+2.79f})F(frame", self.w)
        self.assertIn("F(StoneTop(),x,x+3f,y+.815f,y+.85f,z-.5f,z+.5f)", self.w)
        self.assertNotIn("Box(walnut,Wd*.5f-.9f,Wd*.5f+.9f,y,y+.80f,2.0f,2.9f,true)", self.s)   # the walnut block table

    def test_nothing_stands_on_clear_acrylic(self):
        self.assertNotIn("Box(glass,t-.22f,t+.22f,py,py+.28f", self.s)
        self.assertNotIn("Box(glass,tk-.12f,tk+.12f", self.s)
        self.assertNotIn("Box(glass,tr-.10f,tr+.10f,y+.90f", self.s)
        self.assertIn("Box(StoneTop(),t-.22f,t+.22f,py,py+.255f,dc-.22f,dc-.02f,true)", self.s)

    def test_office_is_an_office(self):
        o = self.s[self.s.index("bool office=nm.Contains"):self.s.index('if(!nm.Contains("PRALNIA"))')]
        for piece in ('"biurko"', "the modesty panel", "the pedestal", "BraxKatalog.KeyboardMouse", "desk_lamp_arm_01",
                      "modern_ceiling_lamp_01", "dining_chair_02", "sofa_02", "mid_century_lounge_chair", "coffee_table_round_01",
                      "potted_plant_02", "ceramic_vase_0", "HRug("):
            self.assertIn(piece, o)
        self.assertIn("Monitor(mp,toBack)", o)                               # the screen to the adviser
        self.assertIn('for(int k=-1;k<=1;k++)BraxKatalog.Tv(Kat(),P(Wd*.5f+k*1.50f,y+1.75f,D-.16f),toFront,65f', o)   # the video wall

    def test_veneer_feature_wall_in_panels(self):
        self.assertIn("for(float tj=.3f+1.2f;tj<fw-.3f;tj+=1.2f)Box(gCinemaDark,tj-.004f,tj+.004f,y+.10f,y+3.6f,D-.172f,D-.16f)", self.s)

    def test_shoe_try_on_ottomans_and_foot_mirror(self):
        self.assertIn('Ph("HotelSobczak/Ottoman_01/Ottoman_01_2k.gltf"', self.s)
        self.assertNotIn("for(int i=0;i<2;i++)Box(leatherNavy,Wd*.3f+i*Wd*.4f-.6f", self.s)

    def test_poly_haven_helper_and_raft_ceilings(self):
        self.assertIn("static GameObject G1PH(string path,Vector3 local,float yaw,string label,bool collide,bool top=false)", self.w)
        self.assertIn("BraxChampionsRealSize.Scale(full)", self.w)
        self.assertIn("float rx0=ix0+.45f,rx1=ix1-.45f,rz0=iz0+.45f,rz1=iz1-.45f,ry=yc-.14f;", self.w)
        for slug in ("HotelSobczak/Ottoman_01", "HotelSobczak/desk_lamp_arm_01", "HotelSobczak/modern_ceiling_lamp_01",
                     "HotelSobczak/dining_chair_02", "ArenaLobby/sofa_02", "HotelSobczak/mid_century_lounge_chair",
                     "HotelSobczak/coffee_table_round_01", "ArenaLobby/potted_plant_02", "HotelSobczak/potted_plant_04",
                     "HotelSobczak/ceramic_vase_01", "HotelSobczak/ceramic_vase_02"):
            name = slug.split("/")[1]
            prep = (TOOLS / ("prepare_hotel_sobczak_models.py" if slug.startswith("Hotel") else "prepare_arena_lobby_models.py")).read_text(encoding="utf-8")
            self.assertIn("'%s'" % name, prep, slug)                       # downloaded by CI before the build


if __name__ == "__main__":
    unittest.main()
