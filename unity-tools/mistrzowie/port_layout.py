"""Approved-space premium harbour plan for MIASTO MISTRZÓW (Unity 6 only).

One main yacht pier with 15 individual finger berths; a U-shaped motorboat
basin with 10 bays; 10 separately accessible low jetski bays; ferry quay;
two empty passenger-cruise ship positions. No invented cruise-ship models.
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
    """650m long beach; naturally tapering ends and 110-125m central width."""
    if not 50<=x<=700:
        return -150.0
    blend=smoothstep((x-50)/37)*smoothstep((700-x)/49)
    deep=-273.0+7.2*math.sin(x*.013+0.4)+3.3*math.sin(x*.034+1.1)
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
    assert x1>x0 and z1>z0,(name,x0,x1,z0,z1)
    return dict(id=name,x=x0,z=z0,w=x1-x0,d=z1-z0,role=role)


def berth(id,kind,x,z,length,width,dock,access_x,access_z,yaw=0.0):
    return dict(id=id,kind=kind,x=float(x),z=float(z),length=float(length),
                width=float(width),dock=dock,accessX=float(access_x),
                accessZ=float(access_z),yaw=float(yaw))


def plan():
    piers=[]
    add=lambda *args:piers.append(pier(*args))
    berths=[]

    # Jacht marina — one continuous main pier with branching fingers.
    # Access from untouched promenade via 20m sloping timber approach.
    add(826.5,833.5,-410,-170,"quay_wood","YACHT_MAIN_SPINE")
    add(826.5,833.5,-170,-150,"quay_ramp","YACHT_ACCESS_FROM_SHORE")
    yacht_rows=[-193.5-i*27.2 for i in range(8)]
    for i in range(9):
        z=-179.9-27.2*i
        add(792.0,826.5,z-1.0,z+1.0,"quay_wood",f"YACHT_W_FINGER_{i+1}")
        add(833.5,868.0,z-1.0,z+1.0,"quay_wood",f"YACHT_E_FINGER_{i+1}")
    for i,z in enumerate(yacht_rows):
        berths.append(berth(f"J{i*2+1:02}","yacht",809.5,z,21.0,11.0,
                            "YACHT_MAIN_SPINE",809.5,z+13.6,yaw=180))
        if i<7:
            berths.append(berth(f"J{i*2+2:02}","yacht",850.5,z,21.0,11.0,
                                "YACHT_MAIN_SPINE",850.5,z+13.6,yaw=180))
    # Motorówki: connected U opens SOUTH toward the free water, not to land.
    add(911,917,-304,-188,"quay_wood","MOTOR_U_WEST")
    add(999,1005,-304,-188,"quay_wood","MOTOR_U_EAST")
    add(911,1005,-194,-188,"quay_wood","MOTOR_U_BASE")
    add(952,958,-188,-170,"quay_wood","MOTOR_LINK")
    add(952,958,-170,-150,"quay_ramp","MOTOR_ACCESS_FROM_SHORE")
    for i in range(6):
        z=-201-20*i
        add(917,934,z-1,z+1,"quay_wood",f"MOTOR_W_FINGER_{i+1}")
        add(982,999,z-1,z+1,"quay_wood",f"MOTOR_E_FINGER_{i+1}")
    for i in range(5):
        z=-211-20*i
        berths.append(berth(f"L{i*2+1:02}","motorboat",925,z,9.3,3.7,
                            "MOTOR_U_WEST",925,z+10,yaw=180))
        berths.append(berth(f"L{i*2+2:02}","motorboat",991,z,9.3,3.7,
                            "MOTOR_U_EAST",991,z+10,yaw=180))

    # Jetski pontoon: ten individual small bays, away from bathing zone.
    add(1053,1057,-273,-170,"quay_jetski","JETSKI_LOW_MAIN")
    add(1053,1057,-170,-150,"quay_ramp_jetski","JETSKI_ACCESS_FROM_SHORE")
    for i in range(6):
        z=-177.0-17.7*i
        add(1036,1053,z-.75,z+.75,"quay_jetski",f"JETSKI_W_FINGER_{i+1}")
        add(1057,1075,z-.75,z+.75,"quay_jetski",f"JETSKI_E_FINGER_{i+1}")
    for i in range(5):
        z=-185.85-17.7*i
        berths.append(berth(f"S{i*2+1:02}","jetski",1043,z,3.6,1.7,
                            "JETSKI_LOW_MAIN",1043,z+8.85,yaw=180))
        berths.append(berth(f"S{i*2+2:02}","jetski",1067,z,3.6,1.7,
                            "JETSKI_LOW_MAIN",1067,z+8.85,yaw=180))

    # Ferry: compact ticket/passenger quay and a separate large boat berth.
    add(746,754,-368,-150,"quay_ferry","FERRY_BOARDING_QUAY")
    berths.append(berth("F01","ferry",773,-312,56,16,
                        "FERRY_BOARDING_QUAY",755,-312,yaw=180))
    # Cruise ships require independent 80m berths, access gangways and room
    # for two real ship models when supplied; none is fabricated here.
    add(1109,1117,-463,-150,"quay_cruise","CRUISE_PASSENGER_QUAY")
    berths.append(berth("W01","cruise_ship",1139,-273,82,19,
                        "CRUISE_PASSENGER_QUAY",1117,-273,yaw=0))
    berths.append(berth("W02","cruise_ship",1139,-397,82,19,
                        "CRUISE_PASSENGER_QUAY",1117,-397,yaw=0))

    # Passenger gangways with unobstructed pier access, not imaginary ships.
    add(1117,1127,-274,-272,"quay_cruise","CRUISE_GANGWAY_01")
    add(1117,1127,-398,-396,"quay_cruise","CRUISE_GANGWAY_02")
    add(754,764,-313,-311,"quay_ferry","FERRY_GANGWAY")
    validate(piers,berths)
    return piers,berths


def validate(piers,berths):
    counts=Counter(b["kind"] for b in berths)
    assert counts==Counter(yacht=15,motorboat=10,jetski=10,cruise_ship=2,ferry=1),counts
    assert len({b["id"] for b in berths})==len(berths)==38
    assert len({p["id"] for p in piers})==len(piers)
    # Berths (hulls) must NOT overlap their neighbour or any solid dock.
    shells={b["id"]:box(b["x"]-b["width"]/2,b["z"]-b["length"]/2,
                        b["x"]+b["width"]/2,b["z"]+b["length"]/2) for b in berths}
    solid=[box(p["x"],p["z"],p["x"]+p["w"],p["z"]+p["d"]) for p in piers]
    for b in berths:
        shape=shells[b["id"]]
        if shape.bounds[0]<730 or shape.bounds[2]>1188 or shape.bounds[1]<-500 or shape.bounds[3]>-168:
            raise ValueError("Berth hull out of safe harbour bounds: "+b["id"])
        if any(shape.intersection(g).area>.01 for g in solid):
            raise ValueError("Hull overlaps pier/finger: "+b["id"])
        for other in berths:
            if other["id"]>b["id"] and shape.intersection(shells[other["id"]]).area>.01:
                raise ValueError("Two boats overlap: "+b["id"]+" / "+other["id"])
    # Keep the original beach free of ANY yacht, jetski or marine construction.
    if any(p["x"]<730 for p in piers):
        raise ValueError("Pier intrudes into the beach")
    if any(p["z"]+p["d"]>-150+1e-5 for p in piers):
        raise ValueError("Pier intrudes into existing promenade")
    # The west-to-east main navigation corridor remains open between ship pier
    # and all mooring clusters for a minimum 30m.
    if min(b["x"]-b["width"]/2 for b in berths if b["kind"]=="cruise_ship")-1075<30:
        raise ValueError("No separate deep water channel for excursion ships")
    return True


PIERS,BERTHS=plan()
