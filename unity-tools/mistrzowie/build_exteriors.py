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

def arena_entry_panels(path):
 """Split lower facade at +/-4.70m exactly, independent of tessellation.

 Yields (start, end, is_opening). Only two entrances: south public,
 north athletes. Used for both render LODs and permanent collision.
 """
 for a,b in zip(path,path[1:]):
  cuts=[0.0,1.0]
  if abs(a[1])>57.5 and abs(b[1])>57.5 and a[1]*b[1]>0 and abs(b[0]-a[0])>1e-9:
   for edge in (-4.70,4.70):
    t=(edge-a[0])/(b[0]-a[0])
    if 1e-9<t<1-1e-9:cuts.append(t)
  cuts=sorted(cuts)
  for t0,t1 in zip(cuts,cuts[1:]):
   p=a*(1-t0)+b*t0;q=a*(1-t1)+b*t1
   mid=(p+q)*.5
   opening=abs(mid[0])<4.70 and abs(mid[1])>57.5
   yield p,q,opening

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
 # Owner reference 7.11: low two-storey premium glass arena, NOT the old 48m tower.
 # The detailed black stepped pillars, gold insets, warm crown and sign frames are
 # added by BraxChampionsArenaArrival.PremiumExterior on the same 28m shell.
 m=Meshes();n=128;path=ellipse(96,59,n);foot=Polygon(path)
 m.slab('stone',box(-110,-75,110,75),.16,.41)
 m.slab('dark_stone',foot,.18,.6)
 # Lower curtain wall. Only the two real door apertures start at 4.5m.
 for p,q,entry in arena_entry_panels(path):
  wall(m,'facade_glass',np.array([p,q]),4.5 if entry else .18,10.75)
 # Upper curtain wall remains glass; no pale stone belt across the owner's signs.
 wall(m,'facade_glass',path,10.75,27.15)
 for yy in [10.75,18.20,27.15]:
  ribbon(m,'steel',path,0,.16,yy,.10)
 # Dark roof cap and parapet match the reference silhouette.
 m.slab('roof',foot.buffer(.55),27.35,.30)
 rim=np.asarray(foot.buffer(.65).exterior.coords)
 if not Polygon(rim).exterior.is_ccw:rim=rim[::-1]
 ribbon(m,'dark_stone',rim,0,.42,27.72,.18)
 # Exactly two real entrances; premium canopies are generated once in Unity by
 # BraxChampionsArenaArrival, so the obsolete 8.7m duplicate canopy is removed.
 door(m,0,-59.04,180,detail,open_entry=True)
 door(m,0,59.04,0,detail,open_entry=True)
 # Rear service kiosk remains low and outside the public reference facade.
 services(m,35,64,.16,12,5,detail)
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
