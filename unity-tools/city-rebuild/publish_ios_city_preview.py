#!/usr/bin/env python3
"""Publish exact verified city-only Unity WebGL build on a new TEST branch.

This intentionally refuses an unbuilt preview, an unpublished first-run build,
an unsigned SHA, a failed desktop startup, or an oversized bootstrap. It can
never push main, change production domains or modify gym/estate source.
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
BRANCH="codex/city-ios-memory-review-20261029"
ROOT=Path.cwd()
PLAYER=ROOT/"ios-ready-player"
QA=ROOT/"ios-ready-smoke"
DEST=ROOT/"ios-stage-preview"
RECEIPT=ROOT/"ios-preview-publish-evidence"

def require(value,message):
    if not value:
        raise ValueError(message)

def sha256(path):
    with path.open("rb") as stream:
        return hashlib.file_digest(stream,"sha256").hexdigest()

def write_json(path,obj):
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_text(json.dumps(obj,ensure_ascii=False,indent=2)+"\n")

def main():
    commit=os.environ.get("GITHUB_SHA","")
    run=os.environ.get("GITHUB_RUN_ID","")
    require(os.environ.get("GITHUB_REPOSITORY")==REPO,"Wrong repository")
    require(os.environ.get("GITHUB_REF")=="refs/heads/"+BRANCH,"Wrong source ref")
    require(re.fullmatch("[a-f0-9]{40}",commit),"Missing exact Unity source commit")
    require(PLAYER.is_dir() and QA.is_dir() and not DEST.exists(),"Missing verified Unity artifacts")
    release=json.loads((PLAYER/"release.json").read_text())
    review=json.loads((PLAYER/"city-rebuild.json").read_text())
    city=json.loads((PLAYER/"city-migration.json").read_text())
    qa=json.loads((QA/"ios-review.json").read_text())
    require(release.get("sourceCommit")==commit and str(release.get("sourceRun"))==run
            and release.get("productionPublication") is False,"Compiled Unity SHA/run mismatch")
    require(city.get("commit")==commit and
            city.get("scope")=="city-only-mobile-architecture-review" and
            [x.get("area") for x in city.get("areas",[])]==["city"],
            "Must serve only actual city scene without large estate")
    require(review.get("actuallyBuiltStreetBuildings")==18 and
            review.get("reviewOnlyVehicleObjectsRemoved")==397 and
            review.get("physicalElevatorInstalled") is True,
            "The 18 buildings/397 vehicle removal/lift contract was not built")
    require(qa.get("sourceCommit")==commit and qa.get("passed") is True and
            qa.get("physicalIphoneTested") is False,
            "Actual desktop Unity screenshot and controller QA must pass")
    require(release["transport"]["uncompressedBytes"]<400_000_000,
            "iOS uncompressed WebGL payload too large")
    parts=release.get("files")
    require(isinstance(parts,dict) and len(parts)>25,"No Unity player byte manifest")
    for name,expected in parts.items():
        require(isinstance(name,str) and re.fullmatch("[A-Za-z0-9._/-]+",name)
                and not name.startswith("/") and ".." not in name.split("/"),
                "Unsafe Unity player path")
        source=PLAYER/name
        require(source.is_file() and not source.is_symlink(),
                "Missing compiled payload "+name)
        require(source.stat().st_size < 99*1024*1024,
                "Git preview transport file >99MiB: "+name)
        require(sha256(source)==expected,"Compiled payload changed: "+name)
    require({x.relative_to(PLAYER).as_posix() for x in PLAYER.rglob("*") if x.is_file()} ==
            set(parts)|{"release.json"},"Extra or missing Unity payloads")
    shutil.copytree(PLAYER,DEST)
    shots=sorted(QA.glob("*.png"))
    require(shots and all(x.read_bytes()[:8]==bytes((137,80,78,71,13,10,26,10)) for x in shots),
            "No real Unity desktop screenshots")
    evidence=DEST/"evidence";evidence.mkdir()
    for shot in shots:
        shutil.copy2(shot,evidence/shot.name)
    summary={
        "status":"CITY_IOS_STAGE1_DESKTOP_REVIEW_ONLY",
        "sourceCommit":commit,"sourceRun":run,"reviewBuild":release["buildId"],
        "desktopUnityQAPassed":True,
        "evidenceScreenshots":[{"name":x.name,"sha256":sha256(x)} for x in shots],
        "iosPhoneAcceptance":"NOT TESTED; user's physical device check is pending",
        "cityWalkStatus":"separate continuous-walk workflow; not accepted by this desktop smoke test",
        "cityBuildings":review["actuallyBuiltStreetBuildings"],
        "vehicleSourceMeshesRemoved":review["reviewOnlyVehicleObjectsRemoved"],
        "cityCompressedBytes":release["transport"]["bytes"],
        "cityDecompressedBytes":release["transport"]["uncompressedBytes"],
        "estateScene":"EXCLUDED FROM REVIEW PLAYER ONLY; full source untouched",
        "productionPublication":False,
        "nextStep":"User opens this exact TEST URL in iPhone Safari, walks city, reports any reset",
    }
    write_json(DEST/"preview-qa-summary.json",summary)
    (DEST/"robots.txt").write_text("User-agent: *\nDisallow: /\n")
    write_json(DEST/"vercel.json",{
       "framework":None,"buildCommand":"","installCommand":"",
       "outputDirectory":".","cleanUrls":False,
       "headers":[
         {"source":"/","headers":[{"key":"Cache-Control","value":"no-store"}]},
         {"source":"/index.html","headers":[{"key":"Cache-Control","value":"no-store"}]},
         {"source":"/release.json","headers":[{"key":"Cache-Control","value":"no-store"}]},
       ]
    })
    label="preview/city-ios-"+commit[:12]
    auth_env=os.environ.copy()
    gh_token=auth_env.pop("GH_TOKEN",None)
    require(bool(gh_token),"GitHub short-lived workflow token was not provided")
    auth_env.update(GIT_CONFIG_COUNT="1",
                    GIT_CONFIG_KEY_0="http.https://github.com/.extraheader",
                    GIT_CONFIG_VALUE_0="AUTHORIZATION: basic "+
                      base64.b64encode(("x-access-token:"+gh_token).encode()).decode())
    def git(*args,auth=False):
        return subprocess.check_output(["git","-c","core.hooksPath=/dev/null",*args],
          cwd=DEST,env=auth_env if auth else None,text=True).strip()
    git("init","--initial-branch="+label)
    git("config","user.name","github-actions[bot]")
    git("config","user.email","41898282+github-actions[bot]@users.noreply.github.com")
    git("remote","add","origin","https://github.com/"+REPO+".git")
    require(not git("ls-remote","--heads","origin","refs/heads/"+label,auth=True),
            "An exact visual preview already exists; refusing to overwrite")
    git("add","--all")
    git("commit","-m","preview(city-ios): verified iPhone-sized Unity street "+commit[:12])
    preview_commit=git("rev-parse","HEAD")
    git("push","origin","HEAD:refs/heads/"+label,auth=True)
    publication={
      "branch":label,"previewCommit":preview_commit,"citySourceCommit":commit,
      "unityRun":run,"QApassed":True,"physicalIphoneTested":False,
      "estateAndGymSourceChanged":False,"productionPublication":False,
    }
    write_json(RECEIPT/"publication.json",publication)
    print("CITY_IOS_STAGE_ONE_PREVIEW_PUBLISHED",json.dumps(publication),flush=True)
    if os.environ.get("GITHUB_STEP_SUMMARY"):
        with open(os.environ["GITHUB_STEP_SUMMARY"],"a") as f:
            f.write("## City-only Unity iPhone visual preview\n\n")
            f.write("Test branch: "+label+"\n\n")
            f.write("Exact compiled Unity source: "+commit+"\n\n")
            f.write("**Phone PASS has not been claimed.** iPhone Safari review required.\n")

if __name__=="__main__":
    main()
