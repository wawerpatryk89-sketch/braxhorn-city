"""Deterministic foundation geometry for the existing Unity game. Nominal metres -> K once."""
from pathlib import Path
import json, math, hashlib, os
import numpy as np
ROOT=Path(__file__).resolve().parents[2]
REPORT=ROOT/'mistrzowie-evidence';REPORT.mkdir(exist_ok=True)
from layout import ENTRANCE_HOLES,ENTRANCE_PATHS,BUILDING_ACCESS,PAVING_Y,TRAM_STOPS,BUS_STOPS,METRO
from port_layout import PIERS,BERTHS,SHORE_BACK,SEA_LEVEL,shoreline,beach_height,validate as validate_port
from promenade_layout import PAVILIONS,PAVILION_PADS,CYCLE_Z0,CYCLE_Z1,CYCLE_WIDTH,X0 as PROM_X0,X1 as PROM_X1,validate as validate_promenade
SRC=ROOT/'unity-city/Assets/Source/Mistrzowie'
K=float(np.float32(2.33)/np.float32(1.80))  # exact existing Unity float expression
base=json.loads((Path(__file__).with_name('plan_inventory.json')).read_text())
buildings=base['buildings']
# Surface rectangles are disjoint after priority subdivision. x,z,width,depth.
plots=[(b['x']-b['w']/2,b['z']-b['d']/2,b['w'],b['d']) for b in buildings]
plots += [(135,-108,70,18),(285,-108,70,18),(565,-108,70,18),(890,-108,70,18)]
# T11 casino underground garage: a true hole in the existing terrain's
# plot_base collider, not a visual ramp hidden beneath an unbroken ground mesh.
# Nominal local x=40.5..48.5, z=-22..24 for the (495,465) approved plot.
T11_GARAGE_RAMP=(535.5,443.0,8.0,46.0)
XROAD=[70,400,800,1130];ZROAD=[-70,70,330,650,935]
walks=[(x-4,-180,8,93) for x in (230,530,700,1080)]
# Preserve the original seafront promenade and its inland planting.
# Move only the seaward grass border to the back of the renewed crescent beach.
greens=[(50,-120,1100,10),(50,-161,650,11)]
# Reuse one deterministic harbour geometry contract for ground, berth markers,
# imported original boats, gangways and preflight. No more 80-slot old marina.
piers=[(p['x'],p['z'],p['w'],p['d'],p['role']) for p in PIERS]
berths=BERTHS
validate_port(PIERS,berths)
validate_promenade()

def inside(p,r):
 x,z=p;a,b,w,d=r;return a<=x<a+w and b<=z<b+d

def zone(x,z):
 if inside((x,z),T11_GARAGE_RAMP):return None
 if any(inside((x,z),r) for r in ENTRANCE_HOLES):return None
 if any(inside((x,z),r) for r in ENTRANCE_PATHS+BUILDING_ACCESS):return 'paving'
 if any(inside((x,z),p) for p in plots):return 'plot_base'
 # The existing continuous 1100m path is retained. Reclassify only a
 # precise 3.0m strip in the pavement, NO overlay causing z fighting.
 if PROM_X0<=x<PROM_X1 and CYCLE_Z0<=z<CYCLE_Z1:return 'cycle_lane'
 # Inland pavilion pads replace grass only in 12 footprint rectangles;
 # green palm beds and the entire pedestrian promenade stay intact.
 if any(inside((x,z),r) for r in PAVILION_PADS):return 'paving'
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
 # Premium beach is one hand-laid curved geometric grid, not a stepped
 # rectangle independently classified from the 25m city tile grid.
 if 50<=x<700 and z<SHORE_BACK:return None
 for a,b,w,d,role in piers:
  if inside((x,z),(a,b,w,d)):return role
 return None

def height(role,x,z):
 if role in ('sand_dry','sand_wet'):
  return beach_height(x,z)
 if role in ('quay_wood','quay_stone'):
  return -1.08
 if role=='quay_jetski':
  return -1.37
 if role in ('quay_cruise','quay_ferry'):
  return .16
 if role=='quay_ramp':
  return .16-1.24*max(0,min(1,(-z-150)/20))
 if role=='quay_ramp_jetski':
  return .16-1.53*max(0,min(1,(-z-150)/20))
 if role in ('paving','grass','cycle_lane') and z>=-170:return PAVING_Y
 if role=='cycle_mark':return PAVING_Y+.011
 if role=='plot_base':return -.25
 if role=='sea':return SEA_LEVEL
 return 0

