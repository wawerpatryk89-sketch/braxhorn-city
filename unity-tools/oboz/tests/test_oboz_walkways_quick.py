"""Fast geometry verification of authored camp walkways (no Unity / APK).
This only proves plan connectivity and source-level invariants, not walkability
or gameplay collisions on an actual Android phone.
"""
import math
from pathlib import Path
import re
import unittest

ROOT=Path(__file__).resolve().parents[3]
ENV=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozEnvironment.cs").read_text("utf-8")
CAMPUS=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozCampus.cs").read_text("utf-8")


def authored_paths():
    section=ENV.split("internal static List<Rect> PathRectangles()",1)[1].split("static void Cabin(",1)[0]
    pattern=r"new Rect\(\s*(-?\d+(?:\.\d+)?)f,\s*(-?\d+(?:\.\d+)?)f,\s*(\d+(?:\.\d+)?)f,\s*(\d+(?:\.\d+)?)f\s*\)"
    entries=[tuple(map(float,m)) for m in re.findall(pattern,section)]
    if len(entries)<23:raise ValueError("Missing source camp walkways: "+str(len(entries)))
    return [(x,z,x+w,z+h) for x,z,w,h in entries]


def touch(a,b):
    # Continuous source rectangles (no 1 m gaps at intersections).
    return a[0]<=b[2]+1e-4 and b[0]<=a[2]+1e-4 and a[1]<=b[3]+1e-4 and b[1]<=a[3]+1e-4


def ellipse_south(x,rx,rz,cz=-22):
    if abs(x)>rx:raise ValueError("Outside actual track ellipse")
    return cz-rz*math.sqrt(1-(x/rx)**2)


class QuickCampWalkways(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.paths=authored_paths()

    def test_all_23_source_walkways_are_connected(self):
        paths=self.paths
        seen={0}
        while True:
            extra={j for i in seen for j,b in enumerate(paths) if touch(paths[i],b)}
            if extra.issubset(seen):break
            seen|=extra
        self.assertEqual(len(seen),len(paths),f"Disconnected original paved paths: {set(range(len(paths)))-seen}")

    def test_bathroom_walkway_reaches_actual_doorway(self):
        # Bathroom at (-66,-52), north face z=-47; stone threshold at +5.12,
        # so 3D entrance z=-46.88. Tolerance includes 0.3 m stone step.
        door=(-66,-46.88)
        self.assertTrue(any(p[0]<=door[0]<=p[2] and p[1]<=door[1]<=p[3] for p in self.paths),
            "Bathroom path does not reach the actual stone entrance threshold")

    def test_gate_zebra_does_not_touch_south_track(self):
        m=re.search(r"const float vehicleCrosswalkZ=(-?\d+(?:\.\d+)?)f;",CAMPUS)
        self.assertIsNotNone(m,"Road zebra has no measured source-Z constant")
        z=float(m.group(1))
        half_depth=2.4/2
        for x in (-3.1,0,3.1):
            south_inner=ellipse_south(x,81.2,94.2)
            south_outer=ellipse_south(x,86,99)
            self.assertGreater(z-half_depth,south_inner+0.06,
                f"Road zebra clips running oval at x={x}; south edges {south_outer},{south_inner}")

    def test_bus_crossing_is_paint_not_stone(self):
        self.assertIn("OwnerTrackPedestrianCrossing(t);",CAMPUS)
        self.assertIn("Malowany znacznik przejścia na tartanie",CAMPUS)
        self.assertIn("new Vector3(2.1f,.002f,.065f),campusMark,false",CAMPUS)
        self.assertIn("if(IntersectsRunningTrack(x0,x1,z0,z1))",CAMPUS)
        self.assertIn("if(IntersectsRunningTrack(x-w*.5f,x+w*.5f,z-d*.5f,z+d*.5f))",CAMPUS)

    def test_no_old_second_zebra_or_unclipped_sidewalk(self):
        self.assertNotIn('Box(t,"Pas przejścia",new Vector3(-2.8f+i*.8f,.098f,-115)',CAMPUS)
        self.assertNotIn('Box(t,"Dojście pasażerów",new Vector3(26',CAMPUS)
        self.assertNotIn('Box(t,"Dojście od furtki",new Vector3(16',CAMPUS)


if __name__=="__main__":
    unittest.main()
