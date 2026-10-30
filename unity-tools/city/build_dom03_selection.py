"""Select brick/roof triangles from the EXACT original dom03 atlases.
Unity validates each triangle centroid before binding; no position-only guesses.
"""
import hashlib, io, json, pathlib, struct
import numpy as np
from PIL import Image

ROOT = pathlib.Path(__file__).resolve().parents[2]
SOURCE = ROOT/'unity-city/Assets/Source/City/braxhorn-estate.glb'
OUT = ROOT/'unity-city/Assets/Source/City/R2Segment'
OUT.mkdir(parents=True, exist_ok=True)
with SOURCE.open('rb') as f:
    f.seek(12); size = struct.unpack('<I',f.read(4))[0]; f.read(4)
    g = json.loads(f.read(size)); base = 28 + size
    def accessor(index):
        a=g['accessors'][index];v=g['bufferViews'][a['bufferView']]
        dtype=np.dtype({5126:'<f4',5123:'<u2',5125:'<u4',5121:'u1',5122:'<i2',5120:'i1'}[a['componentType']])
        count={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}[a['type']]
        f.seek(base+v.get('byteOffset',0));data=f.read(v['byteLength'])
        result=np.ndarray((a['count'],count),dtype=dtype,buffer=data,offset=a.get('byteOffset',0),strides=(v.get('byteStride',count*dtype.itemsize),dtype.itemsize)).astype(np.float64)
        if a.get('normalized'):result/=np.iinfo(dtype).max
        return result
    for name in ['EstateMesh_309','EstateMesh_310','EstateMesh_311']:
        node=next(n for n in g['nodes'] if n['name']==name)
        p=g['meshes'][node['mesh']]['primitives'][0]
        material=g['materials'][p['material']]
        slot=material['pbrMetallicRoughness']['baseColorTexture']
        image=g['images'][g['textures'][slot['index']]['source']]
        view=g['bufferViews'][image['bufferView']]
        f.seek(base+view.get('byteOffset',0));raw=f.read(view['byteLength'])
        photo=np.asarray(Image.open(io.BytesIO(raw)).convert('RGB'))/255
        vertices=accessor(p['attributes']['POSITION']);uv=accessor(p['attributes']['TEXCOORD_0'])
        normals=accessor(p['attributes']['NORMAL'])
        indices=accessor(p['indices']).astype(int).reshape(-1,3)
        transform=np.array(node.get('matrix',np.eye(4).T.flatten())).reshape(4,4).T
        world=np.column_stack([vertices,np.ones(len(vertices))])@transform.T
        centers=world[indices,:3].mean(1);centers[:,0]*=-1
        uv=uv[indices].mean(1)
        mapping=slot.get('extensions',{}).get('KHR_texture_transform',{})
        uv=(uv*np.array(mapping.get('scale',[1,1]))+np.array(mapping.get('offset',[0,0])))%1
        pixels=photo[np.minimum((uv[:,1]*photo.shape[0]).astype(int),photo.shape[0]-1),np.minimum((uv[:,0]*photo.shape[1]).astype(int),photo.shape[1]-1)]
        if name=='EstateMesh_310':
            # Preserve the photographed white fascia; tile only upper dark roof faces.
            selected=(pixels.mean(1)<.43)&(normals[indices].mean(1)[:,1]>.15)
        else:
            # The atlas also contains real door/window details; never cover them.
            selected=(pixels[:,0]>pixels[:,1]*1.18)&(pixels[:,0]>pixels[:,2]*1.3)&(pixels[:,1]>.07)
            selected &= (uv[:,1]<.33)|((uv[:,0]<.41)&(uv[:,1]<.68))
        count=int(selected.sum())
        if count<10:raise RuntimeError('No verified source surface selected: '+name)
        report=dict(renderer=name,sourceAlbedoSha256=hashlib.sha256(raw).hexdigest(),selectedTriangles=count,faces=[dict(center=dict(zip(['x','y','z'],c.tolist())),selected=bool(s)) for c,s in zip(centers,selected)])
        (OUT/(name+'-selection.json')).write_text(json.dumps(report))
        print('R2_DOM03_ATLAS_TRIANGLES_IDENTIFIED',name,count,'/',len(indices))
