"""Deterministic foundation geometry for the existing Unity game. Nominal metres -> K once."""
from pathlib import Path
import json, math, hashlib, os
import numpy as np
from shapely import constrained_delaunay_triangles
from shapely.geometry import box,Point,LineString,Polygon
from shapely.ops import unary_union
ROOT=Path(__file__).resolve().parents[2]
REPORT=ROOT/'mistrzowie-evidence';REPORT.mkdir(exist_ok=True)
from layout import ENTRANCE_HOLES,ENTRANCE_PATHS,BUILDING_ACCESS,A1_PARKING_ENTRANCES,PAVING_Y,TRAM_STOPS,BUS_STOPS,METRO
from port_layout import PIERS,BERTHS,SHORE_BACK,SEA_LEVEL,PROMENADE_OPENINGS,JETSKI_RAMP,shoreline,beach_height,validate as validate_port
from promenade_layout import PAVILIONS,PAVILION_PADS,PAVILION_FOOTPRINTS,CYCLE_Z0,CYCLE_Z1,CYCLE_WIDTH,X0 as PROM_X0,X1 as PROM_X1,PALM_BEDS,PALM_BED_RECTS,FURNITURE,validate as validate_promenade
SRC=ROOT/'unity-city/Assets/Source/Mistrzowie'
K=float(np.float32(2.33)/np.float32(1.80))  # exact existing Unity float expression
base=json.loads((Path(__file__).with_name('plan_inventory.json')).read_text())
buildings=base['buildings']
# Surface rectangles are disjoint after priority subdivision. x,z,width,depth.
plots=[(b['x']-b['w']/2,b['z']-b['d']/2,b['w'],b['d']) for b in buildings]
plots += [(135,-108,70,18),(285,-108,70,18),(565,-108,70,18),(890,-108,70,18)]
# T11 casino underground garage: a true hole in the existing terrain's
# plot_base collider, not a visual ramp hidden beneath an unbroken ground mesh.
# Actual subterranean ramp AND LEVEL left-turn chamber: local X39.15..49.30,
# Z=-22..29. The former 46m opening stopped at Z24 and left the next
# 5m of turning space covered by the foundation's solid plot_base collider.
# New 51m opening matches the casino prefab and does not reach the fire lane
# (the lane starts at local X49.5); the roof is a separate deliberate slab.
# World x=534.15..544.30, z=443..494, on the existing T11 plot (495,465).
T11_GARAGE_RAMP=(534.15,443.0,10.15,51.0)
# Left STREET approach: remove only the 7m sidewalk strip at the old
# X400 city road edge. The casino generator bridges this exact opening with
# a single graded drivable mesh (city road y=.16 -> casino apron y=.2485).
# Preserve the city road itself and all sidewalk outside z=417..428.
T11_STREET_ENTRANCE=(410.0,417.0,7.0,11.0)
# 9.11 owner: pool P1 removed completely - no excavations, the lot is ordinary ground.
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
 if inside((x,z),T11_STREET_ENTRANCE):return None
 if any(inside((x,z),r) for r in A1_PARKING_ENTRANCES):return None
 if any(inside((x,z),r) for r in ENTRANCE_HOLES):return None
 if any(inside((x,z),r) for r in ENTRANCE_PATHS+BUILDING_ACCESS):return 'paving'
 if any(inside((x,z),p) for p in plots):return 'plot_base'
 # Cut the 3.0m cycling corridor completely out of the existing paving.
 # The cycle lane + its white markings are separately triangulated
 # DISJOINTLY below; no surfaces are stacked, even by one millimetre.
 if PROM_X0<=x<PROM_X1 and CYCLE_Z0<=z<CYCLE_Z1:return None
 # Inland pavilion pads replace grass only in 12 footprint rectangles;
 # green palm beds and the entire pedestrian promenade stay intact.
 if any(inside((x,z),r) for r in PAVILION_FOOTPRINTS):return None
 if any(inside((x,z),r) for r in PAVILION_PADS):return 'prom_paving'
 # Owner 10.11: the city walks stop at the sand (grass border z -161) - no grey tongues into the beach or the sea;
 # the beach is reached over timber boardwalks (BraxChampionsBeachFurnishings).
 if any(inside((x,z),r) for r in walks) and (z>=-150 or (x<690 and z>=-161)):return 'paving'
 if z>=-110:
  v=min(abs(x-a) for a in XROAD) if -70<=z<=935 else 1e6
  h=min(abs(z-a) for a in ZROAD) if 70<=x<=1130 else 1e6
  if 0<=x<70 and abs(z-330)<17:h=abs(z-330)
  dist=min(v,h)
  if dist<10:return 'asphalt'
  if dist<17:return 'paving'
  return 'grass'
 if any(inside((x,z),r) for r in greens):return 'grass'
 # palm beds of the seaside row (owner 10.11 09:06 "palmy posadzić"): planted ground flush with the paving
 if any(inside((x,z),r) for r in PALM_BED_RECTS):return 'grass'
 # owner 10.11 "promenady ładne, ekskluzywne": the promenade gets its own premium stone (BraxChampionsFoundation prom_paving)
 if 50<=x<=1150 and -150<=z<-120:return 'prom_paving'
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
 if role=='quay_ramp_along':
  # jetski ramp along the promenade quay wall: promenade level at its west end down to the low pontoon
  return .16-1.53*max(0,min(1,(x-JETSKI_RAMP[0]-2)/(JETSKI_RAMP[1]-JETSKI_RAMP[0]-2)))
 if role=='quay_ramp_jetski':
  return .16-1.53*max(0,min(1,(-z-150)/20))
 if role in ('paving','prom_paving','grass','cycle_lane') and z>=-170:return PAVING_Y
 if role=='cycle_mark':return PAVING_Y
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
rects=plots+walks+greens+[p[:4] for p in piers]+PAVILION_PADS+PAVILION_FOOTPRINTS+[(PROM_X0,CYCLE_Z0,PROM_X1-PROM_X0,CYCLE_WIDTH)]+[T11_GARAGE_RAMP,T11_STREET_ENTRANCE]+A1_PARKING_ENTRANCES+ENTRANCE_HOLES+ENTRANCE_PATHS+BUILDING_ACCESS+[(50,-150,1100,30)]+PALM_BED_RECTS
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
# Lane symbols and white boundary lines are PHYSICALLY INLAID.
# Original paving was removed by the same zone() cutout above. The
# red cycle asphalt is a polygon MINUS all the white marking polygons:
# the flat surfaces meet edge-to-edge in exactly the same y plane.
# This prevents z-fighting and duplicate colliders at every viewport.
lane_full=box(PROM_X0,CYCLE_Z0,PROM_X1,CYCLE_Z1)
white=[]
white.append(box(PROM_X0,CYCLE_Z0+.07,PROM_X1,CYCLE_Z0+.12))
white.append(box(PROM_X0,CYCLE_Z1-.12,PROM_X1,CYCLE_Z1-.07))
# Two-way centre divider: short dashes. No fake raised paint on cobbles.
for xx in np.arange(PROM_X0+4,PROM_X1-3,12.):
 x0=float(xx)
 if any(abs(x0-c)<6 for c in (230,530,700,1080)):continue
 white.append(box(x0,-147.04,min(x0+2.8,PROM_X1),-146.96))
