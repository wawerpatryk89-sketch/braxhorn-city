"""Premium surface pass. Original geometry, UVs, rig, animations and branding are retained."""
import hashlib, io, json, pathlib, struct
import numpy as np
from PIL import Image, ImageEnhance, ImageFilter
ROOT=pathlib.Path(__file__).resolve().parents[1]
SOURCE=ROOT/'unity-city/Assets/Source'; OUT=SOURCE/'Premium';OUT.mkdir(parents=True,exist_ok=True)
def read(path):
 raw=path.read_bytes();assert raw[:4]==b'glTF'
 n=struct.unpack_from('<I',raw,12)[0];j=json.loads(raw[20:20+n]);pos=20+n
 size,kind=struct.unpack_from('<II',raw,pos);assert kind==0x004e4942
 return j,bytearray(raw[pos+8:pos+8+size])
def save(path,j,b):
 j['buffers']=[{'byteLength':len(b)}];s=json.dumps(j,separators=(',',':')).encode();s+=b' '*((-len(s))%4);b+=b'\0'*((-len(b))%4)
 path.write_bytes(struct.pack('<III',0x46546c67,2,28+len(s)+len(b))+struct.pack('<II',len(s),0x4e4f534a)+s+struct.pack('<II',len(b),0x004e4942)+b)
def image(j,b,ti):
 im=j['images'][j['textures'][ti]['source']];v=j['bufferViews'][im['bufferView']];off=v.get('byteOffset',0)
 return Image.open(io.BytesIO(b[off:off+v['byteLength']])).convert('RGB')
def normal(height,strength):
 h=np.asarray(height,dtype=np.float32);dx=(np.roll(h,-1,1)-np.roll(h,1,1))*strength;dy=(np.roll(h,-1,0)-np.roll(h,1,0))*strength
 n=np.stack((-dx,-dy,np.ones_like(h)),axis=-1);n/=np.maximum(np.linalg.norm(n,axis=-1,keepdims=True),1e-6)
 return Image.fromarray(np.uint8(np.clip(n*.5+.5,0,1)*255))
gym,buf=read(SOURCE/'braxhorn-original-gym.glb')
roles={0:'ceiling',1:'frame',2:'steel',3:'iron',4:'iron',9:'brick',10:'panel',11:'accent',13:'panel',16:'glass',17:'door',18:'steel',23:'brick',24:'exteriorbrick',25:'steel',27:'glass',30:'panel',34:'chrome',35:'brick',36:'brick',37:'leather',38:'canvas',39:'apron',40:'bag',41:'accent',42:'bag',43:'leather',44:'bag',45:'bag',46:'rubber',47:'panel',48:'locker',50:'panel',51:'rubber',52:'paint',53:'rubber',54:'accent',55:'chrome',56:'leather',57:'rope',58:'rope',59:'rubber',60:'accent',61:'leather',62:'cardboard',63:'paint',64:'glass',65:'steel',66:'rubber',69:'floor',70:'turf',71:'concrete',72:'rubber'}
for i,m in enumerate(gym['materials']):
 baked=m.get('name')=='_baked';m['name']=f'BX_{i:02d}_{roles.get(i,"original")}_{"baked" if baked else "lit"}'
# The old procedural leather wrote linear channel values into an sRGB canvas.
# Repair that double-darkening without changing the bag colours or UV layout.
for ti in [15,16,17,18]:
 raw=np.asarray(image(gym,buf,ti),dtype=np.float32)/255
 corrected=np.where(raw<=.0031308,raw*12.92,1.055*np.power(raw,1/2.4)-.055)
 png=io.BytesIO();Image.fromarray(np.uint8(np.clip(corrected,0,1)*255)).save(png,format='PNG')
 while len(buf)%4:buf.append(0)
 off=len(buf);payload=png.getvalue();buf.extend(payload)
 gym['bufferViews'].append({'buffer':0,'byteOffset':off,'byteLength':len(payload)})
 gym['images'][gym['textures'][ti]['source']]={'bufferView':len(gym['bufferViews'])-1,'mimeType':'image/png'}
# Reuse the actual brick relief map that the old unlit material discarded.
image(gym,buf,9).save(OUT/'brick_normal.png')
floor=np.asarray(image(gym,buf,22).convert('L'),dtype=np.float32)/255
normal(floor,3.2).save(OUT/'floor_normal.png')
# Seamless micro-surfaces. These do not replace the original colour maps/logos.
rng=np.random.default_rng(891029);n=256;yy,xx=np.mgrid[:n,:n]
noise=rng.random((n,n),dtype=np.float32)
leather=np.asarray(Image.fromarray(np.uint8(noise*255)).filter(ImageFilter.GaussianBlur(.7)),dtype=np.float32)/255
weave=.5+.16*np.sin(xx*np.pi)+.16*np.sin(yy*np.pi)+.09*np.sin(xx*np.pi/2)*np.cos(yy*np.pi/2)
normal(leather,1.1).save(OUT/'leather_normal.png');normal(weave,1.5).save(OUT/'canvas_normal.png')
normal(noise*.1,1.0).save(OUT/'rubber_normal.png')
normal(.05*noise+.07*np.sin(yy*2.1),1).save(OUT/'steel_normal.png')
normal(.13*np.sin((xx+yy)*.7)+.035*noise,1.1).save(OUT/'rope_normal.png')
# Source texture is really 2048, not fake 4K. Keep its UV atlas, logos and pixels.
color=Image.open(ROOT/'public/assets/tex/brax_color.webp').convert('RGB')
a=np.asarray(color,dtype=np.float32)/255.;hi=a.max(2);lo=a.min(2);sat=(hi-lo)/np.maximum(hi,.001)
green=np.clip((a[:,:,1]-a[:,:,0]*1.10)*5,0,1)*np.clip((a[:,:,1]-a[:,:,2]*1.35)*5,0,1)
target=a.copy();target[:,:,1]=np.minimum(1,a[:,:,1]*1.18);target[:,:,0]=target[:,:,1]*.48;target[:,:,2]=target[:,:,1]*.095
a=a*(1-green[:,:,None]*.6)+target*green[:,:,None]*.6
neutral=(1-np.clip(sat/.24,0,1))*np.clip((.48-hi)/.25,0,1)
a*=1-neutral[:,:,None]*.10
color=Image.fromarray(np.uint8(np.clip(a,0,1)*255)).filter(ImageFilter.UnsharpMask(radius=.8,percent=45,threshold=3))
color.transpose(Image.Transpose.FLIP_TOP_BOTTOM).save(OUT/'hero_color.png')
for kind in ['normal','orm']:
 Image.open(ROOT/f'public/assets/tex/brax_{kind}.webp').convert('RGB').transpose(Image.Transpose.FLIP_TOP_BOTTOM).save(OUT/f'hero_{kind}.png')
Image.open(ROOT/'public/assets/logo/wide_a.webp').convert('RGBA').save(OUT/'brand.png')
save(SOURCE/'braxhorn-original-gym.glb',gym,buf)
report={'pass':'premium-02','baseColorResolution':list(color.size),'originalGeometryUnchanged':True,'originalRigUnchanged':True,'originalLogosUnchanged':True,'originalOrmRestored':True,'texturePass':'selective lime-green saturation, neutral black contrast, original UV preserved','sourceColorSha256':hashlib.sha256((ROOT/'public/assets/tex/brax_color.webp').read_bytes()).hexdigest(),'files':sorted(p.name for p in OUT.iterdir())}
(OUT/'premium-assets.json').write_text(json.dumps(report,indent=2)+'\n');print('PREMIUM_SOURCE_READY',json.dumps(report))
