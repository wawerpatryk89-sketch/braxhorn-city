import unittest
import numpy as np
import audit_metro as audit


class MetroRegressions(unittest.TestCase):
    def test_run4_logged_point_hits_original_bin(self):
        # Convert the logged world point using M1's authored placement.
        point=np.array([567.45,-4.54,467.81])-np.array([400*audit.K-22.5,.16*audit.K-.15,359*audit.K-28.5])
        hits=[o[0] for o in audit.fixtures() if audit.capsule_overlaps(point,o)]
        self.assertIn('mezzanine bin',hits)

    def test_original_north_wall_route_also_hits_ticket_machines(self):
        path=[np.array([77.595,-4.6,31.6]),np.array([66,-4.6,31.6])]
        self.assertEqual(audit.furniture_hits(path),['mezzanine bin','ticket machine'])

    def test_original_platform_diagonal_hits_furniture(self):
        path=[np.array([42.9,-11.2,27.15]),np.array([20,-11.2,28.5])]
        self.assertIn('platform bench',audit.furniture_hits(path))
        self.assertIn('platform column',audit.furniture_hits(path))

    def test_current_route_clears_authored_furniture(self):
        self.assertEqual(audit.furniture_hits(audit.walking_route()),[])

    def test_capsule_detects_contact_but_not_a_clear_parallel_route(self):
        obstacle=audit.box('wall',[0,1,0],[1,2,1])
        self.assertTrue(audit.capsule_overlaps(np.array([.8,0,0]),obstacle))
        self.assertFalse(audit.capsule_overlaps(np.array([.9,0,0]),obstacle))

    def test_technical_enclosure_keeps_the_whole_first_flight_clear(self):
        result=audit.stair_clearance()
        self.assertGreater(result['oldIntrusionMetres'],1.8)
        self.assertGreaterEqual(result['newMinimumBelowRampMetres'],.399)

    def test_real_train_root_rotation_is_required_to_keep_carriage_upright(self):
        correct=audit.train_bounds();broken=audit.train_bounds(True)
        self.assertAlmostEqual(correct[2],broken[1],places=5)
        self.assertAlmostEqual(correct[1],broken[2],places=5)
        self.assertGreater(correct[2]/correct[1],4)
        self.assertGreater(broken[1]/broken[2],4)

    def test_complete_analytical_contract(self):
        self.assertFalse(audit.audit()['unityPhysicsTested'])


if __name__=='__main__':unittest.main()