# Bike symbols as intersecting filled wheel-annuli and slender frame strokes.
# The union operation removes intersections within each symbol as well.
for x in range(105,1140,95):
 z=-147.
 for cx in (x-.48,x+.48):
  ring=Point(cx,z).buffer(.31,12).difference(Point(cx,z).buffer(.25,12))
  white.append(ring)
 for ax,az,bx,bz in [(x-.48,z,x-.06,z+.35),
                     (x-.06,z+.35,x+.48,z),
                     (x+.48,z,x-.48,z),
                     (x-.06,z+.35,x+.17,z+.35)]:
  white.append(LineString([(ax,az),(bx,bz)]).buffer(.027,cap_style=2,join_style=2))
# Exactly four marked zebra crossings at existing perpendicular approaches.
for cross in (230,530,700,1080):
 for d in range(-5,6,2):
  white.append(box(cross+d,CYCLE_Z0+.35,cross+d+.75,CYCLE_Z1-.35))
# Trim a single union to the approved lane before triangulating.
mark_union=unary_union(white).intersection(lane_full)
assert mark_union.is_valid and mark_union.area>1.0
assert lane_full.contains(mark_union) or lane_full.covers(mark_union)

def inlay_polygon(role,geom):
 if geom.is_empty:return
 polygons=[geom] if geom.geom_type=='Polygon' else [
    p for p in geom.geoms if p.geom_type=='Polygon']
 for poly in polygons:
  for face in constrained_delaunay_triangles(poly).geoms:
   if face.area<=1e-8:continue
   corners=list(face.exterior.coords)[:3]
   # Shapely does not guarantee the same triangle winding for constrained
   # Delaunay output. Force upward-facing Unity mesh normals deterministically.
   dx1=corners[1][0]-corners[0][0];dz1=corners[1][1]-corners[0][1]
   dx2=corners[2][0]-corners[0][0];dz2=corners[2][1]-corners[0][1]
   cross_y=dz1*dx2-dx1*dz2
   if abs(cross_y)<1e-7:continue
   if cross_y<0:corners=[corners[0],corners[2],corners[1]]
   mid=face.centroid
   key=f'x{int(mid.x)//300:02}_z{int((mid.y+500)//250):02}_{role}'
   m=meshes.setdefault(key,dict(id=key,role=role,positions=[],uv=[],triangles=[]))
   start=len(m['positions'])//3
   for x,z in corners:
    m['positions'] += [round(x*K,6),round(PAVING_Y*K,6),round(z*K,6)]
    m['uv'] += [round(x,6),round(z,6)]
   m['triangles'] += [start,start+1,start+2]

