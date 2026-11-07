"""7 Nov owner visual review: shelter, murals, doors, gate sign and panorama transition."""
from pathlib import Path
import unittest
ROOT=Path(__file__).resolve().parents[3]
C=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozCampus.cs").read_text("utf8")
E=(ROOT/"unity-city/Assets/Editor/Oboz/BraxObozEnvironment.cs").read_text("utf8")

class OwnerVisualReview20261107(unittest.TestCase):
    def test_panorama_has_non_colliding_perspective_road(self):
        self.assertIn("PerspectiveRoadToPanorama(t);",C)
        self.assertIn("Perspektywa drogi -> panorama — bez collidera",C)
        self.assertIn("BRAXHORN_OBOZ_PANORAMA_ROAD_BLEND",C)

    def test_gate_sign_edge_is_finer_and_feathered(self):
        self.assertIn("const int columns = 512;",C)
        self.assertIn("outsideNeighbours",C)
        self.assertIn("alphaScale=Mathf.Lerp(.42f,.86f,insideCoverage)",C)
        self.assertIn("importer.maxTextureSize = 4096",C)
        self.assertIn("importer.streamingMipmaps = true",C)
        # Sidewall boundary faces legitimately start at faceZ. Reject only the old
        # full front quad that pixelated the whole sign face.
        self.assertNotIn(
            "Face(new Vector3(x0,y0,faceZ),new Vector3(x0,y1,faceZ),\\n"
            "                 new Vector3(x1,y1,faceZ),new Vector3(x1,y0,faceZ));",
            C
        )

    def test_all_four_cabins_receive_owner_murals(self):
        for key in (
            "TRAINING_CAMP_DOMEK_1","WOJOWNIKOW_DOMEK_2",
            "DROGA_MISTRZA_DOMEK_3","FIGHT_ZONE_DOMEK_4"
        ):
            self.assertIn(key,E)
        self.assertIn("Quaternion.identity,5.85f,false",E)
        self.assertIn('parent.name.StartsWith("Domek ",StringComparison.Ordinal)',E)

    def test_every_cabin_gets_visible_external_door_trim(self):
        self.assertIn("CabinEntranceTrim(t);",E)
        self.assertIn('new Vector3(-.45f,.17f,-4.18f)',E)
        self.assertIn('Group("Futryna drzwi — komplet zewnętrzny"',C)
