"""Restore exact owner-library textures and fetch PBR from Poly Haven only.
The ambientCG archives below are the same packs already present in the owner's
Library; their extracted maps are pinned to verified local SHA256 values.
No procedural placeholder material is accepted when a required source is absent.
"""
from pathlib import Path
import hashlib,io,json,urllib.request,zipfile,time
ROOT=Path(__file__).resolve().parents[2]
DEST=ROOT/'unity-city/Assets/Source/Mistrzowie/Textures'
LOCK=json.loads(Path(__file__).with_name('verified_textures.json').read_text())

def get(url):
 for attempt in range(4):
  try:
   req=urllib.request.Request(url,headers={'User-Agent':'BRAXHORN-UnityAssetBuild/1.0'})
   with urllib.request.urlopen(req,timeout=120) as r:return r.read()
  except Exception:
   if attempt==3:raise
   time.sleep(2*(attempt+1))

def save(name,blob,source):
 path=DEST/name;digest=hashlib.sha256(blob).hexdigest()
 if name in LOCK and digest!=LOCK[name]:raise RuntimeError('Owner-library source changed: '+name)
 path.write_bytes(blob)
 return dict(file=name,sha256=digest,bytes=len(blob),source=source)

def main():
 DEST.mkdir(parents=True,exist_ok=True);report=[]
 for slug in ['coast_sand_02','damp_sand','clean_asphalt','concrete_pavement_02','granite_tile_02','granite_tile','rectangular_facade_tiles','concrete_floor_02','concrete_wall_008','fabric_pattern_05','teak_veneer']:
  metadata=json.loads(get('https://api.polyhaven.com/files/'+slug))
  for kind,ext in [('diff','jpg'),('nor_gl','exr'),('rough','exr')]:
   key=('Diffuse' if 'Diffuse' in metadata else 'diff') if kind=='diff' else kind
   item=metadata[key]['4k'][ext];url=item['url'];name=slug+'_'+kind+'_4k.'+ext
   path=DEST/name
   data=path.read_bytes() if path.exists() and (name not in LOCK or hashlib.sha256(path.read_bytes()).hexdigest()==LOCK[name]) else get(url)
   report.append(save(name,data,url))
 archive_url='https://ambientcg.com/get?file=Grass004_4K-JPG.zip'
 names=['Grass004_4K-JPG_'+k+'.jpg' for k in ['Color','NormalGL','Roughness']]
 missing=any(not (DEST/n).exists() or hashlib.sha256((DEST/n).read_bytes()).hexdigest()!=LOCK[n] for n in names)
 z=zipfile.ZipFile(io.BytesIO(get(archive_url))) if missing else None
 for name in names:report.append(save(name,z.read(name) if z else (DEST/name).read_bytes(),'owner-library:Grass004_4K-JPG.zip'))
 archive_url='https://ambientcg.com/get?file=Metal048A_4K-JPG.zip'
 names=['Metal048A_4K-JPG_'+k+'.jpg' for k in ['Color','NormalGL','Roughness','Metalness']]
 missing=any(not (DEST/n).exists() or hashlib.sha256((DEST/n).read_bytes()).hexdigest()!=LOCK[n] for n in names)
 z=zipfile.ZipFile(io.BytesIO(get(archive_url))) if missing else None
 for name in names:report.append(save(name,z.read(name) if z else (DEST/name).read_bytes(),'owner-library:Metal048A_4K-JPG.zip'))
 archive_url='https://ambientcg.com/get?file=Leather037_4K-JPG.zip'
 names=['Leather037_4K-JPG_'+k+'.jpg' for k in ['Color','NormalGL','Roughness']]
 missing=any(not (DEST/n).exists() or hashlib.sha256((DEST/n).read_bytes()).hexdigest()!=LOCK[n] for n in names)
 z=zipfile.ZipFile(io.BytesIO(get(archive_url))) if missing else None
 for name in names:report.append(save(name,z.read(name) if z else (DEST/name).read_bytes(),'owner-library:Leather037_4K-JPG.zip'))
 (DEST.parent/'source-materials.json').write_text(json.dumps(report,indent=2))
 print('BRAX_MISTRZOWIE_PBR_OK maps='+str(len(report))+' original=4K')

if __name__=='__main__':main()
