#!/usr/bin/env python3
"""Signed standalone segment: only Unity-compiled bytes and real Brax pass."""
import json
import os
from pathlib import Path
import re

root=Path("unity-preview")
commit=os.environ.get("GITHUB_SHA","")
if not re.fullmatch("[a-f0-9]{40}",commit):raise RuntimeError("Missing full source commit")
release=json.loads((root/"release.json").read_text())
city=json.loads((root/"city-migration.json").read_text())
segment=json.loads((root/"city-segment-review.json").read_text())
stage=json.loads((root/"city-rebuild.json").read_text())
transport=json.loads((root/"transport.json").read_text())

def verify(condition,what,measured=None):
    if not condition: raise RuntimeError("CITY_SEGMENT_FAIL "+what+" "+str(measured))
    print("CITY_SEGMENT_PASS",what,measured if measured is not None else "")

verify(city.get("commit")==commit,"same actual source SHA")
verify(release.get("sourceCommit")==commit,"WebGL package exact SHA")
verify(city.get("scope")=="city-segment-mobile-architecture-review","one-scene street segment")
verify([x.get("area") for x in city.get("areas",[])]==["city"],"no estate or extra loaded scene")
verify(city.get("originalBraxRig") is True,"original Brax rig")
verify(segment.get("fullCitySourceRetained") is True,"original 18-building city source retained")
verify(segment.get("selectedBuildingFrontages",0)>=5,"architectural storefront selection",
       segment.get("selectedBuildingFrontages"))
verify(segment.get("selectedHighRises",0)>=2,"selected local skyline",
       segment.get("selectedHighRises"))
verify(segment.get("originalCityMeshesPruned",0)>0,"distant source meshes removed",
       segment.get("originalCityMeshesPruned"))
verify(segment.get("distantCityRenderersPruned",0)>0,"distant new-city meshes removed",
       segment.get("distantCityRenderersPruned"))
verify(abs(segment["playerSpawn"]["x"]-6.75)<.01 and
       abs(segment["playerSpawn"]["z"]-27.85)<.01,"Brax spawns at original street coordinates")
verify(stage.get("reviewOnlyVehicleObjectsRemoved")==397,"all 397 hidden vehicle meshes stripped")
verify(stage.get("physicalElevatorInstalled") is True,"original city elevator preserved")
verify(release.get("productionPublication") is False,"never production")
verify(transport.get("compiledDataBytesUnchanged") is True,"original Unity WebGL bytes")
verify(release.get("loadingFix",{}).get("nativeStreamingGzip") is True,"bounded iOS data streaming")
verify(transport.get("compressedBytes") is None,"transport format preserved")
verify(0<transport["bytes"]<235_000_000,
       "iPhone section gzip payload below 235MB",
       {"segment":transport["bytes"],"oldFullCity":411968267})
verify(0<transport["uncompressedBytes"]<340_000_000,
       "iPhone section decoded bootstrap below 340MB",
       {"segment":transport["uncompressedBytes"],"oldFullCity":558052588})
verify(transport.get("districtStreaming") is False,"no false district-streaming claim")
print("CITY_SEGMENT_STAGE_ONE_READY_FOR_DESKTOP_QA",json.dumps({
  "sourceCommit":commit,"segment":segment["mode"],
  "sourceMeshesPruned":segment["originalCityMeshesPruned"],
  "generatedMeshesPruned":segment["distantCityRenderersPruned"],
  "compressedBytes":transport["bytes"],
  "uncompressedBytes":transport["uncompressedBytes"],
  "phoneAcceptance":"PENDING"
},sort_keys=True))
