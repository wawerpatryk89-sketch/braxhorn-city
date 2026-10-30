"""Photometric correction of the original Verdant Trail foliage source RGBA.

Original Braxhorn Estate GLB is read directly, before glTF externalization.
Exact mesh/material IDs are validated and the original alpha masks are retained.
This is a color/exposure edit on the user's real original meshes, not fake trees.
"""
import io,json,struct,hashlib,pathlib
import numpy as np
from PIL import Image

ROOT=pathlib.Path(__file__).resolve().parents[2]
SOURCE=ROOT/"unity-city/Assets/Source/City/braxhorn-estate.glb"
OUTPUT=ROOT/"unity-city/Assets/Source/City/Dom04"
OUTPUT.mkdir(parents=True,exist_ok=True)
REPORT=ROOT/"city-review"
REPORT.mkdir(parents=True,exist_ok=True)
sets=[
    ("EstateMesh_453","estate_lit_194","VerdantLeavesColorCorrected.png",.35,(.70,1.12,.85)),
    ("EstateMesh_467","estate_lit_196","VerdantShrubsColorCorrected.png",.78,(.86,1.27,.78)),
]
results=[]
with SOURCE.open("rb") as f:
    if f.read(4)!=b"glTF":raise RuntimeError("DOM04 original estate source not GLB")
    f.seek(12)
    n=struct.unpack("<I",f.read(4))[0]
    if f.read(4)!=b"JSON":raise RuntimeError("DOM04 original source GLB JSON chunk missing")
    d=json.loads(f.read(n))
    data_offset=28+n
    for node_name,material_name,filename,gamma,tint in sets:
        node=next((v for v in d["nodes"] if v.get("name")==node_name),None)
        if node is None:raise RuntimeError("Missing exact real foliage mesh "+node_name)
        p=d["meshes"][node["mesh"]]["primitives"][0]
        material=d["materials"][p["material"]]
        if material.get("name")!=material_name:
            raise RuntimeError("Original source material identity changed "+node_name)
        if material.get("alphaMode")!="MASK":
            raise RuntimeError("DOM04 original leaf/shrub source alpha is no longer MASK")
        idx=material["pbrMetallicRoughness"]["baseColorTexture"]["index"]
        image=d["images"][d["textures"][idx]["source"]]
        view=d["bufferViews"][image["bufferView"]]
        f.seek(data_offset+view.get("byteOffset",0))
        raw=f.read(view["byteLength"])
        original=np.asarray(Image.open(io.BytesIO(raw)).convert("RGBA"),dtype=np.uint8)
        if original.shape!=(1024,1024,4):raise RuntimeError("Actual source foliage atlas unexpected dimension")
        mask=original[:,:,3]
        present=int((mask>127).sum())
        if present<20000 or present>850000:raise RuntimeError("Original source foliage alpha invalid")
        rgb=original[:,:,:3].astype(np.float32)/255.
        grade=np.clip(np.power(rgb,gamma)*np.asarray(tint,dtype=np.float32),0,1)
        changed=original.copy()
        changed[:,:,:3]=np.rint(grade*255).astype(np.uint8)
        if not np.array_equal(changed[:,:,3],mask):raise RuntimeError("Foliage alpha modified")
        target=OUTPUT/filename
        Image.fromarray(changed,"RGBA").save(target,optimize=True)
        result=dict(node=node_name,material=material_name,publisherSourceSha256=hashlib.sha256(raw).hexdigest(),
                    output=filename,outputSha256=hashlib.sha256(target.read_bytes()).hexdigest(),
                    alphaPixelCount=present,medianSourceRgb=np.median(original[mask>127,:3],axis=0).tolist(),
                    medianGradedRgb=np.median(changed[mask>127,:3],axis=0).tolist(),
                    alphaUnchanged=True,originalGeometryUnmodified=True)
        results.append(result)
receipt=dict(scope="DOM04_ONLY",sourceAtlasesVerified=True,alphaPreserved=True,
             originalModelsUnchanged=True,sourceGLB="braxhorn-estate.glb",
             outputMaps=results,unityVisualVerified=False)
(REPORT/"dom04-verdant-color-qa.json").write_text(json.dumps(receipt,indent=2))
print("DOM04_ORIGINAL_VERDANT_ATLAS_COLOR_GRADE_VERIFIED",[(a["node"],a["medianSourceRgb"],a["medianGradedRgb"]) for a in results])
