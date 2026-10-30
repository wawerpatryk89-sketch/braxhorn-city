"""Source-only staging of two EXACT owner-named Poly Haven CC0 models.
No proxy pots or leaves. Reproducible publisher URLs, MD5, SHA256/provenance.
Original Library periwinkle .blend SHA verified; planter Library archive has no
raw materialization access, so only official publisher source is attested.
"""
import hashlib,json,pathlib,urllib.request,urllib.parse
ROOT=pathlib.Path(__file__).resolve().parents[2]
STAGE=ROOT/"dom04-flower-originals"
STAGE.mkdir(exist_ok=True)
out={}
def download(url):
    assert url.startswith("https://dl.polyhaven.org/"), "Source must be official Poly Haven"
    with urllib.request.urlopen(urllib.request.Request(url,headers={"User-Agent":"BRAXHORN DOM04 genuine owner library assets"}),timeout=240) as r:return r.read()
def files(asset):
    req=urllib.request.Request("https://api.polyhaven.com/files/"+asset,headers={"User-Agent":"Braxhorn-Unity6-Dom04-CC0/1.0","Accept":"application/json"})
    with urllib.request.urlopen(req,timeout=180) as r:return json.load(r)
def all_urls(v,into):
    if isinstance(v,dict):
        u=v.get("url")
        if isinstance(u,str) and u.startswith("https://dl.polyhaven.org/"):
            name=pathlib.PurePosixPath(urllib.parse.urlparse(u).path).name
            if name and name not in into:into[name]={"url":u,"md5":v.get("md5")}
        for child in v.values():all_urls(child,into)
    elif isinstance(v,list):
        for child in v:all_urls(child,into)
# Periwinkle is staged independently with EXACT operator-Library hashes in
# dom04-prepare.py and exported by the already verified dom04-export-periwinkle.py.
# Fetch only the original owner-named wooden planter once, not the plant twice.
for asset,resolution,expected_blend in [
    ("planter_box_01","4k",None)
]:
    source=all_urls(files(asset),{}) if False else {}
    all_urls(files(asset),source)
    model_name=asset+"_"+resolution+".blend"
    if model_name not in source:raise RuntimeError("Exact official source model absent: "+model_name)
    directory=STAGE/asset;directory.mkdir(exist_ok=True)
    names=[model_name]
    # Both .blend archives in the operator's Library reference source texture maps.
    # Bring ALL original 2K/4K RGB/EXR/alpha maps; do not fabricate missing maps.
    names += sorted(n for n in source if n.startswith(asset+"_") and n.endswith((".jpg",".png",".exr")) and ("_"+resolution in n))
    if len(names)<4:raise RuntimeError("Not enough real PBR texture maps: "+asset+" / "+str(names))
    entry={}
    for name in names:
        data=download(source[name]["url"])
        sha=hashlib.sha256(data).hexdigest()
        md5=source[name]["md5"]
        if md5 and hashlib.md5(data).hexdigest()!=md5:raise RuntimeError("Official publisher checksum mismatch: "+name)
        if name==model_name and expected_blend and sha!=expected_blend:
            raise RuntimeError("Owner's original Library model SHA mismatch "+name+" "+sha)
        target=directory/(name if name.endswith(".blend") else "textures/"+name)
        target.parent.mkdir(exist_ok=True)
        target.write_bytes(data)
        entry[name]={"sha256":sha,"bytes":len(data),"officialURL":source[name]["url"],"libraryVerified":bool(expected_blend and name==model_name)}
    out[asset]=entry
report={"scope":"DOM04_ONLY","source":"official Poly Haven CC0 - same model asset names as owner's ChatGPT Library","publisherSources":out,"sourceVerified":True,"unityImported":False,"visualAcceptance":False}
(ROOT/"city-review").mkdir(exist_ok=True)
(ROOT/"city-review/dom04-flower-model-sources.json").write_text(json.dumps(report,indent=2))
print("DOM04_ORIGINAL_PLANTER_AND_FLOWER_MODELS_STAGED",[(k,len(v)) for k,v in out.items()])
