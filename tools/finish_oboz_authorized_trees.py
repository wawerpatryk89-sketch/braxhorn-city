"""Preserve original foliage RGB/alpha, then use the existing mobile GLB path.

Run with the repo's existing requirements-assets.txt Python environment, after
the Blender authored-LOD export. All intermediate models stay outside Assets.
Only verified finished rows enter the installable list; no substitute geometry.
"""
from __future__ import annotations
import argparse
import copy
import hashlib
import io
import json
from pathlib import Path
import struct


def read_glb(raw: bytes):
    if len(raw)<20:raise ValueError("Truncated original GLB")
    magic,version,total=struct.unpack_from("<4sII",raw)
    if magic!=b"glTF" or version!=2 or total!=len(raw):raise ValueError("Invalid original GLB header")
    position=12;document=None;binary=None
    while position<len(raw):
        if position+8>len(raw):raise ValueError("Truncated original GLB chunk")
        size,kind=struct.unpack_from("<II",raw,position);position+=8
        chunk=raw[position:position+size];position+=size
        if len(chunk)!=size:raise ValueError("Truncated original GLB data")
        if kind==0x4E4F534A:document=json.loads(chunk)
        elif kind==0x004E4942:binary=chunk
    if document is None or binary is None:raise ValueError("Missing original GLB JSON/BIN")
    return document,binary


def pack_original_rgba(diffuse: Path, alpha: Path) -> bytes:
    import numpy as np
    import png
    def read(path):
        with path.open("rb") as file:
            width,height,rows,info=png.Reader(file=file).asDirect()
            if info["bitdepth"] not in (8,16):
                raise ValueError("Unsupported original PNG bit depth: " + str(path))
            dtype=np.uint16 if info["bitdepth"]==16 else np.uint8
            pixels=np.stack([np.asarray(row,dtype=dtype) for row in rows]).reshape(height,width,info["planes"])
            return pixels,info["bitdepth"]
    color,color_depth=read(diffuse);opacity,alpha_depth=read(alpha)
    if color.shape[:2] != opacity.shape[:2] or color_depth != alpha_depth or color.shape[2] < 3:
        raise ValueError("Original colour/alpha dimensions or precision differ; no implicit resampling")
    rgba=np.concatenate([color[:,:,:3],opacity[:,:,:1]],axis=2)
    height,width=rgba.shape[:2];output=io.BytesIO()
    png.Writer(width=width,height=height,greyscale=False,alpha=True,bitdepth=color_depth).write(output,rgba.reshape(height,width*4))
    raw=output.getvalue();rw,rh,rows,info=png.Reader(bytes=raw).asDirect()
    reread=np.stack([np.asarray(row,dtype=rgba.dtype) for row in rows]).reshape(height,width,4)
    if (rw,rh)!=(width,height) or not np.array_equal(rgba,reread):
        raise ValueError("Original RGB/alpha changed during packing")
    return raw


def repair_model(path: Path, asset_id: str, rgba: bytes, evidence: dict) -> dict:
    original=path.read_bytes();document,binary=read_glb(original);before=copy.deepcopy(document)
    token="twig" if asset_id=="pine_tree_01" else "leaves"
    image_indices=set();restored=[]
    expected='pine_tree_01_twig' if asset_id=='pine_tree_01' else 'tree_small_02_leaves'
    if not evidence.get('sourceCoverageVerified') or not evidence.get('allImageSamplersIdentical') or evidence.get('material')!=expected:
        raise ValueError('No verified original foliage graph/sampler evidence')
    for material in document.get("materials",[]):
        name=material.get("name","").lower()
        if name!=expected:continue
        if material.get('alphaMode','OPAQUE')=='OPAQUE':
            if asset_id!='pine_tree_01' or not evidence.get('restoreExportedOpaqueAlpha'):
                raise ValueError('Export lost source coverage without verified repair rule')
            material['alphaMode']='BLEND';restored.append(name)
        info=material.get("pbrMetallicRoughness",{}).get("baseColorTexture")
        if not info:raise ValueError("Foliage has no original colour map")
        texture=document["textures"][info["index"]]
        if "source" not in texture:raise ValueError("Unsupported foliage texture extension")
        image_indices.add(texture["source"])
    if not image_indices:
        raise ValueError("No matching original masked/transparent foliage material; no guessed replacement")
    # The inherited mobile externalizer supports only one sampler per image.
    # Do not silently feed ambiguous semantics into it.
    samplers={}
    for texture in document.get("textures",[]):
        if "source" not in texture:raise ValueError("Unsupported texture source")
        samplers.setdefault(texture["source"],set()).add(texture.get("sampler",-1))
    if any(len(keys)>1 for keys in samplers.values()):
        raise ValueError("Multiple samplers for one image require the newer role-aware converter")
    result=bytearray(binary)
    for index in sorted(image_indices):
        offset=len(result);result.extend(rgba);result.extend(b"\0"*(-len(result)%4))
        document["images"][index].pop("uri",None)
        document["images"][index]["bufferView"]=len(document["bufferViews"])
        document["images"][index]["mimeType"]="image/png"
        document["bufferViews"].append({"buffer":0,"byteOffset":offset,"byteLength":len(rgba)})
    document["buffers"][0]["byteLength"]=len(result)
    for material in before.get('materials',[]):
        if material.get('name') in restored:material['alphaMode']='BLEND'
    for key in ("meshes","accessors","nodes","skins","animations","materials","textures","samplers"):
        if document.get(key)!=before.get(key):raise ValueError("Geometry/material/sampler changed: "+key)
    encoded=json.dumps(document,separators=(",",":"),ensure_ascii=False).encode();encoded+=b" "*(-len(encoded)%4)
    raw=struct.pack("<4sII",b"glTF",2,28+len(encoded)+len(result))+struct.pack("<II",len(encoded),0x4E4F534A)+encoded+struct.pack("<II",len(result),0x004E4942)+result
    path.write_bytes(raw)
    return {"sourceExportSha256":hashlib.sha256(original).hexdigest(),"colorRepairedSha256":hashlib.sha256(raw).hexdigest(),
            "originalGeometryBytesUnchanged":bytes(result[:len(binary)])==binary,"foliageImagesRepaired":len(image_indices),
            "sourceAlphaCoverageRestored":restored}


