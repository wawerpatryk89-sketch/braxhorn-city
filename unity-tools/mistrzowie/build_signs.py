"""Build real, closed, bevelled sign meshes; no raster letters or pretend sculpture.
Python: numpy, matplotlib, shapely>=2.1. Rebuilds compressed Signs.bytes deterministically.
All dimensions are nominal metres. The existing Unity K is applied once to vertices.
"""
from pathlib import Path
from collections import Counter
import json, math, shutil, gzip
import numpy as np
from matplotlib.textpath import TextPath
from matplotlib.font_manager import FontProperties
from shapely import symmetric_difference_all, constrained_delaunay_triangles, segmentize
from shapely.geometry import Polygon, MultiPolygon, LineString, Point
from shapely.geometry.polygon import orient
from shapely.affinity import scale, translate

ROOT=Path(__file__).resolve().parents[2]
SRC=ROOT/'unity-city/Assets/Source/Mistrzowie'
K=float(np.float32(2.33)/np.float32(1.80))
FONT=Path('/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf')
if not FONT.exists():raise FileNotFoundError('Install fonts-dejavu-core: licensed source font required')
FP=FontProperties(fname=str(FONT))

def polygons(g):
 return [g] if g.geom_type=='Polygon' else [p for p in g.geoms if p.geom_type=='Polygon']

def text_shape(s,height,maxwidth,y=0):
 # Large font size gives curved glyph outlines dense enough for physical bevels.
 path=TextPath((0,0),s,size=100,prop=FP)
 rings=[Polygon(q) for q in path.to_polygons() if len(q)>3]
 g=symmetric_difference_all(rings)
 assert g.is_valid and not g.is_empty,s
 x0,y0,x1,y1=g.bounds
 f=min(height/(y1-y0),maxwidth/(x1-x0))
 return translate(scale(g,xfact=f,yfact=f,origin=(0,0)),xoff=-(x0+x1)*f/2,yoff=y-y0*f)

def stroke(points,width):
 return LineString(points).buffer(width/2,quad_segs=4,cap_style='round',join_style='round')

def star(x,y,r):
 return Polygon([(x+math.cos(math.pi/2+i*math.pi/5)*r*(1 if i%2==0 else .43),y+math.sin(math.pi/2+i*math.pi/5)*r*(1 if i%2==0 else .43)) for i in range(10)])

class Geometry:
 def __init__(self):
  self.roles={}
  self.solid_checks=[]
 def triangle(self,role,points,uv=None):
  v=np.asarray(points,float)
  n=np.cross(v[1]-v[0],v[2]-v[0]); length=np.linalg.norm(n)
  if length<1e-14: raise ValueError('zero area triangle')
  n/=length
  m=self.roles.setdefault(role,dict(role=role,positions=[],normals=[],uv=[],triangles=[]))
  i=len(m['positions'])//3
  m['positions'].extend(v.flatten().tolist());m['normals'].extend(np.tile(n,3).tolist())
  m['uv'].extend((v[:,:2]*4 if uv is None else np.asarray(uv)).flatten().tolist())
  m['triangles'].extend([i,i+1,i+2])
  self.solid_checks.extend([tuple(np.round(p,8)) for p in v])
 def cap(self,g,z,role,front=True):
  tris=constrained_delaunay_triangles(g)
  area=0
  for tri in tris.geoms:
   pts=np.array(orient(tri,sign=1).exterior.coords)[:3]
   area+=tri.area
   if not front:pts=pts[::-1]
   self.triangle(role,[(x,y,z) for x,y in pts])
  assert abs(area-g.area)<1e-8*max(1,g.area),'cap area / hole coverage'
 def solid(self,g,z0,z1,front='gold',side='dark',bevel=.008,bevel_role='gold',densify=None):
  for poly in polygons(g):
   poly=orient(poly,sign=1)
   if densify:poly=orient(segmentize(poly,densify),sign=1)   # dense outline for a sign bent after building
   start=len(self.solid_checks)
   # Bevel is an actual sloped annulus, including counters in A/B/O/R.
   b=min(bevel,(z1-z0)*.2)
   inner=poly.buffer(-b,join_style='mitre')
   for _ in range(8):
    if not inner.is_empty and len(polygons(inner))==1:break
    b*=.5;inner=poly.buffer(-b,join_style='mitre')
   assert not inner.is_empty,'bevel consumed glyph'
   if densify:inner=segmentize(inner,densify)
   edge=poly.difference(inner)
   self.cap(poly,z0,side,False)
   self.cap(inner,z1,front,True)
   for tri in constrained_delaunay_triangles(edge).geoms:
    pts=np.array(orient(tri,sign=1).exterior.coords)[:3]
    vertices=[(x,y,z1 if inner.boundary.distance(Point(x,y))<1e-7 else z1-b) for x,y in pts]
    self.triangle(bevel_role,vertices)
   for ring in [poly.exterior,*poly.interiors]:
    coords=list(ring.coords);length=0
    for a,bp in zip(coords,coords[1:]):
     ll=math.dist(a,bp);a0=(*a,z0);a1=(*a,z1-b);b0=(*bp,z0);b1=(*bp,z1-b)
     uv=[(length*4,z0*4),((length+ll)*4,z0*4),((length+ll)*4,(z1-b)*4),(length*4,(z1-b)*4)]
     self.triangle(side,[a0,b0,b1],[uv[0],uv[1],uv[2]])
     self.triangle(side,[a0,b1,a1],[uv[0],uv[2],uv[3]])
     length+=ll
   # Every separate solid must be closed after welding coincident vertices.
   edges=Counter(); directed=Counter()
   for i in range(start,len(self.solid_checks),3):
    t=self.solid_checks[i:i+3]
    for a,c in zip(t,t[1:]+t[:1]):
     edges[tuple(sorted((a,c)))]+=1;directed[(a,c)]+=1
   assert all(v==2 for v in edges.values()),'nonmanifold/open glyph'
   assert all(directed[(a,c)]==directed[(c,a)] for a,c in directed),'inconsistent winding'

