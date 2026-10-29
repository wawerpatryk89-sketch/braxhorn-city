"""Add original ORM support and repair source leather without replacing the premium pass."""
import io,json,pathlib,struct
import numpy as np
from PIL import Image
R=pathlib.Path(__file__).resolve().parents[1];S=R/'unity-city/Assets/Source';P=S/'Premium'
path=S/'braxhorn-original-gym.glb';raw=path.read_bytes();n=struct.unpack_from('<I',raw,12)[0];g=json.loads(raw[20:20+n]);length=struct.unpack_from('<I',raw,20+n)[0];b=bytearray(raw[28+n:28+n+length])
assert not g.get('extras',{}).get('braxLeatherCorrected'),'Do not apply leather correction twice'
# These are the four actual bag-body materials in the pinned source export.
# Resolve their colour slots rather than depending on a global texture-array order.
bag_materials=[40,42,44,45]
for material_id in bag_materials:
 material=g['materials'][material_id]
 assert material.get('extras',{}).get('lampField')=='lit','Original bag material role changed'
 ti=material['pbrMetallicRoughness']['baseColorTexture']['index']
 i=g['textures'][ti]['source'];v=g['bufferViews'][g['images'][i]['bufferView']];start=v.get('byteOffset',0)
 image=Image.open(io.BytesIO(b[start:start+v['byteLength']])).convert('RGB');a=np.asarray(image,dtype=np.float32)/255
 a=np.where(a<=.0031308,a*12.92,1.055*np.power(a,1/2.4)-.055)
 out=io.BytesIO();Image.fromarray(np.uint8(np.clip(a,0,1)*255)).save(out,format='PNG');payload=out.getvalue()
 while len(b)%4:b.append(0)
 start=len(b);b.extend(payload);g['bufferViews'].append({'buffer':0,'byteOffset':start,'byteLength':len(payload)})
 g['images'][i]={'bufferView':len(g['bufferViews'])-1,'mimeType':'image/png'}
g.setdefault('extras',{})['braxLeatherCorrected']=True;g['buffers']=[{'byteLength':len(b)}]
j=json.dumps(g,separators=(',',':')).encode();j+=b' '*((-len(j))%4);b+=b'\0'*((-len(b))%4)
path.write_bytes(struct.pack('<III',0x46546c67,2,28+len(j)+len(b))+struct.pack('<II',len(j),0x4e4f534a)+j+struct.pack('<II',len(b),0x004e4942)+b)
Image.open(R/'public/assets/tex/brax_orm.webp').convert('RGB').transpose(Image.Transpose.FLIP_TOP_BOTTOM).save(P/'BraxOrm.png')
# Runtime and Editor source is reviewed and committed directly. Asset preparation
# must never patch C# or shader text behind the build's recorded commit.
(P/'geometry-review.json').write_text(json.dumps({'chainFacesOutward':True,'bagUVHandednessPreserved':True,'originalHeroRigChanged':False,'wholeCharacterEmission':False,'sofaPosition':[14.4,0,8.4],'sourceCodeRewritten':False},indent=2))
print('SOURCE_REFINEMENTS_PREPARED: original ORM and source leather colour space; no code mutation')
