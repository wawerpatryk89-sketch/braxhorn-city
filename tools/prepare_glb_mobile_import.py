"""Externalize original GLB images losslessly for Unity texture import settings.

The original GLB remains unchanged. The derived GLB keeps its binary chunk
byte-for-byte; only JSON image references change. No pixels/mesh data are reduced.
"""
import argparse, copy, hashlib, json, struct
from pathlib import Path
from audit_oboz_environment import glb

COLOR={'baseColorTexture','emissiveTexture','diffuseTexture','specularGlossinessTexture','specularColorTexture','sheenColorTexture','diffuseTransmissionColorTexture'}
def compact_externalized(doc,binary):
    """Drop unreferenced embedded images; retain every referenced data view.

    Each retained mesh/animation/skin byte range is copied without conversion.
    Unknown compressed buffer-view extensions are rejected, never guessed.
    """
    if len(doc.get('buffers',[]))!=1 or any('extensions' in v for v in doc['bufferViews']):
        raise ValueError('Unsupported buffer storage for compaction')
    references=set()
    def scan(value):
        if isinstance(value,dict):
            for key,item in value.items():
                if key=='bufferView':references.add(item)
                else:scan(item)
        elif isinstance(value,list):
            for item in value:scan(item)
    scan({k:v for k,v in doc.items() if k!='bufferViews'})
    remap={old:new for new,old in enumerate(sorted(references))};new_views=[];out=bytearray()
    for old in sorted(references):
        view=doc['bufferViews'][old]
        if view.get('buffer',0)!=0:raise ValueError('External geometry buffer')
        start=view.get('byteOffset',0);data=binary[start:start+view['byteLength']]
        if len(data)!=view['byteLength']:raise ValueError('Truncated geometry')
        offset=len(out);out.extend(data);out.extend(b'\0'*((-len(out))%4))
        item=copy.deepcopy(view);item['byteOffset']=offset;new_views.append(item)
        if bytes(out[offset:offset+len(data)])!=data:raise ValueError('Geometry changed')
    def replace(value):
        if isinstance(value,dict):
            for key,item in value.items():
                if key=='bufferView':value[key]=remap[item]
                else:replace(item)
        elif isinstance(value,list):
            for item in value:replace(item)
    replace(doc);doc['bufferViews']=new_views;doc['buffers'][0]['byteLength']=len(out)
    return bytes(out),remap

def prepare(path, shared_textures=None, compact=False):
    original=path.read_bytes();doc,binary=glb(original);source_document=copy.deepcopy(doc)
    usage={}
    def walk(value,key=''):
        if isinstance(value,dict):
            if 'index' in value and key.endswith('Texture'):
                source=doc['textures'][value['index']].get('source')
                if source is None:raise ValueError('Texture extension needs explicit conversion')
                usage.setdefault(source,set()).add('color' if key in COLOR else 'data')
            for k,v in value.items():walk(v,k)
        elif isinstance(value,list):
            for v in value:walk(v,key)
    walk(doc.get('materials',[]))
    if any(len(u)>1 for u in usage.values()):raise ValueError('Image is shared by colour/data slots; do not guess its import colour space')
    directory=path.parent/(shared_textures or (path.stem+'_textures'))
    if directory.parent!=path.parent or not directory.name.endswith('_textures'):
        raise ValueError('Texture directory must be a direct child ending in _textures')
    directory.mkdir(exist_ok=True)
    maps=[]
    for i,image in enumerate(doc.get('images',[])):
        if 'bufferView' not in image:raise ValueError('Source image is not embedded')
        bv=doc['bufferViews'][image['bufferView']];start=bv.get('byteOffset',0);raw=binary[start:start+bv['byteLength']]
        if len(raw)!=bv['byteLength']:raise ValueError('Truncated image')
        ext={'image/jpeg':'.jpg','image/png':'.png'}.get(image.get('mimeType'))
        if ext is None:raise ValueError('Unsupported source image format')
        color=usage.get(i,{'data'})=={'color'}
        sha=hashlib.sha256(raw).hexdigest()
        target=directory/((sha if shared_textures else str(i))+('_Color' if color else '_data__lin')+ext)
        if target.exists() and target.read_bytes()!=raw:
            raise ValueError('Conflicting existing texture; refusing to overwrite '+str(target))
        if not target.exists():target.write_bytes(raw)
        image.pop('bufferView');image['uri']=target.relative_to(path.parent).as_posix()
        maps.append({'imageIndex':i,'file':image['uri'],'color':color,'sha256':hashlib.sha256(raw).hexdigest()})
    original_binary=binary
    remap={}
    if compact:binary,remap=compact_externalized(doc,binary)
    encoded=json.dumps(doc,separators=(',',':'),ensure_ascii=True).encode();encoded+=b' '*((-len(encoded))%4)
    assert len(binary)%4==0
    result=struct.pack('<4sII',b'glTF',2,12+8+len(encoded)+8+len(binary))+struct.pack('<II',len(encoded),0x4E4F534A)+encoded+struct.pack('<II',len(binary),0x004E4942)+binary
    derived=path.with_name(path.stem+'__unity.glb');derived.write_bytes(result)
    d2,b2=glb(result)
    if b2!=binary or path.read_bytes()!=original:raise ValueError('Original or geometry changed')
    for field in ['meshes','nodes','skins','animations','materials','textures']+([] if compact else ['accessors','bufferViews']):
        if d2.get(field)!=source_document.get(field):raise ValueError('Unexpected JSON change '+field)
    report={'source':path.name,'sourceSha256':hashlib.sha256(original).hexdigest(),'derived':derived.name,
      'derivedSha256':hashlib.sha256(result).hexdigest(),'binaryChunkUnchanged':not compact,'sourceUnchanged':True,'maps':maps,'unityImported':False,
      'geometryBufferViewsByteExact':True,'originalBinaryBytes':len(original_binary),'derivedBinaryBytes':len(binary)}
    if compact:report['bufferViewRemap']={str(k):v for k,v in remap.items()}
    return report
if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('source',type=Path,nargs='+');p.add_argument('--report',type=Path,required=True)
    p.add_argument('--shared-textures',help='Sibling directory ending in _textures, deduplicated by SHA-256 and colour space')
    p.add_argument('--compact',action='store_true',help='Remove unused embedded-image bytes; preserve all geometry byte ranges');a=p.parse_args()
    reports=[prepare(path,a.shared_textures,a.compact) for path in a.source]
    a.report.write_text(json.dumps(reports,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps([{'source':r['source'],'maps':len(r['maps']),'sourceUnchanged':r['sourceUnchanged']} for r in reports]))
