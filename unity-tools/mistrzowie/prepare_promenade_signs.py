"""Deterministic 2048x512 original K1-K12 shop signs from owner's reference.

Not external stock artwork, never substitute 512px placeholders.
Unity import must retain native 2K sign resolution with Android ASTC 4x4.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
from promenade_layout import PAVILIONS

ROOT=Path(__file__).resolve().parents[2]
DEST=ROOT/"unity-city/Assets/Source/Mistrzowie"
SIZE=(2048,512)
FONT="/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf"
GOLD="#D7AE58"
CREAM="#FFF6E5"
BLACK="#111927"
PALETTES={
 "cafe":("#1F2425","#DEA969","C"),
 "icecream":("#452642","#FF84CC","L"),
 "fish":("#17384A","#63D2E2","R"),
 "pizza":("#4D2224","#FF8A4D","P"),
 "burger":("#362922","#E9B452","B"),
 "juice":("#1F382B","#A8D768","S"),
 "beach_shop":("#1C3F49","#78DCCF","S"),
 "souvenirs":("#37304B","#C3A0E5","P"),
 "bar":("#302544","#BD83F3","B"),
 "grill":("#44281F","#E7AA63","G"),
 "rental":("#1B3651","#70C4E4","W"),
 "info":("#233644","#86CFF0","i"),
}

def font_fit(draw,text,width=1600):
    for size in range(138,49,-1):
        f=ImageFont.truetype(FONT,size)
        b=draw.textbbox((0,0),text,font=f)
        if b[2]-b[0]<width:return f
    raise ValueError("Name cannot fit in original 4:1 2K sign: "+text)

def render(destination=DEST):
    destination.mkdir(parents=True,exist_ok=True)
    results=[]
    for p in PAVILIONS:
        bg,accent,icon=PALETTES[p["style"]]
        im=Image.new("RGB",SIZE,bg)
        d=ImageDraw.Draw(im)
        d.rectangle((18,18,2030,494),outline=GOLD,width=10)
        d.rectangle((34,34,2014,478),outline="#374550",width=4)
        d.rounded_rectangle((67,74,355,438),radius=30,fill="#0F1821",outline=accent,width=7)
        d.ellipse((124,121,296,293),outline=accent,width=10)
        fIcon=ImageFont.truetype(FONT,130)
        d.text((210,206),icon,font=fIcon,fill=CREAM,anchor="mm")
        title=p["sign"].upper()
        f=font_fit(d,title)
        d.text((1140,249),title,font=f,fill=CREAM,anchor="mm")
        labelFont=ImageFont.truetype(FONT,38)
        d.text((1140,402),"BRAXHORN CITY  •  PROMENADA NADMORSKA",
               font=labelFont,fill=GOLD,anchor="mm")
        d.line([(388,364),(1910,364)],fill=accent,width=5)
        dest=destination/("prom_"+p["id"]+"_sign.png")
        im.save(dest,optimize=True)
        results.append(dest)
    print("CHAMPIONS_PROMENADE_12_OWNER_REFERENCE_SIGNS_2K_OK")
    return results


if __name__=="__main__":
    render()