# 50m chunks permit correct culling and a modest triangle budget on phones.
cycle_area=0.;mark_area=0.
for segment in range(int((PROM_X1-PROM_X0)/50)):
 x0=PROM_X0+segment*50.;x1=min(PROM_X1,x0+50.)
 clip=box(x0,CYCLE_Z0,x1,CYCLE_Z1)
 marked=mark_union.intersection(clip)
 asphalt=clip.difference(marked)
 if not marked.is_valid or not asphalt.is_valid or marked.intersection(asphalt).area>1e-8:
  raise RuntimeError("Red cycle asphalt and white inlays overlap")
 inlay_polygon('cycle_lane',asphalt)
 inlay_polygon('cycle_mark',marked)
 cycle_area+=asphalt.area;mark_area+=marked.area
assert abs(cycle_area+mark_area-3300.0)<.01
assert cycle_area>3000. and mark_area>1.


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
  # owner 10.11 09:01 "plaża ... kawałek jest chujowy, wygląda niby mokra": one golden sand to the water, only the
  # last ~7 m at the waterline is wet (same sand, darker in BraxChampionsFoundation), the foam ribbon over it
  role='sand_wet' if (i+.5)/32>.94 else 'sand_dry'
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
FLOATING=('quay_wood','quay_jetski','quay_ramp','quay_ramp_jetski','quay_ramp_along')
for (a,b),(c,d) in edges.values():
 # The cruise ship quay stays tall; floating pontoons meet water naturally.
 ra=zone(a+(c-a)*.02,b+(d-b)*.02)
 rb=zone(c-(c-a)*.02,d-(d-b)*.02)
 ya=height(ra,a,b) if ra and ra.startswith('quay_') else -1.08
 yb=height(rb,c,d) if rb and rb.startswith('quay_') else ya
 # owner 10.11: floating pontoons are dark floats down to just below the water, not concrete piers to the sea bed
 floating=(ra in FLOATING) or (rb in FLOATING)
 wrole='pontoon_side' if floating else 'quay_wall'
 bottom=SEA_LEVEL-.45 if floating else -4.5
 key=f'x{int((a+c)/2)//300:02}_z{int(((b+d)/2+500)//250):02}_{wrole}'
 m=meshes.setdefault(key,dict(id=key,role=wrole,positions=[],uv=[],triangles=[]))
 i=len(m['positions'])//3
 for x,y,z in [(a,ya,b),(a,bottom,b),(c,bottom,d),(c,yb,d)]:
  m['positions'] += [round(x*K,6),round(y*K,6),round(z*K,6)]
  m['uv'] += [round(x if abs(c-a)>.01 else z,6),y]
 m['triangles'] += [i,i+1,i+2,i,i+2,i+3]

# The promenade was a slab hovering over the harbour water with an open edge (owner photos 10.11): a real quay wall
# under its seaward edge from the end of the beach to the east end, facing the sea (Unity winding: east -> west).
for x in range(700,1150,25):
 x0=float(x+25);x1=float(x)
 key=f'x{int((x0+x1)/2)//300:02}_z{int((-150+500)//250):02}_quay_wall'
 m=meshes.setdefault(key,dict(id=key,role='quay_wall',positions=[],uv=[],triangles=[]))
 i=len(m['positions'])//3
 for xx,y in [(x0,PAVING_Y),(x0,-4.5),(x1,-4.5),(x1,PAVING_Y)]:
  m['positions'] += [round(xx*K,6),round(y*K,6),round(-150*K,6)]
  m['uv'] += [round(xx,6),y]
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
# Seaside palm row first (owner 10.11: thick palms along a walk): the owner's thick-trunk palma in every bed.
for x,z in PALM_BEDS:plant(x,z,'palm',0,'promenade_bed')
assert sum(p['band']=='promenade_bed' for p in plants)==len(PALM_BEDS),'every promenade bed gets its palm'
for x in range(65,1146,25):plant(x,-115,'palm',len(plants)%2,'promenade_soil')
# Owner 10.11 10:16: "wzdłuż plaży, jak się plaża kończy, mają być te grube palmy" - one row of the owner's
# THICK-trunk palma (variant 0) in the grass strip along the back edge of the sand, every 15 m (was 17 m, mixed);
# never on the city walks or the beach-club entry decks (plant() needs a genuine grass root disk).
for x in range(65,693,15):plant(x,-155.5,'palm',0,'beach_soil')
for x in XROAD:
 for side in (-1,1):
  for z in range(110,916,30):
   if min(abs(z-a) for a in ZROAD)<30:continue
   # owner 10.11 08:48: palms stay MIXED across the city ("nie wszędzie grube, ma być pomieszane"); the thick-trunk
   # palm lines only the A1 street front and its entrance walk (BraxChampionsArenaArrival.LawnPalms / palm avenue).
   plant(x+side*21,z,'palm' if x>=800 else 'tree',len(plants)%2,'street_grass')
