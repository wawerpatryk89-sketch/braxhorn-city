"""Independent metro corridor safeguards. No test here is an Android or Unity run."""
import unittest
from shapely.geometry import LineString, Point
from layout import METRO_LINE_WAYPOINTS
from meshkit import rounded_route
from audit_metro_network import assess, station_portal_points


class MetroCivilNetworkTests(unittest.TestCase):
    def test_complete_two_track_network_joins_all_five_original_portals(self):
        r=assess()
        self.assertEqual(r["stationOrder"],["M5","M1","M4","M3","M2"])
        self.assertEqual(r["tunnelSegments"],6)
        self.assertEqual(r["twinTracks"],2)
        self.assertEqual(r["railHeads"],4)
        self.assertGreater(r["lineNominalMetres"],1600)
        self.assertFalse(r["unitySceneTested"])
        self.assertFalse(r["trainDrivingTested"])

    def test_m1_m4_m5_are_true_stops_on_route_and_not_isolated_replicas(self):
        line=LineString(rounded_route(METRO_LINE_WAYPOINTS,60,3.0,False))
        for x,z in [(400,359),(235,645),(965,245),(765,800),(765,545)]:
            self.assertLess(line.distance(Point(x,z)),.03)
        self.assertTrue(line.is_simple)

    def test_casino_b1_and_ramp_excluded_from_track_civil_corridor(self):
        r=assess()
        self.assertIn("T11 underground casino parking",r["collisionExcluded"])

    def test_all_portals_flush_with_original_city_station_dimensions(self):
        r=assess()
        self.assertEqual(len(r["stations"]),5)
        for station in r["stations"]:
            self.assertAlmostEqual(station["originalPortalMetres"],140.6,delta=.4)


if __name__=="__main__":
    unittest.main()
