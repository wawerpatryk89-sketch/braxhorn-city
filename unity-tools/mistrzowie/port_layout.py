"""Approved-space premium harbour plan for MIASTO MISTRZÓW (Unity 6 only).

Rebuilt 10.11 for the owner ("cały port jest do zmiany"): a compact yacht marina
(15 yachts alongside finger pontoons), a Riva pontoon (10), a jetski pontoon right at
the promenade quay (10), a short ferry quay and a short cruise quay with two free
berths for the owner's ship models later. No invented ship models.
All dimensions are nominal source metres; the shared game K is applied once
by the Foundation mesh builder.

The original seaside promenade (z=-150..-120) is NOT modified.
"""
import math
from collections import Counter
from shapely.geometry import box
from shapely.ops import unary_union

YACHTS=15
MOTORBOATS=10
JETSKIS=10
CRUISE_BERTHS=2
FERRY_BERTHS=1
SHORE_BACK=-161.0    # immediately seaward of the existing planted grass bed
SEA_LEVEL=-2.0
DRY_SAND="sand_dry"
WET_SAND="sand_wet"
MARINA_TYPES=("yacht","motorboat","jetski")


def smoothstep(t):
    t=max(0.0,min(1.0,t))
    return t*t*(3-2*t)


def shoreline(x):
    """650m long beach; naturally tapering ends and ~63-77 m of sand in the middle.

    Owner 10.11 10:04 (photo of the beach): "takie wąskie ... coś tu nie gra" - the old 110-125 m of empty sand read as a
    desert with the sea far away. A wide premium city beach is ~60-80 m: the water is seen from the city walks.
    Keep BraxChampionsBeachFurnishings.Shore identical."""
    if not 50<=x<=700:
        return -150.0
    blend=smoothstep((x-50)/37)*smoothstep((700-x)/49)
    deep=-231.0+5.0*math.sin(x*.013+0.4)+2.4*math.sin(x*.034+1.1)
    return -163.0+(deep+163.0)*blend


def beach_height(x,z):
    """Natural shore gradient: dry golden high beach -> gently wet sand."""
    shore=shoreline(x)
    if z>=SHORE_BACK:
        return .16
    t=max(0.,min(1.,(SHORE_BACK-z)/(SHORE_BACK-shore)))
    # x-dependent large-scale dune undulation fades to zero at the seams.
    shore_roll=0.034*math.sin(x*.045)*math.sin(z*.082)*math.sin(math.pi*t)
    # Sand below shallow wash, but without a steep underwater step.
    return .16+(-2.10-.16)*(t**1.32)+shore_roll


def pier(x0,x1,z0,z1,role,name):
    # millimetre grid: 6.8 m steps must not leave 1e-13 slivers in the foundation's cell grid
    x0,x1,z0,z1=(round(float(v),3) for v in (x0,x1,z0,z1))
    assert x1>x0 and z1>z0,(name,x0,x1,z0,z1)
    return dict(id=name,x=x0,z=z0,w=round(x1-x0,3),d=round(z1-z0,3),role=role)


def berth(id,kind,x,z,length,width,dock,access_x,access_z,yaw=0.0):
    x,z,access_x,access_z=(round(float(v),3) for v in (x,z,access_x,access_z))
    return dict(id=id,kind=kind,x=float(x),z=float(z),length=float(length),
                width=float(width),dock=dock,accessX=float(access_x),
                accessZ=float(access_z),yaw=float(yaw))


def hull(b):
    """Footprint of a moored hull: the length runs along the bow (yaw 0/180 -> z, 90/270 -> x)."""
    along_x=round(b["yaw"])%180==90
    hx=(b["length"] if along_x else b["width"])/2
    hz=(b["width"] if along_x else b["length"])/2
    return box(b["x"]-hx,b["z"]-hz,b["x"]+hx,b["z"]+hz)


