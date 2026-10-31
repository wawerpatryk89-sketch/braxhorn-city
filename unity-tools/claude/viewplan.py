# Planer ujęć: dokładnie jak BraxGymPlayer.ReviewView/UpdateCamera + kamera osiedla (FOV pionowy 55°, 1280x576).
import math
VF=55.0; ASP=1280/576
TV=math.tan(math.radians(VF/2)); TH=TV*ASP
def cam(view):
    x,y,z,yaw,p,d=[float(v) for v in view.split(',')]
    p=min(57,max(7,p)); d=min(12,max(.8,d))
    ya,pi=math.radians(yaw),math.radians(p)
    T=(x,y+1.65,z); off=(math.sin(ya)*math.cos(pi)*d, math.sin(pi)*d, math.cos(ya)*math.cos(pi)*d)
    C=tuple(T[i]+off[i] for i in range(3)); return C,T,p,d
def norm(v): l=math.sqrt(sum(a*a for a in v)); return tuple(a/l for a in v)
def cross(a,b): return (a[1]*b[2]-a[2]*b[1], a[2]*b[0]-a[0]*b[2], a[0]*b[1]-a[1]*b[0])
def dot(a,b): return sum(a[i]*b[i] for i in range(3))
def proj(view,P):
    C,T,_,_=cam(view); f=norm(tuple(T[i]-C[i] for i in range(3)))
    r=norm(cross((0,1,0),f)); u=cross(f,r)
    d=tuple(P[i]-C[i] for i in range(3)); zz=dot(d,f)
    if zz<=.1: return None
    return (dot(d,r)/(zz*TH), dot(d,u)/(zz*TV), zz)
def report(name,view,pois):
    C,T,p,d=cam(view)
    print(f"{name}: cam ({C[0]:.2f},{C[1]:.2f},{C[2]:.2f}) target ({T[0]:.2f},{T[1]:.2f},{T[2]:.2f}) pitch {p} dist {d}")
    for k,P in pois.items():
        q=proj(view,P)
        if q is None: print(f"   {k:28s} ZA KAMERĄ"); continue
        ok='OK' if abs(q[0])<=1 and abs(q[1])<=1 else 'POZA KADREM'
        print(f"   {k:28s} ekran x {q[0]:+.2f} y {q[1]:+.2f} odl {q[2]:5.1f} m  {ok}")