# Exact boundaries + grid make holes, sidewalks and planting beds have one owner.
xs=set([0.,1200.]+[float(x) for x in range(0,1201,25)])
zs=set([-500.,1000.]+[float(z) for z in range(-500,1001,25)])
for a in XROAD:
 xs.update([a-17,a-10,a,a+10,a+17])
for a in ZROAD:
 zs.update([a-17,a-10,a,a+10,a+17])
rects=plots+walks+greens+[p[:4] for p in piers]+PAVILION_PADS+[(PROM_X0,CYCLE_Z0,PROM_X1-PROM_X0,CYCLE_WIDTH)]+[T11_GARAGE_RAMP]+ENTRANCE_HOLES+ENTRANCE_PATHS+BUILDING_ACCESS+[(50,-150,1100,30)]
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
def warped_quad(role,x0,z_lo0,z_hi0,x1,z_lo1,z_hi1,foam=False):
 # Real edge follows a smooth x-dependent coastline; two triangles per 2.5m.
 if x1-x0<1e-6 or min(z_hi0-z_lo0,z_hi1-z_lo1)<1e-6:return
 key=f'x{int((x0+x1)/2)//300:02}_z{int(((z_lo0+z_hi0+z_lo1+z_hi1)/4+500)//250):02}_{role}'
 m=meshes.setdefault(key,dict(id=key,role=role,positions=[],uv=[],triangles=[]))
 i=len(m['positions'])//3
 corners=[(x0,z_lo0),(x0,z_hi0),(x1,z_hi1),(x1,z_lo1)]
 for x,z in corners:
  yy=(max(SEA_LEVEL,beach_height(x,z))+.035) if foam else height(role,x,z)
  m['positions'] += [round(x*K,6),round(yy*K,6),round(z*K,6)]
  if foam:
   m['uv'] += [round(x*.0667,6),round((z-shoreline(x)+4)/9.0,6)]
  else:
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
# Flat white PBR lane markings, not text sprites or low-resolution bitmaps.
# Each is offset only 1.1 cm above the existing red PBR lane surface.
# Procedural geometry has NO low-resolution bitmap texture; its material is
# mapped 2K from the existing verified Poly Haven paving library.
for xx in np.arange(PROM_X0,PROM_X1,25.):
 x0=float(xx);x1=min(PROM_X1,x0+25.)
 quad('cycle_mark',x0,CYCLE_Z0+.07,x1,CYCLE_Z0+.12)
 quad('cycle_mark',x0,CYCLE_Z1-.12,x1,CYCLE_Z1-.07)
# Bi-directional divider (broken) and accessible pedestrian crossing points.
for xx in np.arange(PROM_X0+4,PROM_X1-3,12.):
 x0=float(xx)
 if any(abs(x0-c)<6 for c in (230,530,700,1080)):continue
 quad('cycle_mark',x0,-147.04,min(x0+2.8,PROM_X1),-146.96)

def mark_segment(role,ax,az,bx,bz,width):
 dx=bx-ax;dz=bz-az;length=math.hypot(dx,dz)
 if length<.001:return
 nx,nz=-dz/length*width*.5,dx/length*width*.5
 vertices=[(ax+nx,az+nz),(bx+nx,bz+nz),
           (bx-nx,bz-nz),(ax-nx,az-nz)]
 midx=(ax+bx)*.5;midz=(az+bz)*.5
 key=f'x{int(midx)//300:02}_z{int((midz+500)//250):02}_{role}'
 m=meshes.setdefault(key,dict(id=key,role=role,positions=[],uv=[],triangles=[]))
 i=len(m['positions'])//3
 for x,z in vertices:
  m['positions'] += [round(x*K,6),round(height(role,x,z)*K,6),round(z*K,6)]
  m['uv'] += [round(x,6),round(z,6)]
 m['triangles'] += [i,i+1,i+2,i,i+2,i+3]

# Readable bicycles every 95m, with two wheel rings and a frame — exact
# geometry placed ON the bike lane, preserving 3.0m width and service access.
for x in range(105,1140,95):
 z=-147.0
 for cx in (x-.48,x+.48):
  for i in range(16):
   a=2*math.pi*i/16;b=2*math.pi*(i+1)/16
   mark_segment('cycle_mark',cx+.31*math.cos(a),z+.31*math.sin(a),
                cx+.31*math.cos(b),z+.31*math.sin(b),.055)
 for xa,za,xb,zb in [(x-.48,z,x-.06,z+.35),
                     (x-.06,z+.35,x+.48,z),
                     (x+.48,z,x-.48,z),
                     (x-.06,z+.35,x+.17,z+.35)]:
  mark_segment('cycle_mark',xa,za,xb,zb,.055)