for x in [640,670,700,730,760]:
 for z in [115,145,175,205,225]:plant(x,z,'tree',0,'park')
# Owner 10.11 08:40: "posadź drzewa jeszcze przy apartamentach, przy hotelach, przy kasynie". The owner's city palms
# straight into the lawn around the finished plots (apartments T04-T07, Hotel Sobczak T03, casino T11): a ring 6 m
# outside each plot edge, one every ~15 m, only on genuine grass (plant() checks the root disk and 10 m spacing,
# so roads, sidewalks, driveways, ramps and the existing street palms are left alone). Both palm models, mixed.
GARDEN_PLOTS=('T03','T04','T05','T06','T07','T11')
for b in buildings:
 if b['id'] not in GARDEN_PLOTS:continue
 x0,x1,z0,z1=b['x']-b['w']/2-6,b['x']+b['w']/2+6,b['z']-b['d']/2-6,b['z']+b['d']/2+6
 n=lambda a,c:max(1,round((c-a)/15))
 for i in range(n(x0,x1)+1):
  x=x0+(x1-x0)*i/n(x0,x1)
  plant(round(x,2),z0,'palm',len(plants)%2,'plot_garden');plant(round(x,2),z1,'palm',len(plants)%2,'plot_garden')
 for j in range(1,n(z0,z1)):
  z=z0+(z1-z0)*j/n(z0,z1)
  plant(x0,round(z,2),'palm',len(plants)%2,'plot_garden');plant(x1,round(z,2),'palm',len(plants)%2,'plot_garden')
for x,z in [(850,890),(900,890),(950,890),(1000,890),(1080,890),(950,740)]:plant(x,z,'tree',0,'hotel_garden')
layout=dict(version='2.3-compact-premium-marina-curved-golden-beach',regionId='mistrzowie',sourceCommit=os.environ.get('GITHUB_SHA','local-source-uncommitted'),scaleK=K,coordinates='nominal metres; mesh positions already multiplied by K once',buildings=buildings,plots=[dict(x=x,z=z,w=w,d=d) for x,z,w,d in plots],plants=plants,berths=berths,piers=PIERS,promenade=PAVILIONS,palmBeds=[dict(x=x,z=z,w=r[2],d=r[3]) for (x,z),r in zip(PALM_BEDS,PALM_BED_RECTS)],furniture=[dict(kind=k,x=x,z=z) for k,x,z in FURNITURE],openings=[dict(x0=a,x1=b) for a,b in PROMENADE_OPENINGS],tramStops=[dict(x=x,z=z) for _,x,z,_ in TRAM_STOPS],metroStations=[dict(x=m['x'],z=m['z']) for m in METRO],busStops=[dict(x=x,z=z) for _,x,z,_ in BUS_STOPS],meshes=list(meshes.values()))
SRC.mkdir(parents=True,exist_ok=True)
(SRC/'Foundation.json').write_text(json.dumps(layout,separators=(',',':')))
# Geometric checks use the delivered vertices, not only planning rectangles.
tri=0
for m in meshes.values():
 v=np.asarray(m['positions']).reshape(-1,3);ix=np.asarray(m['triangles']).reshape(-1,3)
 cross=np.cross(v[ix[:,1]]-v[ix[:,0]],v[ix[:,2]]-v[ix[:,0]])
 assert np.isfinite(v).all() and (np.linalg.norm(cross,axis=1)>1e-7).all(),m['id']
 if m['role'] not in ('quay_wall','pontoon_side'):assert (cross[:,1]>0).all(),m['id']
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
assert -241<shoreline(330)<-232  # middle beach ~72-78 m (owner 10.11 10:04: the 120 m desert is gone)
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
