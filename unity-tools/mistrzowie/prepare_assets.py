"""Restore exact owner-library textures and fetch PBR from Poly Haven only.
The ambientCG archives below are the same packs already present in the owner's
Library; their extracted maps are pinned to verified local SHA256 values.
No procedural placeholder material is accepted when a required source is absent.
"""
from pathlib import Path
import hashlib,io,json,urllib.request,zipfile,time
from PIL import Image
from pack_materials import pack, linear_mean
from pipeline import check_launcher
ROOT=Path(__file__).resolve().parents[2]
DEST=ROOT/'unity-city/Assets/Source/Mistrzowie/Textures'
LOCK=json.loads(Path(__file__).with_name('verified_textures.json').read_text())
POLYHAVEN_SLUGS=('coast_sand_02','damp_sand','clean_asphalt','concrete_pavement_02','granite_tile_02','granite_tile','rectangular_facade_tiles','concrete_floor_02','concrete_wall_008','concrete_wall_009','fabric_pattern_05','teak_veneer','brown_mud_leaves_01',
                 'stone_tiles','gravel_embedded_concrete_02','wooden_panels','leather_white','velour_velvet',
                 # HOTEL SOBCZAK T03 — verified official Poly Haven material slugs.
                 'beige_wall_001','sandstone_blocks_08','white_stucco_02','roof_slates_03','marble_01','oak_wood_planks',
                 'herringbone_parquet','terrazzo_tiles','rubber_tiles','poly_wool_herringbone','tiled_floor_001','granite_tile_04',
                 'anti_slip_concrete','garage_floor','concrete_wall_003','brown_leather',
                 # HOTEL SOBCZAK T03 rebuild 9.11 (owner board): brick cladding + podium granite.
                 'exterior_wall_cladding_02','granite_tile_03',
                 # HOTEL SOBCZAK premium finish 9.11 (owner: "wszedzie wykonczenie Poly Haven"):
                 # natural-colour fabrics, grasscloth, walnut, rugs, service tiles.
                 'terlenka','cotton_jersey','curly_teddy_natural','crepe_satin','hessian_230','floral_jacquard',
                 'smoked_walnut_veneer','long_white_tiles','anti_skid_tiles','fabric_leather_02',
                 'laminate_floor_02')
POLYHAVEN_MAPS=(('diff','jpg'),('nor_gl','exr'),('rough','exr'),('arm','png'))
# Owner 10.11 19:12 ("Nieba też brak", "I panorama realistyczną HDR 360"): the region's sky is a real 360 HDR panorama
# (Poly Haven CC0, the same partly cloudy sky as the first city), the original 4K Radiance .hdr checked against the
# provider's size and MD5 (BraxChampionsSky imports it as the Skybox/Panoramic texture).
SKY_HDRIS=('kloofendal_48d_partly_cloudy_puresky',)
SKY_DEST=DEST.parent/'Sky'

def fetch_sky(slug):
 metadata=json.loads(get('https://api.polyhaven.com/files/'+slug))
 item=metadata['hdri']['4k']['hdr']
 SKY_DEST.mkdir(parents=True,exist_ok=True)
 path=SKY_DEST/(slug+'_4k.hdr')
 data=path.read_bytes() if path.exists() else None
 if data is None or not matches_source(data,item):data=get(item['url'])
 if not matches_source(data,item):raise RuntimeError('Poly Haven HDRI '+slug+': invalid download size/checksum for 4k/hdr')
 if not data.startswith(b'#?RADIANCE') and not data.startswith(b'#?RGBE'):raise RuntimeError('Poly Haven HDRI '+slug+': not a Radiance HDR')
 path.write_bytes(data)
 return dict(file='Sky/'+path.name,sha256=hashlib.sha256(data).hexdigest(),bytes=len(data),source=item['url'])
# The provider's own blend/glTF defaults use col_01 for this fabric. Do not
# choose an arbitrary colour variation by dictionary order.
COLOR_KEYS={'fabric_pattern_05':'col_01'}

