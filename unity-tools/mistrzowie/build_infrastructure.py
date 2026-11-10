"""Physical street and transit infrastructure; no vehicles or building placeholders.
Runs after legacy_foundation. All surfaces/rails/stairs are real meshes.
"""
from pathlib import Path
import json,math,hashlib
import numpy as np
from shapely.geometry import Polygon,LineString,Point,box
from shapely.ops import unary_union
from shapely import constrained_delaunay_triangles
from shapely.geometry.polygon import orient
from layout import *
from meshkit import Meshes,K,rounded_route,path_frames,ribbon
import legacy_foundation as ground

ROOT=Path(__file__).resolve().parents[2]
SRC=ROOT/'unity-city/Assets/Source/Mistrzowie'
REPORT=ROOT/'mistrzowie-evidence'
mesh=Meshes();labels=[];walk_checks=[];objects=[];metro_lights=[]

def mark(id,kind,at):objects.append(dict(id=id,kind=kind,x=at[0],y=at[1],z=at[2]))
# Owner 10.11 "globalnie zakaz tanich napisów": a label is no longer TextMesh text; BraxChampionsInfrastructure sets
# solid 3D letters (LetterKit.bytes) on the carrier face. at = 6 mm in front of that face; the letters read from -v
# (yaw 0 = from -z), flip=True from the other side.
def label(id,text,at,axis='x',size=.25,flip=False):labels.append(dict(id=id,text=text,x=at[0],y=at[1],z=at[2],yaw=(0 if axis=='x' else 90)+(180 if flip else 0),size=size))
def local(x,z,axis,u,y,v):return (x+u,y,z+v) if axis=='x' else (x+v,y,z+u)
def localbox(role,x,z,axis,u,y,v,w,h,d):mesh.box(role,local(x,z,axis,u,y,v),(w,h,d) if axis=='x' else (d,h,w))
def handrail(a,b):
 mesh.bar('steel',a,b,.034)
 for t in np.linspace(0,1,max(2,math.ceil(math.dist(a,b)/1.5))):
  p=np.array(a)+(np.array(b)-a)*t;mesh.bar('steel',p-[0,1.05,0],p,.025)

# Asphalt boundaries determine the curb, not an unrelated second road layout.
edges={}
for a,b,c,d,role in ground.cells:
 if role!='asphalt':continue
 ring=[(a,b),(a,d),(c,d),(c,b)]
 for u,v in zip(ring,ring[1:]+ring[:1]):
  key=tuple(sorted((u,v)))
  if key in edges:del edges[key]
  else:edges[key]=(u,v)

crossings=[]
for x in X_ROADS:
 for z in Z_ROADS:
  if z==-70 and x in [70,1130]:continue
  for along,axis in [(x-22,'x'),(x+22,'x'),(z-22,'z'),(z+22,'z')]:
   xx,zz=(along,z) if axis=='x' else (x,along)
   if not (70<=xx<=1130 and -70<=zz<=935):continue
   crossings.append((xx,zz,axis))
for _,x,z,axis in TRAM_STOPS:crossings.append((x+25 if axis=='x' else x,z+25 if axis=='z' else z,axis))
crossings.extend(EXTRA_CROSSINGS)

RAMP_HALF=2.5
# Apartment car-park drives (layout.APT_DRIVE_MOUTHS): no city kerb stone across the mouth - the drive's own 3 m kerb
# arcs meet the city kerb at both ends (BraxChampionsApartments.Parking).
def in_mouth(x,z):
 return any(a-1e-6<=x<=c+1e-6 and b-.4<=z<=d+.4 if abs(d-b)<1e-6 else b-1e-6<=z<=d+1e-6 and a-.4<=x<=c+.4 for _,a,b,c,d in APT_DRIVE_MOUTHS)