SERVICES={
 'hotel':('HOTEL',8),'kasyno':('KASYNO',10),'bank':('BANK',7),
 'arena':('ARENA',14),'klub':('KLUB',5),'kawiarnia':('KAWIARNIA',3.8),
 'restauracja':('RESTAURACJA',5),'apartamenty':('APARTAMENTY',6),
 'spa':('SPA',5),'basen':('BASEN',6),'galeria':('GALERIA',9),
 'supermarket':('SUPERMARKET',6),'kino':('KINO',5),'boks':('BOKS',5),
 'kickboxing':('KICKBOXING',5),'mma':('MMA',5),'silownia':('SIŁOWNIA',5),
 'apteka':('APTEKA',3.5),'barber':('BARBER',3.5),'uroda':('SALON URODY',3.5),
 'moda':('MODA',3.5),'obuwie':('OBUWIE',3.5),'sport':('SPORT',3.5),
 'odzywki':('ODŻYWKI',3.5),'elektronika':('ELEKTRONIKA',4),'jubiler':('JUBILER',3.5),
 'ksiegarnia':('KSIĘGARNIA',3.5),'optyk':('OPTYK',3.5),'perfumy':('PERFUMERIA',3.5),
 'kregle':('KRĘGLE',4.5),'bilard':('BILARD',4),'gry':('SALON GIER',4.5),
 'medycyna':('MEDYCYNA SPORTOWA',6),'liga':('CENTRUM LIGI',7),
 'marina':('MARINA',7),'yachtclub':('YACHT CLUB',6),'beachbar':('BEACH BAR',5),
 'nieruchomosci':('NIERUCHOMOŚCI',4)
}
BRANDS=['BRAXHORN','WAWER','SOBCZAK']

