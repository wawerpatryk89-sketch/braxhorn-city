"""Current room plan in Unity X/Z metres; technical drawing, not a game screenshot.

Coordinates match BraxGymReferenceRebuild.cs and BraxGymReferenceEquipment.cs.
The original ring/stairs and city doorway retain their source geometry positions.
"""
from html import escape
from pathlib import Path
import re
R = Path(__file__).resolve().parents[2]
source = (R / 'unity-city/Assets/Editor/BraxGymReferenceRebuild.cs').read_text()

def numbers(value): return tuple(float(x.strip().removesuffix('f')) for x in value.split(','))
mat_match = re.search(r'const float x0 = ([\d.\-]+)f, x1 = ([\d.\-]+)f, z0 = ([\d.\-]+)f, z1 = ([\d.\-]+)f;', source)
mat = tuple(map(float, mat_match.groups()))
billboard_source = source.split('void BuildBillboardMount()', 1)[1].split('void BuildAnnex()', 1)[0]
billboard_center = numbers(re.search(r'var center = new Vector3\(([^)]+)\)', billboard_source).group(1))
billboard_size = numbers(re.search(r'"Billboard recessed backing", center, new Vector3\(([^)]+)\)', billboard_source).group(1))
doors = {m.group(1): numbers(','.join(m.groups()[1:])) for m in re.finditer(
    r'Door\("(gym-[^"]+)", "[^"]+", ([\d.\-]+)f, ([\d.\-]+)f, ([\d.\-]+)f, ([\d.\-]+)f\)', source)}
out = R / 'unity-city/Docs/AI/KickboxingLayout.svg'
out.parent.mkdir(parents=True, exist_ok=True)
parts = ['''<svg xmlns="http://www.w3.org/2000/svg" width="1440" height="1040" viewBox="0 0 1440 1040">
<defs><pattern id="grid" width="24" height="24" patternUnits="userSpaceOnUse"><path d="M24 0H0V24" fill="none" stroke="#ffffff" stroke-opacity=".035"/></pattern><marker id="arrow" markerWidth="8" markerHeight="8" refX="6" refY="3" orient="auto"><path d="M0 0L6 3L0 6" fill="none" stroke="#8bff3a" stroke-width="1.4"/></marker></defs>
<rect width="1440" height="1040" fill="#101413"/>
<g font-family="DejaVu Sans,Arial,sans-serif">
<text x="92" y="62" font-size="30" fill="#f2f5ef" font-weight="700">BRAXHORN KICKBOXING</text>
<text x="92" y="97" font-size="18" fill="#b3c0b5">ETAP A · Aktualny rzut sceny — sala + zaplecze</text>
<text x="92" y="128" font-size="14" fill="#94a799">Współrzędne projektu Unity; widok od góry, główne wejście na dole. Skala: 24 px = 1 m.</text>''']
def xy(x,z): return (650 - 24*x, 170 + 24*(z+13))
def rect(x0,x1,z0,z1,fill,stroke='#47524b',width=2):
    a,b=xy(x1,z0);c,d=xy(x0,z1)
    parts.append(f'<rect x="{a}" y="{b}" width="{c-a}" height="{d-b}" fill="{fill}" stroke="{stroke}" stroke-width="{width}"/>')
def text(x,z,label,size=15,color='#e4ede2',anchor='middle'):
    a,b=xy(x,z);parts.append(f'<text x="{a}" y="{b}" text-anchor="{anchor}" font-size="{size}" fill="{color}">{escape(label)}</text>')
def line(x0,z0,x1,z1,color='#8bff3a',width=2,dash=''):
    a,b=xy(x0,z0);c,d=xy(x1,z1)
    parts.append(f'<path d="M{a} {b}L{c} {d}" fill="none" stroke="{color}" stroke-width="{width}" stroke-dasharray="{dash}"/>')
def doorway(key,number):
    x,z0,z1,height=doors[key]
    # Closed leaf and dashed double-action swing: the existing door opens away
    # from the approaching player, rather than having one fixed opening side.
    line(x,z0,x,z1,'#b9e2d5',2)
    radius=(z1-z0)*24;a,b=xy(x,z1)
    for sign in (-1,1):
        parts.append(f'<path d="M{a} {b-radius}A{radius} {radius} 0 0 {1 if sign>0 else 0} {a+sign*radius} {b}" stroke="#a2bcaa" stroke-opacity=".65" stroke-width="1" stroke-dasharray="3 3" fill="none"/>')
    tx=x+.9 if number==1 else x-.85
    a,b=xy(tx,(z0+z1)/2)
    parts.append(f'<circle cx="{a}" cy="{b}" r="9" fill="#8bff3a"/><text x="{a}" y="{b+4}" text-anchor="middle" font-size="12" font-weight="700" fill="#101413">{number}</text>')
