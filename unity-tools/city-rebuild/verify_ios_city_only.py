#!/usr/bin/env python3
"""Fail closed for the isolated BRAXHORN CITY iPhone architecture-only stage."""
import json
import os
from pathlib import Path
import re
import sys

OUT=Path("unity-preview")
commit=os.environ.get("GITHUB_SHA","")
assert re.fullmatch(r"[a-f0-9]{40}",commit),"Missing exact CI SHA"
release=json.loads((OUT/"release.json").read_text())
data=json.loads((OUT/"city-migration.json").read_text())
review=json.loads((OUT/"city-rebuild.json").read_text())
transport=json.loads((OUT/"transport.json").read_text())

def check(ok,label,evidence=None):
    if not ok:
        raise RuntimeError(label+(" "+str(evidence) if evidence is not None else ""))
    print("PASS",label,evidence or "")

check(data.get("commit")==commit,"review matches actual compiled SHA")
check(data.get("scope")=="city-only-mobile-architecture-review","city-only review build")
check([x.get("area") for x in data.get("areas",[])]==["city"],"estate binaries not included as second scene")
check(data.get("originalBraxRig") is True and data.get("originalModels") is True,"same original Brax and source models")
check(review.get("actuallyBuiltStreetBuildings")==18,"all authored building fronts")
check(review.get("actuallyBuiltHighRises",0)>=9,"original skyline preserved")
check(review.get("vehicleSourceRenderersHidden")==397,"397 source car meshes hidden")
check(review.get("reviewOnlyVehicleObjectsRemoved")==397,"397 source car meshes not retained as hidden assets")
check(review.get("physicalElevatorInstalled") is True,"lift component in Unity city scene")
check(review.get("phoneAccepted") is False or review.get("phoneAccepted") is None,"physical iPhone acceptance not falsely claimed")
check(release.get("productionPublication") is False,"never production")
check(release.get("sourceCommit")==commit,"package source SHA")
check(release.get("loadingFix",{}).get("nativeStreamingGzip") is True,"preserve 64KiB native iOS streaming")
check(transport.get("compiledDataBytesUnchanged") is True,"actual Unity bytes remain unchanged")
check(transport.get("districtStreaming") is False,"do not claim streaming of districts")
check(transport.get("uncompressedBytes",1<<62)<400_000_000,
      "mobile iOS expanded city bootstrap under 400MB",
      transport.get("uncompressedBytes"))
check(transport.get("bytes",1<<62)<300_000_000,
      "mobile iOS compressed city download under 300MB",
      transport.get("bytes"))
check(transport.get("uncompressedBytes",0)>5_000_000,"data not an empty scene")
print("CITY_IPHONE_SINGLE_SCENE_SIZE_REPORT",json.dumps({
 "unity":release.get("unityVersion"),"sourceCommit":commit,
 "compressedCityBytes":transport["bytes"],
 "expandedCityBytes":transport["uncompressedBytes"],
 "baselineCompressedBytes":411968267,
 "baselineExpandedBytes":558052588,
 "cityBuildings":review["actuallyBuiltStreetBuildings"],
 "carSourceMeshesRemoved":review["reviewOnlyVehicleObjectsRemoved"],
 "physicalIphoneTested":False,
},sort_keys=True),flush=True)
