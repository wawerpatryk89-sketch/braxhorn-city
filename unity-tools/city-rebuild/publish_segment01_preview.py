#!/usr/bin/env python3
"""Publish a REAL, verified, small city-segment Unity WebGL artifact.

Run only by the signed source branch's own GitHub Actions run after an actual
Unity 6 build and runtime screenshot QA. Never write main or production.
"""
import base64
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess

REPO="wawerpatryk89-sketch/braxhorn-city"
SOURCE_BRANCH="codex/city-street-segment-review-20261029"
PLAYER=Path("segment-player")
QA=Path("segment-qa")
DEST=Path("segment-stage")
RECEIPT=Path("segment-preview-publication")

def require(ok,message):
    if not ok:raise RuntimeError(message)

def sha256(path):
    with path.open("rb") as f:return hashlib.file_digest(f,"sha256").hexdigest()

def write(path,content):
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_text(json.dumps(content,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")

def main():
    commit=os.environ.get("GITHUB_SHA","")
    run=os.environ.get("GITHUB_RUN_ID","")
    require(re.fullmatch("[a-f0-9]{40}",commit),"No exact source SHA")
    require(os.environ.get("GITHUB_REPOSITORY")==REPO,"Wrong repository")
    require(os.environ.get("GITHUB_REF")=="refs/heads/"+SOURCE_BRANCH,"Wrong branch")
    require(PLAYER.is_dir() and QA.is_dir() and not DEST.exists(),"Exact Unity/QA inputs missing")
    release=json.loads((PLAYER/"release.json").read_text())
    city=json.loads((PLAYER/"city-migration.json").read_text())
    segment=json.loads((PLAYER/"city-segment-review.json").read_text())
    test=json.loads((QA/"segment-qa.json").read_text())
    require(release.get("sourceCommit")==commit and
            str(release.get("sourceRun"))==run and
            release.get("productionPublication") is False,"Actual Unity build ID mismatch")
    require(city.get("scope")=="city-segment-mobile-architecture-review" and
            city.get("commit")==commit and
            [a.get("area") for a in city.get("areas",[])]==["city"],
            "The compiled player is not a one-scene city segment")
    require(segment.get("sourceCommit")==commit and
            segment.get("originalBrax") is True and
            segment.get("selectedBuildingFrontages",0)>=5 and
            segment.get("selectedHighRises",0)>=2 and
            segment.get("fullCitySourceRetained") is True,
            "Part of the source scene was missing")
    require(test.get("passed") is True and test.get("sourceCommit")==commit and
            test.get("physicalIphonePass") is False and
            len(test.get("captures",[]))>=10,
            "The exact compiled Unity screenshots and joystick checks did not pass")
    files=release.get("files",{})
    require(isinstance(files,dict) and len(files)>20,"Missing compiled Unity transport manifest")
    for relative,digest in files.items():
        require(isinstance(relative,str) and re.fullmatch("[A-Za-z0-9._/-]+",relative)
                and not relative.startswith("/") and ".." not in relative.split("/"),
                "Unsafe file path")
        f=PLAYER/relative
        require(f.is_file() and not f.is_symlink() and f.stat().st_size<99*1024*1024,
                "Missing or oversized Unity file "+relative)
        require(sha256(f)==digest,"Tampered compiled game "+relative)
    require({f.relative_to(PLAYER).as_posix() for f in PLAYER.rglob("*") if f.is_file()}==
            set(files)|{"release.json"},"Unmanifested player content")
    require(release["transport"]["bytes"]<235_000_000 and
            release["transport"]["uncompressedBytes"]<340_000_000,
            "City stage is not sufficiently small for quick phone review")
    shutil.copytree(PLAYER,DEST)
    photographs=DEST/"review-photographs";photographs.mkdir()
    captures=test["captures"]
    for capture in captures:
        name=capture["name"]
        require(re.fullmatch("[A-Za-z0-9_.-]+[.]png",name),"Unsafe screenshot name")
        original=QA/name
        require(original.is_file() and original.open("rb").read(8)==bytes((137,80,78,71,13,10,26,10)),
                "Missing real screenshot "+name)
        shutil.copyfile(original,photographs/name)
    (DEST/"robots.txt").write_text("User-agent: *\nDisallow: /\n")
    write(DEST/"vercel.json",{
       "framework":None,"buildCommand":"","installCommand":"",
       "outputDirectory":".","cleanUrls":False,
       "headers":[{"source":"/","headers":[{"key":"Cache-Control","value":"no-store"}]},
                  {"source":"/release.json","headers":[{"key":"Cache-Control","value":"no-store"}]}]
    })
    write(DEST/"preview-qa-summary.json",{
       "scope":"CITY_SEGMENT_01","sourceCommit":commit,
       "desktopQAPassed":True,"photosFromActualUnity":len(captures),
       "westLimit":segment["xMin"],"eastLimit":segment["xMax"],
       "onlyOriginalBrax":True,
       "camera":"Behind Brax while walking forward, manual right-hand orbit, full vertical sky view",
       "sky":"Real baked Unity panoramic cloud map with a sun disc",
       "billboards":"Only source-provenance BRAXHORN images",
       "physicalIphoneAcceptance":"NOT YET TESTED",
       "continuousWalkAcceptance":"NOT YET CLAIMED",
       "visualAcceptance":"PENDING USER CONFIRMATION",
       "productionChanged":False})
    out_branch="preview/city-segment01-"+commit[:12]
    token=os.environ.get("GH_TOKEN")
    require(bool(token),"Cannot publish without GitHub workflow token")
    env=os.environ.copy();env.pop("GH_TOKEN",None)
    env.update(GIT_CONFIG_COUNT="1",GIT_CONFIG_KEY_0="http.https://github.com/.extraheader",
               GIT_CONFIG_VALUE_0="AUTHORIZATION: basic "+
                 base64.b64encode(("x-access-token:"+token).encode()).decode())
    def git(*args,authenticated=False):
        return subprocess.check_output(["git","-c","core.hooksPath=/dev/null",*args],
           cwd=DEST,env=env if authenticated else None,text=True).strip()
    git("init","--initial-branch="+out_branch)
    git("config","user.name","github-actions[bot]")
    git("config","user.email","41898282+github-actions[bot]@users.noreply.github.com")
    git("remote","add","origin","https://github.com/"+REPO+".git")
    require(not git("ls-remote","--heads","origin","refs/heads/"+out_branch,authenticated=True),
            "Existing immutable preview must not be overwritten")
    git("add","--all")
    git("commit","-m","preview(city): playable real Unity segment "+commit[:12]+" (awaiting owner approval)")
    preview_sha=git("rev-parse","HEAD")
    git("push","origin","HEAD:refs/heads/"+out_branch,authenticated=True)
    receipt={"previewBranch":out_branch,"previewCommit":preview_sha,
             "compiledUnitySha":commit,"compiledRun":run,
             "cityOnly":True,"desktopQA":True,"realUnityImages":len(captures),
             "phoneAcceptance":False,"productionChanged":False}
    write(RECEIPT/"receipt.json",receipt)
    print("CITY_SEGMENT_REAL_UNITY_PREVIEW_PUBLISHED",json.dumps(receipt),flush=True)
    with open(os.environ["GITHUB_STEP_SUMMARY"],"a") as f:
        f.write("## Segment 01 — real Unity WebGL iPhone trial\n\n")
        f.write("Preview Git branch: "+out_branch+"\n\n")
        f.write("Actual city source: "+commit+"\n\n")
        f.write("Phone acceptance remains pending; no production branch changed.\n")

if __name__=="__main__":
    main()
