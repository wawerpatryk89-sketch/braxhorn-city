"""Non-destructive material pass. Original rigs, UVs, animations and logos remain intact."""
import colorsys, hashlib, io, json, pathlib, struct
import numpy as np
from PIL import Image, ImageFilter
ROOT=pathlib.Path(__file__).resolve().parents[1]
OUT=ROOT/'unity-city/Assets/Source'; P=OUT/'Premium';P.mkdir(parents=True,exist_ok=True)

def read_glb(path):
 raw=path.read_bytes();jl=struct.unpack_from('<I',raw,12)[0];g=json.loads(raw[20:20+jl]);bl=struct.unpack_from('<I',raw,20+jl)[0]
 return g,bytearray(raw[28+jl:28+jl+bl])
def save_glb(path,g,b):
 g['buffers']=[{'byteLength':len(b)}];j=json.dumps(g,separators=(',',':')).encode();j+=b' '*((-len(j))%4);b+=b'\0'*((-len(b))%4)
 path.write_bytes(struct.pack('<III',0x46546c67,2,28+len(j)+len(b))+struct.pack('<II',len(j),0x4e4f534a)+j+struct.pack('<II',len(b),0x004e4942)+b)
def image_from(g,b,i):
 v=g['bufferViews'][g['images'][i]['bufferView']];start=v.get('byteOffset',0);return Image.open(io.BytesIO(b[start:start+v['byteLength']])).convert('RGB')
def normal(height,strength=2):
 a=np.asarray(height,dtype=np.float32)/255;dx=(np.roll(a,-1,1)-np.roll(a,1,1))*strength;dy=(np.roll(a,-1,0)-np.roll(a,1,0))*strength
 n=np.stack([-dx,dy,np.ones_like(a)],axis=2);n/=np.maximum(np.linalg.norm(n,axis=2,keepdims=True),1e-6)
 return Image.fromarray(np.uint8(np.clip(n*.5+.5,0,1)*255))

g,b=read_glb(OUT/'braxhorn-original-gym.glb')

def texture_image(index):
 return image_from(g,b,g['textures'][index]['source'])
def colour_slot(material):
 return material.get('pbrMetallicRoughness',{}).get('baseColorTexture')
def uv_signature(slot):
 return (slot.get('texCoord',0),json.dumps(slot.get('extensions',{}).get('KHR_texture_transform',{}),sort_keys=True))

def world_material_bounds():
 # GLTFExporter flattens meshes to world transforms under an identity root.
 # Traverse the exported hierarchy anyway, so a changed export cannot silently
 # attach a wall texture to a small prop merely because an array index changed.
 result={}
 def visit(node_id,parent):
  node=g['nodes'][node_id]
  if 'matrix' in node:
   local=np.asarray(node['matrix'],dtype=np.float64).reshape(4,4).T
  else:
   x,y,z,w=node.get('rotation',[0,0,0,1]);local=np.eye(4)
   local[:3,:3]=np.array([[1-2*y*y-2*z*z,2*x*y-2*z*w,2*x*z+2*y*w],[2*x*y+2*z*w,1-2*x*x-2*z*z,2*y*z-2*x*w],[2*x*z-2*y*w,2*y*z+2*x*w,1-2*x*x-2*y*y]])@np.diag(node.get('scale',[1,1,1]))
   local[:3,3]=node.get('translation',[0,0,0])
  matrix=parent@local
  if 'mesh' in node:
   for primitive in g['meshes'][node['mesh']]['primitives']:
    mat=primitive.get('material');accessor=g['accessors'][primitive['attributes']['POSITION']]
    if mat is None or 'min' not in accessor or 'max' not in accessor:continue
    corners=np.array([[x,y,z,1] for x in [accessor['min'][0],accessor['max'][0]] for y in [accessor['min'][1],accessor['max'][1]] for z in [accessor['min'][2],accessor['max'][2]]])
    world=(matrix@corners.T).T[:,:3];lo=world.min(0);hi=world.max(0)
    if mat not in result:result[mat]=[lo,hi]
    else:result[mat]=[np.minimum(result[mat][0],lo),np.maximum(result[mat][1],hi)]
  for child in node.get('children',[]):visit(child,matrix)
 for root in g['scenes'][g.get('scene',0)]['nodes']:visit(root,np.eye(4))
 return result

bounds=world_material_bounds()
slots=[]
for i,m in enumerate(g['materials']):
 box=bounds.get(i)
 slots.append({'material':i,'name':m.get('name',''),'baseColorTexture':colour_slot(m),'normalTexture':m.get('normalTexture'),'bounds':None if box is None else {'min':box[0].tolist(),'max':box[1].tolist()}})
(P/'gltf-material-slots.json').write_text(json.dumps(slots,indent=2))
assert len(g['materials'])>69 and colour_slot(g['materials'][9]) and colour_slot(g['materials'][69]),'Source material slots changed; inspect gltf-material-slots.json'
wall=bounds[9][1]-bounds[9][0];floor_bounds=bounds[69][1]-bounds[69][0]
assert wall[1]>4 and max(wall[0],wall[2])>10,'Material 9 is no longer the original hall masonry'
assert floor_bounds[0]>30 and floor_bounds[2]>20 and floor_bounds[1]<.02,'Material 69 is no longer the original flat hall floor'

