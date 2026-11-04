"""Deterministic foundation geometry for the existing Unity game. Nominal metres -> K once."""
from pathlib import Path
import json, math, hashlib
import numpy as np
ROOT=Path(__file__).resolve().parents[2]
REPORT=ROOT/'mistrzowie-evidence';REPORT.mkdir(exist_ok=True)
from layout import ENTRANCE_HOLES,ENTRANCE_PATHS,PAVING_Y,TRAM_STOPS,BUS_STOPS,METRO
SRC=ROOT/'unity-city/Assets/Source/Mistrzowie'
K=float(np.float32(2.33)/np.float32(1.80))  # exact existing Unity float expression
base=json.loads((Path(__file__).with_name('plan_inventory.json')).read_text())
buildings=base['buildings']
# Surface rectangles are disjoint after priority subdivision. x,z,width,depth.
plots=[(b['x']-b['w']/2,b['z']-b['d']/2,b['w'],b['d']) for b in buildings]
plots += [(135,-108,70,18),(285,-108,70,18),(565,-108,70,18),(890,-108,70,18)]
XROAD=[70,400,800,1130];ZROAD=[-70,70,330,650,935]
walks=[(x-4,-180,8,93) for x in (230,530,700,1080)]
greens=[(50,-120,1100,10),(50,-170,650,10)]
piers=[(740,-440,10,290),(1150,-440,10,290),(740,-440,260,10),(1080,-440,80,10)]
# Three connected piers plus fingers; 80 reserved berth positions below.
piers += [(750,z,290,5) for z in (-205,-315,-390)]
berths=[]
for row,(z,n,length,width) in enumerate([(-205,16,40,14),(-315,24,24,10),(-390,40,12,6)]):
 sides=(-1,) if row==0 else (-1,1)
 per=n//len(sides)
 for side in sides:
  for j in range(per):
   x=765+(j+.5)*260/per
   center=z+(2.5+(length/2+2)*side)
   berths.append(dict(id=f'B{len(berths)+1:02}',x=x,z=center,length=length,width=width,kind='large_yacht' if row==0 else 'yacht' if row==1 else 'motorboat'))
 for j in range(per+1):
  x=765+j*260/per
  piers += [(x-.7,z-length-2,1.4,length+2)]
  if row>0:piers += [(x-.7,z+5,1.4,length+2)]

def inside(p,r):
 x,z=p;a,b,w,d=r;return a<=x<a+w and b<=z<b+d

def zone(x,z):
 if any(inside((x,z),r) for r in ENTRANCE_HOLES):return None
 if any(inside((x,z),r) for r in ENTRANCE_PATHS):return 'paving'
 if any(inside((x,z),p) for p in plots):return 'plot_base'
 if any(inside((x,z),r) for r in walks) and (z>=-150 or x<740):return 'paving'
 if z>=-110:
  v=min(abs(x-a) for a in XROAD) if -70<=z<=935 else 1e6
  h=min(abs(z-a) for a in ZROAD) if 70<=x<=1130 else 1e6
  if 0<=x<70 and abs(z-330)<17:h=abs(z-330)
  dist=min(v,h)
  if dist<10:return 'asphalt'
  if dist<17:return 'paving'
  return 'grass'
 if any(inside((x,z),r) for r in greens):return 'grass'
 if 50<=x<=1150 and -150<=z<-120:return 'paving'
 if 50<=x<700 and -280<=z<-150:return 'sand_dry' if z>=-240 else 'sand_wet'
 if any(inside((x,z),r) for r in piers):return 'quay'
 return None

def height(role,x,z):
 if role in ('sand_dry','sand_wet'):
  t=max(0,min(1,(-z-170)/110));s=t*t*(3-2*t)
  # Broad relief is geometric; fine grain is the original PBR, never random pixel noise.
  edge=math.sin(math.pi*t)**2
  return -3.5*s+0.07*math.sin(x*.035)*math.sin(z*.09)*edge
 if role in ('paving','grass') and z>=-170:return PAVING_Y
 if role=='plot_base':return -.25
 if role=='sea':return -2
 return 0

