"""MIASTO R1 — rysunek planu połączenia osiedla Bartodzieje ze Starym Rynkiem przez Brdę (właściciel 23.11, 17:05: „zrób tak jak najlepiej”).
Stare miasto: dane kolegi przesunięte o (+42; +763) j.; Brda z prawdziwego odcinka przy Mostowej na wschód do morza."""
import json, sys
sys.path.insert(0,'..')
import numpy as np
import matplotlib; matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.patches import Rectangle, Polygon as MP
import osiedle as OS
K=1.294; O=np.array([-145.9,508.8])
d=OS.wczytaj(); sm=json.load(open('../dane/stare-miasto-rynek-mostowa-gdanska.json'))
X0,X1,Z0,Z1=-872,170,-85.45,180
off=np.array([42.,763.])
fig,ax=plt.subplots(figsize=(13,17))
ax.add_patch(Rectangle((X0,Z0),X1-X0,Z1-Z0,fc='#d9d4c7',ec='#555',lw=1.5)); ax.text(-350,40,'MIASTO R1 (dziś)',ha='center',fontsize=13,color='#444')
ax.add_patch(Rectangle((X1,-150),300,2000,fc='#b9d6ea',ec='none')); ax.text(250,700,'MORZE',rotation=90,color='#356',fontsize=13)
ax.add_patch(Rectangle((-152,126),52,36,fc='#e07b39',ec='k',lw=.5)); ax.annotate('stary market → przejście na osiedle',(-126,150),(-600,215),fontsize=10,arrowprops=dict(arrowstyle='->'))
for t in sm['tereny']:
    if t['rodzaj']=='water': ax.add_patch(MP(np.array(t['linia_xz'])+off,fc='#3d7fc1',ec='none'))
riv=np.array([[-700,1241],[-560,1250],[-400,1262],[-200,1258],[0,1250],[120,1240],[170,1232]])
ax.plot(riv[:,0],riv[:,1],color='#3d7fc1',lw=16,solid_capstyle='butt'); ax.text(-300,1285,'BRDA → do morza',color='#1d4f87',fontsize=12)
for b in d['budynki']: ax.add_patch(MP(np.array(b['poly'].exterior.coords)*K+O,fc='#8c6d5a',ec='none'))
for u in d['ulice']:
    if u['klasa'] in ('primary','tertiary','secondary','residential','service'):
        L=np.array(u['linia'])*K+O; ax.plot(L[:,0],L[:,1],color='#888',lw=.6)
for b in sm['budynki']: ax.add_patch(MP(np.array(b['obrys_xz'])+off,fc='#a0523d',ec='none'))
for u in sm['ulice']:
    if len(u['linia_xz'])>1: L=np.array(u['linia_xz'])+off; ax.plot(L[:,0],L[:,1],color='#888',lw=.5)
ax.text(40,700,'OSIEDLE BARTODZIEJE\n32a, szpital, market, rondo',ha='center',fontsize=12,fontweight='bold',color='#3b2a20',bbox=dict(fc='#ffffffcc',ec='none'))
ax.text(-650,1600,'STARY RYNEK\nMostowa, Focha, Opera,\nspichrze, Gdańska',ha='center',fontsize=12,fontweight='bold',color='#5b2010',bbox=dict(fc='#ffffffcc',ec='none'))
ax.plot([-126,-126],[162,240],color='#c0392b',lw=4); ax.text(-110,195,'wjazd 1\n(działka starego\nsklepu)',color='#c0392b',fontsize=9,fontweight='bold')
ax.plot([-450,-450],[156,240],color='#c0392b',lw=4); ax.text(-600,195,'wjazd 2\n(z nowych dzielnic)',color='#c0392b',fontsize=9,fontweight='bold')
ax.plot([150,150,120,-180],[174,1180,1215,1215],color='#c0392b',lw=4,ls='--'); ax.text(160,600,'droga wzdłuż morza\nmiasto → ujście Brdy → Stary Rynek',color='#c0392b',fontsize=9,fontweight='bold',rotation=90)
ax.plot([-430,-560],[930,1150],color='#555',lw=5); ax.text(-470,1010,'ulica jak Jagiellońska\n(osiedle → Stary Rynek)',fontsize=9,rotation=58)
ax.plot([-60,-60],[910,1330],color='#c0392b',lw=6); ax.text(-40,1340,'WYSOKI MOST nad Brdą — z góry widać\nrzekę w obie strony, a na zachodzie spichrze,\nOperę i kamienice',color='#c0392b',fontsize=10,fontweight='bold')
ax.annotate('most na Mostowej + linoskoczek',(-662,1245),(-890,1180),fontsize=10,color='#c0392b',fontweight='bold',arrowprops=dict(arrowstyle='->',color='#c0392b'),bbox=dict(fc='#ffffffcc',ec='none'))
ax.annotate('boisko za 32a — ustawki (misja)',(-175,467),(-560,330),fontsize=10,arrowprops=dict(arrowstyle='->'),bbox=dict(fc='#ffffffcc',ec='none'))
ax.set_title('Połączenia: miasto ↔ osiedle (2 wjazdy), osiedle ↔ Stary Rynek (most), miasto ↔ Stary Rynek (wybrzeże)',loc='left',fontsize=13)
ax.text(-870,-120,'Mapa nie rośnie na wschód ani zachód, tylko na północ. Obok siebie się nie mieszczą (pełne osiedle 750 j. + stare miasto 370 j.\n'
        'przy szerokości mapy 1040 j.), więc stare miasto stoi za osiedlem. Brda wypływa ze starego miasta (prawdziwy odcinek z mapy)\n'
        'i płynie na wschód do morza. Z osiedla na Rynek: ulicą lądem albo wysokim mostem nad Brdą z widokiem na stare miasto.\n'
        'Jednostki gry (1 m = 1,294 j.). Granica dziś z = 180; po zmianie ok. z = 1720.',fontsize=9,va='top')
ax.set_aspect('equal'); ax.set_xlim(-900,480); ax.set_ylim(-260,1760); ax.axis('off')
plt.tight_layout(); plt.savefig('PLAN_polaczenie.png',dpi=85)
plt.savefig('/tmp/claude-0/-home-claude/99abfab2-30a6-517f-869f-7369b49b973e/scratchpad/bdg/out/PLAN_polaczenie_v2.png',dpi=85)
