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
