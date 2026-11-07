"""Independent numerical proof only: this does not execute Unity or C#.

SciPy half-space intersections are the independent oracle. The float32
algorithm follows the proposed C# clipping arithmetic for comparison.
"""
from pathlib import Path
import json
import math
import numpy as np
from scipy.optimize import linprog
from scipy.spatial import ConvexHull, HalfspaceIntersection

ROOT = Path(__file__).resolve().parent
F = np.float32


def rect(x0, x1, z0, z1):
    return np.array([[x0,z0],[x1,z0],[x1,z1],[x0,z1]], dtype=float)


def ellipse(rx, rz, centre=-22.):
    t = np.arange(128) * (math.pi * 2 / 128)
    return np.column_stack((np.cos(t)*rx, centre+np.sin(t)*rz))


def intersection_area(*polygons):
    halves = np.concatenate([ConvexHull(p).equations for p in polygons])
    # Find a strictly interior point with the largest clearance to all edges.
    result = linprog([0,0,-1], A_ub=np.column_stack((halves[:,:2],
        np.linalg.norm(halves[:,:2],axis=1))), b_ub=-halves[:,2],
        bounds=[(None,None),(None,None),(0,None)], method='highs')
    if not result.success or result.x[2] < 1e-8:
        return 0.
    points = HalfspaceIntersection(halves, result.x[:2]).intersections
    return float(ConvexHull(points).volume)


def cross(a,b):
    return F(F(a[0]*b[1])-F(a[1]*b[0]))


def area(p):
    if len(p)<3: return F(0)
    origin=p[0]; total=F(0)
    for i in range(len(p)):
        total=F(total+cross(p[i]-origin,p[(i+1)%len(p)]-origin))
    return F(abs(total)*F(.5))


def halfplane(p,a,b,inside):
    if not len(p): return []
    out=[]; sign=F(1 if inside else -1)
    previous=p[-1]; before=F(sign*cross(b-a,previous-a)); previous_in=before>=F(-1e-6)
    for current in p:
        distance=F(sign*cross(b-a,current-a)); current_in=distance>=F(-1e-6)
        if current_in != previous_in:
            t=F(before/F(before-distance))
            out.append((previous+F(t)*(current-previous)).astype(np.float32))
        if current_in: out.append(current.copy())
        previous=current;before=distance;previous_in=current_in
    for i in range(len(out)-1,-1,-1):
        if len(out)<=1: break
        delta=out[i]-out[(i+1)%len(out)]
        if F(delta[0]*delta[0]+delta[1]*delta[1])<F(1e-12): del out[i]
    return out


def intersect(p,q):
    for i in range(len(q)):
        p=halfplane(p,q[i],q[(i+1)%len(q)],True)
        if not len(p): break
    return p


def subtract(p,q):
    pieces=[]
    for i in range(len(q)):
        if len(p)<3: break
        outside=halfplane(p,q[i],q[(i+1)%len(q)],False)
        if area(outside)>F(1e-7): pieces.append(outside)
        p=halfplane(p,q[i],q[(i+1)%len(q)],True)
    return pieces


def float_ellipse(outer):
    extra=F(F(4)*F(1.2)) if outer else F(0)
    rx=F(F(F(90)-F(8.8))+extra);rz=F(F(F(103)-F(8.8))+extra)
    output=[]
    for i in range(128):
        t=F(F(F(F(i)*F(math.pi))*F(2))/F(128))
        output.append(np.array([F(F(np.cos(t))*rx),F(F(-22)+F(F(np.sin(t))*rz))],dtype=np.float32))
    return output


inner=ellipse(81.2,94.2);outer=ellipse(86,99);sprint=rect(65,71,-74,26)
finner=float_ellipse(False);fouter=float_ellipse(True);fsprint=sprint.astype(np.float32)
fixtures=[];checks=[]
cases=[('road',[-3.5,3.5,-139,-109]),('bus_path',[24.6,27.4,-122,-73]),
       ('straight_crossing',[60,76,-5,-3]),('inner',[0,1,0,1]),
       ('outside',[100,101,100,101]),('oval_east',[80,87,-23,-21]),
       ('oval_north',[-1,1,70,79]),('partial_corner',[25,25.25,-115,-114.75]),
       ('whole_loop',[-90,90,-125,81]),('shared_start_corner',[68,71,-74,-70])]
for name,bounds in cases:
    p=rect(*bounds)
    expected=(float(ConvexHull(p).volume)-intersection_area(p,outer)+intersection_area(p,inner)
              -intersection_area(p,sprint)+intersection_area(p,sprint,outer)-intersection_area(p,sprint,inner))
    pieces=subtract(p.astype(np.float32),fouter)
    inside=intersect(p.astype(np.float32),finner)
    if area(inside)>F(1e-7): pieces.append(inside)
    result=sum(float(area(q)) for piece in pieces for q in subtract(piece,fsprint))
    fixtures.append({'name':name,'bounds':bounds,'areaOutsideTracks':expected})
    checks.append({'name':name,'expectedArea':expected,'float32ModelArea':result,
                   'error':result-expected,'passed':abs(result-expected)<.02})

# One actual inner-lane sector crosses the sprint's southern start. This is
# checked separately from paving, to verify that oval material is removed too.
a=116*math.pi*2/128;b=117*math.pi*2/128
sector=np.array([[math.cos(a)*81.2,math.sin(a)*94.2],
                 [math.cos(a)*82.4,math.sin(a)*95.4],
                 [math.cos(b)*82.4,math.sin(b)*95.4],
                 [math.cos(b)*81.2,math.sin(b)*94.2]])
local_sprint=sprint-np.array([0,-22])
sector_area=float(ConvexHull(sector).volume)
cut_expected=sector_area-intersection_area(sector,local_sprint)
cut_actual=sum(float(area(q)) for q in subtract(sector.astype(np.float32),local_sprint.astype(np.float32)))
checks.append({'name':'oval_sector_116_lane_0_minus_sprint','originalArea':sector_area,
               'expectedArea':cut_expected,'float32ModelArea':cut_actual,'error':cut_actual-cut_expected,
               'passed':abs(cut_actual-cut_expected)<.002})

overlap=intersection_area(sprint,outer)-intersection_area(sprint,inner)
corners=[{'x':float(x),'z':float(z),'fenceEquation':(x/90)**2+((z+22)/103)**2}
         for x,z in sprint]
proof={'verification':'Independent SciPy convex halfspace intersections versus float32 mathematical model; Unity C#, physics and NavMesh NOT executed.',
       'sprint':{'sourceLengthMetres':100,'worldLength':100*1.294,'sourceWidthMetres':6,
                 'worldWidth':6*1.294,'centre':[68,-24],'corners':corners,
                 'ovalOverlapRemovedAtAllLayersSquareMetres':overlap},
       'checks':checks,'passed':sum(x['passed'] for x in checks),'failed':sum(not x['passed'] for x in checks)}
(ROOT/'geometry-proof-input.json').write_text(json.dumps({'verification':proof['verification'],'fixtures':fixtures,
    'ovalSector':{'vertices':sector.tolist(),'areaAfterSprintSubtraction':cut_expected}},ensure_ascii=False,indent=2)+'\n')
(ROOT/'construction-numerical-proof.json').write_text(json.dumps(proof,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(proof,ensure_ascii=False,indent=2))
assert not proof['failed']
assert all(c['fenceEquation']<1 for c in corners)