def build(brand,service):
 g=Geometry();label,width=SERVICES[service]
 if service=='arena':label='CHAMPION ARENA'
 # Individual solid glyphs, with body depth and a smaller, recessed light strip.
 for shape in [text_shape(brand,.78,5.85),text_shape(label,.38,4.9,1.02)]:
  g.solid(shape,.085,.24,front='ivory' if service in ['kasyno','klub'] else 'gold')
  g.solid(shape,.04,.068,front='light',side='light',bevel=.0015)
  for poly in polygons(shape):
   p=poly.representative_point()
   g.solid(p.buffer(.018,quad_segs=4),0,.084,front='dark',side='dark',bevel=.001)
 if service=='arena':g.solid(text_shape('PÓŁNOC',.20,3.2,-.68),.085,.22)
 if service=='hotel':
  for x in np.linspace(-.88,.88,5):g.solid(star(x,1.65,.14),.085,.24)
 # Four frame families; the brands do not become identical square boards.
 top=1.9 if service=='hotel' else 1.7
 prestige=service in ['hotel','kasyno','bank','arena','apartamenty','galeria','liga','marina','yachtclub']
 entertainment=service in ['klub','kino','kregle','bilard','gry','beachbar']
 wellness=service in ['spa','basen','medycyna']
 if prestige and not (brand=='SOBCZAK' and service=='bank'):
  pts=[(3.4*math.cos(t),.1+top*math.sin(t)) for t in np.linspace(.06,math.pi-.06,80)]
  g.solid(stroke(pts,.14 if brand=='WAWER' else .09),.06,.18,front='gold',side='accent',bevel=.009)
  if brand=='WAWER':g.solid(stroke(pts,.075),.181,.205,front='accent',side='accent',bevel=.003)
  if service=='kasyno':
   pts=[(3.28*math.cos(t),.1+(top-.08)*math.sin(t)) for t in np.linspace(.06,math.pi-.06,80)]
   g.solid(stroke(pts,.027),.055,.1,front='accent',side='accent',bevel=.003)
 elif entertainment:
  for side in [-1,1]:
   pts=[(side*(1.7+1.6*t),1.65-.85*t*t) for t in np.linspace(0,1,35)]
   g.solid(stroke(pts,.085),.06,.19,front='accent',side='gold',bevel=.009)
 pts=[(x,-.22-.12*(x/2.9)**2) for x in np.linspace(-2.9,2.9,64)]
 g.solid(stroke(pts,.052),.06,.15,front='gold',side='accent',bevel=.007)
 if wellness:
  pts=[(x,-.42+.08*math.sin(x*1.2)) for x in np.linspace(-2.7,2.7,64)]
  g.solid(stroke(pts,.045),.06,.16,front='accent',side='gold',bevel=.006)
 for x in [-2.7,2.7]:g.solid(Point(x,-.30).buffer(.024,quad_segs=6),0,.06,front='dark',side='dark',bevel=.003)
 if brand=='SOBCZAK':
  yy=top+.78;large=service=='bank'
  ellipse=scale(Point(0,yy).buffer(1,quad_segs=32),xfact=1.05 if large else .63,yfact=.72 if large else .43,origin=(0,yy))
  ring=ellipse.difference(scale(ellipse,xfact=.88,yfact=.82,origin=(0,yy)))
  g.solid(ring,.07,.24,front='gold',bevel=.008)
  g.solid(text_shape('S',.94 if large else .53,1.2 if large else .7,yy-(.47 if large else .265)),.08,.27)
 for role,m in g.roles.items():
  m['positions']=[round(v*K*width/6.9,7) for v in m['positions']]
  m['normals']=[round(v,7) for v in m['normals']]
  m['uv']=[round(v*width/6.9,7) for v in m['uv']]
  m['id']=brand+'_'+service+'_'+role
 return dict(id=brand+'_'+service,brand=brand,service=service,label=label,nominalWidth=width,
  needsSculptedEmblem=brand!='SOBCZAK',emblemAnchor=[0,round((top+.78)*K*width/6.9,7),round(.15*K*width/6.9,7)],
  meshes=list(g.roles.values()),acceptance='SOURCE_GEOMETRY_ONLY_UNITY_REVIEW_PENDING')

if __name__=='__main__':
 catalog=[build('BRAXHORN',service) for service in ['arena','hotel']]
 doc=dict(version='2.2',scaleK=K,frontAxis='+Z',rootScale=[1,1,1],signs=catalog)
 SRC.mkdir(parents=True,exist_ok=True)
 (SRC/'Signs.bytes').write_bytes(gzip.compress(json.dumps(doc,ensure_ascii=False,separators=(',',':')).encode(),compresslevel=9,mtime=0))
 # Keep font license beside the generated assets and in the editable export dependencies.
 shutil.copy('/usr/share/doc/fonts-dejavu-core/copyright',SRC/'SignFont-LICENSE.txt')
 report=dict(status='CLOSED_BEVELLED_GEOMETRY_PASS_UNITY_PENDING',signs=2,hotelStars=5,triangles=sum(len(m['triangles'])//3 for s in catalog for m in s['meshes']),sculptedBull='SOURCE_MODEL_REQUIRED',visualAccepted=False)
 (ROOT/'mistrzowie-evidence/sign-geometry-checks.json').write_text(json.dumps(report,indent=2))
 print(json.dumps(report))
