#!/usr/bin/env python3
"""Import verified CC0 Poly Haven PBR maps into the isolated Unity6 ESTATE build.

Do not edit Brax, input, R1 city, gym, or existing source geometry.
Do not invent a material when an official map cannot be verified.
Source originals from the user's Library are kept intact; runtime 1K copies are
fetched from the same public Poly Haven asset IDs only on the GitHub CI runner.
"""
from __future__ import annotations
import hashlib
import io
import json
import pathlib
import time
import urllib.parse
import urllib.request
from PIL import Image

ROOT=pathlib.Path(__file__).resolve().parents[2]
OUT=ROOT/"unity-city/Assets/Source/EstateRenovation"
OUT.mkdir(parents=True,exist_ok=True)
ASSETS={
 "roof_tiles": {"role":"warm_roof","source":"https://polyhaven.com/a/roof_tiles"},
 "grey_roof_01": {"role":"grey_roof","source":"https://polyhaven.com/a/grey_roof_01"},
 "white_plaster_02": {"role":"white_plaster","source":"https://polyhaven.com/a/white_plaster_02"},
 "beige_wall_001": {"role":"beige_plaster","source":"https://polyhaven.com/a/beige_wall_001"},
 "white_stucco_02": {"role":"stucco","source":"https://polyhaven.com/a/white_stucco_02"},
 "red_brick": {"role":"red_brick","source":"https://polyhaven.com/a/red_brick"},
}
KEYS={
 "diff":("diff","diffuse","albedo","color","basecolor"),
 "nor_gl":("nor_gl","normalgl","normal_gl","normal"),
 "rough":("rough","roughness"),
}
HEADERS={"User-Agent":"BraxhornCity-EstatePBR-Audit/1.0 (CC0 asset quality control)","Accept":"application/json, image/*"}
def fetch(url,maxbytes):
    parsed=urllib.parse.urlparse(url)
    if parsed.scheme!="https" or parsed.hostname not in {"api.polyhaven.com","dl.polyhaven.org","cdn.polyhaven.com"}:
        raise ValueError("Unapproved remote asset provider: "+url)
    last=None
    for attempt in range(3):
        try:
            req=urllib.request.Request(url,headers=HEADERS)
            with urllib.request.urlopen(req,timeout=70) as res:
                if res.status!=200:raise IOError("HTTP "+str(res.status))
                result=bytearray()
                while True:
                    data=res.read(65536)
                    if not data:break
                    result.extend(data)
                    if len(result)>maxbytes:raise IOError("Exceeded download limit "+url)
                if not result:raise IOError("Empty official asset")
                return bytes(result)
        except Exception as e:
            last=e
            if attempt<2:time.sleep(attempt+1)
    raise IOError("Cannot access official asset: "+url+" "+str(last))

def find_map(tree,aliases):
    for key,value in tree.items():
        if key.lower().replace("_","").replace(" ","") in tuple(a.replace("_","") for a in aliases):
            return value
    raise KeyError("Essential Poly Haven map missing: "+str(aliases)+" among "+repr(list(tree)))

def choose(entry):
    for resolution in ("2k","1k","4k"):
        formats=entry.get(resolution) or entry.get(resolution.upper())
        if not isinstance(formats,dict):continue
        for ext in ("jpg","png","jpeg"):
            found=formats.get(ext)
            if isinstance(found,dict) and found.get("url") and isinstance(found.get("md5"),str):
                return found,resolution,ext
    raise RuntimeError("No ordinary mapped image with official md5; refusing substitution")

def resize_image(original,channel):
    with Image.open(io.BytesIO(original)) as raw:
        im=raw.convert("RGB") if channel!="rough" else raw.convert("L")
        if max(im.size)>1024:
            scale=1024/max(im.size)
            im=im.resize((max(1,round(im.width*scale)),max(1,round(im.height*scale))),Image.Resampling.LANCZOS)
        out=io.BytesIO()
        im.save(out,format="PNG",optimize=True)
        return out.getvalue(),list(im.size)

records=[]
for asset,meta in ASSETS.items():
    response=json.loads(fetch("https://api.polyhaven.com/files/"+asset,4_000_000))
    if not isinstance(response,dict):raise RuntimeError("Malformed asset response")
    record={"asset":asset,"url":meta["source"],"license":"CC0","role":meta["role"],"maps":[]}
    for channel,aliases in KEYS.items():
        provider,scale,ext=choose(find_map(response,aliases))
        url=provider["url"]
        raw=fetch(url,70_000_000)
        digest=hashlib.md5(raw).hexdigest()
        if digest.lower()!=provider["md5"].lower():
            raise RuntimeError("Official checksum mismatch "+asset+" "+channel)
        image,dimensions=resize_image(raw,channel)
        dest=OUT/(asset+"_"+channel+"_1k.png")
        dest.write_bytes(image)
        record["maps"].append({"channel":channel,"providerResolution":scale,
          "providerMd5":digest,"sourceURL":url,"output":dest.relative_to(ROOT).as_posix(),
          "outputSha256":hashlib.sha256(image).hexdigest(),"width":dimensions[0],"height":dimensions[1],
          "outputBytes":len(image)})
        if channel=="rough":
            with Image.open(io.BytesIO(image)) as rough:
                grayscale=rough.convert("L")
                rgba=Image.new("RGBA",grayscale.size,(0,0,0,0))
                rgba.putalpha(grayscale.point(lambda v:255-v))
                metallic=OUT/(asset+"_metallic_gloss_1k.png")
                rgba.save(metallic,format="PNG",optimize=True)
                record["packedMetallicGloss"]={"path":metallic.relative_to(ROOT).as_posix(),
                  "sha256":hashlib.sha256(metallic.read_bytes()).hexdigest(),
                  "metallic":"R = 0", "smoothness":"A = 255 - official source roughness"}
    records.append(record)
    print("PBR_OFFICIAL_CC0_VERIFIED",asset,meta["role"],[x["channel"] for x in record["maps"]])
report={"project":"BRAXHORN CITY R2 / ESTATE ONLY","source":"Poly Haven official public API",
 "license":"CC0 (https://polyhaven.com/license)","runMode":"temporary Unity CI only",
 "originalUserLibraryArchivesModified":False,"productionModified":False,
 "textureBudget":"max1024px per map; image maps only; optimized mobile Unity copies",
 "assets":records}
(OUT/"estate-pbr-provenance.json").write_text(json.dumps(report,indent=2)+"\n")
assert len(records)==6 and all(len(x["maps"])==3 and x.get("packedMetallicGloss") for x in records)
print("ESTATE_PBR_COMPLETE",len(records),"assets",sum(p.stat().st_size for p in OUT.glob("*.png")),"bytes")
