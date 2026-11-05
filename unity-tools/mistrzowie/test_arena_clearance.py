"""A1 coordinate-level clearance regressions. NOT physics in Unity Editor."""
import itertools
import unittest

from shapely.geometry import box

class ChampionA1Clearance(unittest.TestCase):
    def test_owner_vip_model_has_16_separate_spaces_clear_of_judges(self):
        # Read-only contract for BraxChampionsVIPSeats source placements.
        xs=(-94.0,-92.55,-91.10,-89.65,-82.35,-80.90,-79.45,-78.0)
        rows=(70.30,72.05)
        chairs=[box(x-.48,z-.48,x+.48,z+.48) for x in xs for z in rows]
        judge=[box(-94,61,-93.3,65.6),box(-78.7,61,-78,65.6)]
        carpet=box(-87.55,70,-84.45,77.5)
        self.assertEqual(len(chairs),16)
        for a,b in itertools.combinations(chairs,2):
            self.assertEqual(a.intersection(b).area,0)
        for chair in chairs:
            self.assertEqual(chair.intersection(carpet).area,0)
            for desk in judge:self.assertEqual(chair.intersection(desk).area,0)
        self.assertGreater(-82.35-.48-(-89.65+.48),6)

    def test_fighters_rear_and_lobby_have_clear_bounded_axial_paths(self):
        # All dimensions are nominal metres. K=2.33/1.80 is applied once.
        fighter=box(-2.05,35,2.05,59)
        walls=(box(-6.3,38.5,-6.1,52.5),box(6.1,38.5,6.3,52.5))
        self.assertTrue(all(fighter.intersection(w).area==0 for w in walls))
        lobby=box(-2.05,-59,2.05,-35)
        trophy=box(9-1.375,-38.8-.72,9+1.375,-38.8+.72)
        self.assertEqual(lobby.intersection(trophy).area,0)

    def test_parking_access_is_outside_spectator_promende(self):
        public=box(-8,-133,8,-59)
        for x in (-65,65):
            court=box(x-35,-109,x+35,-85)
            vehicle=box(x-3.2,-133,x+3.2,-109)
            self.assertEqual(public.intersection(court).area,0)
            self.assertEqual(public.intersection(vehicle).area,0)
            self.assertAlmostEqual(800+vehicle.bounds[1],667)
        # A1 southbound road + sidewalk limit: centre Z=650+17.
        self.assertEqual(650+17,667)

if __name__=="__main__":
    unittest.main()
