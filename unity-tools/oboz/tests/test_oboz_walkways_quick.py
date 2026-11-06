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
APPROACHES=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozPaths.cs").read_text("utf-8")


def authored_paths():
    section=ENV.split("internal static List<Rect> PathRectangles()",1)[1].split("static void Cabin(",1)[0]
    pattern=r"new Rect\(\s*(-?\d+(?:\.\d+)?)f,\s*(-?\d+(?:\.\d+)?)f,\s*(\d+(?:\.\d+)?)f,\s*(\d+(?:\.\d+)?)f\s*\)"
    entries=[tuple(map(float,m)) for m in re.findall(pattern,section)]
    if len(entries)<23:raise ValueError("Missing source camp walkways: "+str(len(entries)))
    return [(x,z,x+w,z+h) for x,z,w,h in entries]


def touch(a,b):
    # Continuous source rectangles (no 1 m gaps at intersections).
    return a[0]<=b[2]+1e-4 and b[0]<=a[2]+1e-4 and a[1]<=b[3]+1e-4 and b[1]<=a[3]+1e-4


def authored_approaches():
    number=r"(-?\d+(?:\.\d+)?|-?\.\d+)f"
    pattern=r'new EntranceApproach\("([^"]+)",'+r",".join([number]*6)+r'\)'
    return re.findall(pattern,APPROACHES)


def ellipse_south(x,rx,rz,cz=-22):
    if abs(x)>rx:raise ValueError("Outside actual track ellipse")
    return cz-rz*math.sqrt(1-(x/rx)**2)


class QuickCampWalkways(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.paths=authored_paths()

    def test_all_source_walkways_are_connected(self):
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
        surfaces=list(self.paths)
        for name,ax,az,bx,bz,width,height in authored_approaches():
            ax,az,bx,bz,width=map(float,(ax,az,bx,bz,width))
            surfaces.append((min(ax,bx)-width/2 if ax==bx else min(ax,bx),
                             min(az,bz)-width/2 if az==bz else min(az,bz),
                             max(ax,bx)+width/2 if ax==bx else max(ax,bx),
                             max(az,bz)+width/2 if az==bz else max(az,bz)))
        self.assertTrue(any(p[0]<=door[0]<=p[2] and p[1]<=door[1]<=p[3] for p in surfaces),
            "Bathroom path does not reach the actual stone entrance threshold")

    def test_walkways_reach_fronts_of_all_service_buildings(self):
        # Independent footprints from the service generator: front=-depth/2.
        # The old paths touched each rear wall and passed this much weaker
        # centre-only connectivity test despite never reaching their doors.
        for name,x,z,depth in (("canteen",-40,-70,9),("sauna",-47,-85,6),
                               ("medical",-25,-85,6),("storage",-25,-67,6)):
            door=(x,z-depth/2)
            self.assertTrue(any(p[0]<=door[0]<=p[2] and p[1]<=door[1]<=p[3]
                                for p in self.paths),name+" has no front approach")

    def test_no_path_crosses_closed_service_interiors(self):
        footprints=(("bathroom",-66,-52,12.3,10.3),
                    ("canteen",-40,-70,12,9),("sauna",-47,-85,7,6),
                    ("medical",-25,-85,8,6),("storage",-25,-67,8,6))
        for name,x,z,w,d in footprints:
            room=(x-w/2+.02,z-d/2+.02,x+w/2-.02,z+d/2-.02)
            for path in self.paths:
                overlap_x=min(room[2],path[2])-max(room[0],path[0])
                overlap_z=min(room[3],path[3])-max(room[1],path[1])
                self.assertFalse(overlap_x>0 and overlap_z>0,
                    f"Walkway crosses {name}: {path}")

    def test_ramps_start_on_network_and_end_at_real_floor(self):
        entries=authored_approaches()
        self.assertEqual(len(entries),10,"Four cabins and six other entrances")
        for name,ax,az,bx,bz,width,height in entries:
            ax,az,bx,bz,width,height=map(float,(ax,az,bx,bz,width,height))
            self.assertTrue(any(p[0]<=ax<=p[2] and p[1]<=az<=p[3] for p in self.paths),name)
            length=math.hypot(bx-ax,bz-az)
            self.assertGreaterEqual(width,.9,name)
            self.assertLess((height-.016)/length,.12,name+" has a steep threshold")
        expected={"Stołówka":(-40,-74.5,.16),"Sauna":(-47,-88,.16),
                  "Punkt medyczny":(-25,-88,.16),"Magazyn":(-25,-70,.16),
                  "WC / ŁAŹNIA":(-66,-47,.24)}
        for name,ax,az,bx,bz,width,height in entries:
            if name in expected:
                self.assertEqual(tuple(map(float,(bx,bz,height))),expected[name],name)

    def test_owner_requested_removal_of_unnecessary_entrance_stripes(self):
        arrival=CAMPUS.split("static void CampusArrival()",1)[1].split("static void ",1)[0]
        for removed in ("Linia krawędzi drogi", "Pasy piesze", "vehicleCrosswalkZ",
                        "OwnerTrackPedestrianCrossing", "UWAGA BIEZNIA"):
            self.assertNotIn(removed,arrival)
        self.assertIn('TrackAnnulus(rondo,"Pełny biały obrys ronda"',arrival)
        self.assertIn('RondoBull(rondo);',arrival)

    def test_paving_still_does_not_cover_running_track(self):
        self.assertNotIn("Malowany znacznik przejścia na tartanie",CAMPUS)
        self.assertIn("if(IntersectsRunningTrack(x0,x1,z0,z1))",CAMPUS)
        self.assertIn("if(IntersectsRunningTrack(x-w*.5f,x+w*.5f,z-d*.5f,z+d*.5f))",CAMPUS)

    def test_no_old_second_zebra_or_unclipped_sidewalk(self):
        self.assertNotIn('Box(t,"Pas przejścia",new Vector3(-2.8f+i*.8f,.098f,-115)',CAMPUS)
        self.assertNotIn('Box(t,"Dojście pasażerów",new Vector3(26',CAMPUS)
        self.assertNotIn('Box(t,"Dojście od furtki",new Vector3(16',CAMPUS)


if __name__=="__main__":
    unittest.main()
