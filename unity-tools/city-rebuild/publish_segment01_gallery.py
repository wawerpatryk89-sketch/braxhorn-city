#!/usr/bin/env python3
"""Fast phone gallery for original screenshots from an actual Unity 6 player.

Only the real captured Unity screenshots are input. JPG thumbnails are resized
copies of PNG bytes, never generated artworks. The gallery is independent of
the WebGL payload; loading 1 image must not fetch 200 MB of Unity data.
"""
import base64
import hashlib
import html
import json
import os
from pathlib import Path
import re
import shutil
import subprocess

from PIL import Image

REPO="wawerpatryk89-sketch/braxhorn-city"
SRC="codex/city-street-segment-review-20261029"
QA=Path("segment-gallery-qa")
OUT=Path("segment-photos-public")
RECEIPT=Path("segment-gallery-publication")

def require(ok,message):
    if not ok:raise ValueError(message)

def digest(p):
    with p.open("rb") as f:
        return hashlib.file_digest(f,"sha256").hexdigest()

def write(p,obj):
    p.parent.mkdir(exist_ok=True,parents=True)
    p.write_text(json.dumps(obj,ensure_ascii=False,indent=2)+"\n")

def main():
    sha=os.environ.get("GITHUB_SHA","")
    require(re.fullmatch("[0-9a-f]{40}",sha),"Source SHA missing")
    require(os.environ.get("GITHUB_REPOSITORY")==REPO and
            os.environ.get("GITHUB_REF")=="refs/heads/"+SRC,
            "Wrong source, cannot publish segment gallery")
    require(not OUT.exists() and QA.is_dir(),"Missing screenshot QA")
    result=json.loads((QA/"segment-qa.json").read_text())
    require(result.get("sourceCommit")==sha and result.get("passed") is True and
            result.get("physicalIphonePass") is False,
            "No authentic passing desktop Unity screenshot QA yet")
    shots=result.get("captures",[])
    require(len(shots)>=10,"Need at least 10 genuine Unity screenshots, including rooftops and clouds")
    (OUT/"img").mkdir(parents=True)
    (OUT/"full").mkdir()
    photos=[]
    total=0
    for item in shots:
        name=item.get("name")
        require(isinstance(name,str) and re.fullmatch("[0-9A-Za-z_.-]+[.]png",name),"Unsafe file")
        source=QA/name
        require(source.is_file() and source.open("rb").read(8)==bytes((137,80,78,71,13,10,26,10)),
                "Screenshot PNG missing "+name)
        image=OUT/"img"/name.replace(".png",".jpg")
        with Image.open(source) as original:
            require(original.size==(915,412),"Unrecognized Unity screenshot size "+name)
            original.convert("RGB").save(image,"JPEG",quality=79,optimize=True,progressive=True)
        shutil.copyfile(source,OUT/"full"/name)
        total+=image.stat().st_size
        photos.append({
            "file":name,"description":item.get("details",""),
            "original":"full/"+name,"small":"img/"+image.name,
            "originalSha256":digest(source),"thumbnailSize":image.stat().st_size,
        })
    def title(photo):
        name=photo["file"].replace(".png","")
        return html.escape(name[3:].replace("-"," ").strip().title())
    nav="".join('<a href="#s'+str(i)+'">'+str(i+1)+'</a>' for i in range(len(photos)))
    cards="".join(
       '<section id="s'+str(i)+'"><a href="'+photo["original"]+'" target="_blank" rel="noopener">'+
       '<img src="'+photo["small"]+'" alt="'+title(photo)+'" width="915" height="412" '+
       ('loading="eager"' if i==0 else 'loading="lazy"')+'></a>'+
       '<div class="info"><div><small>KADR '+str(i+1)+' / '+str(len(photos))+
       '</small><h2>'+title(photo)+'</h2><p>'+html.escape(photo["description"])+'</p></div>'+
       '<a href="'+photo["original"]+'" target="_blank" rel="noopener">Oryginalny PNG</a></div></section>'
       for i,photo in enumerate(photos)
    )
    web='''<!doctype html><html lang="pl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover"><meta name="robots" content="noindex,nofollow"><meta name="color-scheme" content="dark"><title>BRAXHORN CITY / Odcinek 01 / Odbiór</title>
    <style>:root{color-scheme:dark}*{box-sizing:border-box}html{scroll-behavior:smooth}body{margin:0;background:#0e1011;color:#f2f4f1;font:15px/1.5 system-ui,-apple-system,BlinkMacSystemFont,sans-serif}main{max-width:1100px;padding:18px 12px 65px;margin:0 auto}.top{padding:20px 8px 8px}.mark{font-size:11px;letter-spacing:.16em;color:#c0d2a7;font-weight:800}h1{font-size:clamp(25px,6vw,43px);letter-spacing:-.04em;line-height:1.1;margin:13px 0 14px}.lead{max-width:720px;color:#b6bfbb}.note{margin:22px 0;padding:16px 14px;border-top:1px solid #3c4b40;border-bottom:1px solid #3c4b40;background:#151b17;color:#e7e7df;font-size:13px}nav{display:flex;gap:8px;overflow-x:auto;position:sticky;top:0;z-index:2;background:#0e1011ed;padding:12px 0 15px}nav a{flex:0 0 auto;text-decoration:none;min-width:42px;text-align:center;border:1px solid #66755f;color:#e4e6db;border-radius:100px;padding:10px}section{margin:22px 0;border:1px solid #323c37;border-radius:12px;overflow:hidden;background:#14191a;scroll-margin-top:80px}section img{display:block;width:100%;height:auto;aspect-ratio:915/412}.info{padding:12px 15px 18px;display:flex;align-items:end;justify-content:space-between;gap:10px}.info h2{font-size:17px;line-height:1.3;margin:3px 0}.info p{font-size:12px;color:#aebbb2;margin:0}.info small{font-size:10px;color:#c1d1b7;letter-spacing:.12em}.info>a{white-space:nowrap;color:#d0ebbf;font-size:12px}footer{color:#99a69d;border-top:1px solid #333c36;padding:18px 7px}button{border:0}@media(max-width:520px){.info{display:block}.info>a{display:inline-block;margin-top:10px}}</style></head>
    <body><main><header class="top"><div class="mark">BRAXHORN CITY · ODCINEK 01</div><h1>Architektura. Ujęcie po ujęciu.</h1><p class="lead">Prawdziwe screenshoty z gry Unity 6: perspektywa Braxa, billboardy, niebo z chmurami, panoramy dachów i ujęcia z góry. Wybierz kadr. Nie pobierasz całego miasta.</p><div class="note">Weryfikacja: screenshoty pochodzą z rzeczywistego WebGL. Widoki lotnicze wykonuje wyłącznie kamera testowa — Brax nie jest teleportowany. To wstępny odbiór wizualny, nie potwierdzenie działania na fizycznym iPhonie.</div></header>
    <nav aria-label="Przejdź do kadru">'''+nav+'</nav>'+cards+'''<footer>Oryginalne PNG i sumy SHA-256: <a href="/evidence.json">evidence.json</a>. Pełna gra, siłownia i osiedle pozostają poza tym widokiem. To tylko lekka galeria rzeczywistych renderów Unity.</footer></main></body></html>'''
    (OUT/"index.html").write_text(web,encoding="utf-8")
    (OUT/"robots.txt").write_text("User-agent: *\nDisallow: /\n")
    write(OUT/"vercel.json",{
      "framework":None,"buildCommand":"","installCommand":"",
      "outputDirectory":".","cleanUrls":False,
      "headers":[{"source":"/(.*)","headers":[{"key":"Cache-Control","value":"public, max-age=1800"}]}]
    })
    write(OUT/"evidence.json",{
      "schema":1,"sourceCommit":sha,"buildId":result["buildId"],
      "qaPassed":True,"cameraAltitudeDisclaimer":result["cameraAltitudeDisclaimer"],
      "physicalPhonePass":False,"visualAcceptance":"PENDING USER REVIEW",
      "thumbnailBytes":total,"photos":photos,"productionChanged":False
    })
    output_branch="preview/city-segment01-gallery-"+sha[:12]
    token=os.environ.get("GH_TOKEN")
    require(token,"No GitHub workflow permission")
    env=os.environ.copy();env.pop("GH_TOKEN",None)
    env.update(GIT_CONFIG_COUNT="1",GIT_CONFIG_KEY_0="http.https://github.com/.extraheader",
               GIT_CONFIG_VALUE_0="AUTHORIZATION: basic "+
               base64.b64encode(("x-access-token:"+token).encode()).decode())
    def git(*args,auth=False):
        return subprocess.check_output(["git","-c","core.hooksPath=/dev/null",*args],
          cwd=OUT,env=env if auth else None,text=True).strip()
    git("init","--initial-branch="+output_branch)
    git("config","user.name","github-actions[bot]")
    git("config","user.email","41898282+github-actions[bot]@users.noreply.github.com")
    git("remote","add","origin","https://github.com/"+REPO+".git")
    require(not git("ls-remote","--heads","origin","refs/heads/"+output_branch,auth=True),
            "Refusing overwrite of already reviewed gallery")
    git("add","--all")
    git("commit","-m","gallery(city): original Unity 6 aerial and storefront photos "+sha[:12])
    target=git("rev-parse","HEAD")
    git("push","origin","HEAD:refs/heads/"+output_branch,auth=True)
    publication={"branch":output_branch,"commit":target,"sourceCommit":sha,
                 "photos":len(photos),"thumbnailBytes":total,
                 "device":"original Unity browser render; physical phone pending",
                 "productionChanged":False}
    write(RECEIPT/"gallery.json",publication)
    print("CITY_SEGMENT01_FAST_GALLERY_PUBLISHED",json.dumps(publication),flush=True)

if __name__=="__main__":
    main()
