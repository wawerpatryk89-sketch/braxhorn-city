"""Prepare existing source assets only; never synthesize a replacement character or gym."""
import gzip, hashlib, io, json, pathlib, struct
from PIL import Image
ROOT = pathlib.Path(__file__).resolve().parents[1]
OUT = ROOT / 'unity-city/Assets/Source'
OUT.mkdir(parents=True, exist_ok=True)

def read_glb(raw):
    assert raw[:4] == b'glTF' and struct.unpack_from('<I', raw, 4)[0] == 2
    jl, jt = struct.unpack_from('<II', raw, 12)
    assert jt == 0x4E4F534A
    obj = json.loads(raw[20:20+jl])
    pos = 20+jl
    bl, bt = struct.unpack_from('<II', raw, pos)
    assert bt == 0x004E4942
    return obj, bytearray(raw[pos+8:pos+8+bl])

def append_data(obj, buf, payload):
    while len(buf) % 4: buf.append(0)
    off = len(buf); buf.extend(payload)
    obj.setdefault('bufferViews', []).append({'buffer':0, 'byteOffset':off, 'byteLength':len(payload)})
    return len(obj['bufferViews'])-1

def save_glb(path,obj,buf):
    obj['buffers']=[{'byteLength':len(buf)}]
    j=json.dumps(obj,separators=(',',':')).encode();j+=b' '*((-len(j))%4)
    buf+=b'\x00'*((-len(buf))%4)
    path.write_bytes(struct.pack('<III',0x46546c67,2,28+len(j)+len(buf))+struct.pack('<II',len(j),0x4E4F534A)+j+struct.pack('<II',len(buf),0x004E4942)+buf)

# The earlier exporter copied Float16BufferAttribute storage as Uint16. glTF has no
# half-float color encoding. Decode the original IEEE754 half values, do not normalize bits.
gym_path=OUT/'braxhorn-original-gym.glb'
original_hash=hashlib.sha256(gym_path.read_bytes()).hexdigest()
gym,gb=read_glb(gym_path.read_bytes()); fixed=set()
for mesh in gym['meshes']:
    for p in mesh['primitives']:
        ai=p.get('attributes',{}).get('COLOR_0')
        if ai is None or ai in fixed: continue
        a=gym['accessors'][ai]
        if a['componentType']!=5123 or a.get('normalized',False): continue
        n=a['count']*int(a['type'][-1]);v=gym['bufferViews'][a['bufferView']]
        start=v.get('byteOffset',0)+a.get('byteOffset',0)
        if v.get('byteStride') not in (None,int(a['type'][-1])*2): raise RuntimeError('Unexpected interleaved half colors')
        values=struct.unpack_from('<'+'e'*n,gb,start)
        if not all(0<=x<=16 for x in values): raise RuntimeError('Invalid recovered baked color')
        vi=append_data(gym,gb,struct.pack('<'+'f'*n,*values))
        a.update(bufferView=vi,byteOffset=0,componentType=5126)
        a.pop('normalized',None);a.pop('min',None);a.pop('max',None);fixed.add(ai)
save_glb(gym_path,gym,gb)

rig_source=ROOT/'public/assets/chars/brax.glb.gz'
rig_raw=gzip.decompress(rig_source.read_bytes());hero,hb=read_glb(rig_raw)
all_anims=[a.get('name','') for a in hero.get('animations',[])]
required=['Idle','Walking','Running','Jab Cross (1)','Mma Kick (1)']
for name in required:
    if name not in all_anims: raise RuntimeError('Missing real animation: '+name)
keep=set(required+['Jump','Opening','Opening (1)','Boxing','Warming Up','Hook'])
hero['animations']=[a for a in hero['animations'] if a.get('name') in keep]
hero['images']=[];hero['textures']=[];hero['samplers']=[{'magFilter':9729,'minFilter':9987,'wrapS':10497,'wrapT':10497}]
for kind in ['color','normal']:
    src=ROOT/f'public/assets/tex/brax_{kind}.webp'
    image=Image.open(src).convert('RGB')
    # The original Three.js game replaces GLTFLoader textures with flipY=true.
    # Flip the image to preserve that appearance in standard glTF/Unity UV space.
    image=image.transpose(Image.Transpose.FLIP_TOP_BOTTOM)
    if max(image.size)>2048: image.thumbnail((2048,2048),Image.Resampling.LANCZOS)
    data=io.BytesIO();image.save(data,format='PNG')
    view=append_data(hero,hb,data.getvalue());hero['images'].append({'bufferView':view,'mimeType':'image/png'})
    hero['textures'].append({'source':len(hero['images'])-1,'sampler':0})
hero['materials']=[{'name':'Brax original matte PBR','pbrMetallicRoughness':{'baseColorTexture':{'index':0},'metallicFactor':0,'roughnessFactor':0.82},'normalTexture':{'index':1,'scale':0.6}}]
for mesh in hero['meshes']:
    for primitive in mesh['primitives']: primitive['material']=0
save_glb(OUT/'BraxOriginal.glb',hero,hb)
report={'sourceGymSha256':original_hash,'recoveredHalfFloatColorAccessors':len(fixed),'gymMeshes':len(gym['meshes']),'braxSourceSha256':hashlib.sha256(rig_raw).hexdigest(),'originalAnimations':all_anims,'includedAnimations':[a['name'] for a in hero['animations']],'geometryReplaced':False,'originalModelScale':0.024}
(OUT/'source-integrity.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
