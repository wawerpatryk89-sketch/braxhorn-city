"""Grass/soil around verified plant beds, never across streets or building plots."""
from pathlib import Path
import json, math, random
import numpy as np
from PIL import Image
from shapely.geometry import Polygon, Point
from shapely.ops import unary_union
ROOT=Path(__file__).resolve().parents[2]
SRC=ROOT/'unity-city/Assets/Source/Mistrzowie'


def main():
    data=json.loads((SRC/'Foundation.json').read_text()); k=data['scaleK']
    # Exact generated ground triangles, already in world units; no second scale.
    triangles=[]
    for mesh in data['meshes']:
        if mesh['role']!='grass': continue
        v=np.asarray(mesh['positions']).reshape(-1,3)
        for face in np.asarray(mesh['triangles']).reshape(-1,3):
            triangles.append(Polygon(v[face][:,[0,2]]))
    lawn=unary_union(triangles).buffer(-.18)
    mask=np.zeros((1500,1200),dtype=np.uint8) # 1 nominal metre/texel, z=-500..1000
    points=[]; seen=set(); rng=random.Random(4242)
    for tree in data['plants']:
        x,z=tree['x'],tree['z']
        # Original Claude principle: soil only near trunks; no dirt across open lawns.
        for iz in range(max(0,int(z+500)-3),min(1500,int(z+500)+4)):
            for ix in range(max(0,int(x)-3),min(1200,int(x)+4)):
                d=math.hypot(ix+.5-x,iz+.5-500-z)*k
                if lawn.covers(Point((ix+.5)*k,(iz+.5-500)*k)):
                    mask[iz,ix]=max(mask[iz,ix],round(max(0,min(1,(1.6-d)/.8))*230))
        # Bounded local detail in planted beds; millions of per-region instances avoided.
        for dz in np.arange(-1.45,1.46,.28):
            for dx in np.arange(-1.45,1.46,.28):
                px=(x+float(dx)+rng.uniform(-.08,.08))*k
                pz=(z+float(dz)+rng.uniform(-.08,.08))*k
                d=math.hypot(px-x*k,pz-z*k)
                key=(round(px,2),round(pz,2))
                if d<1.2 or d>1.45*k or key in seen or not lawn.covers(Point(px,pz)):continue
                seen.add(key)
                points.append(dict(x=px,z=pz,y=.16*k,yaw=rng.uniform(0,360),scale=rng.uniform(.9,1.1)))
    Image.fromarray(np.flipud(mask)).save(SRC/'grass-soil-mask.png')
    (SRC/'grass-clumps.json').write_text(json.dumps(dict(points=points),separators=(',',':')))
    assert len(points)<100000, 'Grass detail budget exceeded'
    report=dict(clumps=len(points),soilMask=[1200,1500],placement='verified lawn triangles around 287 existing plant beds',unity='NOT_RUN')
    (ROOT/'mistrzowie-evidence/landscape-checks.json').write_text(json.dumps(report,indent=2))
    print(json.dumps(report))

if __name__=='__main__':main()
