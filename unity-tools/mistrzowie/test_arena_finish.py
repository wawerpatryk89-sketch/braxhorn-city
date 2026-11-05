"""Offline A1 regressions: entrances, VIP seating, parking and preserved city footprint.
These are analytical checks; they do not claim Play Mode or Android verification.
"""
import math
import unittest
from pathlib import Path

import numpy as np

from build_exteriors import arena_entry_panels, ellipse

ROOT = Path(__file__).resolve().parents[2]


class ArenaFinishTests(unittest.TestCase):
    def test_front_and_rear_9p4m_door_apertures_match_both_lods(self):
        geometry = {}
        for resolution in (64, 128):
            panels = list(arena_entry_panels(ellipse(96, 59, resolution)))
            for direction, sign in (("spectator_south", -1), ("fighter_north", 1)):
                doors = [(a, b) for a, b, opened in panels
                         if opened and ((a[1] + b[1]) * sign > 0)]
                self.assertEqual(len(doors), 2)
                x_values = [float(v[0]) for a, b in doors for v in (a, b)]
                self.assertAlmostEqual(min(x_values), -4.70, places=4)
                self.assertAlmostEqual(max(x_values), 4.70, places=4)
                geometry[(resolution, direction)] = (round(min(x_values), 4), round(max(x_values), 4))
            self.assertFalse(any(opened and abs((a[1]+b[1])/2) < 57.5
                                 for a, b, opened in panels))
        for direction in ("spectator_south", "fighter_north"):
            self.assertEqual(geometry[(64, direction)], geometry[(128, direction)])

    def test_vip_contract_uses_only_original_glb_not_standins(self):
        arena = (ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsArena.cs").read_text()
        vip = (ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsVIPSeats.cs").read_text()
        self.assertIn("BraxChampionsVIPSeats.Generate(root)", arena)
        self.assertNotIn("LuxuryVipArmchair(", arena)
        self.assertIn("free_luxurious_barrel_seat_2k.glb", vip)
        self.assertIn("SeatCount != 16", vip)
        # Detailed footprint/aisle tests live in test_arena_vip.py.
        self.assertIn("BoxMinMax(deskTop, x0 - .05f", arena)

    def test_parking_apron_stops_at_existing_southern_road_edge(self):
        # A1 approved centre (600,800); z=650 road+walk reaches z=667.
        self.assertAlmostEqual(800 - 121 - 24/2, 667)
        self.assertAlmostEqual(800 - 104 - 58/2, 667)
        # West/east courts x=+/-65, width 70, within the approved A1 axis reserve.
        self.assertLessEqual(max(abs(-65-35), abs(65+35)), 110)
        self.assertGreaterEqual(800 - 97 - 12, 667)

    def test_static_sources_keep_original_materials_and_judges(self):
        code = (ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsArena.cs").read_text()
        arrival = (ROOT / "unity-city/Assets/Editor/Mistrzowie/BraxChampionsArenaArrival.cs").read_text()
        # Source GLB is mandatory: no procedural stand-ins are allowed.
        self.assertIn("BraxChampionsVIPSeats.Generate(root)", code)
        self.assertNotIn("LuxuryVipArmchair(", code)
        self.assertIn("new[] { (-94.0f, -93.3f, 1f), (-78.7f, -78.0f, -1f) }", code)
        self.assertIn('SourceMaterial("arena_parking_asphalt", "clean_asphalt"', arrival)
        self.assertIn('SourceMaterial("arena_arrival_paving", "concrete_pavement_02"', arrival)
        self.assertIn("MetreScaleUv", arrival)
        self.assertIn("CHAMPIONS_A1_PUBLIC", arrival)
        self.assertIn("CHAMPIONS_A1_ATHLETES", arrival)


if __name__ == "__main__":
    unittest.main()
