"""Repair Blender merged RGB/alpha packing from the original source pixels.

The exporter packed linear RGB into a colour PNG for fir needles. Reconstruct
RGBA from original sRGB colour and linear opacity maps, retaining 16-bit data.
Original archives, .blend, textures and mesh buffer views are never changed.
"""
import argparse,copy,hashlib,io,json,struct
from pathlib import Path
import numpy as np
import png
from audit_oboz_environment import glb

def digest(b):return hashlib.sha256(b).hexdigest()
def read16(path):
    with path.open('rb') as file:
        width,height,rows,info=png.Reader(file=file).asDirect()
        if info['bitdepth']!=16:raise ValueError('Expected untouched 16-bit source '+str(path))
        data=np.stack([np.asarray(row,dtype=np.uint16) for row in rows]).reshape(height,width,info['planes'])
    return data
def pack(diff,alpha):
    color=read16(diff);opacity=read16(alpha)
    if color.shape[:2]!=opacity.shape[:2]:raise ValueError('Map dimensions differ')
    rgba=np.concatenate([color[:,:,:3],opacity[:,:,:1]],axis=2)
    h,w=rgba.shape[:2];out=io.BytesIO();png.Writer(width=w,height=h,greyscale=False,alpha=True,bitdepth=16).write(out,rgba.reshape(h,w*4))
    raw=out.getvalue();ww,hh,rows,info=png.Reader(bytes=raw).asDirect()
    decoded=np.stack([np.asarray(row,dtype=np.uint16) for row in rows]).reshape(h,w,4)
    if (ww,hh)!=(w,h) or not np.array_equal(decoded,rgba):raise ValueError('Packing changed source pixel values')
    return raw
def repair(sources,models,report):
    pairs={
      'fir_tree_01_twig_diff-fir_tree_01_twig_alpha':('fir_tree_01_2k','fir_tree_01_twig'),
      'fir_sapling_medium_twigs_diff-fir_sapling_medium_twigs_alpha':('fir_sapling_medium_2k','fir_sapling_medium_twigs')}
    packed={};provenance=[]
    for name,(folder,stem) in pairs.items():
        diff=sources/folder/'textures'/(stem+'_diff_2k.png');alpha=diff.with_name(stem+'_alpha_2k.png');packed[name]=pack(diff,alpha)
        provenance.append({'image':name,'colorSource':str(diff.relative_to(sources)),'colorSha256':digest(diff.read_bytes()),
          'alphaSource':str(alpha.relative_to(sources)),'alphaSha256':digest(alpha.read_bytes()),'rgbaSha256':digest(packed[name]),'bitDepth':16,'sourcePixelsExact':True})
    changed=[]
    for path in sorted(models.glob('*_LOD*.glb')):
        if path.name.endswith('__unity.glb'):continue
        doc,binary=glb(path.read_bytes());before=copy.deepcopy(doc);result=bytearray(binary);count=0
        for im in doc['images']:
            if im.get('name') not in packed:continue
            raw=packed[im['name']];offset=len(result);result.extend(raw);result.extend(b'\0'*((-len(result))%4))
            im['bufferView']=len(doc['bufferViews']);im['mimeType']='image/png'
            doc['bufferViews'].append({'buffer':0,'byteOffset':offset,'byteLength':len(raw)});count+=1
        if not count:raise ValueError('Expected foliage material '+path.name)
        doc['buffers'][0]['byteLength']=len(result)
        for field in ['meshes','accessors','nodes','skins','animations','materials','textures']:
            if doc.get(field)!=before.get(field):raise ValueError('Geometry/materials changed')
        j=json.dumps(doc,separators=(',',':')).encode();j+=b' '*((-len(j))%4)
        raw=struct.pack('<4sII',b'glTF',2,28+len(j)+len(result))+struct.pack('<II',len(j),0x4e4f534a)+j+struct.pack('<II',len(result),0x004e4942)+result
        temp=path.with_suffix('.tmp');temp.write_bytes(raw);temp.replace(path)
        changed.append({'file':path.name,'sha256':digest(raw),'imagesRepacked':count,'originalGeometryBytesUnchanged':True})
    data={'reason':'Merged alpha export had linear RGB in an sRGB image','sourcePixelsPreserved':True,'textures':provenance,'models':changed}
    report.write_text(json.dumps(data,indent=2)+'\n');print({'correctedModels':len(changed),'packedMaps':len(packed)})
    export=report.parent/'vegetation.json';d=json.loads(export.read_text());hashes={r['file']:r['sha256'] for r in changed}
    for r in d['models']:
        if r['file'] in hashes:r['sha256']=hashes[r['file']];r['sourceColorPackingRepaired']=True
    export.write_text(json.dumps(d,indent=2)+'\n')
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--sources',type=Path,required=True);p.add_argument('--models',type=Path,required=True);p.add_argument('--report',type=Path,required=True)
    a=p.parse_args();repair(a.sources,a.models,a.report)