# Owner 10.11 09:01-09:15: "ten port ma być lepiej zrobiony, mniejszy ... jachty mają być dobrze ustawione ... skuterki
# stoją w miejscach niepotrzebnie, to ma być przy brzegu zaraz ... cały port jest do zmiany ... ma zrobić prawidłowo".
# Real marina practice: every boat lies ALONGSIDE its own finger pontoon (finger on one side, the next finger on the
# other), stern a metre off the main pontoon, bow to open water. Real hull sizes of the owner's GLBs after scaling:
# Riva 8.5 x 2.53 m, jetski 3.25 x 1.50 m. Bows point to +z in the boat root (BraxChampionsHarbour).
# Owner 10.11 12:52 ("sprawdzasz globalnie, wszystko w skalę"): the yacht GLB is a real 40.3 m superyacht (8.2 m beam,
# main / upper / bridge / flybridge decks 2.1 / 4.8 / 6.8 / 9.8 m above the waterline). Squeezed to 19.5 m its doors and
# decks were half a person high. It is moored at its REAL size now; the marina is laid out for 40 m yachts.
YACHT_LEN,YACHT_BEAM=40.33,8.6      # beam 8.21 + fenders
YACHT_FINGER_LEN,YACHT_FINGER_W,YACHT_PITCH=30.0,2.0,11.6
CRUISE_B_LEN,CRUISE_B_BEAM=289.4,33.4  # owner's cruise ship B, real size (metres in the GLB)
CRUISE_C_LEN,CRUISE_C_BEAM=123.7,18.2  # owner's 1900 liner C, real size
CRUISE_QUAY_X0,CRUISE_QUAY_X1,CRUISE_QUAY_Z0=1095.0,1120.0,-446.0
RIVA_LEN,RIVA_BEAM=8.5,2.9
SKI_LEN,SKI_BEAM=3.25,1.6
JETSKI_X0,JETSKI_X1=883.0,918.0      # jetski pontoon along the promenade quay, opposite K11 WYNAJEM SPRZĘTU (x 902.5)
JETSKI_RAMP=(866.0,883.0)                # 2 m landing at promenade level, then 15 m down to the pontoon
JETSKI_Z0,JETSKI_Z1=-153.0,-150.0
# Openings in the promenade-edge balustrade (x0,x1): every gangway from the promenade down to a pier or pontoon.
PROMENADE_OPENINGS=[]


