"""Prepare the original city Brax with its own rig/animations; no Patryk retarget."""
import gzip, hashlib, io, json, pathlib, struct
import numpy as np
from PIL import Image, ImageFilter
ROOT = pathlib.Path(__file__).resolve().parents[2]
OUT = ROOT / 'unity-city/Assets/Source/Mistrzowie/Brax'
REQUIRED = ['Idle','Walking','Running','Jab Cross (1)','Mma Kick (1)','Jump']
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

def prepare(root=ROOT, out=OUT):
    raw = gzip.decompress((root/'public/assets/chars/brax.glb.gz').read_bytes())
    hero, hb = read_glb(raw)
    names = [a.get('name') for a in hero.get('animations', [])]
    missing = [n for n in REQUIRED if n not in names]
    if missing: raise ValueError('Original city Brax animations missing: '+', '.join(missing))
    hero['animations'] = [a for a in hero['animations'] if a.get('name') in REQUIRED]
    hero['images']=[];hero['textures']=[]
    hero['samplers']=[{'magFilter':9729,'minFilter':9987,'wrapS':10497,'wrapT':10497}]
    maps={}
    for kind in ['color','normal','orm']:
        with Image.open(root/f'public/assets/tex/brax_{kind}.webp') as im:
            im=im.convert('RGB').transpose(Image.Transpose.FLIP_TOP_BOTTOM)
        if kind != 'orm' and max(im.size)>2048: im.thumbnail((2048,2048),Image.Resampling.LANCZOS)
        maps[kind]=im
        if kind == 'orm': continue
        data=io.BytesIO();im.save(data,format='PNG')
        view=append_data(hero,hb,data.getvalue())
        hero['images'].append({'bufferView':view,'mimeType':'image/png'})
        hero['textures'].append({'source':len(hero['images'])-1,'sampler':0})
    hero['materials']=[{'name':'Brax original matte PBR','pbrMetallicRoughness':{'baseColorTexture':{'index':0},'metallicFactor':0,'roughnessFactor':0.82},'normalTexture':{'index':1,'scale':0.6}}]
    for mesh in hero['meshes']:
        for primitive in mesh['primitives']: primitive['material']=0
    # Exact city premium colour treatment from premium_prepare.py.
    tex=maps['color'];rgb=np.asarray(tex,dtype=np.float32)/255
    hi=rgb.max(2);lo=rgb.min(2);sat=(hi-lo)/np.maximum(hi,1e-5)
    green=np.clip((rgb[:,:,1]-np.maximum(rgb[:,:,0],rgb[:,:,2])*1.12)*12,0,1)
    green*=np.clip((sat-.18)*4,0,1)
    target=np.stack([hi*.53,np.minimum(hi*1.28+.035,.96),hi*.19],axis=2)
    rgb=rgb*(1-green[:,:,None]*.66)+target*(green[:,:,None]*.66)
    cloth=np.clip((.38-sat)*3,0,1)*np.clip((.58-hi)*3,0,1)
    rgb*=1-cloth[:,:,None]*.10
    corrected=Image.fromarray(np.uint8(np.clip(rgb,0,1)*255)).filter(ImageFilter.UnsharpMask(radius=.8,percent=38,threshold=3))
    out.mkdir(parents=True,exist_ok=True)
    save_glb(out/'BraxOriginal.glb',hero,hb)
    corrected.save(out/'BraxAlbedo.png');maps['normal'].save(out/'BraxNormal.png');maps['orm'].save(out/'BraxOrm.png')
    report={'character':'Brax','source':'public/assets/chars/brax.glb.gz','sha256':hashlib.sha256(raw).hexdigest(),'clips':REQUIRED,'modelScale':0.024,'retargeted':False,'unityVerified':False}
    (out/'source-integrity.json').write_text(json.dumps(report,indent=2)+'\n')
    return report

if __name__ == '__main__': print(json.dumps(prepare()))
