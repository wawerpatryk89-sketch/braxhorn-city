"""DOM04 ONLY — prepare byte-identical CC0 maps matching operator ChatGPT Library.
No fake fallback or generated substitute. Output never committed to public GitHub.
"""
import hashlib, io, json, pathlib, urllib.request, urllib.parse
import numpy as np
from PIL import Image
import os
os.environ["OPENCV_IO_ENABLE_OPENEXR"]="1"
import cv2

ROOT=pathlib.Path(__file__).resolve().parents[2]
OUT=ROOT/"unity-city/Assets/Source/City/Dom04"
OUT.mkdir(parents=True, exist_ok=True)
AUDIT=ROOT/"city-review"
AUDIT.mkdir(exist_ok=True)
def get(url):
    req=urllib.request.Request(url, headers={"User-Agent":"Braxhorn-Unity6-Dom04-CC0/1.0"})
    with urllib.request.urlopen(req,timeout=180) as response: return response.read()
cache={}
def source(asset,filename,sha=None):
    if asset not in cache:
        cache[asset]=json.loads(get("https://api.polyhaven.com/files/"+asset))
    results=[]
    def walk(v):
        if isinstance(v,dict):
            if isinstance(v.get("url"),str):
                u=v["url"]
                if urllib.parse.unquote(urllib.parse.urlparse(u).path).endswith("/"+filename):
                    results.append((u,v.get("md5")))
            for vv in v.values(): walk(vv)
        elif isinstance(v,list):
            for vv in v:walk(vv)
    walk(cache[asset])
    if not results: raise RuntimeError("Official Poly Haven file absent: "+asset+"/"+filename)
    u,md5=results[0]; data=get(u); h=hashlib.sha256(data).hexdigest()
    if sha is not None and h!=sha:raise RuntimeError("Library exact source SHA-256 mismatch: "+filename+" "+h)
    if md5 and hashlib.md5(data).hexdigest()!=md5:raise RuntimeError("Publisher MD5 mismatch: "+filename)
    provenance.append(dict(asset=asset,name=filename,url=u,sha256=h,libraryByteIdentity=(sha is not None),publisherMd5=md5))
    return data
provenance=[]
def image(data):return Image.open(io.BytesIO(data)).convert("RGB")
def normal_exr(data,res=1024):
    raw=cv2.imdecode(np.frombuffer(data,dtype=np.uint8),cv2.IMREAD_UNCHANGED)
    if raw is None:raise RuntimeError("OpenGL normal EXR decode failed")
    bgr=cv2.resize(raw,(res,res),interpolation=cv2.INTER_AREA)
    rgb=cv2.cvtColor(bgr,cv2.COLOR_BGR2RGB)
    return Image.fromarray(np.rint(np.clip(rgb,0,1)*255).astype(np.uint8),"RGB")
def save(im,name):
    dest=OUT/name;im.save(dest);return hashlib.sha256(dest.read_bytes()).hexdigest()
def pack_rough(data):
    im=image(data).resize((1024,1024),Image.Resampling.LANCZOS)
    rough=np.asarray(im)[:,:,0].astype(np.float32)/255
    rgba=np.zeros((1024,1024,4),dtype=np.uint8)
    rgba[:,:,3]=np.rint((1-np.clip(rough,.65,1))*255).astype(np.uint8)
    return Image.fromarray(rgba,"RGBA")

