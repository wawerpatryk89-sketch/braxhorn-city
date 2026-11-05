"""Read-only SHA256 gate for all eleven original Champion A1 assets before Unity."""
import hashlib,json,re,uuid
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
BASE="Assets/Source/Mistrzowie/"
GUID_NAMESPACE=uuid.UUID("af7d65e9-aeb5-4cb0-a8a8-e60c0c5fd3c5")
FOLDERS=(
"ArenaSigns","VIP","Trophies",
"Trophies/tripo_convert_3a821f58-4886-4294-9747-e2bf88b35419.fbm",
)
MAP={
"Trophies/tripo_convert_3a821f58-4886-4294-9747-e2bf88b35419.fbx":"38f40d9030d6ce832cf56877dd7adbd0f9adec6511bc97b7f4e9a0979e5fa649",
"Trophies/ChampionshipBelt_ARM.png":"72d4d5a6e81a352d257b9d2b516601df8e842fcb75026387444cbc85f198995a",
"ArenaSigns/champion_arena_red_front.png":"c4ba2c808a1edd4b5c2b504c0ab7ed43f4d258944f3a19bb1edc0676202f5ea2",
"ArenaSigns/champion_arena_gold_arena_walk.png":"5f12bbb44ab088d73fd2fe01c5474d5c9d2d8986199e3f06c4d9a161c45825a3",
"ArenaSigns/champion_arena_gold_front.png":"39ab46bbf700b2f784d10be153a025e0baa05dcbcb713a8fa62bbb9c0eb41e0c",
"ArenaSigns/champion_arena_red_fight_night.png":"1fd30c52b1218aa7e27a028348ef29bde09448f02a7f31fb7210a1fb983dddcf",
"Trophies/tripo_convert_3a821f58-4886-4294-9747-e2bf88b35419.fbm/Normal.png":"b2f67842fe73d29430c0444a3965fc70dfbc6fd2db91f85a815a18f409795f1d",
"Trophies/tripo_convert_3a821f58-4886-4294-9747-e2bf88b35419.fbm/Color.png":"359c35f09ec11eee97370e78d55a42ce4c8ca6ae2d60b1461213454b9988dd03",
"Trophies/tripo_convert_3a821f58-4886-4294-9747-e2bf88b35419.fbm/championship_belt_3d_model_metallic.PNG":"fcb8203527cfe485fa7989f6a7200d8f7a90d70221a8c7deb3123fca524ea684",
"Trophies/tripo_convert_3a821f58-4886-4294-9747-e2bf88b35419.fbm/championship_belt_3d_model_roughness.PNG":"bac9fb6749b5cf8c26736e42a76a333d3bc3ab1335f8d6314e9e65cbdca8dc55",
"VIP/free_luxurious_barrel_seat_2k.glb":"a8896bccfa939235579d0daf9cad294edd4a7c103ae79a1801c2bdc1366356f1",
}
def audit(root=ROOT/"unity-city"):
    results=[]
    for relative,expected in sorted(MAP.items()):
        path=Path(root)/BASE/relative
        if not path.is_file():
            results.append({"path":BASE+relative,"status":"MISSING"})
            continue
        h=hashlib.sha256()
        with path.open("rb") as f:
            for chunk in iter(lambda:f.read(1048576),b""):h.update(chunk)
        results.append({"path":BASE+relative,"status":"PASS" if h.hexdigest()==expected else "WRONG_SOURCE"})
    count_original=len(results)
    # Stable .meta GUIDs are mandatory for future incremental Unity updates.
    for relative in sorted((*MAP.keys(),*FOLDERS)):
        name=BASE+relative
        meta=Path(root)/(name+".meta")
        expected=uuid.uuid5(GUID_NAMESPACE,name).hex
        if not meta.is_file():
            state="MISSING_META"
        else:
            match=re.search(r"^guid:\\s*([a-fA-F0-9]{32})\\s*$",meta.read_text(),re.M)
            state="PASS" if match and match.group(1).lower()==expected else "GUID_MISMATCH"
        results.append({"path":name+".meta","status":state})
    ok=all(x["status"]=="PASS" for x in results)
    return {"status":"PASS" if ok else "BLOCKED",
            "originalAssetCount":count_original,
            "guidSidecarCount":len(results)-count_original,
            "passed":sum(x["status"]=="PASS" for x in results),
            "count":len(results),"results":results,"unityTested":False}
if __name__=="__main__":
    r=audit()
    out=ROOT/"mistrzowie-evidence/champion-arena-assets.json"
    out.parent.mkdir(parents=True,exist_ok=True)
    out.write_text(json.dumps(r,indent=2),encoding="utf-8")
    for item in r["results"]:
        if item["status"]!="PASS":print("::error::",item["status"],item["path"])
    print("A1_OWNER_ASSETS",r["status"],r["passed"],"/",r["count"])
    if r["status"]!="PASS":raise SystemExit(1)
