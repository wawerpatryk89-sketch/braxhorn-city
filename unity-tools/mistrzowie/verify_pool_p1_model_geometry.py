#!/usr/bin/env python3
"""Offline glTF geometry preflight for the owner's original P1 SPA equipment.

Requires only Python stdlib. Does NOT modify archive, models, Unity prefabs
or original material pixels. Checks named source elements that the Unity 6
furnishing, 3D collisions and bubbling-water code actually depend on.
"""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import zipfile

PREFIX="unity-city/Assets/Source/Mistrzowie/PoolP1/OwnerEquipment/"
ARCHIVE="P1_BASEN_SPA_ORIGINALNE_WYPOSAZENIE_20261106.zip"
ARCHIVE_SHA256="5dd31e8422b2bddc046a0692448ce8522f7a22aae3a5fa617e9a0d74a74f1263"
EXPECTED={
    "hot-tub-original-exterior-model.glb":(
        "Spa water surface","Spa outer side","Spa access step","Spa padded rim"),
    "sauna_product_model.glb":("defaultMaterial",),
    "shower_cabin.glb":("Object_2","Object_3","Object_6"),
    "portable-massage-table.glb":(
        "Folding timber table leg","Split folding padded deck","Open padded face cradle"),
    "double-bathroom-vanity.glb":(
        "Case | horizontal panel","Basin | glazed ceramic bowl"),
    "pool-ladder.glb":("pool-ladder_body",),
    "lockers.glb":("Object_2",),
}

def sha256(path):
    h=hashlib.sha256()
    with path.open("rb") as f:
        for chunk in iter(lambda:f.read(1024*1024),b""):h.update(chunk)
    return h.hexdigest()

def read_glb(data,filename):
    if len(data)<20 or data[:4]!=b"glTF":
        raise ValueError("Original owner model is not binary glTF: "+filename)
    magic,version,total=struct.unpack_from("<4sII",data)
    if version!=2 or total!=len(data):
        raise ValueError("Corrupt GLB header/version/size: "+filename)
    length,kind=struct.unpack_from("<I4s",data,12)
    if kind!=b"JSON" or 20+length>len(data):
        raise ValueError("GLB JSON chunk invalid: "+filename)
    return json.loads(data[20:20+length])

def verify(path):
    if path.name!=ARCHIVE or not path.is_file() or sha256(path)!=ARCHIVE_SHA256:
        raise ValueError("Owner P1 private archive is absent or changed")
    with zipfile.ZipFile(path) as z:
        if z.testzip() is not None:
            raise ValueError("Owner P1 archive CRC check failed")
        for filename,required in EXPECTED.items():
            j=read_glb(z.read(PREFIX+filename),filename)
            nodes=j["nodes"]
            meshes=[n for n in nodes if "mesh" in n]
            names=[n.get("name","") for n in meshes]
            for name in required:
                if not any(x.startswith(name) for x in names):
                    raise ValueError("P1 actual model missing original source part: "+filename+" "+name)
            if filename=="hot-tub-original-exterior-model.glb":
                water=[n for n in meshes if n["name"]=="Spa water surface"]
                if len(water)!=1 or abs(water[0]["translation"][1]-.77)>.04:
                    raise ValueError("Jacuzzi original water mesh is absent/moved")
                materials=[m.get("name","") for m in j.get("materials",[])]
                if "Original pool water" not in materials:
                    raise ValueError("Jacuzzi original water material no longer exists")
            if filename=="sauna_product_model.glb":
                parents={index:n.get("name","") for n in nodes for index in n.get("children",[])}
                if not {"Metal","Stand","Base","Glass"}.issubset(set(parents.values())):
                    raise ValueError("Sauna source hierarchy no longer permits safe solid-only colliders")
            if filename=="shower_cabin.glb":
                if set(names)!={"Object_2","Object_3","Object_4","Object_5","Object_6"}:
                    raise ValueError("Owner shower glass/tray/metal geometry changed")
            print("P1_OWNER_GEOMETRY_PASS",filename,"meshNodes",len(meshes),flush=True)
    print("P1_OWNER_GEOMETRY_ALL_PASS",len(EXPECTED),
        "models | no rotations/scales/visual assets changed",flush=True)

if __name__=="__main__":
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument("owner_archive",type=Path)
    args=parser.parse_args()
    verify(args.owner_archive)