def plan():
    piers=[]
    add=lambda *args:piers.append(pier(*args))
    berths=[]
    PROMENADE_OPENINGS.clear()

    # Ferry: compact fixed passenger quay (level with the promenade), the berth alongside its east face.
    add(745,755,-228,-150,"quay_ferry","FERRY_BOARDING_QUAY")
    berths.append(berth("F01","ferry",773,-189,56,16,"FERRY_BOARDING_QUAY",755,-189,yaw=180))
    PROMENADE_OPENINGS.append((745.0,755.0))

    # Yacht marina for real 40 m superyachts: 5 m main pontoon (97 m) from a 20 m gangway; 9 west + 8 east finger
    # pontoons 30 x 2 m every 11.6 m; 8 yachts west (bow west), 7 east (bow east), each alongside its finger 0.5 m off
    # it, stern 1 m off the main pontoon.
    fz=lambda i:-173.0-YACHT_PITCH*i
    spine_end=fz(8)-YACHT_FINGER_W/2
    add(827.5,832.5,-170,-150,"quay_ramp","YACHT_ACCESS_FROM_SHORE")
    add(827.5,832.5,spine_end,-170,"quay_wood","YACHT_MAIN_SPINE")
    PROMENADE_OPENINGS.append((827.5,832.5))
    h=YACHT_FINGER_W/2
    for i in range(9):
        add(827.5-YACHT_FINGER_LEN,827.5,fz(i)-h,fz(i)+h,"quay_wood",f"YACHT_W_FINGER_{i+1}")
    for i in range(8):
        add(832.5,832.5+YACHT_FINGER_LEN,fz(i)-h,fz(i)+h,"quay_wood",f"YACHT_E_FINGER_{i+1}")
    n=0
    for i in range(8):
        z=fz(i)-h-.5-YACHT_BEAM/2
        n+=1;berths.append(berth(f"J{n:02}","yacht",827.5-1-YACHT_LEN/2,z,YACHT_LEN,YACHT_BEAM,
                                 f"YACHT_W_FINGER_{i+1}",823.5,fz(i),yaw=270))
        if i<7:
            n+=1;berths.append(berth(f"J{n:02}","yacht",832.5+1+YACHT_LEN/2,z,YACHT_LEN,YACHT_BEAM,
                                     f"YACHT_E_FINGER_{i+1}",836.5,fz(i),yaw=90))

    # Motorboats (Riva): 4 m main pontoon (30 m) from a 20 m gangway, 6 + 6 fingers 9 x 1.2 m every 5 m.
    add(953.5,957.5,-170,-150,"quay_ramp","MOTOR_ACCESS_FROM_SHORE")
    add(953.5,957.5,-200,-170,"quay_wood","MOTOR_MAIN_PONTOON")
    PROMENADE_OPENINGS.append((953.5,957.5))
    mz=lambda i:-172.5-5.0*i
    for i in range(6):
        add(944.5,953.5,mz(i)-.6,mz(i)+.6,"quay_wood",f"MOTOR_W_FINGER_{i+1}")
        add(957.5,966.5,mz(i)-.6,mz(i)+.6,"quay_wood",f"MOTOR_E_FINGER_{i+1}")
    for i in range(5):
        z=mz(i)-2.5
        berths.append(berth(f"L{i*2+1:02}","motorboat",953.5-.8-RIVA_LEN/2,z,RIVA_LEN,RIVA_BEAM,
                            f"MOTOR_W_FINGER_{i+1}",951.0,mz(i),yaw=270))
        berths.append(berth(f"L{i*2+2:02}","motorboat",957.5+.8+RIVA_LEN/2,z,RIVA_LEN,RIVA_BEAM,
                            f"MOTOR_E_FINGER_{i+1}",960.0,mz(i),yaw=90))

    # Jetskis right at the shore (owner 09:14): a low floating pontoon ALONG the promenade quay wall in front of the
    # rental kiosk, reached by a 15 m ramp running down along the wall; 11 short fingers every 3 m, 10 skis between
    # them, stern 0.4 m off the pontoon, bow to the open sea.
    add(JETSKI_RAMP[0],JETSKI_RAMP[1],JETSKI_Z0,JETSKI_Z1,"quay_ramp_along","JETSKI_RAMP_ALONG_QUAY")
    add(JETSKI_X0,JETSKI_X1,JETSKI_Z0,JETSKI_Z1,"quay_jetski","JETSKI_PONTOON")
    PROMENADE_OPENINGS.append((JETSKI_RAMP[0],JETSKI_RAMP[0]+2.5))
    for i in range(11):
        x=885.0+3.0*i
        add(x-.4,x+.4,-157.5,JETSKI_Z0,"quay_jetski",f"JETSKI_FINGER_{i+1}")
    for i in range(10):
        x=886.5+3.0*i
        berths.append(berth(f"S{i+1:02}","jetski",x,JETSKI_Z0-.4-SKI_LEN/2,SKI_LEN,SKI_BEAM,
                            "JETSKI_PONTOON",x,-151.5,yaw=180))

    # Cruise ships. Owner 10.11 14:46-14:47 (render of ship B at its real 289 m beside the old 12 x 115 m quay): "jakiś
    # taki cienki ... mosty też mają swoją grubość" - the passenger quay is now a real cruise terminal pier: 25 m wide,
    # 296 m long (as long as the ship), a solid concrete edge. W01 east (ship B, real size), W02 west (ship C, the 1900
    # liner, 123.7 x 18.2 m), both 1.0 m off the quay face (on the fenders), stern 2 m off the promenade, bow to the sea.
    add(CRUISE_QUAY_X0,CRUISE_QUAY_X1,CRUISE_QUAY_Z0,-150,"quay_cruise","CRUISE_PASSENGER_QUAY")
    berths.append(berth("W01","cruise_ship",CRUISE_QUAY_X1+1.0+CRUISE_B_BEAM/2,-152.0-CRUISE_B_LEN/2,CRUISE_B_LEN,CRUISE_B_BEAM,
                        "CRUISE_PASSENGER_QUAY",CRUISE_QUAY_X1,-200,yaw=180))
    berths.append(berth("W02","cruise_ship",CRUISE_QUAY_X0-1.0-CRUISE_C_BEAM/2,-152.0-CRUISE_C_LEN/2,CRUISE_C_LEN,CRUISE_C_BEAM,
                        "CRUISE_PASSENGER_QUAY",CRUISE_QUAY_X0,-200,yaw=180))
    PROMENADE_OPENINGS.append((1108.0,1120.0))
    validate(piers,berths)
    return piers,berths


def validate(piers,berths):
    counts=Counter(b["kind"] for b in berths)
    assert counts==Counter(yacht=15,motorboat=10,jetski=10,cruise_ship=2,ferry=1),counts
    assert len({b["id"] for b in berths})==len(berths)==38
    assert len({p["id"] for p in piers})==len(piers)
    # Berths (hulls) must NOT overlap their neighbour or any solid dock.
    shells={b["id"]:hull(b) for b in berths}
    solid=[box(p["x"],p["z"],p["x"]+p["w"],p["z"]+p["d"]) for p in piers]
    for b in berths:
        shape=shells[b["id"]]
        if shape.bounds[0]<730 or shape.bounds[2]>1188 or shape.bounds[1]<-500 or shape.bounds[3]>-150.3:
            raise ValueError("Berth hull out of safe harbour bounds: "+b["id"])
        if any(shape.intersection(g).area>.01 for g in solid):
            raise ValueError("Hull overlaps pier/finger: "+b["id"])
        for other in berths:
            if other["id"]>b["id"] and shape.intersection(shells[other["id"]]).area>.01:
                raise ValueError("Two boats overlap: "+b["id"]+" / "+other["id"])
        if b["kind"] in MARINA_TYPES:
            # alongside its own finger: the hull's long side within 0.6 m of the finger edge, never touching it
            finger=next(g for p,g in zip(piers,solid) if p["id"]==b["dock"])
            gap=shape.distance(finger)
            if not (.05<gap<.7):
                raise ValueError(f"{b['id']} is not alongside {b['dock']} (gap {gap:.2f} m)")
    # Keep the original beach free of ANY yacht, jetski or marine construction.
    if any(p["x"]<730 for p in piers):
        raise ValueError("Pier intrudes into the beach")
    if any(p["z"]+p["d"]>-150+1e-5 for p in piers):
        raise ValueError("Pier intrudes into existing promenade")
    # The navigation channel to the cruise quay stays open: 30 m clear water west of W02.
    marina_east=max(p["x"]+p["w"] for p in piers if p["role"] not in ("quay_cruise",))
    if min(b["x"]-b["width"]/2 for b in berths if b["kind"]=="cruise_ship")-marina_east<30:
        raise ValueError("No separate deep water channel for excursion ships")
    return True


