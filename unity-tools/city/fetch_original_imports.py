"""Read actual Unity6 gym/city/estate sources from authenticated DRAFT GitHub release.
A draft is invisible to 'gh release download <tag>'; search the REST release listing.
Strict SHA-256 verifies all original models; no generated substitute assets.
"""
import hashlib,json,os,pathlib,subprocess,zipfile
from pathlib import PurePosixPath
repo=os.environ["GITHUB_REPOSITORY"]
base=pathlib.Path("unity6-source-backup");base.mkdir(exist_ok=True)
def jget(url):return json.loads(subprocess.check_output(["gh","api",url],text=True))
def digest(path):
    with path.open("rb") as source:return hashlib.file_digest(source,"sha256").hexdigest()
matches=[r for r in jget(f"repos/{repo}/releases?per_page=100")
         if r.get("tag_name")=="unity6-original-source-imports-20261030" and r.get("draft") is True]
if len(matches)!=1:raise RuntimeError("Original Unity6 private draft archive not found via authenticated release list")
assets={a["name"]:a for a in matches[0]["assets"]}
required={"braxhorn-original-gym-unity6.zip","braxhorn-original-city-estate-unity6.zip","braxhorn-unity6-original-imports.json"}
if set(assets)!=required:raise RuntimeError("Original Unity6 release asset inventory changed")
for filename in sorted(required):
    a=assets[filename];target=base/filename
    with target.open("wb") as out:
        subprocess.run(["gh","api",f"repos/{repo}/releases/assets/{a['id']}",
                        "-H","Accept: application/octet-stream"],check=True,stdout=out)
    if target.stat().st_size!=a["size"] or digest(target)!=a.get("digest","").removeprefix("sha256:"):
        raise RuntimeError("Wrong original source bytes from release API: "+filename)
    if filename.endswith(".zip") and target.open("rb").read(4)!=b"PK\x03\x04":
        raise RuntimeError("API returned metadata instead of binary ZIP: "+filename)
receipt=json.loads((base/"braxhorn-unity6-original-imports.json").read_text())
if receipt.get("engine")!="Unity6" or receipt.get("isDraft") is not True:raise RuntimeError("Wrong source manifest")
if {r["sourceRun"] for r in receipt["sources"]}!={40787817505,40840263799}:
    raise RuntimeError("Wrong original source run IDs")
for item in receipt["sources"]:
    archive=base/item["name"]
    if digest(archive)!=item["sha256"]:raise RuntimeError("Original archive SHA mismatch")
    out=pathlib.Path("unity-city/Assets/Source/City" if item["sourceRun"]==40840263799 else "unity-city/Assets/Source")
    out.mkdir(parents=True,exist_ok=True)
    with zipfile.ZipFile(archive) as source:
        if source.testzip() is not None:raise RuntimeError("ZIP CRC failure")
        for f in source.infolist():
            p=PurePosixPath(f.filename)
            if p.is_absolute() or ".." in p.parts:raise RuntimeError("Path traversal in archived input")
        source.extractall(out)
    for f in item["files"]:
        p=out/f["path"]
        if not p.is_file() or digest(p)!=f["sha256"]:raise RuntimeError("Original extracted source changed: "+str(p))
for name in ("unity-city/Assets/Source/braxhorn-original-gym.glb",
             "unity-city/Assets/Source/gym-collisions.json",
             "unity-city/Assets/Source/City/braxhorn-city.glb",
             "unity-city/Assets/Source/City/braxhorn-estate.glb",
             "unity-city/Assets/Source/City/city-manifest.json",
             "unity-city/Assets/Source/City/estate-manifest.json"):
    if not pathlib.Path(name).is_file():raise RuntimeError("Missing original Unity source: "+name)
print("ORIGINAL_UNITY6_DRAFT_RELEASE_ASSETS_SHA256_VERIFIED")
