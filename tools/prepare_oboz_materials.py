"""Copy selected original PBR maps; never resample or recolour owner assets."""
import argparse, hashlib, json, zipfile
from pathlib import Path
from prepare_oboz_characters import safe_name

MATERIALS = {
 'wood':('Wood049','Cabin structure and timber'),
 'floor':('WoodFloor051','Interior floorboards'),
 'gravel':('Ground111','Gravel paths only; not grass'),
 'metal':('Metal063','Worn metal hardware'),
 'towel':('Fabric084','Towels only; not tent canvas'),
}
def prepare(archives, project, output):
    result={'schema':1,'materials':[],'runtimeVerified':False,
            'missing':['verified forest trees with LOD','tent canvas','bunker concrete','ring canvas and ropes']}
    for ident,(name,use) in MATERIALS.items():
        found=list(archives.rglob(name+'_4K-JPG.zip'))
        if len(found)!=1: raise ValueError('Missing or ambiguous '+name)
        item={'id':ident,'source':found[0].name,'use':use,'files':[]}
        with zipfile.ZipFile(found[0]) as z:
            for kind in ['Color','NormalGL','Roughness','Metalness','AmbientOcclusion']:
                matches=[n for n in z.namelist() if n.endswith('_'+kind+'.jpg')]
                if not matches and kind in ['Color','NormalGL','Roughness']:raise ValueError(name+' missing '+kind)
                if len(matches)>1:raise ValueError('Ambiguous map')
                if not matches:continue
                safe_name(matches[0]);data=z.read(matches[0])
                target=project/'Assets/Source/Prywatne/Oboz/environment/materials'/ident/Path(matches[0]).name
                target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data)
                item['files'].append({'kind':kind,'path':target.relative_to(project).as_posix(),'sha256':hashlib.sha256(data).hexdigest(),'bytes':len(data)})
        result['materials'].append(item)
    output.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps({'materials':len(result['materials']),'maps':sum(len(m['files']) for m in result['materials']),'runtimeVerified':False}))
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--archives',type=Path,required=True);p.add_argument('--project',type=Path,required=True);p.add_argument('--output',type=Path,required=True)
    a=p.parse_args();prepare(a.archives,a.project,a.output)