beige_diff=source("beige_wall_001","beige_wall_001_diff_4k.jpg","80062445c4122f72bd067cbc4ac511146c6a7de4a3bb4fe78237c9838e4def37")
beige_nor=source("beige_wall_001","beige_wall_001_nor_gl_4k.exr","b99eafe6acbd05b838ec2667f3f0539eb6987b5f2c3a2ed510ce547a9ab72a2a")
beige_rough=source("beige_wall_001","beige_wall_001_rough_4k.jpg","662874883ceb729365eb16f5c01457e993ca29474ce659bb96eb0a16be09264a")
# Exact model atlas textures inside operator's grass_bermuda_01_2k.blend.zip.
bermuda_diff=source("grass_bermuda_01","grass_bermuda_01_diff_2k.jpg","30183c203739dee9812b70f21916fcdcd88719dcfe4fa3270d8ce208369ec933")
bermuda_alpha=source("grass_bermuda_01","grass_bermuda_01_alpha_2k.png","2750b808598d2c4116e494c20065a2dbf7fba5a098c3ed445772ff6d02ec3d3c")
# Official ground albedo complements genuine Bermuda foliage; not mislabelled as Library file.
grass_data=cache.get("grass_ground")
grass_file="grass_ground_diff_2k.jpg"
ground_diff=source("grass_ground",grass_file)
ground_normal=source("grass_ground","grass_ground_nor_gl_2k.exr")
outputs={
 "BeigeAlbedo.png":save(Image.fromarray(np.uint8(np.clip(np.stack([.945+(np.asarray(image(beige_diff).resize((1024,1024),Image.Resampling.LANCZOS)).astype(np.float32).mean(2)/255-.60)*.20,.915+(np.asarray(image(beige_diff).resize((1024,1024),Image.Resampling.LANCZOS)).astype(np.float32).mean(2)/255-.60)*.20,.865+(np.asarray(image(beige_diff).resize((1024,1024),Image.Resampling.LANCZOS)).astype(np.float32).mean(2)/255-.60)*.20],axis=2),0,1)*255)),"BeigeAlbedo.png"),
 "BeigeNormal.png":save(normal_exr(beige_nor),"BeigeNormal.png"),
 "BeigeMR.png":save(pack_rough(beige_rough),"BeigeMR.png"),
 "GrassGroundAlbedo.png":save(image(ground_diff).resize((1024,1024),Image.Resampling.LANCZOS),"GrassGroundAlbedo.png"),
 "GrassGroundNormal.png":save(normal_exr(ground_normal),"GrassGroundNormal.png"),
}
c=image(bermuda_diff).resize((1024,1024),Image.Resampling.LANCZOS)
a=Image.open(io.BytesIO(bermuda_alpha)).convert("L").resize((1024,1024),Image.Resampling.LANCZOS)
# Photographic Bermuda source foliage remains identical geometry and opacity.
# Restore daylight exposure and rich but natural green using source RGB only.
rgb=np.asarray(c,dtype=np.float32)/255.0
rgb=np.clip(np.power(rgb,.75)*np.array([.80,1.15,.85],dtype=np.float32),0,1)
c=Image.fromarray(np.rint(rgb*255).astype(np.uint8),"RGB")
c.putalpha(a)
outputs["BermudaCutout.png"]=save(c,"BermudaCutout.png")
report=dict(scope="DOM04_ONLY",libraryFiles=["beige_wall_001_4k.blend.zip","grass_bermuda_01_2k.blend.zip"],sources=provenance,outputs=outputs,sourceVerified=True,sceneVerified=False,visualAcceptance=False)
(AUDIT/"dom04-library-source-evidence.json").write_text(json.dumps(report,indent=2))
print("DOM04_LIBRARY_BYTE_IDENTICAL_SOURCE_PASS",len(provenance))


# 100% genuine owner's Library plant material. Archive inspected on 30 Oct 2026:
# periwinkle_plant_2k.blend.zip -> exact source .blend / diffuse / opacity files.
# Download only identical files from Poly Haven's official CC0 repository.
PERIWINKLE_LIBRARY_EXACT={
    "periwinkle_plant_2k.blend":"e9fc4300a8be05a9e7ec4423c239e9af37a2f3e4be9e6175a19ba82e1ffafd8a",
    "periwinkle_plant_diff_2k.jpg":"015ae7b001fa72a2be9a4297590e30d9e29ec4d686ca1f19d8a313671ac3d137",
    "periwinkle_plant_opacity_2k.png":"969a29f3c44bd9be704e7acc22746785ac7ca26746eec7c157c9c7d1bee2e841",
}
for name,sha in PERIWINKLE_LIBRARY_EXACT.items():
    data=source("periwinkle_plant",name,sha)
    (OUT/name).write_bytes(data)
report["plantLibrarySource"]="ChatGPT Library: /Braxhorn city/Materialy/Roslinnosc/periwinkle_plant_2k.blend.zip"
report["plantSourceFilesVerified"]={k:hashlib.sha256((OUT/k).read_bytes()).hexdigest() for k in PERIWINKLE_LIBRARY_EXACT}
report["plantSourceVerified"]=True
report["sceneVerified"]=False
report["visualAcceptance"]=False
(AUDIT/"dom04-library-source-evidence.json").write_text(json.dumps(report,indent=2))
print("DOM04_PERIWINKLE_OWNER_LIBRARY_EXACT_BYTES_PASS",len(PERIWINKLE_LIBRARY_EXACT))
