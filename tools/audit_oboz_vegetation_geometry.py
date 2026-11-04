"""Compare every decoded accessor before/after externalization and compaction."""
import hashlib,json
from pathlib import Path
import numpy as np
from audit_oboz_environment import glb,accessor

def run(root):
    directory=root/'unity-city/Assets/Source/Prywatne/Oboz/environment/vegetation';records=[]
    for r in json.loads((root/'evidence/vegetation-mobile-import.json').read_text()):
        source=(directory/r['source']).read_bytes();derived=(directory/r['derived']).read_bytes()
        assert hashlib.sha256(source).hexdigest()==r['sourceSha256']
        assert hashlib.sha256(derived).hexdigest()==r['derivedSha256']
        a,ab=glb(source);b,bb=glb(derived);assert len(a['accessors'])==len(b['accessors'])
        for index in range(len(a['accessors'])):
            before=accessor(a,ab,index);after=accessor(b,bb,index)
            if not np.array_equal(before,after):raise ValueError('Accessor changed '+r['source']+' / '+str(index))
        for key in ['meshes','nodes','skins','scenes','animations']:
            if a.get(key)!=b.get(key):raise ValueError('Source topology changed '+key)
        records.append({'source':r['source'],'sourceSha256':r['sourceSha256'],'derivedSha256':r['derivedSha256'],
          'accessorsCompared':len(a['accessors']),'allDecodedGeometryIdentical':True})
    result={'models':records,'allPassed':True,'unityExecuted':False}
    (root/'evidence/vegetation-geometry.json').write_text(json.dumps(result,indent=2)+'\n')
    print({'models':len(records),'accessorsCompared':sum(r['accessorsCompared'] for r in records),'allPassed':True})
if __name__=='__main__':run(Path(__file__).resolve().parents[1])
