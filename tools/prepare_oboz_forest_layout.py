"""Deterministic R3 vegetation placement, metres before world scale K.

Exclusion zones protect the agreed anchors and all reconstructed service sites.
This is placement data for review, not an approved final visual composition.
"""
import json,math,random
from pathlib import Path

ZONES=[(-94,-20,10,8),(-94,-40,10,8),(-69,-20,10,8),(-69,-40,10,8),
       (0,53,25,21),(0,-5,15,16),(-23,15,11,11),(-23,-40,12,8),(23,-40,12,8),
       (-51,-35,13,17),(-58,-65,11,8),(-95,-65,8,8),(-115,-65,8,8),
       (-46,-90,9,8),(-24,-90,9,8),(-75,89,13,16),(88,95,24,24),
       (-78,-95,13,12),(100,-85,22,18),(39,-94,15,12),(-24,-127,15,10)]
PATHS=[(0,0,5.5,151),(-50,-55,53,4.8),(-100,-35,3.5,23),(-63,-35,3.5,23),
       (-61.5,-74,64.5,4.8),(-58,-71.75,2.4,3),(-95,-71,2.4,4),(-115,-71,2.4,4),
       (-25,-99,28,4.8),(-46,-96,2.4,4),(-24,-96,2.4,4),
       (-37.5,76,41,4.8),(40,76,44,4.8),(42,-78,46,4.8)]
def excluded(x,z,padding=0):
    if abs(x-72)<7+padding:return True
    return any(abs(x-cx)<hx+padding and abs(z-cz)<hz+padding for cx,cz,hx,hz in ZONES+PATHS)

def make():
    rng=random.Random(421294);trees=[];grass=[]
    # Separate tree cells prevent coincident trunks; jitter breaks grid lines.
    for iz in range(50):
        for ix in range(50):
            x=-205+ix*8.35+rng.uniform(-2.6,2.6);z=-205+iz*8.35+rng.uniform(-2.6,2.6)
            if excluded(x,z,2.8):continue
            outside=abs(x)>146 or abs(z)>146
            if not outside and rng.random()<.36:continue
            model=rng.randrange(3) if rng.random()<.68 else 3+rng.randrange(3)
            trees.append({'x':round(x,4),'z':round(z,4),'yaw':round(rng.uniform(0,360),3),
                          'scale':round(rng.uniform(.88,1.12),4),'prototype':model,'solid':not outside})
    for _ in range(170):
        cx=rng.uniform(-145,145);cz=rng.uniform(-145,145)
        if excluded(cx,cz,2):continue
        for _ in range(90):
            angle=rng.uniform(0,2*math.pi);r=math.sqrt(rng.random())*5
            x=cx+math.cos(angle)*r;z=cz+math.sin(angle)*r
            if excluded(x,z,.2):continue
            grass.append({'x':round(x,4),'z':round(z,4),'yaw':round(rng.uniform(0,360),3),'scale':round(rng.uniform(.85,1.25),4)})
    return {'schema':1,'seed':421294,'worldScale':1.294,'units':'metres before K','ownerAppearanceApproved':False,
            'prototypes':['fir_tree_01_a','fir_tree_01_b','fir_tree_01_c','fir_sapling_medium_a','fir_sapling_medium_b','fir_sapling_medium_c'],
            'trees':trees,'grass':grass,'exclusionZones':ZONES,'paths':PATHS}

if __name__=='__main__':
    root=Path(__file__).resolve().parents[1];data=make();dest=root/'unity-city/Assets/Editor/Oboz/VegetationLayout.json'
    dest.write_text(json.dumps(data,ensure_ascii=False,separators=(',',':'))+'\n');print({'trees':len(data['trees']),'grassClumps':len(data['grass'])})
