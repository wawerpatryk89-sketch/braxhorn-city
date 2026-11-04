"""A1 arena and T01 hotel: exterior construction, same approved plots and K.
Produces reviewable mesh source, NOT proof of Unity rendering or premium acceptance.
No interiors, hotel gameplay, moving vehicles or invented brand sculpture.
"""
from pathlib import Path
import json,math,gzip
import numpy as np
from shapely.geometry import Polygon,box,LineString
from meshkit import Meshes,K,ribbon

ROOT=Path(__file__).resolve().parents[2]
SRC=ROOT/'unity-city/Assets/Source/Mistrzowie'
INVENTORY=json.loads(Path(__file__).with_name('plan_inventory.json').read_text())['buildings']

def ellipse(rx,rz,n):
 return np.array([(rx*math.cos(a),rz*math.sin(a)) for a in np.linspace(0,2*math.pi,n+1)])

def wall(m,role,path,bottom,top):
 """CCW footprint: outside faces, cumulative metre UVs, no stretched wall projections."""
 along=0
 for a,b in zip(path,path[1:]):
  ll=np.linalg.norm(b-a)
  m.quad(role,(a[0],top,a[1]),(b[0],top,b[1]),(b[0],bottom,b[1]),(a[0],bottom,a[1]),
   [(along,top),(along+ll,top),(along+ll,bottom),(along,bottom)])
  along+=ll

def glazing(m,path,y0,y1,detail=True):
 wall(m,'facade_glass',path,y0,y1)
 wall(m,'spandrel',path,y0,y0+.55)
 for y in [y0,y1]:ribbon(m,'bronze',path,0,.13,y,.12)
 if detail:
  for a in path[:-1]:m.box('bronze',(a[0],(y0+y1)/2,a[1]),(.10,y1-y0,.10))

def rectangle_path(x,z,w,d,step=3):
 corners=[(x-w/2,z-d/2),(x+w/2,z-d/2),(x+w/2,z+d/2),(x-w/2,z+d/2)]
 result=[]
 for a,b in zip(corners,corners[1:]+corners[:1]):
  a=np.array(a);b=np.array(b)
  for t in np.linspace(0,1,max(1,math.ceil(np.linalg.norm(b-a)/step)),endpoint=False):result.append(a+(b-a)*t)
 return np.array(result+[result[0]])

def rail(m,path,y,detail):
 ribbon(m,'bronze',path,0,.065,y+1.12,.065)
 wall(m,'clear_glass',path,y+.12,y+1.08)
 if detail:
  line=LineString(path)
  for d in np.arange(0,line.length,1.8):
   p=line.interpolate(d);m.box('bronze',(p.x,y+.56,p.y),(.06,1.12,.06))

def door(m,x,z,yaw,detail,open_entry=False):
 # Closed exterior double doors at ground level; no false claim of a finished lobby.
 a=math.radians(yaw);right=np.array([math.cos(a),0,-math.sin(a)]);normal=np.array([math.sin(a),0,math.cos(a)])
 centre=np.array([x,.16,z]);a0=centre-right*1.7;a1=centre+right*1.7
 if not open_entry:m.quad('facade_glass',a0,a1,a1+[0,3.5,0],a0+[0,3.5,0])
 for u in ([-1.7,1.7] if open_entry else [-1.7,0,1.7]):m.bar('bronze',centre+right*u,centre+right*u+[0,3.5,0],.055,8)
 m.bar('bronze',a0+[0,3.5,0],a1+[0,3.5,0],.07,8)
 if detail and not open_entry:
  for u in [-.18,.18]:m.bar('bronze',centre+right*u+normal*.14+[0,.9,0],centre+right*u+normal*.14+[0,1.8,0],.028,8)

def services(m,x,z,y,w,d,detail):
 # A screen with actual louvres, capped plant room, roof drains and ventilation boxes.
 m.box('spandrel',(x,y+1.1,z),(w,2.2,d));m.box('roof',(x,y+2.25,z),(w+.2,.10,d+.2))
 if detail:
  for yy in np.arange(y+.2,y+2.2,.20):
   m.box('bronze',(x,yy,z-d/2-.06),(w,.055,.10))
   m.box('bronze',(x,yy,z+d/2+.06),(w,.055,.10))
  for xx in [x-w*.25,x+w*.25]:
   m.box('steel',(xx,y+2.45,z),(1.6,.3,1.6))
   for zz in np.linspace(z-.6,z+.6,6):m.box('spandrel',(xx,y+2.61,zz),(1.3,.04,.07))

