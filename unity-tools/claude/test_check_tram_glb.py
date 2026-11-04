import unittest
from check_tram_glb import door_candidates

class TramHierarchyTests(unittest.TestCase):
    def model(self, nodes):
        return {'nodes': nodes, 'meshes': [{'primitives': [{'attributes': {'POSITION': 0}}]}]}
    def test_named_parent_mesh_children_from_sketchfab(self):
        data = self.model([{'name':'left_door_left_a_10','children':[1]}, {'name':'Object_24','mesh':0},
                           {'name':'left_door_right_a_12','children':[3]}, {'name':'Object_30','mesh':0}])
        self.assertEqual([i for i,n in door_candidates(data)], [0,2])
    def test_direct_meshes_still_work(self):
        self.assertEqual(len(door_candidates(self.model([{'name':'door_left','mesh':0},{'name':'door_right','mesh':0}]))),2)
    def test_empty_named_nodes_fail(self):
        self.assertEqual(door_candidates(self.model([{'name':'door_left'},{'name':'door_right','mesh':5}])),[])
    def test_nested_names_not_counted_twice(self):
        self.assertEqual(door_candidates(self.model([{'name':'door_left','children':[1]},{'name':'door_left_mesh','mesh':0}])),[(0,'door_left')])
    def test_invalid_child_rejected(self):
        with self.assertRaises(ValueError):door_candidates(self.model([{'name':'door_left','children':[3]}]))
    def test_cycle_rejected(self):
        with self.assertRaises(ValueError):door_candidates(self.model([{'name':'door_left','children':[1]},{'children':[0]}]))

if __name__=='__main__':unittest.main()
