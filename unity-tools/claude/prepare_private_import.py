"""Export dimensions and an encrypted one-time import key; never export source assets or secrets."""
import importlib.util, json, os, pathlib, struct, subprocess, tempfile
import numpy as np
ROOT = pathlib.Path.cwd()
out = ROOT / 'preparation-output'; out.mkdir(exist_ok=True)
spec = importlib.util.spec_from_file_location('private_assets', ROOT/'unity-tools/claude/prywatne.py')
private = importlib.util.module_from_spec(spec); spec.loader.exec_module(private)
assert private.klucz(), 'Missing repository asset key'
with tempfile.TemporaryDirectory() as td:
    key = pathlib.Path(td)/'import-private.pem'
    subprocess.run(['openssl','genpkey','-algorithm','RSA','-pkeyopt','rsa_keygen_bits:4096','-out',str(key)],check=True,stderr=subprocess.DEVNULL)
    key.chmod(0o600)
    subprocess.run(['openssl','pkey','-in',str(key),'-pubout','-out',str(out/'import-public.pem')],check=True)
    subprocess.run(private.OPENSSL+['-salt','-in',str(key),'-out',str(out/'import-private.pem.enc'),'-pass','env:KLUCZ'],check=True)
report = {}
tar = private.odszyfruj('osiedle_wnetrza')
assert tar is not None, 'Cannot decrypt furniture package'
for member in tar.getmembers():
    if not member.name.endswith('.glb') or not any(n in member.name for n in ['chesterfield','butelki','polki','hoker','stolik']): continue
    raw = tar.extractfile(member).read(); n = struct.unpack_from('<I',raw,12)[0]
    doc=json.loads(raw[20:20+n]); offset=20+n; binary=raw[offset+8:]
    def accessor(i):
        a=doc['accessors'][i]; v=doc['bufferViews'][a['bufferView']]
        dtype={5126:'<f4',5123:'<u2',5125:'<u4',5121:'u1'}[a['componentType']]
        width={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}[a['type']]
        return np.ndarray((a['count'],width), dtype=dtype, buffer=binary, offset=v.get('byteOffset',0)+a.get('byteOffset',0), strides=(v.get('byteStride',np.dtype(dtype).itemsize*width),np.dtype(dtype).itemsize)).astype(float)
    def matrix(node):
        if 'matrix' in node: return np.array(node['matrix']).reshape(4,4).T
        x,y,z,w=node.get('rotation',[0,0,0,1]); m=np.eye(4)
        m[:3,:3]=np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])@np.diag(node.get('scale',[1,1,1]));m[:3,3]=node.get('translation',[0,0,0]);return m
    records=[]
    def visit(i,parent,path):
        node=doc['nodes'][i]; world=parent@matrix(node); name=node.get('name',str(i)); path=path+'/'+name; points=[]
        if 'mesh' in node:
            for p in doc['meshes'][node['mesh']]['primitives']:
                pos=accessor(p['attributes']['POSITION']); points.append((world@np.column_stack([pos,np.ones(len(pos))]).T).T[:,:3])
        for child in node.get('children',[]):
            q=visit(child,world,path)
            if len(q): points.append(q)
        if points:
            pts=np.concatenate(points); lo=pts.min(0); hi=pts.max(0)
            rec={'name':name,'path':path,'index':i,'children':node.get('children',[]),'mesh':node.get('mesh'),'min':lo.tolist(),'max':hi.tolist(),'size':(hi-lo).tolist(),'rotation':node.get('rotation'),'scale':node.get('scale')}
            rec['axis_end_centers'] = [[pts[pts[:,a] <= lo[a]+max((hi[a]-lo[a])*.15,1e-8)].mean(0).tolist(),pts[pts[:,a] >= hi[a]-max((hi[a]-lo[a])*.15,1e-8)].mean(0).tolist()] for a in range(3)]
            records.append(rec);return pts
        return np.zeros((0,3))
    for i in doc['scenes'][doc.get('scene',0)]['nodes']:visit(i,np.eye(4),'')
    report[pathlib.Path(member.name).stem]=records
(out/'furniture-dimensions.json').write_text(json.dumps(report,indent=2))
for name in ['grafiki']:
    archive=private.odszyfruj(name)
    if archive:(out/(name+'-inventory.json')).write_text(json.dumps([{'name':m.name,'bytes':m.size} for m in archive.getmembers() if m.isfile()],indent=2))
print('Prepared encrypted import key and dimensions for',len(report),'furniture assets.')