def arena(detail):
 m=Meshes();n=128 if detail else 64;path=ellipse(96,59,n);foot=Polygon(path)
 # Plot apron ends flush with the shared sidewalk level; real slab thickness below it.
 m.slab('stone',box(-110,-75,110,75),.16,.41)
 m.slab('dark_stone',foot,.18,.6)
 # Actual central public opening. Keep source stairs/doors inside reachable from the city.
 for a,b in zip(path,path[1:]):
  if a[1]<-58 and b[1]<-58 and min(abs(a[0]),abs(b[0]))<1:
   glazing(m,np.array([a,b]),4.5,10.8,detail)
  else:glazing(m,np.array([a,b]),.18,10.8,detail)
 # Continuous pale stone belt carries the main signage, with bronze reveals.
 wall(m,'stone',path,10.8,18.2)
 for yy in [10.8,11.1,17.9,18.2]:ribbon(m,'bronze',path,0,.22,yy,.14)
 for yy in [18.2,25.4,32.6,39.8]:glazing(m,path,yy,yy+7.2,detail)
 # Tall fins, alternating depth, are part of the curved facade, not flat texture lines.
 if detail:
  for i,(x,z) in enumerate(path[:-1]):
   a=math.atan2(z/59,x/96);out=np.array([math.cos(a)/96,math.sin(a)/59]);out/=np.linalg.norm(out)
   p=np.array([x,z])+out*.35
   m.bar('bronze',(p[0],18.2,p[1]),(p[0],46.7,p[1]),.18 if i%4 else .27,6)
 m.slab('roof',foot.buffer(.55),47.2,.30)
 rim=np.asarray(foot.buffer(.65).exterior.coords)
 # Shapely buffer is clockwise; reverse to preserve outward face/rail convention.
 if not Polygon(rim).exterior.is_ccw:rim=rim[::-1]
 ribbon(m,'stone',rim,0,.45,48,.8)
 if detail:
  for x in np.arange(-88,89,4):
   segment=foot.intersection(LineString([(x,-70),(x,70)]))
   if segment.geom_type=='LineString':ribbon(m,'steel',np.array(segment.coords),0,.035,47.23,.025)
  for x,z in path[::16]:m.bar('bronze',(x*1.008,.25,z*1.008),(x*1.008,46.8,z*1.008),.055,8)
 for x in [-12,-6,0,6,12]:door(m,x,-math.sqrt(1-(x/96)**2)*59-.04,180,detail,open_entry=x==0)
 # Cantilevered arrival canopy, front columns outside the walking line.
 m.slab('clear_glass',box(-23,-68,23,-56),8.7,.10)
 for x in [-22,22]:
  m.bar('bronze',(x,.16,-66),(x,8.9,-66),.16,12)
  m.bar('bronze',(x,8.88,-68),(x,8.88,-56),.12,8)
 for x in np.arange(-20,21,5):m.bar('bronze',(x,8.88,-68),(x,8.88,-56),.09,8)
 for x in [-30,-24,24,30]:m.bar('bronze',(x,.16,-70),(x,1.2,-70),.12,12)
 door(m,96.04,0,90,detail);door(m,-96.04,0,-90,detail);door(m,0,59.04,0,detail)
 # Maintenance enclosure stays below the overall 48m height, on a recessed north terrace.
 # External service kiosk at ground level, separate from the roof profile.
 services(m,0,65,.16,12,5,detail)
 return m