def dropped(x,z):
 # exactly the 5 m of the ramp (was 2.7 m: half-dropped stones next to the full-height walk left a hole)
 return any((abs(x-a)<RAMP_HALF and abs(abs(z-b)-10)<.35) if axis=='x' else (abs(z-b)<RAMP_HALF and abs(abs(x-a)-10)<.35) for a,b,axis in crossings)

curb_count=0
# Owner 10.11 13:04 (IMG_0866, "krawężniki migotają"): the kerb top lay in the plane of the sidewalk (PAVING_Y) over
# the same 18 cm strip. The kerb now OWNS that strip: its footprint is cut out of every ground surface below (see
# cuts at the end), so the stone top and the paving meet edge to edge, never one on the other. The stones are split
# exactly at both ends of every dropped-kerb ramp, so a low stone never stands beside the full-height walk.
curb_polys=[]
for a,b in edges.values():
 a=np.array(a);b=np.array(b);d=b-a;length=np.linalg.norm(d);n=np.array([-d[1],d[0]])/length;t=d/length
 along_x=abs(d[0])>abs(d[1])
 line=a+n*.09                                            # kerb centre line, 9 cm off the asphalt edge
 cuts_u=set(float(u) for u in np.arange(0,length,1))|{float(length)}
 for ca,cb,axis in crossings:
  if along_x and axis=='x' and abs(abs(line[1]-cb)-10)<.35:ends=[(ca+e-a[0])/t[0] for e in (-RAMP_HALF,RAMP_HALF)]
  elif not along_x and axis=='z' and abs(abs(line[0]-ca)-10)<.35:ends=[(cb+e-a[1])/t[1] for e in (-RAMP_HALF,RAMP_HALF)]
  else:continue
  cuts_u.update(float(u) for u in ends if 0<u<length)
 for _,ma,mb,mc,md in APT_DRIVE_MOUTHS:
  if along_x and abs(mb-md)<1e-6 and abs(line[1]-mb)<.35:ends=[(e-a[0])/t[0] for e in (ma,mc)]
  elif not along_x and abs(ma-mc)<1e-6 and abs(line[0]-ma)<.35:ends=[(e-a[1])/t[1] for e in (mb,md)]
  else:continue
  cuts_u.update(float(u) for u in ends if 0<u<length)
 cuts_u=sorted(cuts_u)
 for u0,u1 in zip(cuts_u,cuts_u[1:]):
  ll=u1-u0
  if ll<.02:continue
  p=line+t*(u0+ll/2)
  if in_mouth(*p):continue
  h=.02 if dropped(*p) else .16
  dims=(ll-.008,.18,.18) if along_x else (.18,.18,ll-.008)
  if min(dims)<=0:continue
  mesh.box('curb',(p[0],h-.09,p[1]),dims);curb_count+=1
  # footprint incl. the 8 mm joint, so no sliver of paving is left between two stones
  hw,hd=(ll/2,.09) if along_x else (.09,ll/2)
  curb_polys.append(box(p[0]-hw,p[1]-hd,p[0]+hw,p[1]+hd))

# Paint follows the same street grid. Junction centres stay clear.
def near_junction(x,z):return min(abs(x-a) for a in X_ROADS)<16 and min(abs(z-a) for a in Z_ROADS)<16
for axis,fixed,start,end in [('z',x,-70,935) for x in X_ROADS]+[('x',z,70,1130) for z in Z_ROADS]:
 for v in np.arange(start,end,9):
  x,z=(fixed,v+2) if axis=='z' else (v+2,fixed)
  if near_junction(x,z):continue
  for side in [-1,1]:
   # Two vehicle lanes per direction, outside the central transit reservation.
   localbox('paint',x,z,axis,0,.005,side*7,4,.009,.13)
  localbox('paint',x,z,axis,0,.005,-.12,4,.009,.1)
  localbox('paint',x,z,axis,0,.005,.12,4,.009,.1)
 for side in [-1,1]:
  for v in np.arange(start,end,5):
   x,z=(fixed,v+2.4) if axis=='z' else (v+2.4,fixed)
   if not near_junction(x,z):localbox('paint',x,z,axis,0,.005,side*9.75,4.8,.009,.10)

