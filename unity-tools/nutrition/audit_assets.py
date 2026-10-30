"""Validate the actual authored glTF geometry before Unity imports it."""
from pathlib import Path
import hashlib
import json
import struct
import numpy as np

ROOT=Path(__file__).resolve().parents[2]
SOURCE=ROOT/'unity-city/Assets/Nutrition'
path=SOURCE/'Models/NutritionKit.glb'
raw=path.read_bytes()
magic,version,length=struct.unpack_from('<III',raw)
if magic!=0x46546c67 or version!=2 or length!=len(raw):raise RuntimeError('Invalid authored GLB')
chunks={}
offset=12
while offset<len(raw):
    size,kind=struct.unpack_from('<II',raw,offset)
    chunks[kind]=raw[offset+8:offset+8+size];offset+=size+8
g=json.loads(chunks[0x4e4f534a]);buffer=chunks[0x004e4942]


def accessor(index):
    a=g['accessors'][index];view=g['bufferViews'][a['bufferView']]
    dtype={5126:'<f4',5125:'<u4',5123:'<u2',5121:'u1'}[a['componentType']]
    columns={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}[a['type']]
    size=np.dtype(dtype).itemsize
    return np.ndarray((a['count'],columns),dtype=dtype,buffer=buffer,
        offset=view.get('byteOffset',0)+a.get('byteOffset',0),
        strides=(view.get('byteStride',columns*size),size)).copy()


names={n.get('name') for n in g['nodes']}
required={'WheyTub','CreatineJar','Shaker','ShelvingBay','Counter','DisplayIsland','LinearPendant','FloorTile'}
if not required.issubset(names):raise RuntimeError('Authored model parts missing: '+str(required-names))
triangles=vertices=0
for mesh in g['meshes']:
    for primitive in mesh['primitives']:
        if primitive.get('mode',4)!=4:raise RuntimeError('Non-triangle geometry')
        attrs=primitive['attributes']
        positions=accessor(attrs['POSITION']).astype(float)
        normals=accessor(attrs['NORMAL']).astype(float)
        uv=accessor(attrs['TEXCOORD_0']).astype(float) if 'TEXCOORD_0' in attrs else None
        if not np.isfinite(positions).all() or not np.isfinite(normals).all():raise RuntimeError('Nonfinite source geometry')
        lengths=np.linalg.norm(normals,axis=1)
        if np.min(lengths)<.9 or np.max(lengths)>1.1:raise RuntimeError('Invalid normals in '+mesh.get('name','mesh'))
        if uv is not None and not np.isfinite(uv).all():raise RuntimeError('Nonfinite source UV')
        indices=accessor(primitive['indices']).ravel().astype(int).reshape(-1,3)
        a,b,c=(positions[indices[:,i]] for i in range(3))
        area2=np.linalg.norm(np.cross(b-a,c-a),axis=1)
        if np.any(area2<1e-13):raise RuntimeError('Degenerate triangle in '+mesh.get('name','mesh'))
        vertices+=len(positions);triangles+=len(indices)
if not 1000<triangles<80000:raise RuntimeError('Authored kit triangle budget exceeded: '+str(triangles))
for pbr in [m.get('pbrMetallicRoughness',{}) for m in g['materials']]:
    if not 0<=pbr.get('metallicFactor',1)<=1:raise RuntimeError('Invalid metalness')
    if not 0<=pbr.get('roughnessFactor',1)<=1:raise RuntimeError('Invalid roughness')
source_receipt=json.loads((SOURCE/'Models/authored-models.json').read_text())
sha=hashlib.sha256(raw).hexdigest()
if source_receipt['glbSha256']!=sha:raise RuntimeError('Authored model receipt hash mismatch')
textures=json.loads((SOURCE/'Textures/authored-textures.json').read_text())
for name,expected in textures['files'].items():
    if hashlib.sha256((SOURCE/'Textures'/name).read_bytes()).hexdigest()!=expected:
        raise RuntimeError('Authored texture changed without receipt: '+name)
report={'engineImport':'PENDING_UNITY6','sourceGlbSha256':sha,'sourceBytes':len(raw),
    'parts':sorted(required),'sourceVertices':vertices,'sourceTriangles':triangles,
    'materials':len(g['materials']),'embeddedImages':len(g.get('images',[])),
    'finiteGeometry':True,'validNormals':True,'degenerateTriangles':0,
    'ownerAccepted':False,'visualAccepted':False,'runtimeTested':False}
out=ROOT/'nutrition-asset-review';out.mkdir(exist_ok=True)
(out/'source-audit.json').write_text(json.dumps(report,indent=2)+'\n')
print('NUTRITION_SOURCE_GEOMETRY_AUDITED',json.dumps(report))
