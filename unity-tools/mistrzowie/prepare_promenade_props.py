"""Download real Poly Haven CC0 2K glTF models for BRAXHORN promenade.
No fake food meshes, no image generation, no substitutes, no 1K fallback.
Source is HTTPS API/official CDN only. Verifies provider MD5+size and real
2K color/normal/material images. Writes into future Unity 6 staging only.
Run from repo root: python3 unity-tools/mistrzowie/prepare_promenade_props.py
May run on a separate GitHub Actions runner without Unity or APK.

Uses public live API: output includes 'Powered by Poly Haven' credit.
"""
from __future__ import annotations

import argparse
from collections import Counter
from hashlib import md5,sha256
import json
from pathlib import Path,PurePosixPath
import sys
import time
import urllib.request

from PIL import Image

ROOT=Path(__file__).resolve().parents[2]
CATALOG=Path(__file__).with_name("promenade_polyhaven_models.json")
SOURCE=ROOT/"unity-city/Assets/Source/Mistrzowie/PolyHaven"
EVIDENCE=ROOT/"mistrzowie-evidence/promenade-polyhaven-import.json"
BASE="https://api.polyhaven.com"
CDN="https://dl.polyhaven.org/file/ph-assets/Models/"
MAX_SINGLE_BYTES=16*1024*1024
MAX_TOTAL_BYTES=230*1024*1024
USER_AGENT="BRAXHORN-CITY-Unity6-PolyHaven-CC0-Staging/1.0"
EXPECTED_ASSETS=27      # 25 kiosk goods + 2 flowers for the promenade planters (owner board 10.11 18:12)
ALLOWED_EXT={".jpg",".jpeg",".png",".bin",".gltf"}


def fetch(url,expected=None):
    if not(url.startswith(BASE+"/") or url.startswith(CDN)):
        raise ValueError("Disallowed source host/path: "+url)
    failure=None
    for attempt in range(3):
        try:
            req=urllib.request.Request(url,headers={"User-Agent":USER_AGENT,
                 "Accept":"application/json" if url.startswith(BASE) else "*/*"})
            with urllib.request.urlopen(req,timeout=100) as response:
                size=response.headers.get("Content-Length")
                if size and int(size)>MAX_SINGLE_BYTES and expected is not None:
                    raise ValueError("Single Poly Haven file exceeds mobile budget "+url)
                chunks=[]
                total=0
                while True:
                    part=response.read(1024*1024)
                    if not part:break
                    total+=len(part)
                    if total>MAX_SINGLE_BYTES and expected is not None:
                        raise ValueError("Poly Haven payload exceeds size cap: "+url)
                    chunks.append(part)
                return b"".join(chunks)
        except Exception as exc:
            failure=exc
            if attempt<2:time.sleep(2*(attempt+1))
    raise RuntimeError("Official Poly Haven source is unavailable: "+url) from failure


def parse_api(url):
    return json.loads(fetch(url).decode("utf-8"))


def safe_relative(relative):
    if not isinstance(relative,str) or not relative:
        raise ValueError("Model manifest has empty relative asset path")
    path=PurePosixPath(relative)
    if path.is_absolute() or ".." in path.parts or "\\" in relative or path.suffix.lower() not in ALLOWED_EXT:
        raise ValueError("Unsupported or unsafe model asset path: "+relative)
    return Path(*path.parts)


def file_checked(target,reference):
    if not isinstance(reference,dict) or "md5" not in reference or "size" not in reference or "url" not in reference:
        raise ValueError("Provider omitted original byte checksum for "+str(target))
    url=reference["url"]
    if not url.startswith(CDN):
        raise ValueError("Only Poly Haven's published model CDN is allowed: "+url)
    size=int(reference["size"])
    if size<=0 or size>MAX_SINGLE_BYTES:
        raise ValueError("Oversized/invalid model source asset: "+str(target)+" "+str(size))
    if not isinstance(reference["md5"],str) or len(reference["md5"])!=32:
        raise ValueError("Invalid provider checksum")
    if target.exists():
        data=target.read_bytes()
        if len(data)!=size or md5(data).hexdigest()!=reference["md5"]:
            raise ValueError("Existing source changed; refusing silent overwrite: "+str(target))
        return data,False
    data=fetch(url,expected=size)
    if len(data)!=size or md5(data).hexdigest()!=reference["md5"]:
        raise ValueError("Poly Haven bytes do not match official manifest: "+str(target))
    target.parent.mkdir(parents=True,exist_ok=True)
    temp=target.with_name(target.name+".partial")
    temp.write_bytes(data)
    temp.replace(target)
    return data,True


def validate_pbr(directory,relative_map):
    images=[str(x) for x in relative_map if x.suffix.lower() in (".jpg",".jpeg",".png")]
    if not images:
        raise ValueError("Model has no 2K PBR material maps "+str(directory))
    names=[Path(x).name.lower() for x in images]
    if not any("_diff_2k" in x or "_col_2k" in x for x in names):
        raise ValueError("Model lacks true 2K albedo: "+str(directory))
    if not any("_nor_gl_2k" in x for x in names):
        raise ValueError("Model lacks true OpenGL 2K normal: "+str(directory))
    if not any("_arm_2k" in x or "_rough_2k" in x for x in names):
        raise ValueError("Model lacks true 2K roughness/ARM: "+str(directory))
    for img in images:
        p=directory/img
        with Image.open(p) as source:
            width,height=source.size
        if min(width,height)<2048:
            raise ValueError("Texture below required minimum 2K: "+str(p)+" "+str((width,height)))


