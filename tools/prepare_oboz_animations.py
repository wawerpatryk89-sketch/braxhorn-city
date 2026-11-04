import argparse,hashlib,json,zipfile
from pathlib import Path
from prepare_oboz_characters import Fbx,safe_name

PACKS={'brax':'brax new (2)(1).zip','nyxa':'nyxa.zip','vexa':'vexa.zip','krov':'krov.zip'}
COMMON={'Idle':('brax','Idle.fbx',True),'CombatIdle':('krov','Fighting Idle.fbx',True),
 'Walk':('nyxa','Walking.fbx',True),'Run':('brax','Running.fbx',True),
 'Punch':('brax','Jab Cross (1).fbx',False),'Kick':('brax','Mma Kick (1).fbx',False),
 'Jump':('brax','Jump.fbx',False),'HeadHit':('brax','Head Hit.fbx',False),
 'HitToBody':('brax','Hit To Body.fbx',False),'GettingUp':('vexa','Getting Up.fbx',False),
 'Block':('brax','Center Block.fbx',False),'Dodge':('brax','Dodging.fbx',False),
 'Training':('brax','Boxing.fbx',True),'Taunt':('brax','Taunt.fbx',False)}
OVERRIDES={
 'monika':{'Kick':('nyxa','Mma Kick (1).fbx',False)},
 'vorn':{'Punch':('krov','Hook Punch.fbx',False),'Training':('krov','Punching Bag.fbx',True)},
 'zyro':{'Kick':('nyxa','Mma Kick (1).fbx',False)},
 'rakk':{'Punch':('krov','Jab Cross.fbx',False),'Kick':('krov','Mma Kick.fbx',False)},
 'kade':{'Punch':('brax','Jab Cross (1).fbx',False),'Training':('brax','Boxing.fbx',True)},
 'gorr':{'Punch':('krov','Hook Punch.fbx',False),'Kick':('krov','Mma Kick.fbx',False)},
 'mis':{'Punch':('krov','Hook Punch.fbx',False),'Kick':('krov','Mma Kick.fbx',False)}}

def prepare(archives,project,manifest_path):
 data=json.loads(manifest_path.read_text());selected={};catalog=[]
 for c in data['characters']:
  mapping={**COMMON,**OVERRIDES[c['id']]};c['clips']=[]
  for name,(donor,file,loop) in mapping.items():
   path='Assets/Source/Prywatne/Oboz/animations/'+donor+'/'+file
   c['clips'].append({'name':name,'path':path,'loop':loop,'inPlace':name in ['Idle','CombatIdle','Walk','Run','Jump','Training'], 'donor':donor})
   selected[(donor,file)]=path
 for donor,filename in PACKS.items():
  found=list(archives.rglob(filename))
  if len(found)!=1:raise ValueError('Ambiguous or missing '+filename)
  with zipfile.ZipFile(found[0]) as z:
   members={Path(n).name:n for n in z.namelist() if n.lower().endswith('.fbx')}
   for file,path in [(f,p) for (d,f),p in selected.items() if d==donor]:
    if file not in members:raise ValueError('Animation missing '+donor+'/'+file)
    safe_name(members[file]);raw=z.read(members[file]);report=Fbx(raw).report()
    if report['bone_count']!=65:raise ValueError('Donor skeleton differs: '+donor+'/'+file)
    if not report['animation_stacks']:raise ValueError('FBX has no animation stack: '+file)
    out=project/path;out.parent.mkdir(parents=True,exist_ok=True);out.write_bytes(raw)
    catalog.append({'path':path,'archive':filename,'sha256':hashlib.sha256(raw).hexdigest(),'boneCount':65,'animations':report['animation_stacks']})
 data['animationSources']=catalog
 data['motionSelectionStatus']='Provisional mappings; Unity baking and visual review not performed'
 manifest_path.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')
 out=project/'Assets/Editor/Oboz/CharacterManifest.json';out.parent.mkdir(parents=True,exist_ok=True)
 out.write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')
 print(json.dumps({'sourceClips':len(catalog),'characters':len(data['characters']),'runtimeVerified':False}))

if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--archives',type=Path,required=True);p.add_argument('--project',type=Path,required=True);p.add_argument('--manifest',type=Path,required=True)
 a=p.parse_args();prepare(a.archives,a.project,a.manifest)