rect(-18,18,-13,11,'#242b27','#e3eadf',4)
rect(-18,18,-13,11,'url(#grid)','none',0)
rect(-25.5,-18,-3,8,'#1b271e','#8bff3a',2)
rect(-25.2,-22.25,1.79,7.7,'#36443a','none',0)
rect(-25.2,-22.25,-2.7,1.61,'#3b4541','none',0)
# Actual full-height wall footprints; lintels are omitted from this floor plan.
rect(-25.5,-25.2,-3,8,'#bcc7ba','none',0)
rect(-25.5,-18.6,7.7,8,'#bcc7ba','none',0)
rect(-25.5,-18.6,-3,-2.7,'#bcc7ba','none',0)
rect(-18.6,-18,-3,4.2,'#bcc7ba','none',0)
rect(-18.6,-18,6,8,'#bcc7ba','none',0)
rect(-18.6,-17.9,4.2,6,'#1b271e','none',0)
rect(-20.54,-20.36,-2.7,4.2,'#bcc7ba','none',0)
rect(-20.54,-20.36,6,7.7,'#bcc7ba','none',0)
rect(-25.2,-22.15,1.61,1.79,'#bcc7ba','none',0)
for z0,z1 in [(-2.7,-1.75),(-.55,2.8),(4,7.7)]:
    rect(-22.25,-22.1,z0,z1,'#bcc7ba','none',0)
# Eight lockers and the 2.8 m bench at their actual scene locations.
for i in range(8):
    z=2.28+.70*i
    rect(-25.18,-24.66,z-.33,z+.33,'#18201b','#849181',.8)
rect(-22.91,-22.43,4.4,7.2,'#6a624b','#aa9c76',1)
for z in [4.68,5.35,6.02,6.69]:
    line(-22.29,z,-22.44,z,'#a5b2a0',1.5)
text(-23.65,2.48,'SZATNIA',12)
text(-23.65,6.15,'1,73 m',10,'#c0cfb9')
text(-23.65,6.75,'przejścia',9,'#a7b6a3')
text(-23.7,.1,'WC',15)
# Sanitary fixture centres and collision footprints, including mirror above sink.
cx,cy=xy(-24.4,-1.92);parts.append(f'<ellipse cx="{cx}" cy="{cy}" rx="{.34*24}" ry="{.45*24}" fill="#dce3db"/>')
rect(-25.17,-24.59,.11,.79,'#dce3db','#9bb1aa',1)
cx,cy=xy(-24.88,.45);parts.append(f'<ellipse cx="{cx}" cy="{cy}" rx="4" ry="5" fill="#8ca29c"/>')
line(-25.13,.105,-25.13,.795,'#a5d3d5',2.5)
text(-21.15,-3.75,'KORYTARZ · 1,56 m',12,'#8bff3a')
a,b=xy(-16.8,5.1);c,d=xy(-21.25,5.1);e,f=xy(-21.25,-1.15);g,h=xy(-23.35,-1.15)
parts.append(f'<path d="M{a} {b}H{c}V{f}H{g}" stroke="#8bff3a" stroke-width="2" stroke-dasharray="6 4" fill="none" marker-end="url(#arrow)"/>')
a,b=xy(-21.25,3.4);c,d=xy(-23.35,3.4)
parts.append(f'<path d="M{a} {b}H{c}" stroke="#8bff3a" stroke-width="2" stroke-dasharray="6 4" fill="none" marker-end="url(#arrow)"/>')
for key,number in [('gym-backrooms',1),('gym-locker',2),('gym-wc',3)]: doorway(key,number)
# Main mat uses the current C# bounds, so its plan cannot drift silently.
rect(*mat,'#202821','#8bff3a',3)
x0,x1,z0,z1=mat
for n in range(1,int(x1-x0)+1):
    if x0+n<x1:line(x0+n,z0,x0+n,z1,'#384438',.6)
for n in range(1,int(z1-z0)+1):line(x0,z0+n,x1,z0+n,'#384438',.6)
text(1.2,1.0,'MATA / SPARING',16)
text(1.2,4.45,'BRAXHORN',24)
text(1.2,5.4,'KICKBOXING',18,'#8bff3a')
text(1.2,7.2,f'{x1-x0:.1f} × {z1-z0:.1f} m · wolna powierzchnia'.replace('.',','),13,'#b5c5b0')
# Existing ring kept in its measured position.
rect(-13.2,-4.8,-9.7,-1.3,'#151b18','#859280',3)
rect(-12.45,-5.55,-8.95,-2.05,'#303a32','#8bff3a',3)
for x in [-12.5,-5.5]:
    for z in [-9,-2]:
        cx,cy=xy(x,z);parts.append(f'<circle cx="{cx}" cy="{cy}" r="6" fill="#8bff3a"/>')
