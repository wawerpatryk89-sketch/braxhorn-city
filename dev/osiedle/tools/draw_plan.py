"""Measured district plan generated from the runtime layout (not a concept render)."""
import json,pathlib,math
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.patches import Rectangle,Circle,RegularPolygon,FancyArrowPatch
ROOT=pathlib.Path(__file__).resolve().parents[3]
level=json.loads((ROOT/'dev/osiedle/review/layout.json').read_text())
plt.rcParams.update({'font.family':'DejaVu Sans','font.size':11})
fig=plt.figure(figsize=(14,15),facecolor='#f4f2eb');ax=fig.add_axes([.055,.15,.89,.73]);ax.set_aspect('equal');ax.set_xlim(-119,119);ax.set_ylim(106,-110);ax.axis('off')
ink='#243a36';muted='#60706a';road='#53615c';walk='#d2ccbb';green='#a9c29a'
fig.text(.055,.955,'BRAXHORN CITY',fontsize=13,weight='bold',color=muted)
fig.text(.055,.917,'OSIEDLE WIŚNIOWA',fontsize=30,weight='bold',color=ink)
fig.text(.055,.887,'Aktualny układ planszy • 8 domów • 6 obiektów • park i sport',fontsize=12,color=muted)
def rect(x,z,w,d,fc,ec=None,lw=1,zorder=1):
 p=Rectangle((x-w/2,z-d/2),w,d,facecolor=fc,edgecolor=ec,linewidth=lw,zorder=zorder);ax.add_patch(p);return p
def label(x,z,s,size=10,col=ink,weight='normal',**kw):ax.text(x,z,s,ha='center',va='center',fontsize=size,color=col,weight=weight,zorder=12,**kw)
rect(0,-1.5,228,201,'#e3e7d5',green,2)
# Public paths are drawn before roads.
for z in [-19,39,86]:rect(0,z,224,15,walk);rect(0,z,224,7,road)
for x in [-58,58]:rect(x,-4,15,180,walk);rect(x,-4,6,180,road)
rect(0,37,15,122,walk);rect(0,37,9,122,road)
for x in [-43,43]:rect(x,10,9,53,walk);rect(x,10,5,53,road)
rect(43,-43,5,33,road)
for z in [-19,39,86]:
 for x in [-58,0,58]:
  for a in [-2,-1,0,1,2]:rect(x+a,z,.45,2.5,'#f4eee1',zorder=2)
label(0,-19,'P O Z N A Ń S K A',10,'white',weight='bold')
label(0,39,'B Y D G O S K A',10,'white',weight='bold')
label(0,69,'WIŚNIOWA',9,'white',weight='bold',rotation=90)
# Private plots and original model envelopes.
for h in level['houses']:
 x,z=h['x'],h['z'];rect(x,z,23,24 if z==0 else 21,'#d0dabb','#b1bda5',.8)
 rect(x,z,h['width'],min(15,18 if z!=59 else 12),'#b89570','#8c735a',1.2,zorder=3)
 label(x,z,'DOM '+h['id'][-2:],9,'#23362d',weight='bold')
 rect(x/2,z,abs(x)-10,2.4,walk,zorder=2)
rect(0,-42,31,39,'#d2debd','#a9b79a')
rect(0,-38,24,16,'#b89570','#846c52',1.4,3);rect(0,-51,17,8,'#c9b798',zorder=3)
label(0,-38,'WILLA WAWERA\nWiśniowa 4',9,weight='bold')
label(0,-54,'ogród · taras',8,muted)
rect(30,-42,18,37,'#d2debd','#a9b79a');rect(30,-37,10,14,'#b89570','#846c52',1.2,3)
label(30,-37,'DOM\nKONTAKTOWY',8,weight='bold')
rect(-35,-39,25,22,walk);rect(-35,-39,23,10,'#9ba39a','#68776e',1,3);label(-35,-39,'GARAŻE',9,weight='bold')
# Complete architectural shells and reserved arena spaces.
colours={'casino':'#bd8f79','club':'#9b91aa','police':'#91a9b1','hospital':'#e0d8c3','boxing':'#b5c2a5','arena':'#a8bba6'}
texts={'casino':'KASYNO','club':'DYSKOTEKA','police':'KOMISARIAT','hospital':'SZPITAL','boxing':'SALA BOKSERSKA','arena':'HALA WALK'}
for f in level['facilities']:
 x,z=f['x'],f['z'];rect(x,z,46,40,walk);rect(x,z,40,30,colours[f['id']],'#586d64',1.3,3)
 label(x,z-8,texts[f['id']],10,weight='bold')
 if f['id']=='boxing':
  rect(x,z,9,9,'#eee9d9','#516356',1.8,4);label(x,z+8,'ring · szatnia · kasa',8)
 elif f['id']=='arena':
  ax.add_patch(RegularPolygon((x,z),8,radius=5.5,orientation=math.pi/8,facecolor='#eee9d9',edgecolor='#516356',lw=1.8,zorder=4));label(x,z+8,'oktagon · szatnia · kasa',8)
 else:label(x,z+2,{'casino':'automaty · stoły\nzaplecze','club':'parkiet · zaplecze','police':'biura · recepcja','hospital':'rejestracja\n4 gabinety'}[f['id']],9)
 # Open front/rear doors.
 for dz in [-15,15]:rect(x,z+dz,3,.9,'#f4eee1',zorder=5)
 ax.add_patch(FancyArrowPatch((x,z+20),(x,z+14),arrowstyle='-|>',mutation_scale=12,color=ink,zorder=8))
