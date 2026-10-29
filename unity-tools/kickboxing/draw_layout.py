"""Dimensioned stage-A plan from measured existing Unity bounds (not a game screenshot)."""
from pathlib import Path
R = Path(__file__).resolve().parents[2]
out = R / 'unity-city/Docs/AI/KickboxingLayout.svg'
out.parent.mkdir(parents=True, exist_ok=True)
parts = ['''<svg xmlns="http://www.w3.org/2000/svg" width="1440" height="1040" viewBox="0 0 1440 1040">
<defs><pattern id="grid" width="24" height="24" patternUnits="userSpaceOnUse"><path d="M24 0H0V24" fill="none" stroke="#ffffff" stroke-opacity=".035"/></pattern><marker id="arrow" markerWidth="8" markerHeight="8" refX="6" refY="3" orient="auto"><path d="M0 0L6 3L0 6" fill="none" stroke="#8bff3a" stroke-width="1.4"/></marker></defs>
<rect width="1440" height="1040" fill="#101413"/>
<g font-family="DejaVu Sans,Arial,sans-serif">
<text x="92" y="62" font-size="30" fill="#f2f5ef" font-weight="700">BRAXHORN KICKBOXING</text>
<text x="92" y="97" font-size="18" fill="#b3c0b5">ETAP A · Rzut do realizacji — istniejąca sala + dobudowane zaplecze</text>
<text x="92" y="128" font-size="14" fill="#94a799">Wymiary sali odczytane z projektu Unity. Widok od góry; wejście na dole.</text>''']
def xy(x,z): return (650 - 24*x, 170 + 24*(z+13))
def rect(x0,x1,z0,z1,fill,stroke='#47524b',width=2):
    a,b=xy(x1,z0);c,d=xy(x0,z1)
    parts.append(f'<rect x="{a}" y="{b}" width="{c-a}" height="{d-b}" fill="{fill}" stroke="{stroke}" stroke-width="{width}"/>')
def text(x,z,label,size=15,color='#e4ede2',anchor='middle'):
    a,b=xy(x,z);parts.append(f'<text x="{a}" y="{b}" text-anchor="{anchor}" font-size="{size}" fill="{color}">{label}</text>')
rect(-18,18,-13,11,'#242b27','#e3eadf',4)
rect(-18,18,-13,11,'url(#grid)','none',0)
rect(-25.5,-18,-3,8,'#293828','#8bff3a',4)
rect(-25.25,-22.15,2,7.75,'#36443a')
rect(-25.25,-22.15,-2.75,1.7,'#3b4541')
rect(-22.15,-20.2,-2.75,7.75,'#1b271e')
rect(-20.2,-18,4.2,6,'#1b271e','none',0)
# Opaque screen keeps the toilet outside every direct hall sight line.
rect(-20.32,-20.08,-3,4.15,'#bcc7ba','none',0)
rect(-22.25,-22.05,-3,1.1,'#bcc7ba','none',0)
rect(-22.25,-22.05,2.4,8,'#bcc7ba','none',0)
rect(-18.12,-17.88,4.2,6,'#1b271e','none',0)
rect(-22.25,-22.05,-.85,.45,'#1b271e','none',0)
rect(-22.25,-22.05,4,5.35,'#1b271e','none',0)
rect(-24.95,-24.45,2.8,7.1,'#18201b','#849181',1)
rect(-24.05,-23.55,3.4,6.3,'#6a624b','#aa9c76',1)
text(-23.7,2.65,'SZATNIA',12)
text(-23.7,1.0,'WC',14)
cx,cy=xy(-24.25,-1.55);parts.append(f'<ellipse cx="{cx}" cy="{cy}" rx="9" ry="12" fill="#dce3db"/>')
text(-21.2,-3.7,'KORYTARZ',12,'#8bff3a')
a,b=xy(-17,5.1);c,d=xy(-21.2,5.1);e,f=xy(-21.2,-.15)
parts.append(f'<path d="M{a} {b}H{c}V{f}" stroke="#8bff3a" stroke-width="2.5" stroke-dasharray="7 5" fill="none" marker-end="url(#arrow)"/>')
# Existing ring kept in its measured position.
rect(-13.2,-4.8,-9.7,-1.3,'#151b18','#859280',3)
rect(-12.5,-5.5,-9,-2,'#303a32','#8bff3a',3)
for x in [-12.5,-5.5]:
    for z in [-9,-2]:
        cx,cy=xy(x,z);parts.append(f'<circle cx="{cx}" cy="{cy}" r="6" fill="#8bff3a"/>')