# Raised tactile strips and dropped curb locations. Ramps replace paving below.
ramp_polys=[];zebra=[]
for number,(x,z,axis) in enumerate(crossings):
 # zebra stripes are laid after the tram rails exist: a stripe never lies on a rail (they are cut at the grooves)
 for q in np.arange(-9.5,10,1):zebra.append((x,z,axis,q))
 for side in [-1,1]:
  # 3m ramp: 16cm sidewalk -> 2cm dropped kerb. No raised sheet over the old walk. It starts at the back of the
  # dropped kerb (10.18 m), flush with its 2 cm top - never over the stone (zero flicker).
  p=[local(x,z,axis,u,0,v) for u,v in [(-2.5,side*10.18),(2.5,side*10.18),(2.5,side*13),(-2.5,side*13)]]
  poly=Polygon([(a,c) for a,_,c in p]);ramp_polys.append(poly)
  y=[.02,.02,.16,.16]
  coords=[(p[i][0],y[i],p[i][2]) for i in range(4)]
  if side<0:coords=coords[::-1]
  # Surface normals are checked below.
  if np.cross(np.array(coords[1])-coords[0],np.array(coords[2])-coords[0])[1]<0:coords=coords[::-1]
  mesh.quad('paving',*coords)
  # paved cheeks at both ends of the ramp, up to the walk level (no open side under the walk)
  for end in [-1,1]:
   u=end*RAMP_HALF;c=[local(x,z,axis,u,y,v) for v,y in [(side*10.18,.02),(side*13,.02),(side*13,.16),(side*10.18,.16)]]
   inward=np.array(local(x,z,axis,-end,0,0))-np.array(local(x,z,axis,0,0,0))
   if np.dot(np.cross(np.array(c[1])-c[0],np.array(c[2])-c[0]),inward)<0:c=c[::-1]
   mesh.quad('paving',*c)
  for rib in np.linspace(-2.2,2.2,18):localbox('tactile',x,z,axis,rib,.167,side*13.5,.06,.014,.65)
 mark('CROSS_'+str(number+1),'crossing',(x,0,z))

# Rounded, closed tram loop; both tracks and all four physical rails are continuous.
tram=rounded_route(TRAM_CORNERS,25,1.8,True);tn=path_frames(tram)
rail_lines=[]
for track in [-TRAM_TRACK_OFFSET,TRAM_TRACK_OFFSET]:
 for side in [-1,1]:
  off=track+side*GAUGE/2;line=tram+tn*off;rail_lines.append(LineString(line))
  ribbon(mesh,'rail',tram,off,.065,.003,.065)
  ribbon(mesh,'rail_web',tram,off,.14,-.061,.025)
assert np.linalg.norm(tram[0]-tram[-1])<1e-8
assert np.linalg.norm(tn[0]-tn[-1])<1e-8
grooves=unary_union([l.buffer(.078,cap_style=2,join_style=2) for l in rail_lines])

# Owner 10.11 13:04-14:36 (zero flicker, "wszystkie przystanki ... krawężniki ... wszystko"): a zebra stripe crossing the
# tram tracks lay on the rail heads; the stripe is cut at every rail groove and keeps real 12 mm edges.
def paint_slab(poly,top=.013,thick=.012):mesh.slab('paint',poly,top,thick)      # top, bottom and real edges
zebra_cut=0
for x,z,axis,q in zebra:
 a0=local(x,z,axis,-2,0,q-.25);a1=local(x,z,axis,2,0,q+.25)
 stripe=box(min(a0[0],a1[0]),min(a0[2],a1[2]),max(a0[0],a1[0]),max(a0[2],a1[2]))
 if not stripe.intersects(grooves):
  localbox('paint',x,z,axis,0,.007,q,4,.012,.5);continue
 zebra_cut+=1
 rest=stripe.difference(grooves)
 for part in ([rest] if rest.geom_type=='Polygon' else [g for g in getattr(rest,'geoms',[]) if g.geom_type=='Polygon']):
  if part.area>1e-4:paint_slab(orient(part,sign=1))

