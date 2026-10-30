"""Import exact operator-delivered Poly Haven roof diffuse textures into isolated R2 Unity staging.
Fail closed if source changed. Only approved real external material, never a synthesized fallback.
The delivery ZIPs were verified against the same original map hashes in ChatGPT Library.
"""
import hashlib, io, json, pathlib, urllib.request
from PIL import Image
BASE = pathlib.Path(__file__).resolve().parents[2]
OUTPUT = BASE / "unity-city/Assets/Source/City/R2Approved"
OUTPUT.mkdir(parents=True, exist_ok=True)
MAPS = (
 ("GreyRoof", "https://dl.polyhaven.org/file/ph-assets/Textures/jpg/4k/grey_roof_01/grey_roof_01_diff_4k.jpg",
  "599a76538d75cd814a8ade220100fdd5fb4fb57b0c66f16e567583dec0aed2b4", "grey_roof_01_4k.blend.zip"),
 ("RoofTiles", "https://dl.polyhaven.org/file/ph-assets/Textures/jpg/4k/roof_tiles/roof_tiles_diff_4k.jpg",
  "accad55fa8473f7ae9a7b49101d1c077824a4e428e9a553398599efd43755945", "roof_tiles_4k.blend.zip"),
)
manifest = {"scope":"R2_ONLY","source":"Poly Haven CC0; verified against delivered originals",
            "maps":[],"unitySceneApplied":False,"verifiedRender":False}
for name, url, expected, source_zip in MAPS:
    with urllib.request.urlopen(urllib.request.Request(url,headers={"User-Agent":"Braxhorn-R2-asset-source-verifier/1.0"}),timeout=150) as response:
        blob = response.read()
    found = hashlib.sha256(blob).hexdigest()
    if found != expected:
        raise RuntimeError(f"BLOCKED SOURCE HASH MISMATCH for {name}: {found}")
    with Image.open(io.BytesIO(blob)) as im:
        if im.width != 4096 or im.height != 4096:
            raise RuntimeError(f"BLOCKED SOURCE RESOLUTION for {name}: {im.size}")
        resized = im.convert("RGB").resize((2048,2048),Image.Resampling.LANCZOS)
        target = OUTPUT / f"R2{name}Albedo.jpg"
        resized.save(target, quality=95, subsampling=0, optimize=True)
    # PBR maps belong to the EXACT same two operator-delivered Poly Haven assets.
    # No generated normal/roughness substitutes. Fail closed if a source map is absent.
    slug = "grey_roof_01" if name == "GreyRoof" else "roof_tiles"
    extra = {}
    for role, fmt in (("nor_dx", "png"), ("rough", "jpg")):
        map_url = f"https://dl.polyhaven.org/file/ph-assets/Textures/{fmt}/4k/{slug}/{slug}_{role}_4k.{fmt}"
        with urllib.request.urlopen(urllib.request.Request(map_url,headers={"User-Agent":"Braxhorn-R2-asset-source-verifier/1.0"}),timeout=150) as response:
            raw = response.read()
        with Image.open(io.BytesIO(raw)) as im:
            if im.size != (4096,4096):
                raise RuntimeError(f"BLOCKED ORIGINAL PBR MAP RESOLUTION {slug} {role}: {im.size}")
            if role == "nor_dx":
                # Unity Standard tangent-space normals use the DirectX source map.
                pbr_target = OUTPUT / f"R2{name}Normal.png"
                im.convert("RGB").resize((2048,2048),Image.Resampling.LANCZOS).save(pbr_target,optimize=True)
            else:
                # Original roughness, converted to Unity Standard smoothness alpha.
                rough = im.convert("L").resize((2048,2048),Image.Resampling.LANCZOS)
                color = Image.open(OUTPUT / f"R2{name}Albedo.jpg").convert("RGB")
                rgba = color.convert("RGBA")
                rgba.putalpha(rough.point(lambda value: 255-value))
                pbr_target = OUTPUT / f"R2{name}SmoothnessAlbedo.png"
                rgba.save(pbr_target,optimize=True)
        extra[role]={"sourceUrl":map_url,"sourceSha256":hashlib.sha256(raw).hexdigest(),
                     "runtimePath":str(pbr_target.relative_to(BASE)),
                     "runtimeSha256":hashlib.sha256(pbr_target.read_bytes()).hexdigest(),
                     "pixelWidth":2048,"pixelHeight":2048}
    manifest["maps"].append({"name":name,"originZip":source_zip,"sourceUrl":url,
        "sourceSha256":found,"runtimeSha256":hashlib.sha256(target.read_bytes()).hexdigest(),
        "runtimePath":str(target.relative_to(BASE)),"pixelWidth":2048,"pixelHeight":2048,"pbrMaps":extra})
(OUTPUT/"source-provenance.json").write_text(json.dumps(manifest,indent=2)+"\n",encoding="utf8")
print("R2_APPROVED_REAL_ROOF_SOURCE_TEXTURES_STAGED",json.dumps(manifest))
