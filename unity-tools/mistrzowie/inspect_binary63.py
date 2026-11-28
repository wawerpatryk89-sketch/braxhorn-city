import collections, gc, json, sys, zipfile
from pathlib import Path
import UnityPy
def ref(ptr,obj):
    if not ptr.m_PathID: return None
    file = obj.assets_file.externals[ptr.m_FileID-1].path if ptr.m_FileID else obj.assets_file.name
    return Path(file).name.lower()+':'+str(ptr.m_PathID)
textures={}; mats={}; renderers=[]; errors=[]; bundles=[]
with zipfile.ZipFile(sys.argv[1]) as apk:
    for f in apk.infolist():
        with apk.open(f) as src: magic=src.read(7)
        if not (magic.startswith(b'UnityFS') or f.filename.endswith(('.assets','.bundle','globalgamemanagers'))): continue
        bundles.append(f.filename)
        env=UnityPy.load(apk.read(f))
        for obj in env.objects:
            k=obj.type.name
            if k not in ('Texture2D','Cubemap','Texture3D','Texture2DArray','Material','MeshRenderer','SkinnedMeshRenderer'): continue
            key=Path(obj.assets_file.name).name.lower()+':'+str(obj.path_id)
            try:
                v=obj.read()
                if k=='Material':
                    mats[key]=[v.m_Name,f.filename,{slot:ref(val.m_Texture,obj) for slot,val in v.m_SavedProperties.m_TexEnvs}]
                elif k.endswith('Renderer'):
                    renderers.append([f.filename,[ref(pt,obj) for pt in v.m_Materials]])
                else: textures[key]=v.m_Name
            except Exception as e: errors.append([f.filename,k,str(e)[:160]])
        del env
        gc.collect()
for m in mats.values(): m[2]={slot:(textures.get(ptr,'?') if ptr else None) for slot,ptr in m[2].items()}
by_scene={}
for bundle,slots in renderers:
    d=by_scene.setdefault(bundle,{'renderers':0,'missingSlots':0,'materials':set()});d['renderers']+=1
    for ptr in slots:
        if ptr in mats:d['materials'].add(mats[ptr][0])
        elif not ptr:d['missingSlots']+=1
for d in by_scene.values(): d['materials']=sorted(d['materials'])
Path('audit63.json').write_text(json.dumps(dict(materials=list(mats.values()),scenes=by_scene,errors=errors,bundles=bundles)))