# White zebra crossings across the cycling lane at public accesses.
for cross in (230,530,700,1080):
 for d in range(-5,6,2):
  quad('cycle_mark',cross+d,CYCLE_Z0+.35,cross+d+.75,CYCLE_Z1-.35)


# CURVED golden sand: a continuous 2.5m grid across the whole 650m
# recreational coast. The original promenade/plan remain unchanged.
for xx in np.arange(50,700,2.5):
 x0=float(xx);x1=min(700.,x0+2.5)
 s0=shoreline(x0);s1=shoreline(x1)
 for i in range(32):
  t0=i/32;t1=(i+1)/32
  z00=SHORE_BACK+(s0-SHORE_BACK)*t0
  z10=SHORE_BACK+(s1-SHORE_BACK)*t0
  z01=SHORE_BACK+(s0-SHORE_BACK)*t1
  z11=SHORE_BACK+(s1-SHORE_BACK)*t1
  if min(z00-z01,z10-z11)<.00001:continue
  role='sand_wet' if (i+.5)/32>.775 else 'sand_dry'
  warped_quad(role,x0,z01,z00,x1,z11,z10)
  # Record walkable sand faces for analytics but keep the base grid isolated.
# Shore foam is a separate uncollidable ribbon. No particles/flat bitmap.
for xx in np.arange(50,700,2.5):
 x0=float(xx);x1=min(700.,x0+2.5)
 s0=shoreline(x0);s1=shoreline(x1)
 for j in range(5):
  off0=-4+9*j/5;off1=-4+9*(j+1)/5
  warped_quad('foam',x0,s0+off0,s0+off1,x1,s1+off0,s1+off1,True)

# Quay outer walls: cancel shared edges, so there are no internal coplanar walls.
edges={}
for a,b,c,d,role in cells:
 if not role.startswith('quay_'):continue
 ring=[(a,b),(a,d),(c,d),(c,b)]
 for u,v in zip(ring,ring[1:]+ring[:1]):
  key=tuple(sorted([u,v]))
  if key in edges:del edges[key]
  else:edges[key]=(u,v)
for (a,b),(c,d) in edges.values():
 key=f'x{int((a+c)/2)//300:02}_z{int(((b+d)/2+500)//250):02}_quay_wall'
 m=meshes.setdefault(key,dict(id=key,role='quay_wall',positions=[],uv=[],triangles=[]))
 i=len(m['positions'])//3
 # The cruise ship quay stays tall; floating pontoons meet water naturally.
 ra=zone(a+(c-a)*.02,b+(d-b)*.02)
 rb=zone(c-(c-a)*.02,d-(d-b)*.02)
 ya=height(ra,a,b) if ra and ra.startswith('quay_') else -1.08
 yb=height(rb,c,d) if rb and rb.startswith('quay_') else ya
 for x,y,z in [(a,ya,b),(a,-4.5,b),(c,-4.5,d),(c,yb,d)]:
  m['positions'] += [round(x*K,6),round(y*K,6),round(z*K,6)]
  m['uv'] += [round(x if abs(c-a)>.01 else z,6),y]
 m['triangles'] += [i,i+1,i+2,i,i+2,i+3]

# Water physically follows the curved beach, instead of ending on a
# straight 650m x-line. Full 5m mesh is kept away from the beach; the fine
# near-shore strip follows the EXACT sand shoreline at both x endpoints.
for x in range(0,1200,5):
 x0=float(x);x1=x0+5
 if 50<=x<700:
  for z in range(-500,-300,5):quad('sea',x0,z,x1,z+5)
  s0=shoreline(x0);s1=shoreline(x1)
  for j in range(12):
   t0=j/12;t1=(j+1)/12
   a0=-300+(s0+300)*t0;a1=-300+(s1+300)*t0
   b0=-300+(s0+300)*t1;b1=-300+(s1+300)*t1
   warped_quad('sea',x0,a0,b0,x1,a1,b1)
 else:
  for z in range(-500,-150,5):quad('sea',x0,z,x1,z+5)