def surface_normal(material_id,filename,strength):
 material=g['materials'][material_id];base=colour_slot(material)
 # Baked MeshBasicMaterial drops its normal map. Reuse it only when the GLB
 # contains a lit material with the SAME albedo texture and UV transform.
 candidates=[material]+[m for m in g['materials'] if colour_slot(m)==base and m is not material]
 for candidate in candidates:
  normal_slot=candidate.get('normalTexture')
  if normal_slot and uv_signature(normal_slot)==uv_signature(base):
   texture_image(normal_slot['index']).save(P/filename)
   return {'material':material_id,'baseTexture':base['index'],'normalTexture':normal_slot['index'],'method':'source-normal-same-albedo-and-UV'}
 # Explicit derived relief, not an unrelated guessed texture or an alleged
 # recovered source normal map. It follows the original surface's exact UVs.
 height=texture_image(base['index']).convert('L').filter(ImageFilter.GaussianBlur(.6))
 normal(height,strength).save(P/filename)
 return {'material':material_id,'baseTexture':base['index'],'normalTexture':None,'method':'relief-derived-from-original-albedo'}

surface_sources=[surface_normal(9,'BrickNormal.png',2.2),surface_normal(69,'RubberNormal.png',1.6)]
for i,m in enumerate(g['materials']):m['name']='BX_'+str(i)+('_baked' if m.get('name')=='_baked' else '_original')
rng=np.random.default_rng(7482);N=512;y,x=np.mgrid[:N,:N]
leather=np.clip(128+rng.normal(0,17,(N,N)),0,255).astype('uint8')
normal(Image.fromarray(leather).filter(ImageFilter.GaussianBlur(.6)),1.7).save(P/'LeatherNormal.png')
weave=128+13*np.sin(x*np.pi/2)*np.cos(y*np.pi/2)+rng.normal(0,3,(N,N))
normal(Image.fromarray(np.uint8(np.clip(weave,0,255))),.65).save(P/'CanvasNormal.png')
steel=np.clip(128+rng.normal(0,1,(N,N))+rng.normal(0,7,(1,N)),0,255).astype('uint8')
normal(Image.fromarray(steel),.6).save(P/'SteelNormal.png')
# Roughness is data, not colour. It is imported as linear in Unity. Keep every
# original albedo/logo intact; only the microscopic response changes by surface.
def roughness(name, values):
 a=np.asarray(values,dtype=np.float32)
 Image.fromarray(np.uint8(np.clip(a,0,1)*255),mode='L').save(P/name)
leather_height=np.asarray(Image.fromarray(leather).filter(ImageFilter.GaussianBlur(.8)),dtype=np.float32)/255
roughness('LeatherRoughness.png',.68+(leather_height-.5)*.65)
roughness('CanvasRoughness.png',.88+(weave-128)/255*.55)
roughness('SteelRoughness.png',.40+(steel.astype(np.float32)-128)/255*.8)
rubber_grain=np.asarray(Image.fromarray(np.uint8(np.clip(128+rng.normal(0,22,(N,N)),0,255))).filter(ImageFilter.GaussianBlur(.5)),dtype=np.float32)/255
roughness('RubberRoughness.png',.82+(rubber_grain-.5)*.45)
normal(Image.fromarray(np.uint8(np.clip(rubber_grain,0,1)*255)),.7).save(P/'RubberMicroNormal.png')
brick_detail=np.asarray(Image.open(P/'BrickNormal.png').convert('RGB'),dtype=np.float32)/255
roughness('BrickRoughness.png',.83+(1-brick_detail[:,:,2])*.2)
rope=128+18*np.sin((x+y)*np.pi/4)+5*np.sin((x-y)*np.pi/2)
normal(Image.fromarray(np.uint8(np.clip(rope,0,255))),.70).save(P/'RopeNormal.png')
roughness('RopeRoughness.png',.85+(rope-128)/255*.45)
save_glb(OUT/'braxhorn-original-gym.glb',g,b)

h,hb=read_glb(OUT/'BraxOriginal.glb')
tex=image_from(h,hb,0);rgb=np.asarray(tex,dtype=np.float32)/255
hi=rgb.max(2);lo=rgb.min(2);sat=(hi-lo)/np.maximum(hi,1e-5)
green=np.clip((rgb[:,:,1]-np.maximum(rgb[:,:,0],rgb[:,:,2])*1.12)*12,0,1)
green*=np.clip((sat-.18)*4,0,1)
target=np.stack([hi*.53,np.minimum(hi*1.28+.035,.96),hi*.19],axis=2)
rgb=rgb*(1-green[:,:,None]*.66)+target*(green[:,:,None]*.66)
cloth=np.clip((.38-sat)*3,0,1)*np.clip((.58-hi)*3,0,1)
rgb*=1-cloth[:,:,None]*.10
corrected=Image.fromarray(np.uint8(np.clip(rgb,0,1)*255)).filter(ImageFilter.UnsharpMask(radius=.8,percent=38,threshold=3))
corrected.save(P/'BraxAlbedo.png');image_from(h,hb,1).save(P/'BraxNormal.png')
report={'originalHeroGeometryChanged':False,'originalHeroRigChanged':False,'originalUVsChanged':False,'originalLogosChanged':False,'sourceTextureSize':list(tex.size),'greenPixelsAdjusted':int(np.count_nonzero(green>.05)),'newMaps':['source-linked brick relief','rubber relief','leather grain','canvas weave','brushed metal','Brax colour and normal','linear brick/rubber/leather/canvas/steel/rope roughness'], 'roughnessMapsAreLinear':True, 'sourceCodeRewritten':False,'surfaceSources':surface_sources}
(P/'material-review.json').write_text(json.dumps(report,indent=2))

print('PREMIUM_SOURCE_MATERIALS_PREPARED',json.dumps(report))