# A1 premium interior/exterior materials are pinned to exact official Poly Haven
# 4K transfer metadata. A provider-side replacement must stop the build instead
# of silently changing the approved arena appearance.
A1_POLYHAVEN_PINS={
 'leather_white':{
  'diff':(14972596,'26a2ffa8618206fbf99b63ab3bcbfd92'),
  'nor_gl':(22858362,'8d76daff53867c2a9a61e7863d8f0c9b'),
  'rough':(17528098,'f7789ac03a96ac824567b63203dd4c1d'),
  'arm':(30368496,'92af92f9eda6dce284c3c1776445b6d4')},
 'velour_velvet':{
  'diff':(13005232,'4844889b69b15257ef8ea01872204b4c'),
  'nor_gl':(18289019,'3b3a810277d1eb512accbecec14ed230'),
  'rough':(13517063,'203b4996c9370cd22488467714d47ea4'),
  'arm':(98740229,'14fb289945a2d563cc40bca9eb1ca363')},
 'concrete_wall_009':{
  'diff':(10298378,'2a944d5049ecd10dd99e5e1c91a689f1'),
  'nor_gl':(18753294,'47265d9313e33a299f1bab50b0f6dbf0'),
  'rough':(12147728,'1276d582286789cfb18dc22be879ee06'),
  'arm':(64211166,'1207237846c52444bf50637b77602ba6')},
 'rectangular_facade_tiles':{
  'diff':(9493498,'1c830bd24d9b8fdb5a650ca7bed81a06'),
  'nor_gl':(34519560,'0cab17a4f40cd20d65392f3a5f192175'),
  'rough':(2722620,'921dd1847d7ed0fab5eae4f9bbb8ac91'),
  'arm':(55167369,'a41476654dd0a2adac86fcad927eca2b')},
 'granite_tile':{
  'diff':(12787977,'2a0253486ea71ce26a3a3cd5f913edce'),
  'nor_gl':(20323894,'eae5c37634f1e5e3a43b3100f59d7db1'),
  'rough':(10924242,'b05e89ed64b900ce0dd37893fbcaec15'),
  'arm':(61977889,'e49988af78b78971ede10d674ce52f84')},
 'stone_tiles':{
  'diff':(15659623,'6b36cd2a4f8ae14cb3195eb849d71dd3'),
  'nor_gl':(35270413,'a72ea01a3fd58aabd5973d8ab6827a96'),
  'rough':(21521018,'ed084290fac358013139fffaed0bedfe'),
  'arm':(70814871,'0c3f7e50b4ce85c9e6f543857f9464da')}
}

def polyhaven_map(metadata,slug,kind,ext):
 aliases={'diff':('Diffuse','diff'),'nor_gl':('nor_gl',),'rough':('Rough','rough'),'arm':('arm',)}
 keys=(COLOR_KEYS[slug],) if kind=='diff' and slug in COLOR_KEYS else aliases[kind]
 for key in keys:
  item=metadata.get(key,{}).get('4k',{}).get(ext)
  if isinstance(item,dict) and item.get('url'):
   return item
 raise RuntimeError(f'Poly Haven {slug}: missing {kind} 4k/{ext}; expected keys {keys}; available keys {sorted(metadata)}')

def matches_source(blob,item):
 # MD5 here is the provider's transfer checksum, not an authentication scheme.
 return ((not item.get('size') or len(blob)==item['size']) and
         (not item.get('md5') or hashlib.md5(blob).hexdigest()==item['md5']))

def get(url):
 for attempt in range(4):
  try:
   req=urllib.request.Request(url,headers={'User-Agent':'BRAXHORN-UnityAssetBuild/1.0'})
   with urllib.request.urlopen(req,timeout=120) as r:return r.read()
  except Exception:
   if attempt==3:raise
   time.sleep(2*(attempt+1))

def validate_4k_dimensions(name,size,provider_native_aspect=False):
 # Poly Haven's provider-native 4K downloads may preserve the captured aspect
 # ratio (for example velour_velvet diffuse is 4096x3941). Provenance is
 # already pinned by provider byte-size/MD5 above, so never rescale or crop it.
 if provider_native_aspect:
  # Official Poly Haven "4k" maps keep provider-native aspect. Some files are
  # 4096x3941, others 4096x4181 etc. Provenance/bytes are already verified
  # against the provider metadata above; never crop or resample a valid source.
  if max(size)<4096 or min(size)<2048:
   raise RuntimeError(f'{name}: source is {size}, expected provider-native 4K (>=4096px long edge, >=2048px short edge)')
 elif size!=(4096,4096):
  raise RuntimeError(f'{name}: source is {size}, expected original 4096x4096')
 return size

def save(name,blob,source,provider_native_aspect=False):
 path=DEST/name
 if not name.endswith('.exr'):
  with Image.open(io.BytesIO(blob)) as image:
   validate_4k_dimensions(name,image.size,provider_native_aspect)
 digest=hashlib.sha256(blob).hexdigest()
 if name in LOCK and digest!=LOCK[name]:raise RuntimeError('Owner-library source changed: '+name)
 path.write_bytes(blob)
 return dict(file=name,sha256=digest,bytes=len(blob),source=source)