def hotel(detail):
 m=Meshes();plot=box(-60,-55,60,55);podium=box(-52,-47,52,47)
 m.slab('stone',plot,.16,.41)
 p=rectangle_path(0,0,104,94,3 if detail else 9)
 glazing(m,p,.16,7.9,detail);wall(m,'stone',p,7.9,12.2);glazing(m,p,12.2,15.8,detail)
 for y in [7.9,12.2,15.8]:ribbon(m,'bronze',p,0,.28,y,.18)
 pool=box(-17,-43,17,-31)
 m.slab('teak',podium.difference(pool),16,.22)
 m.slab('pool_tile',pool,14.2,.25)
 # Basin walls face inward, water has no collider. A real opening is left in the deck.
 wall(m,'pool_tile',np.array(pool.exterior.coords)[::-1],14.2,16)
 m.slab('pool_water',pool.buffer(-.035),15.70,0)
 coping=pool.buffer(.32,join_style='mitre').difference(pool)
 m.slab('stone',coping,16.05,.12)
 # Submerged steps in one end; no solids across the pool opening.
 for i in range(5):m.box('pool_tile',(-16.6+i*.38,14.2+(5-i)*.32/2,-37),(.38,(5-i)*.32,2.4))
 if detail:
  for z in [-38,-36]:m.bar('steel',(-16,15,z),(-16,16.8,z),.035,10)
  for x in [-38,-32,28,34,40]:
   m.box('teak',(x,16.35,-37),(1.3,.12,2.5));m.box('ivory',(x,16.45,-37),(1.1,.10,2.2))
 rail(m,p,16,detail)
 for y in np.arange(16,124.01,3.6):
  glazing(m,rectangle_path(8,4,60,48,3 if detail else 9),float(y),float(y+3.6),detail)
 wall(m,'stone',rectangle_path(8,4,60,48),127.6,128)
 # Set-back crown and enclosed roof: silhouette stays exactly within the 150m brief.
 m.slab('roof',box(-22,-20,38,28),128,.35)
 crown=rectangle_path(8,4,48,36,3 if detail else 9)
 for y in [128,133.4,138.8,144.2]:glazing(m,crown,y,min(149.5,y+5.4),detail)
 m.slab('roof',box(-16,-14,32,22),149.6,.22)
 ribbon(m,'bronze',crown,0,.3,150,.4)
 services(m,8,8,128,12,8,detail) # Hidden by crown, roofs retained for later interiors.
 if detail:
  for x in [-22,38]:
   for z in [-20,28]:m.box('bronze',(x,72,z),(.45,112,.45))
  for z in [-46,46]:m.bar('bronze',(51.8,.3,z),(51.8,15.9,z),.055,8)
 for z in [-4,4]:door(m,-52.04,z,-90,detail)
 door(m,52.04,20,90,detail);door(m,0,47.04,0,detail)
 m.slab('clear_glass',box(-59,-12,-50,12),7.6,.1)
 for z in [-11,11]:
  m.bar('bronze',(-58,.16,z),(-58,7.8,z),.13,12)
  m.bar('bronze',(-59,7.75,z),(-50,7.75,z),.1,8)
 for z in [-15,15]:m.bar('bronze',(-57,.16,z),(-57,1.2,z),.12,12)
 return m

def validate_meshes(meshes,plot):
 total=0
 for m in meshes:
  v=np.array(m['positions']).reshape(-1,3)/K;t=np.array(m['triangles']).reshape(-1,3)
  assert np.isfinite(v).all() and len(m['uv'])==len(v)*2
  assert np.max(np.abs(v[:,0]))<=plot['w']/2+1e-5 and np.max(np.abs(v[:,2]))<=plot['d']/2+1e-5,(plot['id'],m['id'])
  assert v[:,1].max()<=plot['height']+1e-5,(plot['id'],v[:,1].max())
  area=np.linalg.norm(np.cross(v[t[:,1]]-v[t[:,0]],v[t[:,2]]-v[t[:,0]]),axis=1)
  assert np.min(area)>1e-9,m['id'];total+=len(t)
 return total

def main():
 builds=[];checks=[]
 for id,builder,sign,anchor,yaw in [('A1',arena,'BRAXHORN_arena',[0,12.1,-59.16],180),('T01',hotel,'BRAXHORN_hotel',[-52.16,8.45,0],-90)]:
  plot=next(b for b in INVENTORY if b['id']==id)
  hi=builder(True).export();lo=builder(False).export()
  counts=[validate_meshes(a,plot) for a in [hi,lo]]
  assert counts[1]<counts[0]
  # Water and transparent decorations cannot seal the pool or become giant solid volumes.
  collision=[m for m in lo if m['role'] in ['stone','dark_stone','roof','teak','pool_tile','facade_glass','spandrel','bronze','clear_glass']]
  builds.append(dict(id=id,name=plot['name'],x=plot['x'],z=plot['z'],height=plot['height'],lod0=hi,lod1=lo,collision=collision,sign=sign,signAnchor=anchor,signYaw=yaw))
  checks.append(dict(id=id,nominalHeight=plot['height'],trianglesLOD0=counts[0],trianglesLOD1=counts[1],withinApprovedPlot=True))
 doc=dict(version='2.2',scaleK=K,buildings=builds,arenaInterior='adapted estate bowl generated by BraxChampionsArena',hotelInteriorBuilt=False)
 (SRC/'Exteriors.bytes').write_bytes(gzip.compress(json.dumps(doc,separators=(',',':')).encode(),compresslevel=9,mtime=0))
 report=dict(status='EXTERIOR_GEOMETRY_PASS_UNITY_PENDING',buildings=checks,missingAsset='BRAXHORN sculpted bull, exact approved design',unity='NOT_RUN',visualAccepted=False,deviceTested=False)
 (ROOT/'mistrzowie-evidence/exterior-checks.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))

if __name__=='__main__':main()
