"""Convert selected CC0 web-optimised GLBs to lossless-in-image Unity .gltf.

Source mesh bytes, geometry, PBR factors and node transforms are copied unchanged.
Only embedded WebP/JPEG/PNG images are externalised to PNG for native Unity ETC2
compression; each source and decoded-pixel digest is recorded.
"""
import hashlib
import io
import json
import pathlib
import struct
import uuid
from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[2]
SOURCE = ROOT / "build-source/Fitness"
DEST = ROOT / "unity-city/Assets/Kickboxing/Models/Fitness"

def digest(raw): return hashlib.sha256(raw).hexdigest()
def write(path,data):
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_bytes(data)
def meta(path,folder=False):
    f=pathlib.Path(str(path)+".meta")
    if not f.exists():
        guid=uuid.uuid5(uuid.NAMESPACE_URL,"braxhorn/kickboxing/fitness/"+str(path.relative_to(DEST.parent))).hex
        write(f,("fileFormatVersion: 2\nguid: "+guid+"\n"+("folderAsset: yes\n" if folder else "")).encode())
def read_glb(raw):
    assert struct.unpack_from("<4sII",raw)==(b"glTF",2,len(raw))
    parts={}
    pos=12
    while pos<len(raw):
        length,kind=struct.unpack_from("<I4s",raw,pos);pos+=8
        if kind in parts or pos+length>len(raw):raise ValueError("Malformed GLB chunk")
        parts[kind]=raw[pos:pos+length];pos+=length
    if set(parts)!={b"JSON",b"BIN\x00"}:raise ValueError("Missing GLB JSON / BIN")
    return json.loads(parts[b"JSON"]),parts[b"BIN\x00"]

def main():
    receipt=json.loads((SOURCE/"download-receipt.json").read_text())
    if receipt["modelCount"]!=5: raise RuntimeError("Expected five verified CC0 fitness models")
    DEST.mkdir(parents=True,exist_ok=True);meta(DEST,True)
    images=DEST/"Images";images.mkdir(exist_ok=True);meta(images,True)
    records=[]
    for item in receipt["assets"]:
        name=item["name"];raw=(SOURCE/(name+".glb")).read_bytes()
        if digest(raw)!=item["sha256"]: raise RuntimeError(name+" source GLB changed")
        original,binchunk=read_glb(raw);source=json.loads(json.dumps(original))
        if len(original.get("buffers",[]))!=1:raise RuntimeError("More than one GLB buffer")
        original["buffers"][0]["uri"]=name+".bin"
        changed=[]
        for i,img in enumerate(original.get("images",[])):
            if "bufferView" not in img:raise RuntimeError("Expected embedded source image")
            buf=original["bufferViews"][img["bufferView"]]
            content=binchunk[buf.get("byteOffset",0):buf.get("byteOffset",0)+buf["byteLength"]]
            with Image.open(io.BytesIO(content)) as picture:
                picture.load()
                if max(picture.size)>2048:raise RuntimeError("Unexpected unoptimised texture size")
                picture=picture.convert("RGBA" if picture.mode in ("RGBA","LA") or "transparency" in picture.info else "RGB")
                pixhash=digest(picture.tobytes())
                dst=images/(digest(picture.tobytes())+".png")
                data=io.BytesIO();picture.save(data,format="PNG",optimize=True)
                write(dst,data.getvalue());meta(dst)
            img.pop("bufferView",None);img.pop("mimeType",None);img["uri"]="Images/"+dst.name
            changed.append({"imageIndex":i,"sourceImageSha":digest(content),
                            "decodedPixelsSha256":pixhash,"convertedPngSha":digest(dst.read_bytes()),
                            "width":picture.width,"height":picture.height})
        for tex in original.get("textures",[]):
            extensions=tex.get("extensions",{})
            if "EXT_texture_webp" in extensions:
                tex["source"]=extensions.pop("EXT_texture_webp")["source"]
            if "KHR_texture_webp" in extensions:
                tex["source"]=extensions.pop("KHR_texture_webp")["source"]
            if not extensions:tex.pop("extensions",None)
        for key in ("extensionsUsed","extensionsRequired"):
            if key in original:
                original[key]=[x for x in original[key] if x not in ("EXT_texture_webp","KHR_texture_webp")]
                if not original[key]:original.pop(key)
        write(DEST/(name+".bin"),binchunk[:source["buffers"][0]["byteLength"]]);meta(DEST/(name+".bin"))
        path=DEST/(name+".gltf")
        write(path,json.dumps(original,sort_keys=True,separators=(",",":")).encode());meta(path)
        if original.get("accessors")!=source.get("accessors") or original.get("meshes")!=source.get("meshes") or original.get("nodes")!=source.get("nodes"):
            raise RuntimeError("Fitness geometry, node, or accessor mutated")
        records.append({"name":name,"sourceSha":item["sha256"],"triangles":sum(
            original["accessors"][p["indices"]]["count"]//3
            for m in original["meshes"] for p in m["primitives"] if p.get("indices") is not None),
            "meshGraphPreserved":True,"externalisedImages":changed})
        print("FITNESS_UNITY_IMPORT_MODEL_READY",name,len(original["meshes"]),len(changed))
    write(DEST/"conversion-receipt.json",json.dumps(
        {"sourcePack":"https://3dassets.dev/packs/gym-and-fitness-studio",
         "scope":"BRAXHORN isolated gym only, no ring import", "models":records},indent=2).encode())
    meta(DEST/"conversion-receipt.json")
if __name__=="__main__":main()
