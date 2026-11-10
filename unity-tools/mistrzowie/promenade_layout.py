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
 # owner 10.11 09:06/09:16: "budki wyposażyć w kebaby" - the grill kiosk is the kebab (rotating doner) + grill
 ("K10","KEBAB / GRILL","KEBAB • GRILL",827.5,6.,4.5,3.8,"grill",False),
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
# No original grass OR pad paving beneath a shop floor.
# The outer 35cm apron remains a SINGLE continuous paved ring.
PAVILION_FOOTPRINTS=[
 (p["x"]-p["w"]/2,KIOSK_FRONT,p["w"],p["d"])
 for p in PAVILIONS
]

# Owner 10.11 09:01-09:06: "drzewa posadzić ... palmy posadzić ... promenady ładne, ekskluzywne". A seaside row of the
# owner's thick-trunk palms (palma, the same model as the arena entrance walk) in 3.2 m planted beds flush with the
# paving, 2.5 m inland of the cycle lane, every 18 m; a bench facing the sea or a lamp in every gap. Gaps are left at
# every gangway to a pier and at every perpendicular walk from the city (zebra crossings on the cycle lane).
PALM_ROW_Z=-143.0
PALM_BED=3.2
PALM_STEP=18.0


def _palm_beds():
    from port_layout import PROMENADE_OPENINGS
    keep_clear=[((a+b)/2,(b-a)/2+5.0) for a,b in PROMENADE_OPENINGS]+[(c,w/2+5.0) for c,w in PEDESTRIAN_CONNECTIONS]
    beds=[]
    x=X0+12.0
    while x<=X1-12.0:
        if all(abs(x-c)>r+PALM_BED/2 for c,r in keep_clear):
            beds.append((round(x,2),PALM_ROW_Z))
        x+=PALM_STEP
    return beds


PALM_BEDS=_palm_beds()
# (x0, z0, w, d) grass rectangles cut out of the promenade paving
PALM_BED_RECTS=[(x-PALM_BED/2,z-PALM_BED/2,PALM_BED,PALM_BED) for x,z in PALM_BEDS]
# Benches and lamps in the gaps between neighbouring beds (only gaps of one regular step).
FURNITURE=[]
for (xa,_),(xb,_) in zip(PALM_BEDS,PALM_BEDS[1:]):
    if abs(xb-xa-PALM_STEP)<.01:
        FURNITURE.append(("bench" if len(FURNITURE)%2==0 else "lamp",round((xa+xb)/2,2),PALM_ROW_Z))

# Owner 10.11 10:28: "między budkami ... krzesełka obok, stanowiska do jedzenia, palmy posadzić, w składzie dopracować".
# A dining island in the middle of every gap between two kiosks, on the landward side of the promenade paving (the
# walking corridor toward the sea, z -145.5..-126.5, stays clear): a 14 x 6 m silver-teak deck with a granite edge,
# three marble bistro tables (four teak chairs with cotton cushions, a woven white parasol through each), two standing
# counters with leather stools, and a 3.2 m flush bed with the owner's thick palm at each end of the deck.
DINING_W,DINING_D,DINING_Z=14.0,6.0,-123.5
DINING_BED=3.2
DINING=[(round((a["x"]+b["x"])/2,2),DINING_Z) for a,b in zip(PAVILIONS,PAVILIONS[1:])]
DINING_BEDS=[(round(x+s*(DINING_W/2+DINING_BED/2+.4),2),z) for x,z in DINING for s in (-1,1)]
DINING_BED_RECTS=[(x-DINING_BED/2,z-DINING_BED/2,DINING_BED,DINING_BED) for x,z in DINING_BEDS]


def validate_dining():
    assert len(DINING)==11 and len(DINING_BEDS)==22
    shapes=[box(x-DINING_W/2,z-DINING_D/2,x+DINING_W/2,z+DINING_D/2) for x,z in DINING]
    shapes+=[box(a,b,a+w,b+d) for a,b,w,d in DINING_BED_RECTS]
    for s in shapes:
        assert s.bounds[1]>=PEDESTRIAN_ZONE[0]+18.0 and s.bounds[3]<=-120.0+1e-6, s.bounds   # corridor clear, on paving
        for path_x,path_width in PEDESTRIAN_CONNECTIONS:
            assert s.bounds[2]<path_x-path_width/2-.3 or s.bounds[0]>path_x+path_width/2+.3, (s.bounds,path_x)
        for a,b,w,d in PAVILION_PADS:                      # kiosk, its pad and its terrace (3.6 m in front)
            assert not s.intersects(box(a-2,b-5,a+w+2,b+d)), (s.bounds,a)
        for bx,bz in PALM_BEDS:
            assert not s.intersects(box(bx-PALM_BED/2,bz-PALM_BED/2,bx+PALM_BED/2,bz+PALM_BED/2))
    for i,s in enumerate(shapes):
        for t in shapes[i+1:]:
            assert s.intersection(t).area==0
    return True


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
        for bx,bz in PALM_BEDS:
            assert not box(bx-PALM_BED/2,bz-PALM_BED/2,bx+PALM_BED/2,bz+PALM_BED/2).intersects(shape)
        for earlier in used:
            if shape.intersection(earlier).area>0:
                raise ValueError("Overlapping pavilion slabs "+p["id"])
        used.append(shape)
    return validate_dining()

validate()