def shelter(id,x,z,axis,side,tram_stop=False):
 base=.30 if tram_stop else .16;v=0 if tram_stop else side*14.6
 # Four steel posts, footing plates, transparent windscreen and shallow curved roof.
 for u in [-2.4,2.4]:
  for vv in [v-.65,v+.65]:
   a=local(x,z,axis,u,base,vv);b=local(x,z,axis,u,base+2.5,vv)
   mesh.bar('steel',a,b,.045,12);localbox('steel',x,z,axis,u,base+.035,vv,.22,.07,.22)
 roof=[]
 for u in np.linspace(-2.8,2.8,16):roof.append((u,base+2.55+.18*(1-(u/2.8)**2)))
 for (a,ya),(b,yb) in zip(roof,roof[1:]):
  mesh.quad('glass',local(x,z,axis,a,ya,v-.9),local(x,z,axis,a,ya,v+.9),local(x,z,axis,b,yb,v+.9),local(x,z,axis,b,yb,v-.9))
  for edge in [-.9,.9]:mesh.bar('steel',local(x,z,axis,a,ya,v+edge),local(x,z,axis,b,yb,v+edge),.035)
 for u in [-1.6,0,1.6]:localbox('glass',x,z,axis,u,base+1.3,v+side*.68,1.55,2.2,.018)
 localbox('bench',x,z,axis,0,base+.46,v+side*.25,2.8,.10,.46)
 for u in [-1.1,1.1]:localbox('steel',x,z,axis,u,base+.22,v+side*.25,.07,.44,.34)
 pole=local(x,z,axis,4,base,v);mesh.bar('steel',pole,np.array(pole)+[0,2.7,0],.045)
 localbox('sign',x,z,axis,4,base+2.42,v,.60,.72,.12)     # 12 cm: the 9 cm pole stays inside it (was 8 cm: flicker)
 # the stop name on a navy fascia under the front edge of the roof (was floating text under the roof)
 localbox('sign',x,z,axis,0,base+2.40,v-.86,5.0,.26,.05)
 label(id,('TRAMWAJ' if tram_stop else 'AUTOBUS')+' '+id,local(x,z,axis,0,base+2.40,v-.891),axis,.20)
 mark(id,'tram_stop' if tram_stop else 'bus_stop',(x,base,z))

for id,x,z,axis in TRAM_STOPS:
 localbox('paving',x,z,axis,0,.15,0,42,.30,3.2)
 for side in [-1,1]:
  localbox('tactile',x,z,axis,0,.307,side*1.26,39,.014,.35)
  # Tapered pedestrian access at both platform ends.
 for end in [-1,1]:
  coords=[local(x,z,axis,end*21,.30,-1.6),local(x,z,axis,end*25,.02,-1.6),local(x,z,axis,end*25,.02,1.6),local(x,z,axis,end*21,.30,1.6)]
  if np.cross(np.array(coords[1])-coords[0],np.array(coords[2])-coords[0])[1]<0:coords=coords[::-1]
  mesh.quad('paving',*coords)
 shelter(id,x,z,axis,1,True)
for id,x,z,axis in BUS_STOPS:
 for side in [-1,1]:
  shelter(id+('A' if side>0 else 'B'),x,z,axis,side)
  for u in [-16,16]:localbox('paint',x,z,axis,u,.007,side*8.5,.15,.012,2.8)
  localbox('paint',x,z,axis,0,.007,side*6.9,32,.012,.13)

