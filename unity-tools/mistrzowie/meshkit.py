"""Deterministic grouped meshes; nominal dimensions -> shared game K once."""
import math
import numpy as np
from shapely import constrained_delaunay_triangles
from shapely.geometry.polygon import orient
K=float(np.float32(2.33)/np.float32(1.80))

class Meshes:
 def __init__(self):self.data={}
 def tri(self,role,a,b,c,uv=None):
  v=np.array([a,b,c],float)
  if np.linalg.norm(np.cross(v[1]-v[0],v[2]-v[0]))<1e-10:return
  centre=v.mean(axis=0);chunk=f'{int(centre[0]//200):02}_{int((centre[2]+500)//200):02}'
  key=chunk+'_'+role
  m=self.data.setdefault(key,dict(id=key,role=role,positions=[],uv=[],triangles=[]))
  i=len(m['positions'])//3;m['positions']+=np.round(v*K,6).flatten().tolist()
  m['uv']+=np.round(v[:,[0,2]] if uv is None else uv,6).flatten().tolist();m['triangles']+=[i,i+1,i+2]
 def quad(self,role,a,b,c,d,uv=None):
  self.tri(role,a,b,c,None if uv is None else [uv[0],uv[1],uv[2]])
  self.tri(role,a,c,d,None if uv is None else [uv[0],uv[2],uv[3]])
 def box(self,role,c,size):
  x,y,z=c;w,h,d=np.array(size)/2
  vs=[(x-w,y-h,z-d),(x+w,y-h,z-d),(x+w,y+h,z-d),(x-w,y+h,z-d),(x-w,y-h,z+d),(x+w,y-h,z+d),(x+w,y+h,z+d),(x-w,y+h,z+d)]
  for ids,dim in [((0,3,2,1),(size[0],size[1])),((4,5,6,7),(size[0],size[1])),((0,4,7,3),(size[2],size[1])),((1,2,6,5),(size[2],size[1])),((3,7,6,2),(size[0],size[2])),((0,1,5,4),(size[0],size[2]))]:
   a,b=dim;self.quad(role,*[vs[i] for i in ids],uv=[(0,0),(0,b),(a,b),(a,0)])
 def bar(self,role,a,b,r,sides=10):
  a=np.array(a,float);b=np.array(b,float);d=b-a;length=np.linalg.norm(d)
  if length<1e-6:return
  n=d/length;ref=np.array([0,1,0]) if abs(n[1])<.9 else np.array([1,0,0]);u=np.cross(n,ref);u/=np.linalg.norm(u);v=np.cross(n,u)
  ring=[r*(u*math.cos(t)+v*math.sin(t)) for t in np.linspace(0,2*math.pi,sides,endpoint=False)]
  for i in range(sides):
   p,q=ring[i],ring[(i+1)%sides]
   self.quad(role,a+p,b+p,b+q,a+q,[(i*r,0),(i*r,length),((i+1)*r,length),((i+1)*r,0)])
   self.tri(role,a,a+q,a+p);self.tri(role,b,b+p,b+q)
 def slab(self,role,g,y,thick):
  if g.is_empty:return
  polys=[g] if g.geom_type=='Polygon' else [p for p in g.geoms if p.geom_type=='Polygon']
  for p in polys:
   p=orient(p,sign=1)
   for tri in constrained_delaunay_triangles(p).geoms:
    q=list(orient(tri,sign=1).exterior.coords)[:3]
    self.tri(role,*[(x,y,z) for x,z in q[::-1]])
    if thick>0:self.tri(role,*[(x,y-thick,z) for x,z in q])
   if thick>0:
    for ring in [p.exterior,*p.interiors]:
     points=list(ring.coords)
     for a,b in zip(points,points[1:]):
      ll=math.dist(a,b)
      self.quad(role,(a[0],y,a[1]),(b[0],y,b[1]),(b[0],y-thick,b[1]),(a[0],y-thick,a[1]),[(0,0),(ll,0),(ll,thick),(0,thick)])
 def export(self):return list(self.data.values())

def rounded_route(points,radius=25,spacing=2,closed=False):
 p=[np.array(x,float) for x in points];out=[]
 if not closed:out.append(p[0])
 for i in range(len(p)):
  if not closed and (i==0 or i==len(p)-1):continue
  a=p[(i-1)%len(p)];b=p[i];c=p[(i+1)%len(p)];u=(b-a)/np.linalg.norm(b-a);v=(c-b)/np.linalg.norm(c-b)
  angle=math.acos(np.clip(np.dot(u,v),-1,1));t=min(radius*math.tan(angle/2),np.linalg.norm(b-a)*.4,np.linalg.norm(c-b)*.4)
  if angle<1e-6:out.append(b);continue
  before=b-u*t;after=b+v*t;sign=np.sign(u[0]*v[1]-u[1]*v[0]);normal=np.array([-u[1],u[0]])*sign
  r=t/math.tan(angle/2);centre=before+normal*r;start=math.atan2(before[1]-centre[1],before[0]-centre[0])
  out.extend(centre+r*np.array([math.cos(start+sign*ang),math.sin(start+sign*ang)]) for ang in np.linspace(0,angle,max(4,math.ceil(r*angle/spacing))))
 if not closed:out.append(p[-1])
 # Resample straight portions as well; every point belongs to the physical route.
 result=[];pairs=list(zip(out,out[1:]+([out[0]] if closed else [])))
 for a,b in pairs:
  for t in np.linspace(0,1,max(1,math.ceil(np.linalg.norm(b-a)/spacing)),endpoint=False):result.append(a+(b-a)*t)
 result.append(result[0] if closed else out[-1])
 return np.asarray(result)

def path_frames(path):
 d=np.gradient(path,axis=0)
 if np.allclose(path[0],path[-1]):d[0]=d[-1]=path[1]-path[-2]
 d/=np.linalg.norm(d,axis=1)[:,None]
 return np.column_stack([-d[:,1],d[:,0]])

def ribbon(mesh,role,path,offset,width,y,thick=.04):
 normal=path_frames(path)
 for i in range(len(path)-1):
  a=path[i]+normal[i]*(offset-width/2);b=path[i]+normal[i]*(offset+width/2)
  c=path[i+1]+normal[i+1]*(offset+width/2);d=path[i+1]+normal[i+1]*(offset-width/2)
  mesh.quad(role,(a[0],y,a[1]),(d[0],y,d[1]),(c[0],y,c[1]),(b[0],y,b[1]))
  if thick>0:
   mesh.quad(role,(b[0],y-thick,b[1]),(c[0],y-thick,c[1]),(d[0],y-thick,d[1]),(a[0],y-thick,a[1]))
   for u,v in [(a,d),(c,b)]:mesh.quad(role,(u[0],y,u[1]),(u[0],y-thick,u[1]),(v[0],y-thick,v[1]),(v[0],y,v[1]))
