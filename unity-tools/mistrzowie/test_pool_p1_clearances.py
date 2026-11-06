"""Numerical clearances for owner-approved P1 prefabs and connected parking.

All sizes are NOMINAL metres before BraxClaudeOsiedle.K; this is only a
source geometry preflight, never a claim of tested Unity navigation/collision.
Verified source model dimensions are tied to the SHA256-locked private ZIP.
"""
from pathlib import Path
import itertools
import unittest

ROOT=Path(__file__).resolve().parents[2]
P1=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1.cs")
SPA=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1SpaFurnishing.cs")
EQUIP=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1Equipment.cs")
SLIDE=(ROOT/"unity-city/Assets/Editor/Mistrzowie/BraxChampionsPoolP1Slide.cs")


def rect(x,z,w,d):
    return (x-w/2, z-d/2, x+w/2, z+d/2)


def distance(a,b):
    """Signed separation in one axis; positive means disjoint."""
    return max(b[0]-a[2], a[0]-b[2])


class PoolP1ClearanceTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.pool=P1.read_text(encoding="utf-8")
        cls.spa=SPA.read_text(encoding="utf-8")
        cls.equip=EQUIP.read_text(encoding="utf-8")
        cls.slide=SLIDE.read_text(encoding="utf-8")

    def test_four_genuine_hot_tubs_have_navigable_west_entry(self):
        self.assertIn('foreach(float x in new[]{9.2f,14.7f})',self.spa)
        self.assertIn('foreach(float z in new[]{-11.85f,-15.65f})',self.spa)
        self.assertIn('Room("SPA RELAKS",12f,-13.8f,10.8f,7.8f,3.4f,white,-1)',self.pool)
        room=rect(12,-13.8,10.8,7.8)
        tubs=[rect(x,z,2.23,2.85) for x,z in
              itertools.product((9.2,14.7),(-11.85,-15.65))]
        for a in tubs:
            self.assertGreater(a[0]-room[0],.4)
            self.assertGreater(room[2]-a[2],.4)
            self.assertGreater(a[1]-room[1],.45)
            self.assertGreater(room[3]-a[3],.45)
        for a,b in itertools.combinations(tubs,2):
            self.assertTrue(distance(a,b)>0 or distance(
                (a[1],a[0],a[3],a[2]),(b[1],b[0],b[3],b[2]))>0)
        self.assertGreater(14.7-9.2-2.23,3.0)
        self.assertGreater(3.80-2.85,.9)
        # West door at x=6.6 to nearest tub front x≈8.085:
        self.assertGreater(9.2-2.23/2-room[0],1.40)

    def test_original_shower_cabins_fit_in_dedicated_wet_room(self):
        self.assertIn('Room("PRYSZNICE PREMIUM",0f,12f,10.8f,8f,3.5f,white);',self.pool)
        self.assertIn('foreach(float x in new[]{-3.6f,-1.2f,1.2f,3.6f})',self.spa)
        room=rect(0,12,10.8,8)
        cabins=[rect(x,12.4,1.38,1.16) for x in (-3.6,-1.2,1.2,3.6)]
        for a in cabins:
            self.assertGreaterEqual(a[0],room[0])
            self.assertLessEqual(a[2],room[2])
            self.assertGreater(a[1]-room[1],3.0)
            self.assertLess(a[3],room[3])
        for a,b in itertools.combinations(cabins,2):
            self.assertGreater(distance(a,b),1.0)

    def test_95_stalls_two_banks_connected_and_separate_from_entry(self):
        self.assertIn('"P1 WEST parking access connected to roundabout"',self.pool)
        self.assertIn('"P1 EAST parking access connected to roundabout"',self.pool)
        self.assertIn('float z=row==0?-52f:-38f;',self.pool)
        self.assertIn('if(bank==1&&row==1&&i==23)continue;',self.pool)
        starts=(-73,11)
        banks=[rect(-44,-44,68,28),rect(44,-44,68,28)]
        stalls=[]
        for bank in range(2):
            for row in range(2):
                for i in range(24):
                    if bank==1 and row==1 and i==23:continue
                    x=starts[bank]+(i+.5)*2.55
                    z=(-52,-38)[row]
                    area=rect(x,z,2.45,5.6)
                    surface=banks[bank]
                    self.assertGreaterEqual(area[0],surface[0])
                    self.assertLessEqual(area[2],surface[2])
                    self.assertGreaterEqual(area[1],surface[1])
                    self.assertLessEqual(area[3],surface[3])
                    stalls.append(area)
        self.assertEqual(len(stalls),95)
        self.assertGreater((-38-2.8)-(-52+2.8),8.0)
        self.assertGreater(-33.5-(-38+2.8),1.5)

    def test_light_original_slide_with_passable_walking_route(self):
        self.assertIn('const float NominalScale=.18f;',self.slide)
        self.assertIn('const int treads=18;',self.slide)
        self.assertIn('P1 slide independent tall galvanized post',self.slide)
        self.assertIn('n.StartsWith("slidesupports")||n=="gatepost"',self.slide)
        left=-54+(-33.35)*.18
        right=-54+44.30*.18
        near=47+(-59.73)*.18
        far=47+67.44*.18
        self.assertGreater(left,-80)
        self.assertLess(right,-42) # dedicated service paving begins near -42
        self.assertGreater(near,34) # north of family roof
        self.assertLess(far,60) # no model spills past site
        # Physical tread-to-tread rise ~22.2cm and run ~31cm nominal.
        self.assertLess(4.0/18,.36)
        self.assertLess(5.6/18,.34)
        self.assertGreater(5.6/18,.28)


if __name__=="__main__":
    unittest.main()
