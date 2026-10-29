"""Non-destructive material pass. Original rigs, UVs, animations and logos remain intact."""
import colorsys, hashlib, io, json, pathlib, struct
import numpy as np
from PIL import Image, ImageFilter
ROOT=pathlib.Path(__file__).resolve().parents[1]
OUT=ROOT/'unity-city/Assets/Source'; P=OUT/'Premium';P.mkdir(exist_ok=True)

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
for i,m in enumerate(g['materials']):m['name']='BX_'+str(i)+('_baked' if m.get('name')=='_baked' else '_original')
image_from(g,b,g['textures'][9]['source']).save(P/'BrickNormal.png')
floor=image_from(g,b,g['textures'][22]['source']).convert('L')
normal(floor.filter(ImageFilter.GaussianBlur(.5)),2.2).save(P/'RubberNormal.png')
rng=np.random.default_rng(7482);N=512;y,x=np.mgrid[:N,:N]
leather=np.clip(128+rng.normal(0,17,(N,N)),0,255).astype('uint8')
normal(Image.fromarray(leather).filter(ImageFilter.GaussianBlur(.6)),1.7).save(P/'LeatherNormal.png')
weave=128+13*np.sin(x*np.pi/2)*np.cos(y*np.pi/2)+rng.normal(0,3,(N,N))
normal(Image.fromarray(np.uint8(np.clip(weave,0,255))),.65).save(P/'CanvasNormal.png')
steel=np.clip(128+rng.normal(0,1,(N,N))+rng.normal(0,7,(1,N)),0,255).astype('uint8')
normal(Image.fromarray(steel),.6).save(P/'SteelNormal.png')
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
report={'originalHeroGeometryChanged':False,'originalHeroRigChanged':False,'originalUVsChanged':False,'originalLogosChanged':False,'sourceTextureSize':list(tex.size),'greenPixelsAdjusted':int(np.count_nonzero(green>.05)),'newMaps':['original brick relief','rubber relief','leather grain','canvas weave','brushed metal','Brax colour and normal']}
(P/'material-review.json').write_text(json.dumps(report,indent=2))

p=ROOT/'unity-city/Assets/BraxGymPlayer.cs';s=p.read_text()
s=s.replace('float a=yaw*Mathf.Deg2Rad;\n        Vector3 direction=new Vector3(Mathf.Cos(a)*move.x-Mathf.Sin(a)*move.y,0,-Mathf.Sin(a)*move.x-Mathf.Cos(a)*move.y);','Vector3 right=Vector3.ProjectOnPlane(view.transform.right,Vector3.up).normalized;\n        Vector3 forward=Vector3.ProjectOnPlane(view.transform.forward,Vector3.up).normalized;\n        Vector3 direction=right*move.x+forward*move.y;')
assert 'Vector3 direction=right*move.x+forward*move.y;' in s
s=s.replace('public float x,y,z,yaw,pitch,speed,handX,handY,handZ;','public float x,y,z,yaw,pitch,speed,handX,handY,handZ,rightX,rightZ,forwardX,forwardZ;')
s=s.replace('hits=trainingHits,frames=Time.frameCount,','hits=trainingHits,frames=Time.frameCount,rightX=view.transform.right.x,rightZ=view.transform.right.z,forwardX=view.transform.forward.x,forwardZ=view.transform.forward.z,')
s=s.replace('vertical=0;speed=0;move=Vector2.zero;actionEnd=0;','vertical=0;speed=0;move=Vector2.zero;yaw=0;pitch=19;distance=5.8f;cameraTarget=transform.position+Vector3.up*1.65f;hitPending=false;actionEnd=0;')
review='''    // Explicit QA view, available only with ?qa=1. Not a replacement gameplay camera.
    public void ReviewView(string text)
    {
        if(!Application.absoluteURL.Contains("qa=1"))return;
        var a=text.Split(',');if(a.Length!=6)return;var v=new float[6];
        for(int i=0;i<6;i++)if(!float.TryParse(a[i],System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out v[i]))return;
        move=Vector2.zero;speed=0;vertical=0;body.enabled=false;transform.position=new Vector3(v[0],v[1],v[2]);body.enabled=true;
        yaw=v[3];pitch=v[4];distance=v[5];cameraTarget=transform.position+Vector3.up*1.65f;UpdateCamera(1);Report();
    }
'''
s=s.replace('    private void OnApplicationFocus',review+'    private void OnApplicationFocus');p.write_text(s)
p=ROOT/'unity-city/Assets/Editor/BraxGymBuild.cs';s=p.read_text()
s=s.replace('name="Matte original "+cache.Count','name=m.name')
s=s.replace('        const string scenePath=', '        BraxGymPremium.Apply(gym,hero,bags.ToArray(),camera,light,fill,Generated);\n        const string scenePath=')
assert 'BraxGymPremium.Apply' in s
p.write_text(s)
print('PREMIUM_SOURCE_MATERIALS_PREPARED',json.dumps(report))
