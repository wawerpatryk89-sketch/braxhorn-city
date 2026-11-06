"""Stage the owner's unchanged temporary 3x4 promenade signage atlas from
the existing A1+T11 draft GitHub Release. Never generates or crops artwork.
Runs only on the explicitly requested Champions APK preparation path.
"""
from pathlib import Path
import hashlib
import json
import os
import struct
import urllib.request
import uuid

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/"unity-city/Assets/Source/Mistrzowie/Promenade/BRAXHORN_PROMENADA_K1_K12_ATLAS.png"
RELEASE_ID=435656828
ASSET_ID=715528115
UPLOAD_NAME="D73D6E26-5D8B-4C6B-B585-0748476343DD.png"
EXPECTED_SHA256="eac1e17f55d8b77340e7d2cca4c9f97ea72e256acaf8237286ff550012145886"
EXPECTED_SIZE=2564862
EXPECTED_WH=(2172,724)
META_NAMESPACE=uuid.UUID("1a97e7ad-8c44-4ccd-9c80-c0c49d7abe75")


def sha(blob):
    return hashlib.sha256(blob).hexdigest()


def validate(blob):
    if len(blob)!=EXPECTED_SIZE or sha(blob)!=EXPECTED_SHA256:
        raise ValueError("Uploaded owner atlas does not match the original image bytes")
    if (blob[:8]!=b"\x89PNG\r\n\x1a\n" or blob[12:16]!=b"IHDR" or
        struct.unpack(">II",blob[16:24])!=EXPECTED_WH or blob[25]!=6):
        raise ValueError("Owner atlas is not the original 2172x724 RGBA PNG")
    return True


def api(url,token,accept="application/vnd.github+json"):
    request=urllib.request.Request(url,headers={
        "Authorization":"Bearer "+token,
        "Accept":accept,
        "X-GitHub-Api-Version":"2022-11-28",
        "User-Agent":"BRAXHORN-Champions-Atlas-SourceCheck/1.0"})
    with urllib.request.urlopen(request,timeout=90) as result:
        return result.read()


def original_asset():
    token=os.environ.get("GH_TOKEN")
    if not token:
        raise RuntimeError("GH_TOKEN required to read owner's draft release")
    repo=os.environ.get("GITHUB_REPOSITORY","wawerpatryk89-sketch/braxhorn-city")
    prefix="https://api.github.com/repos/"+repo
    info=json.loads(api(prefix+"/releases/"+str(RELEASE_ID),token))
    if (info.get("id")!=RELEASE_ID or info.get("tag_name")!="champions-a1-20261105"
        or info.get("target_commitish")!="gpt/miasto-mistrzow-kasyno-t11-20261105"):
        raise RuntimeError("Wrong owner's Miasto Mistrzów A1/T11 draft Release")
    matches=[a for a in info.get("assets",[]) if a.get("id")==ASSET_ID
             and a.get("name")==UPLOAD_NAME and a.get("size")==EXPECTED_SIZE]
    if len(matches)!=1:
        raise RuntimeError("Owner's original atlas asset ID or metadata no longer match")
    result=api(prefix+"/releases/assets/"+str(ASSET_ID),token,
               accept="application/octet-stream")
    validate(result)
    return result


def ensure_meta(path):
    relative=path.relative_to(ROOT/"unity-city").as_posix()
    expected=uuid.uuid5(META_NAMESPACE,relative).hex
    meta=Path(str(path)+".meta")
    if meta.exists():
        import re
        match=re.search(r"^guid:\s*([0-9a-fA-F]{32})",meta.read_text(),re.M)
        if not match or match.group(1).lower()!=expected:
            raise ValueError("Original sign atlas Unity GUID must not be overwritten: "+str(meta))
    else:
        meta.write_text("fileFormatVersion: 2\nguid: "+expected+"\n",encoding="utf-8")


def prepare():
    OUT.parent.mkdir(parents=True,exist_ok=True)
    if OUT.exists():
        validate(OUT.read_bytes())
    else:
        blob=original_asset()
        tmp=OUT.with_name(OUT.name+".partial")
        try:
            tmp.write_bytes(blob)
            tmp.replace(OUT)
        finally:
            tmp.unlink(missing_ok=True)
    ensure_meta(OUT.parent)
    ensure_meta(OUT)
    print("CHAMPIONS_OWNER_ATLAS_PASS source=GitHubRelease owner_original_sha256="+EXPECTED_SHA256+
          " source=2172x724 pixels cells=3x4 source_file_untouched=true",
          flush=True)


if __name__=="__main__":
    prepare()