text(-9,-5.5,'RING',21)
text(-9,-4.35,'podest 8,4 × 8,4 m',12,'#b5c5b0')
rect(-9.8,-8.2,-1.3,.1,'#636a5b','#a8b49e',1)
text(-9,1.15,'schodki',12)
# Main training area and mirror/bags as in the reference.
rect(-2,10,-1,8,'#202821','#8bff3a',3)
text(4,2.4,'BRAXHORN',22)
text(4,3.65,'KICKBOXING',17,'#8bff3a')
text(4,5.15,'MATA / SPARING',13)
rect(17.5,17.8,-10.2,4.9,'#a5d3d5','#d6f3ed',1)
for z in [-8.4,-5.4,-2.4,.6]:
    cx,cy=xy(14,z);parts.append(f'<circle cx="{cx}" cy="{cy}" r="13" fill="#151a17" stroke="#8bff3a" stroke-width="2"/>')
text(13.9,3.5,'WORKI',14)
text(13.9,4.6,'na stalowej belce',11,'#a1b29f')
a,b=xy(18.9,-3);parts.append(f'<text x="{a}" y="{b}" fill="#b5d8db" font-size="13" transform="rotate(-90 {a} {b})" text-anchor="middle">LUSTRO — ODBICIE POSTACI NA ŻYWO</text>')
rect(-13.8,7.5,-12.9,-12.5,'#8bff3a','#8bff3a',1)
text(-3.1,-11.3,'BILLBOARD · BRAX + NYXA',15)
text(-3.1,-10.45,'grafika zatwierdzana osobno',11,'#a8b8a4')
rect(-3.4,.8,8,10,'#303b30','#8bff3a',2)
text(-1.3,9.2,'RECEPCJA',12)
rect(5.2,6.8,10.85,11.15,'#101413','none',0)
a,b=xy(6,13.2);c,d=xy(6,10.4);parts.append(f'<path d="M{a} {b}L{c} {d}" stroke="#8bff3a" stroke-width="3" marker-end="url(#arrow)"/>')
text(6,14.7,'ISTNIEJĄCE WEJŚCIE Z MIASTA',15)
text(6,15.8,'położenie zachowane',12,'#a9b8a5')
text(0,-13.75,'36 m · pełna istniejąca szerokość sali',14,'#aebda9')
text(-21.75,10.0,'DOBUDÓWKA',14,'#8bff3a')
text(-21.75,11.1,'7,5 × 11 m brutto',13)
parts.append('''<path d="M92 885H1348" stroke="#3c473e"/>
<circle cx="105" cy="925" r="6" fill="#8bff3a"/><text x="126" y="930" font-size="17" fill="#e6eee1">Zaplecze powiększa budynek; sala treningowa zachowuje obecny metraż.</text>
<circle cx="105" cy="965" r="6" fill="#8bff3a"/><text x="126" y="970" font-size="17" fill="#e6eee1">Korytarz z zakrętem + pełna ściana osłonowa + osobne drzwi szatni i WC.</text>
<text x="92" y="1010" font-size="13" fill="#8e9f90">To rzut układu, nie zrzut z działającej gry. Wyposażenie i przejścia są dopracowywane w scenie Unity.</text></g></svg>''')
out.write_text('\n'.join(parts))
print(out)