def download(only=None,metadata_only=False):
    data=json.loads(CATALOG.read_text(encoding="utf-8"))
    assets=data["assets"]
    if len(assets)!=EXPECTED_ASSETS or len({x["id"] for x in assets})!=EXPECTED_ASSETS:
        raise ValueError("Expected "+str(EXPECTED_ASSETS)+" distinct confirmed source assets")
    if only:
        assets=[x for x in assets if x["id"] in only]
        if len(assets)!=len(set(only)):raise ValueError("Unknown selected model IDs")
    info=parse_api(BASE+"/assets?type=models")
    report=[]
    total_budget=0
    for index,asset in enumerate(assets,1):
        id=asset["id"]
        listing=info.get(id)
        if not listing or listing.get("type")!=2:
            raise ValueError("3D model no longer exists on official Poly Haven: "+id)
        if listing.get("polycount")!=asset["polycount"]:
            raise ValueError("Source model geometry changed unexpectedly: "+id)
        if max(listing.get("max_resolution",[]),default=0)<2048:
            raise ValueError("Model no longer supports real 2K maps: "+id)
        details=parse_api(BASE+"/files/"+id)
        gltf=details.get("gltf",{}).get("2k",{}).get("gltf")
        if not gltf or not gltf.get("include"):
            raise ValueError("Official original 2K glTF source missing: "+id)
        elements=[(Path(id)/safe_relative(id+"_2k.gltf"),gltf)]
        components=gltf["include"]
        for name,ref in sorted(components.items()):
            elements.append((Path(id)/safe_relative(name),ref))
        names=[p.as_posix() for p,_ in elements]
        if len(set(names))!=len(names):
            raise ValueError("Provider duplicate files in one model: "+id)
        if not any(p.suffix==".bin" for p,_ in elements):
            raise ValueError("Source mesh binary is absent: "+id)
        estimated=sum(int(ref.get("size",0)) for _,ref in elements)
        total_budget+=estimated
        if total_budget>MAX_TOTAL_BYTES:
            raise ValueError("Premium 2K assets exceed capped source-package budget 230MiB")
        if not metadata_only:
            generated=0;files=[]
            for rel,ref in elements:
                destination=SOURCE/rel
                blob,created=file_checked(destination,ref)
                generated+=int(created)
                files.append(dict(file=rel.as_posix(),bytes=len(blob),
                                  md5=md5(blob).hexdigest(),sha256=sha256(blob).hexdigest()))
            validate_pbr(SOURCE/id,[safe_relative(n) for n in components])
        else:
            generated=0;files=[]
        report.append(dict(id=id,name=listing["name"],polycount=listing["polycount"],
                           kiosks=asset["kiosks"],format="gltf2k",
                           source_url="https://polyhaven.com/a/"+id,
                           source_bytes=estimated,
                           file_count=len(elements),new_files=generated,files=files))
        print(f"POLYHAVEN_SOURCE_OK {index}/{len(assets)} id={id} files={len(elements)} "+
              f"size={estimated} source_only={metadata_only}",flush=True)
    conclusion=dict(status="POLYHAVEN_2K_OFFICIAL_SOURCE_VERIFIED" if not metadata_only
                    else "POLYHAVEN_2K_METADATA_VERIFIED",
                    provider="Poly Haven / CC0",credits="Powered by Poly Haven",
                    assetCount=len(report),
                    totalOriginalBytes=sum(r["source_bytes"] for r in report),
                    maxSourceBytes=MAX_TOTAL_BYTES,
                    unityCompiled=False,androidTested=False,noFakes=True,
                    models=report)
    EVIDENCE.parent.mkdir(parents=True,exist_ok=True)
    EVIDENCE.write_text(json.dumps(conclusion,ensure_ascii=False,indent=2)+"\n")
    if not metadata_only:
        SOURCE.mkdir(parents=True,exist_ok=True)
        (SOURCE/"CREDITS.md").write_text(
            "# BRAXHORN CITY — imported original Poly Haven models\n\n"
            "Powered by Poly Haven — https://polyhaven.com\n"
            "All "+str(EXPECTED_ASSETS)+" model originals under CC0. Source references in "+
            "mistrzowie-evidence/promenade-polyhaven-import.json.\n"
            "Do not republish or modify original author/source declarations.\n")
    print("PROMENADE_POLYHAVEN_MODELS "+conclusion["status"]+
          " unique="+str(len(report))+" totalSourceBytes="+str(conclusion["totalOriginalBytes"]))
    return conclusion


if __name__=="__main__":
    cli=argparse.ArgumentParser()
    cli.add_argument("--ids",nargs="*",help="Optional selected original IDs for source connectivity tests")
    cli.add_argument("--metadata-only",action="store_true",help="Verify provider catalogue/URLs without downloading files")
    args=cli.parse_args()
    download(only=args.ids,metadata_only=args.metadata_only)