plants=[]
def plant(x,z,kind,source,band):
 # A genuine grass patch is required, with >=1.5m clear root disk, never pavement.
 if not all(zone(x+dx,z+dz)=='grass' for dx,dz in [(0,0),(1.5,0),(-1.5,0),(0,1.5),(0,-1.5)]):return
 if any((p['x']-x)**2+(p['z']-z)**2<100 for p in plants):return
 plants.append(dict(id=f'V{len(plants)+1:04}',x=x,z=z,kind=kind,variant=source,band=band,yaw=(len(plants)*137.508)%360))
for x in range(65,1146,25):plant(x,-115,'palm',len(plants)%2,'promenade_soil')
# More real imported palms facing the bright beach, planted in the coastal
# grass bed, never obstructing the preserved pedestrian promenade.
for x in range(65,693,17):plant(x,-155.5,'palm',len(plants)%2,'beach_soil')
for x in XROAD:
 for side in (-1,1):
  for z in range(110,916,30):
   if min(abs(z-a) for a in ZROAD)<30:continue
   plant(x+side*21,z,'palm' if x>=800 else 'tree',len(plants)%2,'street_grass')
for x in [640,670,700,730,760]:
 for z in [115,145,175,205,225]:plant(x,z,'tree',0,'park')
for x,z in [(850,890),(900,890),(950,890),(1000,890),(1080,890),(950,740)]:plant(x,z,'tree',0,'hotel_garden')
layout=dict(version='2.3-compact-premium-marina-curved-golden-beach',regionId='mistrzowie',sourceCommit=os.environ.get('GITHUB_SHA','local-source-uncommitted'),scaleK=K,coordinates='nominal metres; mesh positions already multiplied by K once',buildings=buildings,plots=[dict(x=x,z=z,w=w,d=d) for x,z,w,d in plots],plants=plants,berths=berths,piers=PIERS,promenade=PAVILIONS,tramStops=[dict(x=x,z=z) for _,x,z,_ in TRAM_STOPS],metroStations=[dict(x=m['x'],z=m['z']) for m in METRO],busStops=[dict(x=x,z=z) for _,x,z,_ in BUS_STOPS],meshes=list(meshes.values()))
SRC.mkdir(parents=True,exist_ok=True)
(SRC/'Foundation.json').write_text(json.dumps(layout,separators=(',',':')))
# Geometric checks use the delivered vertices, not only planning rectangles.
tri=0
for m in meshes.values():
 v=np.asarray(m['positions']).reshape(-1,3);ix=np.asarray(m['triangles']).reshape(-1,3)
 cross=np.cross(v[ix[:,1]]-v[ix[:,0]],v[ix[:,2]]-v[ix[:,0]])
 assert np.isfinite(v).all() and (np.linalg.norm(cross,axis=1)>1e-7).all(),m['id']
 if m['role']!='quay_wall':assert (cross[:,1]>0).all(),m['id']
 assert len(m['uv'])==len(v)*2
 tri+=len(ix)
assert len(berths)==38
assert len(PAVILIONS)==12
assert CYCLE_WIDTH==3.0
assert sum(p['kind']=='yacht' for p in berths)==15
assert sum(p['kind']=='motorboat' for p in berths)==10
assert sum(p['kind']=='jetski' for p in berths)==10
assert sum(p['kind']=='cruise_ship' for p in berths)==2
assert sum(p['kind']=='ferry' for p in berths)==1
# The port-layout module validates non-overlapping original hulls, gangways,
# the U-shaped motorboat basin, clear ferry and cruise-ship slots.
validate_port(PIERS,berths)
assert -286<shoreline(330)<-276  # middle beach ~115-125m wide
assert abs(beach_height(330,SHORE_BACK)-.16)<1e-6
assert beach_height(330,shoreline(330))<SEA_LEVEL
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
report=dict(status='STATIC_GEOMETRY_CHECKS_PASSED_UNITY_NOT_RUN',scaleK=K,meshes=len(meshes),triangles=tri,plants=len(plants),palms=sum(p['kind']=='palm' for p in plants),trees=sum(p['kind']=='tree' for p in plants),berths=len(berths),yachts=15,motorboats=10,jetskis=10,cruiseBerths=2,ferryBerths=1,checks=['finite vertices','upward nondegenerate winding','UV cardinality','38 berths with individual dock access and no hull overlap','continuous beach-water boundary with curved shallow shore and foam','existing promenade walk retained with 3m colour-mapped cycle lane and twelve inset kiosks','source-derived beach palms','K applied once','open marine fairways'],unityCompilation='NOT_RUN',visualAcceptance='NOT_RUN',deviceTest='NOT_RUN')
(REPORT/'geometry-checks.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))
