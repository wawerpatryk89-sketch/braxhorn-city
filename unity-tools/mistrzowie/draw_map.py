"""Exact vector plan from the same layout used by the Unity geometry generator."""
from pathlib import Path
import json,sys,html
from shapely.geometry import LineString
from layout import *
ROOT=Path(__file__).resolve().parents[2]
d=json.loads((ROOT/'unity-city/Assets/Source/Mistrzowie/Infrastructure.json').read_text())
f=json.loads((ROOT/'unity-city/Assets/Source/Mistrzowie/Foundation.json').read_text())
out=ROOT/'docs/zlecenia/mistrzowie-podstawa.svg'
parts=['<svg xmlns="http://www.w3.org/2000/svg" width="1440" height="1800" viewBox="0 0 1440 1800">','<rect width="1440" height="1800" fill="#f4f2ec"/>','<style>text{font-family:DejaVu Sans,Arial,sans-serif;fill:#142d3d}.small{font-size:10px}.label{font-size:14px;font-weight:bold}</style>']
def rect(x,y,w,h,color):parts.append(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{color}"/>')
def pt(x,z):return x+100,1200-z
def text(x,y,t,size=13,color='#142d3d'):parts.append(f'<text x="{x}" y="{y}" font-size="{size}" fill="{color}">{html.escape(t)}</text>')
def line(points,color,width=3,dash=None):
 points=LineString(points).simplify(1e-7).coords
 s=' '.join(f'{x+100:.2f},{1200-z:.2f}' for x,z in points)
 parts.append(f'<polyline points="{s}" fill="none" stroke="{color}" stroke-width="{width}"'+(f' stroke-dasharray="{dash}"' if dash else '')+'/>')
text(100,65,'MIASTO MISTRZÓW',36);text(100,98,'Metro i podstawa · pierwsze budynki · plan zgodny z geometrią źródłową',17)
rect(100,200,1200,1500,'#83b7bf');rect(100,200,1200,1110,'#d6dfd2')
rect(150,1350,650,130,'#e7d2a6');rect(150,1320,1100,30,'#eee2ce')
for x in X_ROADS:rect(x+83,265,34,1005,'#edeeeb');rect(x+90,265,20,1005,'#707a7e')
for z in Z_ROADS:rect(153,1200-z-17,1094,34,'#edeeeb');rect(170,1200-z-10,1060,20,'#707a7e')
rect(100,853,70,34,'#edeeeb');rect(100,860,70,20,'#707a7e')
for b in f['buildings']:
 rect(b['x']-b['w']/2+100,1200-b['z']-b['d']/2,b['w'],b['d'],'#c3a46d' if b['id'] in ['A1','T01'] else '#bbc6c9')
 x,y=pt(b['x'],b['z']);text(x-b['w']/2+4,y,b['id'],11)
 if b['id']=='A1':text(x-85,y+20,'CHAMPION · PÓŁNOC',12)
 if b['id']=='A2':text(x-85,y+20,'CHAMPION · POŁUDNIE',12)
for p in f['plants']:
 x,y=pt(p['x'],p['z']);parts.append(f'<circle cx="{x:.2f}" cy="{y:.2f}" r="2.3" fill="#52775b"/>')
for x,z,w,h in ENTRANCE_PATHS+BUILDING_ACCESS:rect(x+100,1200-z-h,w,h,'#f8f6eb')
for x,z,w,h in ENTRANCE_HOLES:rect(x+100,1200-z-h,w,h,'#172e42')
line(d['tramRoute'],'#d47625',4)
line(d['metroRoute'],'#2452b3',4,'9 5')
for sid,x,z,axis in TRAM_STOPS:
 a,b=pt(x,z);parts.append(f'<circle cx="{a}" cy="{b}" r="5" fill="#fff" stroke="#d47625" stroke-width="2"/>');text(a+8,b-6,sid,11)
for sid,x,z,axis in BUS_STOPS:
 a,b=pt(x,z);rect(a-3,b-3,6,6,'#203e49');text(a+7,b+12,sid,10)
for m in METRO:
 x,y=pt(m['x'],m['z']);parts.append(f'<circle cx="{x}" cy="{y}" r="10" fill="#2452b3" stroke="#fff" stroke-width="3"/>')
 text(x+15,y-13,m['id']+' '+m['name'],14)
 ex,ez=m['entryX'],m['entryZ'];line([(m['x'],m['z']),(ex,ez)],'#2452b3',1)
for x,z,w,h in [(740,-440,10,290),(1150,-440,10,290),(740,-440,260,10),(1080,-440,80,10)]+[(750,z,290,5) for z in [-205,-315,-390]]:
 rect(x+100,1200-z-h,w,h,'#e6dfd1')
for b in f['berths']:
 x,y=pt(b['x']-b['width']/2,b['z']+b['length']/2)
 parts.append(f'<rect x="{x:.2f}" y="{y:.2f}" width="{b["width"]}" height="{b["length"]}" fill="none" stroke="#e8f2ed" stroke-width="1"/>')
text(280,1410,'PLAŻA · 650 m',23);text(460,1339,'PROMENADA · 1100 m',17);text(915,1683,'MARINA · 80 miejsc',19)
text(100,149,'M1–M3: istniejące stacje miasta, wspólna skala, połączenia bez ślepych końców',14)
text(100,174,'Niebieska przerywana: metro  ·  Pomarańczowa: tramwaj  ·  U01–U16: pary przystanków autobusowych',12)
text(100,1740,'Złote: A1 + T01 w kodzie. Szare: rezerwacje. Północ u góry. Jednostki nominalne × K = 2.33 / 1.80.',13)
text(100,1770,'To rysunek układu, nie render z Unity. Wygląd, kolizje w Unity i test telefonu wymagają osobnej kontroli.',13)
parts.append('</svg>');out.write_text('\n'.join(parts));print(out)