text(-9,-5.5,'RING',21)
text(-9,-4.35,'podest 8,4 × 8,4 m',12,'#b5c5b0')
for z0,z1 in [(-1.3,-.84),(-.84,-.38)]: rect(-9.85,-8.15,z0,z1,'#636a5b','#a8b49e',1)
text(-10.05,.55,'schodki',12)
# Mirror spans 14.5 m, centred at (17.86, -3.25).
rect(17.83,17.89,-10.5,4,'#a5d3d5','#d6f3ed',2)
line(14,-10.15,14,4.15,'#8c9a8d',3,'5 4')
for z in [-10,4]:rect(13.90,14.10,z-.10,z+.10,'#c2ccc0','#c2ccc0',1)
for z in [-8.4,-5.4,-2.4,.6]:
    cx,cy=xy(14,z)
    parts.append(f'<circle cx="{cx}" cy="{cy}" r="{.275*24}" fill="#151a17" stroke="#8bff3a" stroke-width="1.5"/>')
text(13.9,5.05,'4 WORKI',14)
text(13.9,6.0,'Ø 0,55 m · rozstaw 3 m',10,'#a1b29f')
a,b=xy(18.9,-3);parts.append(f'<text x="{a}" y="{b}" fill="#b5d8db" font-size="13" transform="rotate(-90 {a} {b})" text-anchor="middle">LUSTRO — ODBICIE POSTACI NA ŻYWO</text>')
bx,by,bz=billboard_center;bw,bh,bd=billboard_size
rect(bx-bw/2,bx+bw/2,bz-bd/2,bz+bd/2,'#8bff3a','#8bff3a',2)
text(bx,-11.5,'BILLBOARD · BRAX + NYXA',15)
text(bx,-10.65,f'oprawa {bw:g} × {bh:g} m · grafika zatwierdzana osobno',11,'#a8b8a4')
line(-17.89,-2.4,-17.89,2.4,'#8bff3a',1.5)
a,b=xy(-17.2,0);parts.append(f'<text x="{a}" y="{b}" fill="#a6bc9d" font-size="11" transform="rotate(-90 {a} {b})" text-anchor="middle">LOGO BRAXHORN</text>')
rect(-4.375,-.825,9.18,10.12,'#6a624b','#8bff3a',2)
text(-2.6,9.88,'RECEPCJA',11)
rect(-16.125,-13.675,8.46,8.94,'#6a624b','#aa9c76',1)
text(-14.9,10,'ŁAWKA 2,45 m',11)
rect(5.2,6.8,10.85,11.15,'#101413','none',0)
a,b=xy(6,13.2);c,d=xy(6,10.4);parts.append(f'<path d="M{a} {b}L{c} {d}" stroke="#8bff3a" stroke-width="3" marker-end="url(#arrow)"/>')
text(6,14.7,'ISTNIEJĄCE WEJŚCIE Z MIASTA',15)
text(6,15.8,'położenie zachowane',12,'#a9b8a5')
text(0,-13.75,'36 m · pełna istniejąca szerokość sali',14,'#aebda9')
text(-21.75,10.0,'ZAPLECZE',14,'#8bff3a')
text(-21.75,11.1,'7,5 × 11 m brutto',13)
parts.append('''<path d="M92 885H1348" stroke="#3c473e"/>
<circle cx="105" cy="918" r="6" fill="#8bff3a"/><text x="126" y="923" font-size="16" fill="#e6eee1">Dojście: sala → 1. zaplecze → korytarz z zakrętem → 2. szatnia / 3. toaleta.</text>
<circle cx="105" cy="952" r="6" fill="#8bff3a"/><text x="126" y="957" font-size="16" fill="#e6eee1">Światło otworów: 1 = 1,80 m; 2 i 3 = 1,20 m. Pełna ściana osłonowa między salą a WC.</text>
<text x="92" y="989" font-size="13" fill="#a7b6a3">Zielona linia przerywana: dojście; szara: belka nad workami. Łuki: dwustronne drzwi. Zaplecze poza obrysem sali.</text>
<text x="92" y="1017" font-size="13" fill="#8e9f90">To rzut techniczny aktualnej sceny Unity, nie zrzut z gry ani potwierdzenie zaliczenia testów przejść.</text></g></svg>''')
out.write_text('\n'.join(parts))
print(out)
