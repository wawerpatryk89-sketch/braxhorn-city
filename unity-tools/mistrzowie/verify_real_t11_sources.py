"""Audits REAL T11 2K files and exact glTF-to-GLB byte preservation. No Unity/APK."""
import hashlib
import json
from pathlib import Path
import struct
from PIL import Image
from pack_casino_vegetation_glb import _read_uri

ROOT=Path(__file__).resolve().parents[2]
PROJECT=ROOT/"unity-city"
VEG=PROJECT/"OriginalSources/Mistrzowie/CasinoT11/Vegetation2K"
DEST=PROJECT/"Assets/Source/Mistrzowie/CasinoT11"
OUTPUT=ROOT/"mistrzowie-evidence/t11-real-source-audit.json"

def digest(data):
    return hashlib.sha256(data).hexdigest()

def check_glb(spec):
    source=Path(spec["originalGltf"])
    packed=Path(spec["embeddedGlb"])
    original_data=source.read_bytes()
    glb=packed.read_bytes()
    if digest(original_data)!=spec["originalSha256"] or digest(glb)!=spec["glbSha256"]:
        raise ValueError("Original glTF or lossless GLB checksum changed")
    if len(glb)<28 or struct.unpack_from("<4sII",glb) != (b"glTF",2,len(glb)):
        raise ValueError("Invalid 2K GLB header")
    jsonlen,kind=struct.unpack_from("<I4s",glb,12)
    if kind!=b"JSON": raise ValueError("GLB missing JSON chunk")
    output=json.loads(glb[20:20+jsonlen])
    size,kind=struct.unpack_from("<I4s",glb,20+jsonlen)
    if kind!=b"BIN\x00" or 28+jsonlen+size!=len(glb):
        raise ValueError("GLB missing binary data")
    blob=glb[28+jsonlen:]
    orig=json.loads(original_data)
    for k in ("meshes","materials","textures","nodes","scenes","accessors","animations"):
        if orig.get(k)!=output.get(k):
            raise ValueError("Owner original model changed in "+k)
    if any("uri" in x for x in output.get("images",[])) or "uri" in output["buffers"][0]:
        raise ValueError("Packed GLB still relies on external texture or buffer")
    if not orig.get("meshes") or len(output.get("images",[]))!=len(orig.get("images",[])):
        raise ValueError("Missing original mesh/image")
    for old,new in zip(orig.get("bufferViews",[]),output.get("bufferViews",[])):
        parent=_read_uri(source.parent,orig["buffers"][old["buffer"]]["uri"])
        length=old["byteLength"]
        left=parent[old.get("byteOffset",0):old.get("byteOffset",0)+length]
        right=blob[new.get("byteOffset",0):new.get("byteOffset",0)+length]
        if left!=right:
            raise ValueError("Original mesh/geometry binary was modified")
    for old,new in zip(orig.get("images",[]),output.get("images",[])):
        if "uri" not in old: continue
        data=_read_uri(source.parent,old["uri"])
        view=output["bufferViews"][new["bufferView"]]
        if blob[view.get("byteOffset",0):view.get("byteOffset",0)+view["byteLength"]]!=data:
            raise ValueError("Original image bytes were modified: "+old["uri"])
    return {"file":packed.name,"meshes":len(orig["meshes"]),
            "images":len(orig["images"]),"bytes":len(glb),"sha256":digest(glb)}

def main():
    m=json.loads((VEG/"source-vegetation-2k.json").read_text())
    if len(m.get("losslessEmbeddedModels",[]))!=2:
        raise ValueError("Expected two original Poly Haven 2K models")
    glbs=[check_glb(x) for x in m["losslessEmbeddedModels"]]
    for row in m["originalSourceFiles"]:
        data=(PROJECT/row["file"]).read_bytes()
        if len(data)!=row["bytes"] or digest(data)!=row["sha256"]:
            raise ValueError("Original vegetation source corrupted: "+row["file"])
    metadata=json.loads((DEST/"t11-polyhaven-2k-manifest.json").read_text())
    rows=metadata.get("rows",[])
    if metadata.get("sourceResolution")!="2k" or metadata.get("upscaled") is not False:
        raise ValueError("2K original source contract broken")
    if len(rows)!=21 or len({r["file"] for r in rows})!=21:
        raise ValueError("Expected all 21 genuine 2K casino maps")
    for row in rows:
        name=row["file"]
        if name!=Path(name).name or not name.endswith(("_2k.jpg","_2k.png")):
            raise ValueError("Wrong texture path or resolution marker")
        file=DEST/"Textures2K"/name
        data=file.read_bytes()
        if len(data)!=row["bytes"] or digest(data)!=row["sha256"]:
            raise ValueError("Original 2K file changed: "+name)
        with Image.open(file) as im:
            if im.size!=(2048,2048):
                raise ValueError("Native 2K image dimensions invalid: "+name)
    result={"status":"SOURCES_VERIFIED_UNITY_NOT_TESTED","maps":21,"originalFiles":len(m["originalSourceFiles"]),
            "models":glbs,"originalGeometryAndPixelsUnchanged":True,"apkBuilt":False}
    OUTPUT.parent.mkdir(parents=True,exist_ok=True)
    OUTPUT.write_text(json.dumps(result,indent=2),encoding="utf-8")
    print("T11_REAL_ASSETS_CHECK_PASS maps=21 glb=2 bytes="+str(sum(x["bytes"] for x in glbs)))

if __name__=="__main__":
    main()
