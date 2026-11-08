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
                 'stone_tiles','gravel_embedded_concrete_02','wooden_panels','leather_white','velour_velvet')
POLYHAVEN_MAPS=(('diff','jpg'),('nor_gl','exr'),('rough','exr'),('arm','png'))
# The provider's own blend/glTF defaults use col_01 for this fabric. Do not
# choose an arbitrary colour variation by dictionary order.
COLOR_KEYS={'fabric_pattern_05':'col_01'}

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

def save(name,blob,source):
 path=DEST/name
 if not name.endswith('.exr'):
  with Image.open(io.BytesIO(blob)) as image:
   if image.size!=(4096,4096):raise RuntimeError(f'{name}: source is {image.size}, expected original 4K')
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
   url=item['url'];name=slug+'_'+kind+'_4k.'+ext
   path=DEST/name
   data=path.read_bytes() if path.exists() else None
   if data is None or not matches_source(data,item) or (name in LOCK and hashlib.sha256(data).hexdigest()!=LOCK[name]):data=get(url)
   if not matches_source(data,item):raise RuntimeError(f'Poly Haven {slug}: invalid download size/checksum for {kind} 4k/{ext}')
   report.append(save(name,data,url))
 # Extract the complete original packs once; retain original maps and pack ARM outside Unity.
 for asset, kinds in [('Grass004',['Color','NormalGL','Roughness','AmbientOcclusion']),
                      ('Metal048A',['Color','NormalGL','Roughness','Metalness']),
                      ('Leather037',['Color','NormalGL','Roughness'])]:
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

