"""Single, metre-based civil layout shared by terrain, transit and validation."""
X_ROADS=[70,400,800,1130]
Z_ROADS=[-70,70,330,650,935]
TRAM_CORNERS=[(400,330),(1130,330),(1130,935),(70,935),(70,650),(400,650)]
TRAM_STOPS=[('TR01',460,330,'x'),('TR02',400,500,'z'),('TR03',125,650,'x'),('TR04',70,800,'z'),('TR05',220,935,'x'),('TR06',620,935,'x'),('TR07',1130,800,'z'),('TR08',1130,510,'z'),('TR09',960,330,'x'),('TR10',650,330,'x')]
BUS_STOPS=[('U01',70,240,'z'),('U02',70,500,'z'),('U03',70,845,'z'),('U04',685,935,'x'),('U05',1130,845,'z'),('U06',1130,555,'z'),('U07',1130,180,'z'),('U08',650,70,'x'),('U09',455,70,'x'),('U10',400,550,'z'),('U11',855,650,'x'),('U12',705,330,'x'),('U13',170,-70,'x'),('U14',470,-70,'x'),('U15',780,-70,'x'),('U16',1080,-70,'x')]
# Existing city station dimensions are already world units; never multiply its root by K.
import numpy as np
K=float(np.float32(2.33)/np.float32(1.80))
PAVING_Y=.16
# Five accessible station destinations. Keep the original three stations
# exactly where the successful Champions APK placed them.
METRO=[
 dict(id='M1',name='DWORZEC',x=400,z=359,axis='x'),
 dict(id='M2',name='CENTRUM',x=765,z=545,axis='z'),
 dict(id='M3',name='ARENY / HOTELE',x=765,z=800,axis='z'),
 dict(id='M4',name='SPORT ZACHÓD',x=235,z=645,axis='z'),
 dict(id='M5',name='GALERIA WSCHODNIA',x=965,z=245,axis='x'),
]
# Physically continuous single route, not a claim of operating train service.
# Platform tangents match all five reused station prefabs; no level crossings.
# Order in the line: M5 -> M1 -> M4 -> M3 -> M2.
METRO_LINE_WAYPOINTS=[
 # M5 gallery -> M1 dworzec: cross east-west via X800, outside G2, P1,
 # T11 and their ground/basement reservations. Avoid the prior line through
 # the underground casino garage at T11 (x=495,z=465).
 (1130,245),(800,245),(800,359),(235,359),
 # M4 west sports -> M3 and M2 north-east: all 90deg curves have
 # clearance before each actual source metro portal.
 (235,975),(765,975),(765,380),
]
METRO_ROUTE_ORDER=('M5','M1','M4','M3','M2')
def station_point(m,x,z):
 u,v=(x-22.5)/K,(z-28.5)/K
 return (m['x']+u,m['z']+v) if m['axis']=='x' else (m['x']-v,m['z']+u)
ENTRANCE_HOLES=[];ENTRANCE_PATHS=[]
for m in METRO:
 m['hallX'],m['hallZ']=m['x'],m['z']
 m['entryX'],m['entryZ']=station_point(m,66.5,34.3)
 pts=[station_point(m,x,z) for x in (66.5,76.7) for z in (32.6,36.0)]
 a,b=min(p[0] for p in pts),min(p[1] for p in pts)
 w,d=max(p[0] for p in pts)-a,max(p[1] for p in pts)-b
 ENTRANCE_HOLES.append((a,b,w,d));ENTRANCE_PATHS.append((a-3,b-3,w+6,d+6))
 # Connect the new stations to a nearby real street, not a 500m strip of
 # paving through unrelated property reservations.
 if m['id']=='M4':
  ENTRANCE_PATHS.append((a-1,667,w+2,b-667+4))  # road Z650, north sidewalk
 elif m['id']=='M5':
  ENTRANCE_PATHS.append((a+w-1,b-.5,1113-(a+w-1),d+1))  # road X1130
 elif m['axis']=='x':
  ENTRANCE_PATHS.append((417,b-3,a-417+1,d+6))
 else:
  ENTRANCE_PATHS.append((a-1,b-3,783-a+1,3))
METRO_PLATFORM_Y=(-11.2+PAVING_Y*K-.15)/K
METRO_RAIL_Y=METRO_PLATFORM_Y-1.1
METRO_TRACK_OFFSET=7.6/K
TRAM_TRACK_OFFSET=3.0
GAUGE=1.435
# Ground-level approaches to the first two finished exterior plots. No paving in tree beds.
# Owner 10.11 08:36 ("tam gdzie są palmy, nie lepiej trawnik? to wejście dla ludzi, nie dla samochodu"): the A1 walk
# from the Z650 sidewalk to the doors is a 10 m pedestrian boulevard (was 30 m of paving that read as a road); lawn either side.
BUILDING_ACCESS=[(595,667,10,58),(817,805,8,20),(710,790,73,12)]
# A1 side car parks are centred at world X535/X665. Their 6.4m vehicle lanes
# must cross ONLY the north sidewalk of Z650 (world z660..667). These two
# rectangles are cut out of foundation paving and replaced by graded asphalt
# meshes from the city-road surface to the arena parking level.
# 9.11 owner ("parking mały dla zawodników"): the private athletes' car park
# behind A1 gets one 6.4m driveway across the SOUTH sidewalk of Z935
# (world z918..925) at world x577.8..584.2 (A1 local x -22.2..-15.8), clear
# of reserved tram stop TR06 (x620) and bus stop U04 (x685).
A1_PARKING_ENTRANCES=[(531.8,660.0,6.4,7.0),(661.8,660.0,6.4,7.0),(577.8,918.0,6.4,7.0)]
EXTRA_CROSSINGS=[(800,815,'z')]

