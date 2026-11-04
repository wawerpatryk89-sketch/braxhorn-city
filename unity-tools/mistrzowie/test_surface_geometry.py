"""Physical contracts: outward faces and usable UVs, independent of Unity rendering."""
import unittest
import numpy as np
from meshkit import Meshes,K,ribbon

class PhysicalSurfaces(unittest.TestCase):
 def assert_outside(self,mesh,centre):
  for m in mesh.export():
   v=np.asarray(m['positions']).reshape(-1,3)/K;t=np.asarray(m['triangles']).reshape(-1,3)
   faces=v[t];n=np.cross(faces[:,1]-faces[:,0],faces[:,2]-faces[:,0])
   self.assertTrue(np.all(np.sum(n*(faces.mean(axis=1)-centre),axis=1)>1e-8),m['id'])
 def test_street_and_tunnel_ribbons_face_outward(self):
  m=Meshes();ribbon(m,'test',np.array([[10.,10.],[14.,10.]]),0,2,.3,.2)
  self.assert_outside(m,np.array([12.,.2,10.]))
 def test_rotated_rail_faces_outward(self):
  m=Meshes();ribbon(m,'test',np.array([[10.,10.],[10.,14.]]),0,2,.3,.2)
  self.assert_outside(m,np.array([10.,.2,12.]))
 def test_guardrail_has_visible_outer_wall(self):
  m=Meshes();m.bar('test',[10,1,10],[12,3,11],.1,12)
  self.assert_outside(m,np.array([11.,2.,10.5]))
 def test_box_has_visible_outer_faces(self):
  m=Meshes();m.box('test',[10,2,10],[4,2,3]);self.assert_outside(m,np.array([10.,2.,10.]))
 def test_vertical_stone_uvs_have_area(self):
  m=Meshes();m.quad('test',[0,0,0],[0,3,0],[4,3,0],[4,0,0])
  for mesh in m.export():
   uv=np.array(mesh['uv']).reshape(-1,2);idx=np.array(mesh['triangles']).reshape(-1,3);p=uv[idx]
   a,b=p[:,1]-p[:,0],p[:,2]-p[:,0]
   self.assertTrue(np.all(np.abs(a[:,0]*b[:,1]-a[:,1]*b[:,0])>1))
if __name__=='__main__':unittest.main()