# Same north/south venue names as the approved inventory and facade signs.
for id,name,x,z in [('WAY_A1','CHAMPION ARENA / PÓŁNOC',777,803),('WAY_A2','CHAMPION ARENA / POŁUDNIE',384,545)]:
 mesh.bar('steel',(x,.16,z),(x,3.4,z),.06,12)
 mesh.box('sign',(x,2.95,z),(4.8,.62,.10))
 label(id,name,(x,2.95,z-.056),'x',.22)
 label(id+'_B',name,(x,2.95,z+.056),'x',.22,flip=True)                  # both faces of the sign

# Reuse all five existing-city-style stations with original full-size escalators.
# The connected route is a single, non-self-crossing civil alignment, with
# curves outside platform footprints. No simulated service is advertised.
metro=rounded_route(METRO_LINE_WAYPOINTS,60,3.0,False)
station_boxes=[]
for m in METRO:
 pts=[station_point(m,x,z) for x in (-70,112) for z in (17,40)]
 station_boxes.append(box(min(p[0] for p in pts),min(p[1] for p in pts),max(p[0] for p in pts),max(p[1] for p in pts)))
 mark(m['id'],'reused_city_metro',(m['x'],METRO_PLATFORM_Y,m['z']))
 mark(m['id']+'_ENTRY','metro_entry',(m['entryX'],PAVING_Y,m['entryZ']))
remaining=LineString(metro).difference(unary_union(station_boxes))
connections=list(remaining.geoms) if remaining.geom_type=='MultiLineString' else [remaining]
for connection in connections:
 path=np.asarray(connection.coords);normals=path_frames(path)
 for side in [-1,1]:
  off=side*METRO_TRACK_OFFSET
  for rail_side in [-1,1]:ribbon(mesh,'metro_rail',path,off+rail_side*GAUGE/2,.08/K,METRO_RAIL_Y,.19/K)
  lo,hi=sorted([side*5.7/K,side*10.4/K]);bottom=METRO_RAIL_Y-.35/K;top=METRO_RAIL_Y+5.1/K
  ribbon(mesh,'metro_concrete',path,(lo+hi)/2,hi-lo,bottom,.2/K)
  ribbon(mesh,'metro_concrete',path,lo-.15/K,.3/K,top,top-bottom+.2/K)
  ribbon(mesh,'metro_concrete',path,hi+.15/K,.3/K,top,top-bottom+.2/K)
  ribbon(mesh,'metro_concrete',path,(lo+hi)/2,hi-lo+.6/K,top+.3/K,.3/K)
  # Original city's 0.6*K sleeper spacing, full 3D concrete sleepers on curves.
  distances=np.r_[0,np.cumsum(np.linalg.norm(np.diff(path,axis=0),axis=1))]
  for distance in np.arange(.3/K,distances[-1],.6):
   i=min(len(path)-2,max(0,np.searchsorted(distances,distance,side='right')-1))
   t=(distance-distances[i])/(distances[i+1]-distances[i]);n=normals[i]*(1-t)+normals[i+1]*t;n/=np.linalg.norm(n)
   q=path[i]*(1-t)+path[i+1]*t+n*off;tangent=np.array([n[1],-n[0]])
   vertices=[(q+n*v+tangent*u) for u,v in [(-.13/K,-1),( .13/K,-1),(.13/K,1),(-.13/K,1)]]
   mesh.slab('metro_concrete',Polygon(vertices),METRO_RAIL_Y-.19/K,.16/K)
  # Outer emergency walkway, three cable trays and recessed maintenance lights.
  walkway=off+side*2.36/K
  ribbon(mesh,'metro_concrete',path,walkway,.72/K,METRO_RAIL_Y+.05/K,.4/K)
  for h in [1.0,1.4,1.8]:ribbon(mesh,'metro_steel',path,off+side*2.64/K,.22/K,METRO_RAIL_Y+h/K,.05/K)
  # 18m fixture pitch keeps the extended system affordable on Android;
  # every other fixture has a real vertex light, without shadows.
  for light_index,distance in enumerate(np.arange(4/K,distances[-1],18.0)):
   i=min(len(path)-2,max(0,np.searchsorted(distances,distance,side='right')-1))
   t=(distance-distances[i])/(distances[i+1]-distances[i]);n=normals[i]*(1-t)+normals[i+1]*t;n/=np.linalg.norm(n)
   q=path[i]*(1-t)+path[i+1]*t+n*(off+side*2.62/K);tangent=np.array([n[1],-n[0]])
   if light_index%2==0:
    lamp=q-n*side*.35/K
    metro_lights.append(dict(x=float(lamp[0]),y=METRO_RAIL_Y+3.05/K,z=float(lamp[1])))
   for role,width,height,depth,y in [('metro_steel',.66,.17,.14,3.20),('metro_light',.54,.08,.15,3.20)]:
    # The concrete wall is behind this fitting; its light face is proud of the housing.
    centre=q-n*side*(.10/K if role=='metro_light' else 0)
    a=centre-tangent*width/(2*K);b=centre+tangent*width/(2*K)
    mesh.bar(role,(a[0],METRO_RAIL_Y+y/K,a[1]),(b[0],METRO_RAIL_Y+y/K,b[1]),height/(2*K),6)
  for rail_side in [-1,1]:ribbon(mesh,'metro_rail',path,off+rail_side*GAUGE/2,.16/K,METRO_RAIL_Y-.16/K,.03/K)
  third=off+side*(GAUGE/2+.75/K)
  ribbon(mesh,'metro_third',path,third,.24/K,METRO_RAIL_Y+.1/K,.04/K)
