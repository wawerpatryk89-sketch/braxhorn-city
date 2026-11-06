#!/usr/bin/env python3
"""Real content inspection of ORIGINAL owner geometry before expensive Unity.

Run AFTER restoring private sources/Blender exports, BEFORE starting Unity.
This is an integrity/asset-structure gate, NOT an Android appearance test.
No project files are written or modified by this script.
"""
from __future__ import annotations
import json
from pathlib import Path
import struct
import sys

ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/"unity-city/Assets/Source/Prywatne/Oboz/OwnerAssets"
PUBLIC=ROOT/"unity-city/Assets/Source/Public/Oboz"

# All required components are independent sibling nodes of the original
# Sketchfab GLB. A bare tabletop/base/basin is NOT a finished facility.
# Reject incomplete original geometry before invoking the Unity editor.
KITCHEN_PARTS=(
    "CookingTable.001_", "CookingTable_Bolts1.001_",
    "CookingTable_Bolts2.001_", "CookingTable_Legs.001_",
    "CookingTable_Shelf.001_", "PortableStove_Base.001_",
    "PortableStove_Burners.001_", "PortableStove_Grate_L_LP.001_",
    "PortableStove_Grate_R_LP.001_", "PortableStove_Tumbler_LP.001_",
    "PortableStove_Wire.001_", "GasCylinder.001_",
    "GasCylinder_Tap.001_", "PotRack.001_",
    "PotRack_Hooks.001_", "PotRack_Rails.001_", "Plate_Regular.001_",
    "Sink.001_", "Faucet_LP.001_", "Faucet_Tap_LP.001_",
    "Sink_Drain_LP.001_", "Sink_Legs_LP.001_",
)
GYM_PARTS=("treadmill","weight_bench","weight_rack",
           "exercise_bike","dumbbell_1","kettlebell_1","pull_up_rack")


def glb(path:Path,required=()):
    if not path.is_file():
        raise ValueError("Missing required original GLB: "+str(path))
    data=path.read_bytes()
    if len(data)<64 or data[:4]!=b"glTF" or struct.unpack_from("<II",data,4)!=(2,len(data)):
        raise ValueError("Invalid GLB 2.0 length/header: "+str(path))
    jlen=struct.unpack_from("<I",data,12)[0]
    if data[16:20]!=b"JSON" or jlen<20 or jlen+20>len(data):
        raise ValueError("Invalid GLB JSON chunk: "+str(path))
    info=json.loads(data[20:20+jlen])
    if not info.get("nodes") or not info.get("meshes"):
        raise ValueError("Original GLB contains no mesh/scene: "+str(path))
    names=[x.get("name","") for x in info["nodes"]]
    for part in required:
        if not any(n.startswith(part) for n in names):
            raise ValueError("Original GLB missing actual source part "+part+" in "+str(path))
    bad=[image["uri"] for image in info.get("images",[])
         if image.get("uri") and not image["uri"].startswith("data:")]
    if bad:raise ValueError("Original GLB needs unresolved external image "+str(bad[:2]))
    return len(info["meshes"])


def require(path:Path):
    if not path.is_file() or path.stat().st_size<=100:
        raise ValueError("Missing/empty original source: "+str(path))
    return path


def check_gym():
    fb=SOURCE/"OwnerExtras/Gym/gym_environment.fbx"
    raw=require(fb).read_bytes()
    if not (raw.startswith(b"Kaydara FBX Binary") or raw.lstrip().startswith(b"; FBX")):
        raise ValueError("Owner gym is not a genuine FBX: "+str(fb))
    lower=raw.lower()
    for name in GYM_PARTS:
        if name.encode() not in lower:
            raise ValueError("Owner gym FBX is missing requested equipment "+name)
    png=require(SOURCE/"OwnerExtras/Gym/gym_environment.png").read_bytes()[:24]
    if len(png)<24 or png[:8]!=b"\x89PNG\r\n\x1a\n" or struct.unpack_from(">II",png,16)!=(2048,2048):
        raise ValueError("Owner gym original color atlas is not 2048x2048 PNG")


def check_grass():
    for i in (1,2):
        key="grass_medium_0"+str(i)+"_2k"
        folder=SOURCE/key
        glb(folder/(key+".glb"))
        for role,ext in (("diff","jpg"),("alpha","png"),("nor_gl","exr"),("rough","exr")):
            require(folder/"textures"/("grass_medium_0"+str(i)+"_"+role+"_2k."+ext))


def check_facility():
    for material in ("rectangular_facade_tiles","long_white_tiles"):
        for role in ("Color","NormalGL","Roughness","AmbientOcclusion"):
            require(PUBLIC/material/(role+".jpg"))


def main():
    scores={}
    scores["kitchen_meshes"]=glb(SOURCE/"kitchen_-_assets.glb",KITCHEN_PARTS)
    scores["medicine_ball_rack"]=glb(SOURCE/"model(1).glb",("medicine-ball-rack",))
    scores["timber_bench"]=glb(SOURCE/"picnic_set_1.glb",("FNPC0101_Timber_bench",))
    for key,filename in (
        ("real_leather_bag","leather_punching_bag.glb"),
        ("metal_locker","metal_cabinet_low_poly.glb"),
        ("toilet","close-coupled-toilet.glb"),
        ("urinal","wall-urinal.glb"),
        ("washbasin","trough-washbasin.glb"),
        ("shower","shower-tray-square.glb"),
        ("bed","OwnerExtras/Bedroom/ikea_idanas_single_bed.glb"),
        ("tractor_tire","OwnerExtras/tractor_tire.glb"),
        ("drawer_cabinet_4k","drawer_cabinet_4k/drawer_cabinet_4k.glb"),
    ):
        scores[key]=glb(SOURCE/filename)
    check_gym()
    check_grass()
    check_facility()
    print("BRAXHORN_OBOZ_PREUNITY_OWNER_GEOMETRY=PASS "+json.dumps(scores,sort_keys=True))


if __name__=="__main__":
    try:main()
    except (OSError,ValueError,KeyError,UnicodeError) as err:
        print("::error::BRAXHORN_OBOZ_PREUNITY_OWNER_GEOMETRY_FAIL "+str(err),file=sys.stderr)
        raise SystemExit(2)