PIERS,BERTHS=plan()


# Owner 10.11 14:56-14:57 ("pomost też z jakiejś tekstury ... żeby wyglądał zajebiście ... a nie tylko beton wylany",
# "most wykończ"): the two fixed passenger quays are finished like the promenade they start from. The deck stays ONE
# surface of two stones cut edge to edge (never one laid on the other): dark flamed granite on the berth aprons (the
# working strip with bollards, fenders and the coping) and the promenade's light vein-cut slabs on the passenger walk.
# Dark granite cross bands (0.8 m) run through the walk at every bed and lamp, a 12 m rhythm.
# On the walk: rows of the owner's thick palms in flush granite-kerbed beds (uplit, like the promenade), teak benches
# facing the ships and the promenade lamps between them; the walk between the rows stays clear.
QUAY_APRON_CRUISE,QUAY_APRON_FERRY=4.5,3.0
QUAY_BED,QUAY_STEP=4.4,24.0
QUAY_BED_FERRY=3.6     # the ferry walk is 7 m wide


QUAY_BAND=0.8           # dark granite cross bands through the walk at every bed and lamp (a 12 m rhythm)


def quay_fit():
    """aprons and cross bands [(x,z,w,d)] (dark granite), beds [(x,z)], furniture [(kind,x,z)] of the fixed quays."""
    aprons,beds,furniture,sizes=[],[],[],[]
    for p in PIERS:
        x0,x1,z0,z1=p["x"],p["x"]+p["w"],p["z"],p["z"]+p["d"]
        if p["role"]=="quay_cruise":
            aprons+=[(x0,z0,QUAY_APRON_CRUISE,z1-z0),(x1-QUAY_APRON_CRUISE,z0,QUAY_APRON_CRUISE,z1-z0)]
            walk=(x0+QUAY_APRON_CRUISE,x1-QUAY_APRON_CRUISE)
            cx=(x0+x1)/2
            # owner 10.11 15:48 ("palmy się ledwo mieszczą w tej trawie albo wystają poza"): 4.4 m beds (were 3.2), the
            # trunk foot on the bed centre (BraxChampionsPalmTrunk); each row faces its own ship; palms 10.2 m apart
            rows=[(cx-5.1,"west"),(cx+5.1,"east")]
            bed=QUAY_BED
            first=z1-40.0                              # the passenger shelter stands at the root (z1-24 .. z1-8)
        elif p["role"]=="quay_ferry":
            aprons.append((x1-QUAY_APRON_FERRY,z0,QUAY_APRON_FERRY,z1-z0))
            walk=(x0,x1-QUAY_APRON_FERRY)
            rows=[(x0+2.5,"east")]                     # the west side is a railing, the ferry lies east
            bed=QUAY_BED_FERRY
            first=z1-20.0
        else:
            continue
        zs=[]
        z=first
        while z-bed/2>=z0+6.0:
            zs.append(z);z-=QUAY_STEP
        lamps=[z for z in [first+QUAY_STEP/2-QUAY_STEP*k for k in range(len(zs)+1)] if z0+3.0<=z<=z1-6.0]
        for z in sorted(zs+lamps):
            aprons.append((walk[0],round(z-QUAY_BAND/2,3),walk[1]-walk[0],QUAY_BAND))
        for x,face in rows:
            for z in zs:
                beds.append((round(x,2),round(z,2)));sizes.append(bed)
                for dz in (-6.0,6.0):
                    furniture.append(("quay_bench_"+face,round(x,2),round(z+dz,2)))
            for z in lamps:
                furniture.append(("lamp",round(x,2),round(z,2)))
    return aprons,beds,furniture,sizes


QUAY_APRONS,QUAY_BEDS,QUAY_FURNITURE,QUAY_BED_SIZES=quay_fit()
QUAY_BED_RECTS=[(x-b/2,z-b/2,b,b) for (x,z),b in zip(QUAY_BEDS,QUAY_BED_SIZES)]