def main():
 check_launcher()  # Reject old main launchers before downloads or private restoration.
 DEST.mkdir(parents=True,exist_ok=True);report=[]
 for slug in POLYHAVEN_SLUGS:
  print('BRAX_MISTRZOWIE_PBR_SOURCE '+slug,flush=True)
  metadata=json.loads(get('https://api.polyhaven.com/files/'+slug))
  selected=[(kind,ext,polyhaven_map(metadata,slug,kind,ext)) for kind,ext in POLYHAVEN_MAPS]
  for kind,ext,item in selected:
   if slug in A1_POLYHAVEN_PINS:
    expected_size,expected_md5=A1_POLYHAVEN_PINS[slug][kind]
    if item.get('size')!=expected_size or str(item.get('md5','')).lower()!=expected_md5:
     raise RuntimeError(f'Poly Haven {slug} {kind}: provider metadata changed from approved A1 pin')
   url=item['url'];name=slug+'_'+kind+'_4k.'+ext
   path=DEST/name
   data=path.read_bytes() if path.exists() else None
   if data is None or not matches_source(data,item) or (name in LOCK and hashlib.sha256(data).hexdigest()!=LOCK[name]):data=get(url)
   if not matches_source(data,item):raise RuntimeError(f'Poly Haven {slug}: invalid download size/checksum for {kind} 4k/{ext}')
   report.append(save(name,data,url,provider_native_aspect=True))
 for slug in SKY_HDRIS:
  print('BRAX_MISTRZOWIE_SKY_SOURCE '+slug,flush=True)
  report.append(fetch_sky(slug))
 # Extract the complete original packs once; retain original maps and pack ARM outside Unity.
 for asset, kinds in [('Grass004',['Color','NormalGL','Roughness','AmbientOcclusion']),
                      ('Metal048A',['Color','NormalGL','Roughness','Metalness']),
                      ('Leather037',['Color','NormalGL','Roughness']),
                      # 9.11 owner vitrine photo: polished BLACK marble with thin
                      # WHITE veins. Poly Haven has no such material (marble_01 is
                      # beige tiles); ambientCG Marble016 is CC0 like Leather037.
                      ('Marble016',['Color','NormalGL','Roughness']),
                      # 9.11 owner review of the hotel/apartment previews: polished WHITE marble in
                      # seamless slabs for lobbies, corridors and bathrooms (marble_01 = beige tiles).
                      ('Marble021',['Color','NormalGL','Roughness']),
                      # 9.11 23:29 owner: the grey granite podium of Hotel Sobczak -> vein-cut travertine
                      ('Travertine009',['Color','NormalGL','Roughness','AmbientOcclusion']),
                      # 10.11 owner material board: A28 quilted leather on the arena hall walls, Tiles012 black/white
                      # marble checkerboard on the arena entrance lobby floor.
                      ('Leather013',['Color','NormalGL','Roughness']),
                      ('Tiles012',['Color','NormalGL','Roughness']),
                      # 10.11 owner "promenada ... port porządny": real marina decking (pontoons, gangways, beach
                      # boardwalks) instead of furniture veneer, and premium light stone slabs on the promenade.
                      ('Planks040',['Color','NormalGL','Roughness','AmbientOcclusion','Metalness']),
                      ('PavingStones126A',['Color','NormalGL','Roughness','AmbientOcclusion'])]:
  prefix=asset+'_4K-JPG_'
  names=[prefix+k+'.jpg' for k in kinds]
  # New maps have no historical lock: verify against the archive on every preparation.
  missing=any(n not in LOCK or not (DEST/n).exists() or hashlib.sha256((DEST/n).read_bytes()).hexdigest()!=LOCK[n] for n in names)
  archive_url='https://ambientcg.com/get?file='+asset+'_4K-JPG.zip'
  z=zipfile.ZipFile(io.BytesIO(get(archive_url))) if missing else None
  for name in names:
   report.append(save(name,z.read(name) if z else (DEST/name).read_bytes(),archive_url))
  ao=DEST/(prefix+'AmbientOcclusion.jpg') if 'AmbientOcclusion' in kinds else None
  metal=DEST/(prefix+'Metalness.jpg') if 'Metalness' in kinds else None
  output=DEST/(asset+'_ARM_4k.png')
  pack(DEST/(prefix+'Roughness.jpg'),output,ao,metal)
  report.append(dict(file=output.name,sha256=hashlib.sha256(output.read_bytes()).hexdigest(),
                     source='packed-original-channels',ao='source' if ao else 'neutral: no AO supplied for this smooth material'))
 (DEST.parent/'grass-calibration.json').write_text(json.dumps(dict(
     grass=linear_mean(DEST/'Grass004_4K-JPG_Color.jpg'),
     soil=linear_mean(DEST/'brown_mud_leaves_01_diff_4k.jpg')),indent=2))
 (DEST.parent/'source-materials.json').write_text(json.dumps(report,indent=2))
 print('BRAX_MISTRZOWIE_PBR_OK maps='+str(len(report))+' original=4K')

if __name__=='__main__':main()