# Owner 10.11 15:25-15:31 ("apartamentowce mają parkingi zrobione?" -> chose "Parking naziemny"): every apartment plot
# T04-T07 gets a residents' car park beside the building (BraxChampionsApartments.Parking: 20 bays 2.5 x 4.75 m along a
# 5.8 m aisle) and a two-way drive straight to its road. Plot-local metres, front = -z, x mirrored by `side`
# (the side away from bus stops: T04 east, because bus stop U11 stands at x 852-859 on its west). The drive crosses the
# city lawn and the sidewalk; at the road its kerbs turn in 3 m arcs (ZASADA-DROGI-NAROZNIKI-ZAOKRAGLONE 10.11) and the
# city kerb is open across the mouth. Must match the C# constants (test_apartment_parking_20261110.py).
APT_PARKING={'T04':dict(x=870,z=570,yaw=180,walk=-63.2,side=-1),'T05':dict(x=1040,z=570,yaw=180,walk=-63.2,side=1),
             'T06':dict(x=870,z=450,yaw=90,walk=-53.2,side=1),'T07':dict(x=1040,z=450,yaw=270,walk=-73.2,side=1)}
import math
APT_PARK_KERB,APT_AISLE_X0,APT_AISLE_X1,APT_DRIVE_R=.25,19.0,24.8,3.0
APT_PLOT_HALF=30.0
def apt_world(p,x,z):
 """plot-local (x mirrored by side, z) -> world, Unity yaw (x cos + z sin, -x sin + z cos)."""
 x*=p['side'];a=math.radians(p['yaw']);c,s=round(math.cos(a)),round(math.sin(a))
 return (p['x']+x*c+z*s,p['z']-x*s+z*c)
def apt_rect(p,x0,x1,z0,z1):
 pts=[apt_world(p,x,z) for x in (x0,x1) for z in (z0,z1)]
 a,b=min(q[0] for q in pts),min(q[1] for q in pts)
 return (a,b,max(q[0] for q in pts)-a,max(q[1] for q in pts)-b)
APT_DRIVE_CUTS=[];APT_DRIVE_MOUTHS=[]
for _id,_p in APT_PARKING.items():
 _inner,_road=_p['walk']+.2,_p['walk']-6.8          # sidewalk inner edge / road edge (plot-local z)
 # city lawn between the plot and the sidewalk (kerbs + asphalt), then the whole sidewalk across the mouth
 APT_DRIVE_CUTS.append(apt_rect(_p,APT_AISLE_X0-APT_PARK_KERB,APT_AISLE_X1+APT_PARK_KERB,_inner,-APT_PLOT_HALF))
 APT_DRIVE_CUTS.append(apt_rect(_p,APT_AISLE_X0-APT_DRIVE_R,APT_AISLE_X1+APT_DRIVE_R,_road,_inner))
 _a,_b=apt_world(_p,APT_AISLE_X0-APT_DRIVE_R,_road),apt_world(_p,APT_AISLE_X1+APT_DRIVE_R,_road)
 APT_DRIVE_MOUTHS.append((_id,min(_a[0],_b[0]),min(_a[1],_b[1]),max(_a[0],_b[0]),max(_a[1],_b[1])))

# Owner 10.11 15:25-15:26 (Hotel Sobczak: "do teraz parkingu nie ma ... nie ma nic na później"): the B1 garage of T03
# (BraxChampionsHotelSobczakT03.Garage, 60 bays). Hotel-local rects (centre 1030,710, no rotation) opened in the
# foundation: the ramp trench beside the podium, the drop-off lane extended to the ramp, the stair + lift core under the
# pavilion. Must match the C# constants (test_hotel_garage_20261110.py).
HOTEL_CENTRE=(1030.0,710.0)
HOTEL_RAMP_WALLS=(21.9,29.5)
HOTEL_GARAGE_PORTAL_Z=.5
HOTEL_CORE_OUTER=(-29.5,-23.4,-8.3,1.1)
def _hotel_rect(x0,x1,z0,z1):
 return (HOTEL_CENTRE[0]+x0,HOTEL_CENTRE[1]+z0,x1-x0,z1-z0)
HOTEL_GARAGE_CUTS=[_hotel_rect(HOTEL_RAMP_WALLS[0],HOTEL_RAMP_WALLS[1],-30.0,HOTEL_GARAGE_PORTAL_Z),   # ramp trench
                   _hotel_rect(16.0,HOTEL_RAMP_WALLS[1]+.18,-34.18,-30.0),                            # lane + kerbs
                   _hotel_rect(*HOTEL_CORE_OUTER)]                                                    # stair + lift core
