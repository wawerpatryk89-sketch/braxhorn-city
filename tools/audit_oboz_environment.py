"""Read/stage GLB originals and their source metadata without changing geometry."""
import argparse, hashlib, io, json, shutil, struct
from pathlib import Path
import numpy as np
from PIL import Image

SOURCES=['underground_bunker.glb','update_dirt_road_through_forest.glb','forest_ground_texture_seamless__4k_pbr.glb']
def glb(raw):
    magic,version,total=struct.unpack_from('<4sII',raw)
    if magic!=b'glTF' or version!=2 or total!=len(raw):raise ValueError('Invalid GLB header/length')
    pos=12;document=None;binary=None
    while pos<len(raw):
        size,kind=struct.unpack_from('<II',raw,pos);pos+=8;chunk=raw[pos:pos+size];pos+=size
        if len(chunk)!=size:raise ValueError('Truncated GLB')
        if kind==0x4E4F534A:document=json.loads(chunk)
        if kind==0x004E4942:binary=chunk
    if document is None or binary is None:raise ValueError('Missing GLB chunks')
    return document,binary
def accessor(d,b,index):
    a=d['accessors'][index];v=d['bufferViews'][a['bufferView']]
    if 'sparse' in a:raise ValueError('Sparse accessor requires a separate audit')
    types={5120:'i1',5121:'u1',5122:'<i2',5123:'<u2',5125:'<u4',5126:'<f4'}
    size={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4,'MAT4':16}[a['type']]
    dtype=np.dtype(types[a['componentType']]);offset=v.get('byteOffset',0)+a.get('byteOffset',0)
    stride=v.get('byteStride',size*dtype.itemsize)
    return np.ndarray((a['count'],size),dtype=dtype,buffer=b,offset=offset,strides=(stride,dtype.itemsize)).copy()
def transform(n):
    if 'matrix' in n:return np.array(n['matrix']).reshape(4,4,order='F')
    x,y,z,w=n.get('rotation',[0,0,0,1])
    m=np.eye(4);m[:3,:3]=[[1-2*y*y-2*z*z,2*x*y-2*z*w,2*x*z+2*y*w],[2*x*y+2*z*w,1-2*x*x-2*z*z,2*y*z-2*x*w],[2*x*z-2*y*w,2*y*z+2*x*w,1-2*x*x-2*y*y]]
    m[:3,:3]=m[:3,:3]@np.diag(n.get('scale',[1,1,1]));m[:3,3]=n.get('translation',[0,0,0]);return m
def inspect(path,project,output):
    raw=path.read_bytes();d,b=glb(raw)
    report={'file':path.name,'sha256':hashlib.sha256(raw).hexdigest(),'bytes':len(raw),'provenance':d.get('asset',{}),
        'meshes':len(d.get('meshes',[])),'nodes':len(d.get('nodes',[])),'skins':len(d.get('skins',[])),
        'animations':len(d.get('animations',[])),'materials':len(d.get('materials',[])),'extensions':d.get('extensionsUsed',[]),
        'trianglesInMeshDefinitions':0,'images':[],'meshNodes':[],'unityRuntimeVerified':False}
    for m in d.get('meshes',[]):
        for p in m['primitives']:
            if p.get('mode',4)!=4:raise ValueError('Nontriangle primitive needs manual audit')
            count=d['accessors'][p['indices']]['count'] if 'indices' in p else d['accessors'][p['attributes']['POSITION']]['count']
            report['trianglesInMeshDefinitions']+=count//3
    bounds=[]
    def visit(i,parent):
        n=d['nodes'][i];m=parent@transform(n)
        if 'mesh' in n:
            positions=[]
            for p in d['meshes'][n['mesh']]['primitives']:
                v=accessor(d,b,p['attributes']['POSITION']);v=v@m[:3,:3].T+m[:3,3];positions.append(v)
            v=np.concatenate(positions);lo=v.min(axis=0);hi=v.max(axis=0);bounds.extend([lo,hi])
            report['meshNodes'].append({'name':n.get('name',''),'mesh':n['mesh'],'min':lo.tolist(),'max':hi.tolist(),'skinned':'skin' in n})
        for child in n.get('children',[]):visit(child,m)
    for i in d['scenes'][d.get('scene',0)].get('nodes',[]):visit(i,np.eye(4))
    if bounds:
        v=np.array(bounds);report['staticBounds']={'min':v.min(axis=0).tolist(),'max':v.max(axis=0).tolist(),'size':(v.max(axis=0)-v.min(axis=0)).tolist()}
    for i,im in enumerate(d.get('images',[])):
        if 'bufferView' not in im:raise ValueError('External image requires original dependency')
        bv=d['bufferViews'][im['bufferView']];start=bv.get('byteOffset',0);data=b[start:start+bv['byteLength']]
        with Image.open(io.BytesIO(data)) as image:w,h=image.size
        report['images'].append({'index':i,'name':im.get('name',''),'mime':im.get('mimeType'),'width':w,'height':h,'sha256':hashlib.sha256(data).hexdigest()})
        # Extract the ground maps losslessly for inspection/material binding.
        if path.name.startswith('forest_ground'):
            ext={'image/jpeg':'.jpg','image/png':'.png'}.get(im.get('mimeType'))
            if ext:
                dest=project/'Assets/Source/Prywatne/Oboz/environment/forest-ground'/('image_'+str(i)+ext)
                dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(data)
    target=project/'Assets/Source/Prywatne/Oboz/environment/models'/path.name
    target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(path,target)
    if target.read_bytes()!=raw:raise ValueError('Staged GLB differs')
    report['path']=target.relative_to(project).as_posix()
    if path.name.startswith('forest_ground'):report['materialDefinitions']=d['materials'];report['textureDefinitions']=d.get('textures',[])
    return report
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--sources',type=Path,required=True);p.add_argument('--project',type=Path,required=True);p.add_argument('--output',type=Path,required=True)
    a=p.parse_args();results=[]
    for name in SOURCES:
        paths=list(a.sources.rglob(name))
        if len(paths)!=1:raise ValueError('Missing or ambiguous '+name)
        r=inspect(paths[0],a.project,a.output);results.append(r)
        print(json.dumps({k:r[k] for k in ['file','trianglesInMeshDefinitions','meshes','skins','staticBounds']}),flush=True)
    a.output.write_text(json.dumps(results,ensure_ascii=False,indent=2)+'\n')