def finish(sources: Path, models: Path, manifest_path: Path, export_path: Path, report_path: Path):
    from prepare_glb_mobile_import import prepare
    if any(part.lower()=="assets" for path in (sources,models) for part in path.resolve().parts):
        raise ValueError("Tree preparation must remain outside Unity Assets")
    manifest=json.loads(manifest_path.read_text());export=json.loads(export_path.read_text())
    assets={item["id"]:item for item in manifest["assets"]}
    result={"title":"OBÓZ TRENINGOWY — przygotowane oryginalne LOD drzew","schema":1,"models":[],"warnings":[],
            "unityImported":False,"apkBuilt":False,"geometryDecimated":False,"foliageColorPackingVerified":False,
            "knownLimitations":export.get("knownLimitations",[])}
    packed={}
    for row in export.get("models",[]):
        try:
            if row.get("authoredLod") not in (1,2) or row.get("geometryDecimated") or row.get("sourceAsset") not in assets:
                raise ValueError("Unapproved source or LOD")
            if row.get("exportedTriangles") != row.get("triangles"):
                raise ValueError("Exported triangle count differs from authored source; no installation")
            source=assets[row["sourceAsset"]];root=sources/source["sourceSubdirectory"]
            if source["id"] not in packed:
                packed[source["id"]]=pack_original_rgba(root/source["foliageDiffuse"],root/source["foliageAlpha"])
            path=models/row["file"]
            if hashlib.sha256(path.read_bytes()).hexdigest()!=row["sha256"]:
                raise ValueError("Raw source export hash mismatch")
            repaired=repair_model(path,source["id"],packed[source["id"]],row.get('foliageSourceEvidence',{}))
            mobile=prepare(path,shared_textures="authorized_forest_textures",compact=False)
            result["models"].append(dict(row,**repaired,mobile=mobile,installable=True))
        except Exception as error:
            text=row.get("file","unknown")+": "+str(error);result["warnings"].append(text)
            print("BRAXHORN_OBOZ_AUTHORED_TREE_WARNING "+text,flush=True)
    # Required authored levels must be complete. Small tree has only LOD1.
    prototypes={}
    for row in result["models"]:prototypes.setdefault(row["prototype"],{})[row["authoredLod"]]=row
    for prototype,lods in prototypes.items():
        source=assets[next(iter(lods.values()))["sourceAsset"]]
        if set(lods)!=set(source.get("requiredLodLevels",[1,2])):
            result["warnings"].append(prototype+": incomplete finished LOD pair; no installation")
            for row in lods.values():row["installable"]=False
    result["foliageColorPackingVerified"]=bool(result["models"]) and all(row["originalGeometryBytesUnchanged"] for row in result["models"])
    result["complete"]=export.get("complete",False) and not result["warnings"] and bool(result["models"])
    report_path.parent.mkdir(parents=True,exist_ok=True);report_path.write_text(json.dumps(result,ensure_ascii=False,indent=2)+"\n")
    return result


if __name__=="__main__":
    p=argparse.ArgumentParser(description=__doc__);p.add_argument("--sources",type=Path,required=True)
    p.add_argument("--models",type=Path,required=True);p.add_argument("--manifest",type=Path,required=True)
    p.add_argument("--export-report",type=Path,required=True);p.add_argument("--report",type=Path,required=True)
    a=p.parse_args();finish(a.sources,a.models,a.manifest,a.export_report,a.report)