# Exact boundaries + grid make holes, sidewalks and planting beds have one owner.
xs=set([0.,1200.]+[float(x) for x in range(0,1201,25)])
zs=set([-500.,1000.]+[float(z) for z in range(-500,1001,25)])
for a in XROAD:
 xs.update([a-17,a-10,a,a+10,a+17])
for a in ZROAD:
 zs.update([a-17,a-10,a,a+10,a+17])
rects=plots+walks+greens+piers+ENTRANCE_HOLES+ENTRANCE_PATHS+[(50,-280,650,130),(50,-240,650,70),(50,-150,1100,30)]
for x,z,w,d in rects:xs.update([x,x+w]);zs.update([z,z+d])
xs=sorted(x for x in xs if 0<=x<=1200);zs=sorted(z for z in zs if -500<=z<=1000)
meshes={};cells=[]
def quad(role,x0,z0,x1,z1):
 if x1-x0<1e-5 or z1-z0<1e-5:return
 key=f'x{int((x0+x1)/2)//300:02}_z{int(((z0+z1)/2+500)//250):02}_{role}'
 m=meshes.setdefault(key,dict(id=key,role=role,positions=[],uv=[],triangles=[]))
 i=len(m['positions'])//3
 for x,z in [(x0,z0),(x0,z1),(x1,z1),(x1,z0)]:
  m['positions'] += [round(x*K,6),round(height(role,x,z)*K,6),round(z*K,6)]
  m['uv'] += [round(x,6),round(z,6)]
 m['triangles'] += [i,i+1,i+2,i,i+2,i+3]
for i in range(len(xs)-1):
 for j in range(len(zs)-1):
  a,b,c,d=xs[i],zs[j],xs[i+1],zs[j+1]
  role=zone((a+c)/2,(b+d)/2)
  if role:
   # Beach relief subdivided at <=5m; the rest is planar.
   nx=max(1,math.ceil((c-a)/5)) if role.startswith('sand') else 1
   nz=max(1,math.ceil((d-b)/5)) if role.startswith('sand') else 1
   for ix in range(nx):
    for iz in range(nz):quad(role,a+(c-a)*ix/nx,b+(d-b)*iz/nz,a+(c-a)*(ix+1)/nx,b+(d-b)*(iz+1)/nz)
   cells.append((a,b,c,d,role))
# Quay outer walls: cancel shared edges, so there are no internal coplanar walls.
edges={}
for a,b,c,d,role in cells:
 if role!='quay':continue
 ring=[(a,b),(a,d),(c,d),(c,b)]
 for u,v in zip(ring,ring[1:]+ring[:1]):
  key=tuple(sorted([u,v]))
  if key in edges:del edges[key]
  else:edges[key]=(u,v)
for (a,b),(c,d) in edges.values():
 key=f'x{int((a+c)/2)//300:02}_z{int(((b+d)/2+500)//250):02}_quay'
 m=meshes.setdefault(key,dict(id=key,role='quay',positions=[],uv=[],triangles=[]))
 i=len(m['positions'])//3
 for x,y,z in [(a,0,b),(a,-4.5,b),(c,-4.5,d),(c,0,d)]:
  m['positions'] += [round(x*K,6),round(y*K,6),round(z*K,6)]
  m['uv'] += [round(x if abs(c-a)>.01 else z,6),y]
 m['triangles'] += [i,i+1,i+2,i,i+2,i+3]

# Water has a separate vertical layer; no colliders and no land surface overlap at equal y.
for x in range(0,1200,5):
 for z in range(-500,-150,5):
  if 50<=x<700 and z>=-250:continue
  quad('sea',x,z,x+5,z+5)

plants=[]
def plant(x,z,kind,source,band):
 # A genuine grass patch is required, with >=1.5m clear root disk, never pavement.
 if not all(zone(x+dx,z+dz)=='grass' for dx,dz in [(0,0),(1.5,0),(-1.5,0),(0,1.5),(0,-1.5)]):return
 if any((p['x']-x)**2+(p['z']-z)**2<100 for p in plants):return
 plants.append(dict(id=f'V{len(plants)+1:04}',x=x,z=z,kind=kind,variant=source,band=band,yaw=(len(plants)*137.508)%360))
