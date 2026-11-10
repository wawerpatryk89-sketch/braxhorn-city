"""Owner 10.11 13:04-13:06 (video IMG_0866): "Każde migotanie, krawężniki migotają, wszystko ma zniknąć, co
niepotrzebne ... Zakaz nakładania czegoś na cokolwiek." / "zakaz nakładania tekstury na teksturę ... Zero migotania".

* City kerbs (build_infrastructure.py): every stone top lay in the sidewalk plane (PAVING_Y) over the same 18 cm strip -
  5091 m2 of flickering kerb before the fix. The stone now owns its strip: the footprint is cut out of every ground
  surface at walk level; stones split exactly at the 5 m dropped-kerb ramps; the ramp starts behind the dropped stone
  and has paved cheeks at both ends.
* Arena (z-fight scan 160 -> 31 pairs, the rest hidden undersides / sub-cm2 edge contacts): stand nosings are real bars
  10 mm above the tread and 8 mm proud of the riser; gold aisle edges stand 6 mm proud; the stair runner hangs in
  front of the stone riser; the hall carpets are 12 mm slabs; the changing rooms have butt joints.
"""
import re
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
TOOLS = ROOT / "unity-tools/mistrzowie"
ED = ROOT / "unity-city/Assets/Editor/Mistrzowie"


class Kerbs(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.s = (TOOLS / "build_infrastructure.py").read_text(encoding="utf-8")

    def test_kerb_footprint_is_cut_from_every_walk_level_surface(self):
        self.assertIn("curb_polys.append(box(p[0]-hw,p[1]-hd,p[0]+hw,p[1]+hd))", self.s)
        self.assertIn("kerb_cut=curb_polys+ramp_polys", self.s)
        self.assertIn("for role in ('paving','grass','prom_paving','cycle_lane','cycle_mark'):cuts[role]=(level_cut,kerb_cut)", self.s)
        # cut faces stay on the plane of the original face; untouched faces keep their exact vertices
        self.assertIn("A,B,C=np.linalg.solve(M,P[:,1])", self.s)
        self.assertIn("for k in face:old['positions'] += list(pos[k]);old['uv'] += list(uv[k])", self.s)

    def test_dropped_kerbs_match_the_ramp_exactly(self):
        self.assertIn("RAMP_HALF=2.5", self.s)
        self.assertNotIn("<2.7", self.s)
        self.assertIn("cuts_u.update(float(u) for u in ends if 0<u<length)", self.s)
        self.assertIn("[(-2.5,side*10.18),(2.5,side*10.18),(2.5,side*13),(-2.5,side*13)]", self.s)
        self.assertNotIn("(-2.5,side*10),(2.5,side*10)", self.s)
        self.assertIn("# paved cheeks at both ends of the ramp", self.s)


class StopsAndCrossings(unittest.TestCase):
    """Owner 10.11 14:36: "wszystkie przystanki, potem na ulicach krawężniki ... żeby nic nie migotało". Scan of the
    whole street layer (Infrastructure + Foundation, 1.04 M faces) after the fixes: 5 sub-cm2 edge contacts left."""
    @classmethod
    def setUpClass(cls):
        cls.s = (TOOLS / "build_infrastructure.py").read_text(encoding="utf-8")

    def test_zebra_cut_at_the_tram_rails(self):
        self.assertIn("for q in np.arange(-9.5,10,1):zebra.append((x,z,axis,q))", self.s)
        self.assertIn("rest=stripe.difference(grooves)", self.s)
        self.assertLess(self.s.index("grooves=unary_union("), self.s.index("for x,z,axis,q in zebra:"))

    def test_stop_sign_holds_its_pole(self):
        self.assertIn("localbox('sign',x,z,axis,4,base+2.42,v,.60,.72,.12)", self.s)


class Arena(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.a = (ED / "BraxChampionsArena.cs").read_text(encoding="utf-8")
        cls.ath = (ED / "BraxChampionsArenaAthletes.cs").read_text(encoding="utf-8")

    def test_nosings_are_real_bars(self):
        self.assertIn("BoxMinMax(nose, a0 + .006f, a1 - .006f, top - .03f, top + .010f, e0, e1, false)", self.a)
        self.assertIn("W.OBox(nose,c+d*(r0+.021f)+Vector3.up*(top-.01f),new Vector3(noseChord,.04f,.058f),q,false);", self.a)
        self.assertNotIn("top + .004f", self.a)

    def test_aisle_gold_and_light_line_never_in_a_marble_face(self):
        self.assertIn("float tf=p0+s*.006f,tb=p1+s*.006f;", self.a)
        self.assertIn("Side(interiorGold,top-.4f,top-.035f,", self.a)
        self.assertIn("float lineFront=front-s*(1.05f*2f+.03f);", self.a)

    def test_stair_runner_hangs_in_front_of_the_riser(self):
        self.assertIn("float outer=raw0+facing*.009f,back=raw1+facing*.008f;", self.a)
        self.assertNotIn("z-.004f,z+.004f", self.a)
        self.assertNotIn("x-.004f,x+.004f", self.a)

    def test_carpets_are_slabs_and_sign_clears_the_gold_lines(self):
        self.assertEqual(len(re.findall(r"W\.MM\(carpet, [^;]*yF, yF \+ \.012f", self.a)), 3)
        self.assertNotIn("Top(carpet", self.a)
        self.assertIn('Sign("hala_walk", new Vector3(cx, 4.75f, Z0 + .08f)', self.a)

    def test_changing_rooms_have_butt_joints(self):
        self.assertIn("Lo(RoomInner - h, RoomOuter + h)", self.ath)
        self.assertIn("W.MM(ceiling, Lo(RoomInner + h, RoomOuter - h), Hi(RoomInner + h, RoomOuter - h), 3.555f, 3.685f, RoomFront + h, RoomBack - h);", self.ath)
        self.assertIn("W.MM(woodWall, s * RoomOuter - h, s * RoomOuter + h, .15f, 3.65f, RoomFront + h, RoomBack - h, true);", self.ath)


if __name__ == "__main__":
    unittest.main()
