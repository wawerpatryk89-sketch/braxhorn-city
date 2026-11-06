#!/usr/bin/env python3
"""Official Poly Haven CC0 PBR for the ONE concrete bathroom of BRAXHORN OBÓZ.

Never substitute artificial monochrome tiles. Each downloaded JPEG is cross-
verified against Poly Haven's HTTPS /files API, checked for 2048×2048 pixels,
and assigned deterministic Unity GUID. Source payload SHA-256 is logged.
This does NOT run Unity, change the player's rig, or launch an APK.
"""
from __future__ import annotations
import hashlib
import io
import json
from pathlib import Path
import re
import urllib.parse
import urllib.request

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / "unity-city/Assets/Source/Public/Oboz"
ASSETS = {
    "rectangular_facade_tiles": "dark concrete exterior cladding, owner requested",
    "long_white_tiles": "white wall tiling inside WC/ŁAŹNIA/UMYWALNIA",
}
ROLES = {
    "Color": ("Diffuse", "_diff_"),
    "NormalGL": ("nor_gl", "_nor_gl_"),
    "Roughness": ("Rough", "_rough_"),
    "AmbientOcclusion": ("AO", "_ao_"),
}
API = "https://api.polyhaven.com/files/"

def source(asset, role):
    return ("https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/"
            f"{asset}/{asset}{ROLES[role][1]}2k.jpg")

def official_file(api_data, role):
    alias = {
        "Diffuse": ["Diffuse", "diff", "diffuse"],
        "nor_gl": ["nor_gl", "NormalGL", "normal_gl", "Normal (GL)"],
        "Rough": ["Rough", "rough", "roughness"],
        "AO": ["AO", "ao", "AmbientOcclusion"],
    }[ROLES[role][0]]
    for name in alias:
        row = api_data.get(name)
        if isinstance(row, dict):
            jpg = row.get("2k", {}).get("jpg")
            if isinstance(jpg, dict): return jpg
    raise ValueError(f"Official Poly Haven API missing 2K JPEG role {role}")

def fetch_bytes(url, limit=28_000_000):
    u = urllib.parse.urlparse(url)
    if u.scheme != "https" or u.netloc not in ("api.polyhaven.com", "dl.polyhaven.org"):
        raise ValueError("Unapproved host")
    with urllib.request.urlopen(urllib.request.Request(
            url, headers={"User-Agent":"BraxhornUnity6-OwnerFacility/1"}), timeout=100) as response:
        final = urllib.parse.urlparse(response.geturl())
        if final.scheme != "https" or final.netloc not in ("api.polyhaven.com", "dl.polyhaven.org"):
            raise ValueError("Unexpected redirect")
        data = response.read(limit+1)
    if len(data)>limit: raise ValueError("Official asset unexpectedly large")
    return data

def jpeg_size(data):
    if len(data)<10 or data[:2]!=b"\xff\xd8": raise ValueError("Not a JPEG")
    # Minimal no-Pillow header parser: recognizes baseline and progressive JPEG.
    i=2
    while i<len(data):
        if data[i]!=0xff: raise ValueError("Malformed JPEG segment")
        while i<len(data) and data[i]==0xff:i+=1
        if i>=len(data):break
        marker=data[i];i+=1
        if marker in (0xD8,0xD9,0x01) or 0xD0<=marker<=0xD7:continue
        if i+2>len(data):break
        length=int.from_bytes(data[i:i+2],"big")
        if length<2 or i+length>len(data):break
        if marker in (0xC0,0xC1,0xC2,0xC3,0xC5,0xC6,0xC7,0xC9,0xCA,0xCB):
            if length<7:break
            return (int.from_bytes(data[i+5:i+7],"big"),
                    int.from_bytes(data[i+3:i+5],"big"))
        i+=length
    raise ValueError("JPEG dimensions unavailable")

def main():
    receipt=[]
    for asset in ASSETS:
        raw=fetch_bytes(API+asset,limit=4_000_000)
        metadata=json.loads(raw)
        for role in ROLES:
            row=official_file(metadata,role)
            url=source(asset,role)
            if row.get("url")!=url or not re.fullmatch(r"[0-9a-f]{32}",row.get("md5","")):
                raise ValueError(f"Official URL or MD5 mismatch: {asset} {role}")
            expected_size=int(row.get("size",0))
            if not (500<=expected_size<=28_000_000):
                raise ValueError("Invalid official file size")
            dest=BASE/asset/(role+".jpg")
            dest.parent.mkdir(parents=True,exist_ok=True)
            if dest.is_file():
                data=dest.read_bytes()
            else:
                data=fetch_bytes(url)
            if len(data)!=expected_size or hashlib.md5(data).hexdigest()!=row["md5"]:
                raise ValueError(f"Official map fails MD5/size: {asset} {role}")
            if jpeg_size(data)!=(2048,2048):
                raise ValueError(f"Official map must be genuine 2048x2048: {asset} {role}")
            if not dest.exists():dest.write_bytes(data)
            # Same deterministic GUID convention as public_materials.py.
            from public_materials import ensure_meta
            rel=f"unity-city/Assets/Source/Public/Oboz/{asset}/{role}.jpg"
            ensure_meta(dest,{"path":rel})
            receipt.append({"asset":asset,"role":role,"url":url,"bytes":len(data),
                "sha256":hashlib.sha256(data).hexdigest(),"md5":row["md5"],
                "width":2048,"height":2048,"license":"CC0-1.0"})
    report=ROOT/"unity-city/facility-polyhaven-audit.json"
    report.write_text(json.dumps({"schema":1,"sources":
        ["https://polyhaven.com/a/"+a for a in ASSETS],
        "files":receipt},indent=2)+"\n")
    print(f"BRAXHORN_OBOZ_FACILITY_POLYHAVEN=PASS sources={len(ASSETS)} maps={len(receipt)} real2K=true")

if __name__=="__main__":
    main()