# Terminal closures only. Never add a wall between ANY of the five stations.
for p,n in [(metro[0],path_frames(metro)[0]),(metro[-1],path_frames(metro)[-1])]:
 for side in [-1,1]:
  c=p+n*side*METRO_TRACK_OFFSET
  mesh.box('metro_concrete',(c[0],METRO_RAIL_Y+2.3/K,c[1]),(.3/K,5.6/K,4.8/K) if abs(n[0])<.5 else (4.8/K,5.6/K,.3/K))

# Replace the affected paving and asphalt faces: no buried old sidewalk under ramps or kerbs, and no solid asphalt
# obstructing recessed tram rail grooves. Faces untouched by a cut keep their exact vertices; cut faces are
# re-triangulated on the plane of the original face.
from shapely.strtree import STRtree
kerb_cut=curb_polys+ramp_polys
level_cut=STRtree(kerb_cut)
groove_parts=list(grooves.geoms) if hasattr(grooves,'geoms') else [grooves]
cuts={'asphalt':(STRtree(groove_parts),groove_parts)}
for role in ('paving','grass','prom_paving','cycle_lane','cycle_mark'):cuts[role]=(level_cut,kerb_cut)
cut_faces=0
for old in ground.meshes.values():
 role=old['role']
 if role not in cuts:continue
 tree,parts=cuts[role]
 pos=np.asarray(old['positions'],float).reshape(-1,3);uv=np.asarray(old['uv'],float).reshape(-1,2)
 v=pos/K;indices=np.asarray(old['triangles']).reshape(-1,3)
 old['positions']=[];old['uv']=[];old['triangles']=[]
 for face in indices:
  poly=Polygon(v[face][:,[0,2]])
  hits=[parts[i] for i in tree.query(poly) if parts[i].intersects(poly)]
  if not hits or poly.area<1e-10:
   i=len(old['positions'])//3
   for k in face:old['positions'] += list(pos[k]);old['uv'] += list(uv[k])
   old['triangles'] += [i,i+1,i+2];continue
  cut_faces+=1
  result=poly.difference(unary_union(hits))
  if result.is_empty:continue
  # plane of the original face: y = A x + B z + C
  P=v[face];M=np.c_[P[:,0],P[:,2],np.ones(3)]
  try:A,B,C=np.linalg.solve(M,P[:,1])
  except np.linalg.LinAlgError:A,B,C=0.,0.,float(P[:,1].mean())
  for tri in constrained_delaunay_triangles(result).geoms:
   if tri.area<1e-9:continue
   q=list(orient(tri,sign=1).exterior.coords)[:3][::-1];i=len(old['positions'])//3
   for x,z in q:old['positions'] += [round(x*K,6),round((A*x+B*z+C)*K,6),round(z*K,6)];old['uv'] += [x,z]
   old['triangles'] += [i,i+1,i+2]
