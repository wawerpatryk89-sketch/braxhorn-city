"""Regression checks for reuse boundaries; does not claim a Unity or visual pass."""
from pathlib import Path
import json,math
import numpy as np
from shapely.geometry import LineString,Point,Polygon,box
from layout import *
ROOT=Path(__file__).resolve().parents[2]
d=json.loads((ROOT/'unity-city/Assets/Source/Mistrzowie/Infrastructure.json').read_text())
f=json.loads((ROOT/'unity-city/Assets/Source/Mistrzowie/Foundation.json').read_text())
line=LineString(d['metroRoute']);checks=[]
assert len(METRO)==5 and len(ENTRANCE_HOLES)==5
assert math.isclose(METRO_TRACK_OFFSET*K,7.6,abs_tol=1e-6)
assert math.isclose(METRO_RAIL_Y*K,-11.2-1.1*K+.16*K-.15,abs_tol=1e-6)
checks.append('city station world dimensions retained; same rail height, centres and gauge')
for m in METRO:
 for sx in [-70,112]:
  p=station_point(m,sx,28.5)
  assert line.distance(Point(p))<1e-6,(m,p)
  # A complete straight source tunnel must not cross the route bend.
  p2=station_point(m,sx-1 if sx==-70 else sx+1,28.5)
  assert line.distance(Point(p2))<1e-6,(m,p2)
checks.append('all ten source metro station portal connectors lie on straight approaches, no closed station caps')
holes=[box(x,z,x+w,z+h) for x,z,w,h in ENTRANCE_HOLES]
for mesh in f['meshes']:
 if mesh['role']=='sea':continue
 v=np.array(mesh['positions']).reshape(-1,3)/K
 for t in np.array(mesh['triangles']).reshape(-1,3):
  a=v[t];p=Polygon(a[:,[0,2]])
  if p.area<1e-8:continue
  for hole in holes:
   assert not p.intersects(hole) or p.intersection(hole).area<1e-5,mesh['id']
checks.append('five actual openings through all surface geometry, not painted entrance markers')
# Reserved plots must not claim new metro access courts or underground station corridors.
conflicts=[]
for b in f['buildings']:
 p=box(b['x']-b['w']/2,b['z']-b['d']/2,b['x']+b['w']/2,b['z']+b['d']/2)
 if any(p.intersection(h).area>1e-5 for h in holes):conflicts.append(b['id'])
assert not conflicts,conflicts
checks.append('no reserved building footprint covers a metro entrance')
out=dict(status='STATIC_CONTRACT_PASS',checks=checks,unity='SEPARATE_CI_REQUIRED')
(ROOT/'mistrzowie-evidence/reuse-contract.json').write_text(json.dumps(out,indent=2))
print(json.dumps(out))
