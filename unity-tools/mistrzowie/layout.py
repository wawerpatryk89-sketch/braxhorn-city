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
METRO=[dict(id='M1',name='DWORZEC',x=400,z=359,axis='x'),dict(id='M2',name='CENTRUM',x=765,z=545,axis='z'),dict(id='M3',name='ARENY / HOTELE',x=765,z=800,axis='z')]
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
 if m['axis']=='x':ENTRANCE_PATHS.append((417,b-3,a-417+1,d+6))
 else:ENTRANCE_PATHS.append((a-1,b-3,783-a+1,3))
METRO_PLATFORM_Y=(-11.2+PAVING_Y*K-.15)/K
METRO_RAIL_Y=METRO_PLATFORM_Y-1.1
METRO_TRACK_OFFSET=7.6/K
TRAM_TRACK_OFFSET=3.0
GAUGE=1.435
# Ground-level approaches to the first two finished exterior plots. No paving in tree beds.
BUILDING_ACCESS=[(585,667,30,58),(817,805,8,20),(710,790,73,12)]
EXTRA_CROSSINGS=[(800,815,'z')]