ground.layout['meshes']=[m for m in ground.meshes.values() if m['triangles']]
ground.layout['entranceHoles']=[dict(x=x,z=z,w=w,d=d) for x,z,w,d in ENTRANCE_HOLES]
(SRC/'Foundation.json').write_text(json.dumps(ground.layout,separators=(',',':')))
data=dict(version='2.2-complete-metro-exteriors',scaleK=K,meshes=mesh.export(),labels=labels,objects=objects,walkChecks=walk_checks,metroLights=metro_lights,
 tramRoute=tram.tolist(),metroRoute=metro.tolist(),metroStations=METRO,tramStops=TRAM_STOPS,busStops=BUS_STOPS,
 counts=dict(metroStations=len(METRO),metroEntrances=len(ENTRANCE_HOLES),metroTracks=2,tramTracks=2,tramPlatforms=len(TRAM_STOPS),busPlatforms=len(BUS_STOPS)*2,crossings=len(crossings),curbstones=curb_count))
(SRC/'Infrastructure.json').write_text(json.dumps(data,ensure_ascii=False,separators=(',',':')))
transit={k:data[k] for k in ['version','scaleK','tramRoute','metroRoute','metroStations','tramStops','busStops','counts']}
transit['metroRouteOrder']=list(METRO_ROUTE_ORDER)
transit['trafficEnabled']=False
transit['trainOperationVerified']=False
transit['routeCoordinates']='nominal metres; apply scaleK once; station source geometry is already world units'
(SRC/'Transit.json').write_text(json.dumps(transit,ensure_ascii=False,separators=(',',':')))
triangles=0
for m in data['meshes']:
 v=np.array(m['positions']).reshape(-1,3);t=np.array(m['triangles']).reshape(-1,3)
 assert np.isfinite(v).all() and len(m['uv'])==len(v)*2
 area=np.linalg.norm(np.cross(v[t[:,1]]-v[t[:,0]],v[t[:,2]]-v[t[:,0]]),axis=1)
 assert np.min(area)>1e-9,m['id'];triangles+=len(t)
for m in METRO:
 assert abs(Point(m['hallX'],m['hallZ']).distance(LineString(metro)))<.01
assert all(LineString(tram).distance(Point(x,z))<.01 for _,x,z,_ in TRAM_STOPS)
assert LineString(metro).is_simple, "Metro track cannot cross itself at grade"
assert len(METRO)==5 and len({m['id'] for m in METRO})==5
report=dict(status='GEOMETRY_CHECKS_PASS_UNITY_PENDING',counts=data['counts'],infrastructureMeshes=len(data['meshes']),triangles=triangles,tramMetres=round(LineString(tram).length,2),metroMetres=round(LineString(metro).length,2),checks=['continuous closed tram route','connections match existing city station track centres and height','10 tram platforms on track centreline','5 station centres on one connected non-self-crossing metro line','5 terrain openings for actual stairs','paving removed under curb ramps','asphalt cut out for rail grooves','finite nondegenerate meshes and UVs','one shared game scale'],unity='NOT_RUN',visualQuality='PENDING_UNITY_REVIEW')
(REPORT/'infrastructure-checks.json').write_text(json.dumps(report,indent=2))
print(json.dumps(report))
