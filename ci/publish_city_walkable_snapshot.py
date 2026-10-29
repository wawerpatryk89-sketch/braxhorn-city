#!/usr/bin/env python3
"""Preview deployment from exact signed 2026-10-29 205MB Unity WebGL snapshot.
This is a playable stopgap, NOT the new segmented build or physical iPhone PASS.
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
SNAPSHOT="45a9555f3a449d2d78165882b465c8bb82267d7f"
ORIGINAL_RUN="40930737141"
PREVIEW="preview/city-walkable-"+SNAPSHOT[:12]
BRANCH="test/city-walkable-ios-snapshot-20261029"
PLAYER=Path("player")
SMOKE=Path("walking-smoke")
OUT=Path("walkable-preview")

def require(condition,message):
    if not condition:raise RuntimeError(message)
def digest(file):
    with file.open("rb") as f:return hashlib.file_digest(f,"sha256").hexdigest()
def write(file,obj):
    file.parent.mkdir(parents=True,exist_ok=True)
    file.write_text(json.dumps(obj,ensure_ascii=False,indent=2)+"\n")
def run_git(*args,auth=False):
    return subprocess.check_output(["git","-c","core.hooksPath=/dev/null",*args],cwd=OUT,
        env=auth_env if auth else None,text=True).strip()

def main():
    require(os.environ.get("GITHUB_REPOSITORY")==REPO,"Wrong private repository")
    require(os.environ.get("GITHUB_REF")=="refs/heads/"+BRANCH,"Wrong release branch")
    require(PLAYER.is_dir() and SMOKE.is_dir() and not OUT.exists(),"Missing signed Unity or smoke artifacts")
    release=json.loads((PLAYER/"release.json").read_text())
    qa=json.loads((SMOKE/"smoke.json").read_text())
    require(release.get("sourceCommit")==SNAPSHOT and
            str(release.get("sourceRun"))==ORIGINAL_RUN and
            release.get("productionPublication") is False,
            "Not previously reviewed Unity 6.0.58f2 build")
    require(release.get("target")=="WebGL" and
            release.get("unityVersion")=="6000.0.58f2","Not native Unity 6 WebGL")
    require(qa.get("passed") is True and
            qa.get("originalBraxReady") is True and
            qa.get("movementPassed") is True and
            qa.get("loaderPassed") is True and
            qa.get("physicalIphoneTested") is False and
            qa.get("sourceCommit")==SNAPSHOT,
            "No passing real Unity startup and Brax movement QA")
    meta=json.loads((PLAYER/"city-migration.json").read_text())
    require(meta.get("commit")==SNAPSHOT and
            meta.get("scope")=="city-only-mobile-architecture-review" and
            [a.get("area") for a in meta.get("areas",[])]==["city"],
            "Source includes wrong/extra Unity area")
    require(release.get("transport",{}).get("bytes",999999999)<210000000 and
            release.get("transport",{}).get("uncompressedBytes",999999999)<305000000,
            "Oversized for this deliberately reduced preview")
    files=release.get("files",{})
    require(isinstance(files,dict) and len(files)>=35,"Incomplete signed Unity file manifest")
    for filename,sha in files.items():
        require(isinstance(filename,str) and
                re.fullmatch("[A-Za-z0-9._/-]+",filename) and
                ".." not in filename.split("/") and not filename.startswith("/"),
                "Unsafe path in release manifest")
        file=PLAYER/filename
        require(file.is_file() and not file.is_symlink() and
                file.stat().st_size<99*1024*1024 and digest(file)==sha,
                "Unity compiled byte changed/missing "+filename)
    require({p.relative_to(PLAYER).as_posix() for p in PLAYER.rglob("*") if p.is_file()} ==
            set(files)|{"release.json"},"Unmanifested compiled Unity files")
    shutil.copytree(PLAYER,OUT)
    (OUT/"robots.txt").write_text("User-agent: *\nDisallow: /\n")
    write(OUT/"vercel.json",{
      "framework":None,"buildCommand":"","installCommand":"",
      "outputDirectory":".","cleanUrls":False,
      "headers":[
        {"source":"/","headers":[{"key":"Cache-Control","value":"no-store"}]},
        {"source":"/index.html","headers":[{"key":"Cache-Control","value":"no-store"}]},
        {"source":"/release.json","headers":[{"key":"Cache-Control","value":"no-store"}]}
      ]
    })
    write(OUT/"preview-qa-summary.json",{
      "status":"CITY_ONLY_EARLIER_WALKABLE_SNAPSHOT_NOT_NEW_SEGMENT",
      "sourceCommit":SNAPSHOT,"sourceRun":int(ORIGINAL_RUN),
      "webGLCompiled":True,"desktopBraxWalkPassed":True,"desktopGzipStreamingPassed":True,
      "sourceBinaryVerified":True,
      "originalCompiledBytes":release["transport"]["bytes"],
      "mobileIPhoneNotTested":True,
      "fullWalkingRouteNotAccepted":True,
      "roofElevatorNotAccepted":True,
      "metroSign":"Previous BRAXHORN label in this pinned build; fixed only in later source",
      "knownLimitations":[
        "Old world geometry, not the new 1st segment with clouds/autofollow camera",
        "Whole 170-metre city scene but excludes original estate",
        "No active NPC, police, vehicles, combat, or missions",
        "Roof elevator installed but not end-to-end tested",
        "Original walk test had a route obstruction near NOVA; fix is in next uncompiled branch"
      ],
      "productionPublication":False
    })
    token=os.environ.get("GH_TOKEN")
    require(token,"Missing short-lived GitHub Actions permission")
    global auth_env
    auth_env=os.environ.copy();auth_env.pop("GH_TOKEN",None)
    auth_env.update(GIT_CONFIG_COUNT="1",
       GIT_CONFIG_KEY_0="http.https://github.com/.extraheader",
       GIT_CONFIG_VALUE_0="AUTHORIZATION: basic "+
         base64.b64encode(("x-access-token:"+token).encode()).decode())
    run_git("init","--initial-branch="+PREVIEW)
    run_git("config","user.name","github-actions[bot]")
    run_git("config","user.email","41898282+github-actions[bot]@users.noreply.github.com")
    run_git("remote","add","origin","https://github.com/"+REPO+".git")
    require(not run_git("ls-remote","--heads","origin","refs/heads/"+PREVIEW,auth=True),
            "Refusing to overwrite an already published preview")
    run_git("add","--all")
    run_git("commit","-m","preview(city): genuine optimized WebGL with working Brax "+SNAPSHOT[:12])
    new_sha=run_git("rev-parse","HEAD")
    run_git("push","origin","HEAD:refs/heads/"+PREVIEW,auth=True)
    receipt={"branch":PREVIEW,"previewCommit":new_sha,"compiledSource":SNAPSHOT,
             "originalUnityRun":int(ORIGINAL_RUN),"startupQA":True,"BraxMovementQA":True,
             "iPhonePass":False,"newStageOne":False,"productionChanged":False}
    write(Path("walkable-receipt")/"published.json",receipt)
    print("CITY_ACTUAL_WALKABLE_SNAPSHOT_PUBLISHED",json.dumps(receipt))
    with open(os.environ["GITHUB_STEP_SUMMARY"],"a") as f:
        f.write("## Real Unity WebGL walkable city snapshot\n")
        f.write("Git preview: "+PREVIEW+"\n\n")
        f.write("Source SHA "+SNAPSHOT+" (previous, not newest segment)\n\n")
        f.write("Desktop Brax movement PASS; physical iPhone untested.\n")

if __name__=="__main__":
    main()
