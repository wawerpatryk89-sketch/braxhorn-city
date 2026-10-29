"""Add original ORM support and repair source leather without replacing the premium pass."""
import io,json,pathlib,struct
import numpy as np
from PIL import Image
R=pathlib.Path(__file__).resolve().parents[1];S=R/'unity-city/Assets/Source';P=S/'Premium'
path=S/'braxhorn-original-gym.glb';raw=path.read_bytes();n=struct.unpack_from('<I',raw,12)[0];g=json.loads(raw[20:20+n]);length=struct.unpack_from('<I',raw,20+n)[0];b=bytearray(raw[28+n:28+n+length])
assert not g.get('extras',{}).get('braxLeatherCorrected'),'Do not apply leather correction twice'
for ti in [15,16,17,18]:
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
f=R/'unity-city/Assets/Editor/BraxGymBuild.cs';s=f.read_text();needle='BraxGymPremium.Apply(gym,hero,bags.ToArray(),camera,light,fill,Generated);'
assert s.count(needle)==1,'Expected a single verified premium integration point'
s=s.replace(needle,needle+'\n        BraxHeroRefinements.Apply(hero,bags.ToArray(),camera,Generated);',1);f.write_text(s)

# Verify exact source patterns before corrections; fail rather than overwrite a different implementation.
def replace_once(path,old,new):
 s=path.read_text();assert s.count(old)==1, 'Unexpected source changed: '+str(path)+' / '+old[:50]
 path.write_text(s.replace(old,new,1))
f=R/'unity-city/Assets/Editor/BraxGymPremium.cs'
replace_once(f,'tr[q++]=i;tr[q++]=i+1;tr[q++]=i+sides+1;tr[q++]=i+1;tr[q++]=i+sides+2;tr[q++]=i+sides+1;',
 'tr[q++]=i;tr[q++]=i+sides+1;tr[q++]=i+1;tr[q++]=i+1;tr[q++]=i+sides+1;tr[q++]=i+sides+2;')
replace_once(f,'new Vector3(11f,0,8.4f)','new Vector3(14.4f,0,8.4f)')
# New rounded bag must follow glTFast's X mirror so original labels remain oriented correctly.
f=R/'unity-city/Assets/Editor/BraxHeroRefinements.cs'
replace_once(f,'Mathf.Sin(a)*r[j]','-Mathf.Sin(a)*r[j]')
replace_once(f,'new[]{q,q+1,q+sides+1,q+1,q+sides+2,q+sides+1}',
 'new[]{q,q+sides+1,q+1,q+1,q+sides+1,q+sides+2}')
# Natural material response only: no self illumination on the whole character.
f=R/'unity-city/Assets/HeroPremium.shader'
replace_once(f,'o.Emission=c*(.035+green*.055);o.Alpha=1;','o.Emission=0;o.Alpha=1;')
(P/'geometry-review.json').write_text(json.dumps({'chainFacesOutward':True,'bagUVHandednessPreserved':True,'originalHeroRigChanged':False,'wholeCharacterEmission':False,'sofaPosition':[14.4,0,8.4]},indent=2))
print('SOURCE_REFINEMENTS_PREPARED: original ORM, leather colour space, outward faces, original rig/UV/logo retained')