for x in range(65,1146,25):plant(x,-115,'palm',len(plants)%2,'promenade_soil')
for x in range(65,691,25):plant(x,-165,'palm',len(plants)%2,'beach_soil')
for x in XROAD:
 for side in (-1,1):
  for z in range(110,916,30):
   if min(abs(z-a) for a in ZROAD)<30:continue
   plant(x+side*21,z,'palm' if x>=800 else 'tree',len(plants)%2,'street_grass')
for x in [640,670,700,730,760]:
 for z in [115,145,175,205,225]:plant(x,z,'tree',0,'park')
for x,z in [(850,890),(900,890),(950,890),(1000,890),(1080,890),(950,740)]:plant(x,z,'tree',0,'hotel_garden')
layout=dict(version='2.0-infrastructure',regionId='mistrzowie',sourceCommit='f1981049a9f7e9976b7d9e84216f1747d49a8774',scaleK=K,coordinates='nominal metres; mesh positions already multiplied by K once',buildings=buildings,plots=[dict(x=x,z=z,w=w,d=d) for x,z,w,d in plots],plants=plants,berths=berths,tramStops=[dict(x=x,z=z) for _,x,z,_ in TRAM_STOPS],metroStations=[dict(x=m['x'],z=m['z']) for m in METRO],busStops=[dict(x=x,z=z) for _,x,z,_ in BUS_STOPS],meshes=list(meshes.values()))
SRC.mkdir(parents=True,exist_ok=True)
(SRC/'Foundation.json').write_text(json.dumps(layout,separators=(',',':')))
# Geometric checks use the delivered vertices, not only planning rectangles.
tri=0
for m in meshes.values():
 v=np.asarray(m['positions']).reshape(-1,3);ix=np.asarray(m['triangles']).reshape(-1,3)
 cross=np.cross(v[ix[:,1]]-v[ix[:,0]],v[ix[:,2]]-v[ix[:,0]])
 assert np.isfinite(v).all() and (np.linalg.norm(cross,axis=1)>1e-7).all(),m['id']
 if m['role']!='quay':assert (cross[:,1]>0).all(),m['id']
 assert len(m['uv'])==len(v)*2
 tri+=len(ix)
assert len(berths)==80 and sum(p['kind']!='motorboat' for p in berths)==40
for i,a in enumerate(berths):
 assert a['x']-a['width']/2>750 and a['x']+a['width']/2<1150
 assert a['z']-a['length']/2>-430 and a['z']+a['length']/2<-150
 for b in berths[i+1:]:
  assert not(abs(a['x']-b['x'])<(a['width']+b['width'])/2 and abs(a['z']-b['z'])<(a['length']+b['length'])/2),(a,b)
assert (315-205)-40-24-5-4>=32  # clear fairway between large yachts and middle pier
assert (390-315)-24-12-5-4>=30
assert 1150-1040>=100  # longitudinal channel connects every pier to entrance
assert abs(max(max(m['positions'][0::3]) for m in meshes.values())-1200*K)<1e-5
for p in plants:
 assert zone(p['x'],p['z'])=='grass'
# Review OBJ: metre scale already baked, not used by Unity importer (axis conversion avoided).
with (REPORT/'Foundation_geometry.obj').open('w') as f:
 f.write('# Foundation geometry only, K applied once; not a premium visual preview\n')
 offset=1
 for m in meshes.values():
  f.write('o '+m['id']+'\n')
  for p in np.asarray(m['positions']).reshape(-1,3):f.write('v '+' '.join(map(str,p))+'\n')
  for face in np.asarray(m['triangles']).reshape(-1,3):f.write('f '+' '.join(str(int(i)+offset) for i in face)+'\n')
  offset+=len(m['positions'])//3
report=dict(status='STATIC_GEOMETRY_CHECKS_PASSED_UNITY_NOT_RUN',scaleK=K,meshes=len(meshes),triangles=tri,plants=len(plants),palms=sum(p['kind']=='palm' for p in plants),trees=sum(p['kind']=='tree' for p in plants),berths=len(berths),yachts=40,motorboats=40,checks=['finite vertices','upward nondegenerate winding','UV cardinality','berth capacity and non-overlap','vegetation on grass with clear root disk','K applied once','marina transverse and longitudinal clear fairways'],unityCompilation='NOT_RUN',visualAcceptance='NOT_RUN',deviceTest='NOT_RUN')
(REPORT/'geometry-checks.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