# Park/court. Vegetation positions come directly from the game layout.
rect(0,-83,110,34,'#c7d9b3')
for z in [-67,-99]:rect(0,z,110,2.5,walk)
rect(0,-83,3,32,walk);rect(28,-83,54,2.5,walk)
rect(-28,-83,28,15,'#5e7a6e','#e8eddf',1.4,3)
rect(-28,-83,.1,15,'#e8eddf',zorder=4)
ax.add_patch(Circle((-28,-83),1.8,fill=False,edgecolor='#e8eddf',lw=1,zorder=4))
for x in [-40,-16]:rect(x,-83,3,5,'none','#e8eddf',1,4)
label(-28,-71,'BOISKO DO KOSZYKÓWKI',9,weight='bold')
label(29,-86.5,'PARK WIŚNIOWY',11,weight='bold')
for p in level['vegetation']:
 if p['kind']=='tree':ax.add_patch(Circle((p['x'],p['z']),2.2,facecolor='#557d52',edgecolor='#3d633e',lw=.6,zorder=6))
 elif p['kind']=='shrub':ax.add_patch(Circle((p['x'],p['z']),.75,facecolor='#7d9c67',zorder=5))
 else:ax.add_patch(Circle((p['x'],p['z']),.55,facecolor='#888c7b',zorder=5))
label(12,-90.5,'drążki',8,muted)
for x,z in [(12,-80),(28,-80),(42,-80),(18,-96),(36,-96)]:rect(x,z,2,.55,'#8a7458',zorder=8)
# Later connections have visibly closed gates, not navigable exits.
rect(43,-58,6,.7,'#b67a47',zorder=7);label(42,-62,'BRAMA DO LASU',7,weight='bold')
rect(0,98,12,.7,'#b67a47',zorder=7);label(0,103,'PRZEJŚCIE DO CENTRUM • PÓŹNIEJ',9,weight='bold')
# Cardinal direction and measured scale.
ax.annotate('N',xy=(108,-103),xytext=(108,-91),ha='center',va='center',fontsize=10,weight='bold',color=ink,arrowprops={'arrowstyle':'-|>','color':ink})
rect(-94,102,20,.6,ink,zorder=8);label(-94,106,'20 m',8)
fig.text(.055,.118,'ARCHITEKTURA + MIEJSCA POD MISJE',fontsize=12,color=ink,weight='bold')
fig.text(.055,.093,'• spotkanie w parku i wyzwanie na boisku\n• dostawa przy garażach; zaplecza kasyna i klubu',fontsize=10,color=muted,linespacing=1.7)
fig.text(.54,.093,'• ring i oktagon: wejście, zapisy, miejsce wypłaty\n• auta, ludzie i właściwe misje — kolejny etap',fontsize=10,color=muted,linespacing=1.7)
fig.text(.055,.035,'Układ z plików planszy, 28.10.2026. Kolory i symbole oznaczają funkcje; to plan, nie render wyglądu gry.',fontsize=9,color=muted)
fig.savefig(ROOT/'dev/osiedle/review/plan-osiedla.png',dpi=140,facecolor=fig.get_facecolor())
fig.savefig(ROOT/'dev/osiedle/review/plan-osiedla.pdf',facecolor=fig.get_facecolor())
print('Plan saved.')
