"""Approved MIASTO MISTRZÓW coastal promenade — photo-guided K1..K12.

Source: owner's 5 November 2026 'PROMENADA NADMORSKA – PROJEKT
ROZBUDOWY' image, not a claim of pixel-identical Unity screenshots.
The actual existing 1100 x 30m paved coast is retained and recut only
for a 3m red bike lane. Twelve real shop shells use the inland green verge
without occupying the 20m main pedestrian walkway.
All dimensions are nominal; original Unity K factor is applied ONCE.
"""
from math import hypot
from shapely.geometry import box

X0,X1=50.,1150.
Z0,Z1=-150.,-120.
CYCLE_Z0,CYCLE_Z1=-148.5,-145.5
CYCLE_WIDTH=CYCLE_Z1-CYCLE_Z0
MIN_PEDESTRIAN_CLEAR=14.
PEDESTRIAN_ZONE=(CYCLE_Z1,-125.)
SERVICE_ZONE=(-125.,Z1)
KIOSK_FRONT=-119.0
PAVILION_PAD_MARGIN=.35
# Real perpendicular pedestrian connections to seafront: preserve all.
PEDESTRIAN_CONNECTIONS=((230.,8.),(530.,8.),(700.,8.),(1080.,8.))

DATA=(
 # id, owner-reference text, front sign, x, width, depth, height, style, entry type
 ("K1","CAFÉ / KAWIARNIA","BRAXHORN CAFÉ",102.5,6.,4.5,3.6,"cafe",False),
 ("K2","LODY / GOFRY","LODY • GOFRY",177.5,5.,4.,3.4,"icecream",False),
 ("K3","RYBY NA WYNOS","RYBY NA WYNOS",252.5,6.,4.2,3.8,"fish",False),
 ("K4","PIZZA / BISTRO","PIZZA • BISTRO",327.5,6.,4.5,3.8,"pizza",False),
 ("K5","BURGER","BRAX BURGER",402.5,5.5,4.,3.6,"burger",False),
 ("K6","SOKI / OWOCE","SOKI • OWOCE",477.5,5.,3.5,3.3,"juice",False),
 ("K7","SKLEPIK PLAŻOWY","SKLEPIK PLAŻOWY",552.5,5.5,4.,3.6,"beach_shop",True),
 ("K8","PAMIĄTKI","PAMIĄTKI",627.5,5.,3.5,3.3,"souvenirs",True),
 ("K9","BAR / DRINKI","BAR • DRINKI",727.5,6.,4.5,3.8,"bar",False),
 ("K10","GRILL / BBQ","GRILL • BBQ",827.5,6.,4.5,3.8,"grill",False),
 ("K11","WYNAJEM SPRZĘTU","WYNAJEM SPRZĘTU",902.5,6.,4.,3.6,"rental",True),
 ("K12","INFORMACJA / TOUR","INFORMACJA • TOUR",1027.5,5.,4.,3.5,"info",True),
)
PAVILIONS=[
 dict(id=pid,name=name,sign=sign,x=x,z=KIOSK_FRONT+depth/2,
      w=w,d=depth,h=h,style=style,walkIn=walkin)
 for pid,name,sign,x,w,depth,h,style,walkin in DATA
]
PAVILION_PADS=[
 (p["x"]-p["w"]/2-PAVILION_PAD_MARGIN,KIOSK_FRONT-PAVILION_PAD_MARGIN,
  p["w"]+2*PAVILION_PAD_MARGIN,p["d"]+2*PAVILION_PAD_MARGIN)
 for p in PAVILIONS
]

def validate():
    assert X1-X0==1100. and Z1-Z0==30.0
    assert CYCLE_WIDTH==3.0 and PEDESTRIAN_ZONE[1]-PEDESTRIAN_ZONE[0]>=20.0
    assert len(PAVILIONS)==12 and len(PAVILION_PADS)==12
    assert {p["id"] for p in PAVILIONS}=={"K"+str(i) for i in range(1,13)}
    assert len({p["style"] for p in PAVILIONS})==12
    used=[]
    palm_centres=list(range(65,1146,25))
    for p,pad in zip(PAVILIONS,PAVILION_PADS):
        a,b,w,d=pad
        shape=box(a,b,a+w,b+d)
        assert p["w"]>4.9 and p["h"]>=3.3
        assert shape.bounds[0]>=X0 and shape.bounds[2]<=X1
        # Never put a kiosk on the seaward cycle/pedestrian corridor.
        assert shape.bounds[1]>Z1-0.05
        assert shape.bounds[3]<-110.
        # Source palms are planted at -115 nominal; keep at least a 2m
        # minimum root disk from the walls/pads, not just the tree centre.
        for palm_x in palm_centres:
            if abs(palm_x-p["x"]) < p["w"]/2+PAVILION_PAD_MARGIN+2.0:
                raise ValueError("Palm trunk/root conflicts with "+p["id"])
        for path_x,path_width in PEDESTRIAN_CONNECTIONS:
            if not (a+w+2<path_x-path_width/2 or a-2>path_x+path_width/2):
                raise ValueError("Existing access path blocked by "+p["id"])
        for earlier in used:
            if shape.intersection(earlier).area>0:
                raise ValueError("Overlapping pavilion slabs "+p["id"])
        used.append(shape)
    return True

validate()
