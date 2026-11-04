"""Station variants from the existing game's metro atlas; no external artwork."""
from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'unity-city/Assets/Source/Claude/Wnetrza/metro_atlas.png'
DEST=ROOT/'unity-city/Assets/Source/Mistrzowie';DEST.mkdir(parents=True,exist_ok=True)
FONT='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
NAMES=[('M1','DWORZEC'),('M2','CENTRUM'),('M3','ARENY / HOTELE')]
for sid,name in NAMES:
 im=Image.open(SOURCE).convert('RGB');draw=ImageDraw.Draw(im)
 def panel(rect,text,maxsize=48):
  x,y,w,h=rect;draw.rectangle((x,y,x+w-1,y+h-1),fill='#101d2b')
  size=maxsize
  while size>8:
   font=ImageFont.truetype(FONT,size)
   bb=draw.multiline_textbbox((0,0),text,font=font,spacing=6)
   if bb[2]-bb[0]<=w-24 and bb[3]-bb[1]<=h-14:break
   size-=1
  draw.multiline_text((x+12,y+7-bb[1]),text,font=font,fill='#f4eee3',spacing=6)
 panel((256,0,768,128),sid+'  '+name,66)
 panel((512,320,512,192),'MIASTO MISTRZÓW\nM1 DWORZEC\nM2 CENTRUM\nM3 ARENY / HOTELE',29)
 panel((0,320,512,192),'METRO\nRuch w przygotowaniu',40)
 panel((0,832,1024,64),'M1 DWORZEC   —   M2 CENTRUM   —   M3 ARENY / HOTELE',28)
 panel((0,256,512,64),'WYJŚCIE  /  MIASTO',32);panel((512,256,512,64),'WYJŚCIE  /  MIASTO',32)
 panel((0,896,512,128),'ARENY  /  MISTRZOSTWA',40);panel((512,896,512,128),'PROMENADA  /  MARINA',40)
 im.save(DEST/('metro_'+sid+'.png'))
print('BRAX_MISTRZOWIE_SIGNAGE_OK source=existing_city_atlas stations=3')
