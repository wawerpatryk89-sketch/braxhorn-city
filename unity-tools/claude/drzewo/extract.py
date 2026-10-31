# Wyciąga części drzewa/krzewu Verdant z braxhorn-estate.glb do osobnego GLB (świat wypalony w wierzchołki).
import json,struct,sys,numpy as np
src,out,names=sys.argv[1],sys.argv[2],sys.argv[3].split(',')
d=open(src,'rb').read(); l=struct.unpack('<I',d[12:16])[0]; j=json.loads(d[20:20+l])
binstart=20+l+8
def M(n):
    if 'matrix' in n: return np.array(n['matrix']).reshape(4,4).T
    m=np.eye(4)
    if 'scale' in n: m=np.diag(list(n['scale'])+[1])@m
    if 'rotation' in n:
        x,y,z,w=n['rotation'];R=np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]]);r=np.eye(4);r[:3,:3]=R;m=r@m
    if 'translation' in n: t=np.eye(4);t[:3,3]=n['translation'];m=t@m
    return m
W={}
def walk(i,P):
    n=j['nodes'][i];Wm=P@M(n);W[i]=Wm
    for c in n.get('children',[]): walk(c,Wm)
for r in j['scenes'][j.get('scene',0)]['nodes']: walk(r,np.eye(4))
CT={5120:np.int8,5121:np.uint8,5122:np.int16,5123:np.uint16,5125:np.uint32,5126:np.float32}
NC={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}
def acc(i):
    a=j['accessors'][i]; bv=j['bufferViews'][a['bufferView']]
    off=binstart+bv.get('byteOffset',0)+a.get('byteOffset',0); dt=np.dtype(CT[a['componentType']]); nc=NC[a['type']]
    stride=bv.get('byteStride',0) or dt.itemsize*nc
    raw=np.frombuffer(d,dtype=np.uint8,count=stride*(a['count']-1)+dt.itemsize*nc,offset=off)
    arr=np.lib.stride_tricks.as_strided(raw,shape=(a['count'],dt.itemsize*nc),strides=(stride,1)).copy().view(dt).reshape(a['count'],nc)
    if a.get('normalized'): arr=arr.astype(np.float32)/np.iinfo(dt).max
    return arr.astype(np.float32) if dt!=np.float32 else arr
byname={n.get('name'):i for i,n in enumerate(j['nodes'])}
blob=bytearray(); views=[]; accs=[]; meshes=[]; nodes=[]; mats=[]; texs=[]; imgs=[]; samplers=list(j.get('samplers',[]))
matmap={}; imgmap={}
def addview(b,target=None):
    while len(blob)%4: blob.append(0)
    views.append({'buffer':0,'byteOffset':len(blob),'byteLength':len(b),**({'target':target} if target else {})}); blob.extend(b); return len(views)-1
def addacc(arr,typ,target=34962):
    arr=np.ascontiguousarray(arr.astype(np.float32)); v=addview(arr.tobytes(),target)
    a={'bufferView':v,'componentType':5126,'count':int(arr.shape[0]),'type':typ}
    if typ=='VEC3': a['min']=arr.min(0).tolist(); a['max']=arr.max(0).tolist()
    accs.append(a); return len(accs)-1
def image(ii):
    if ii in imgmap: return imgmap[ii]
    im=j['images'][ii]; bv=j['bufferViews'][im['bufferView']]
    b=d[binstart+bv.get('byteOffset',0):binstart+bv.get('byteOffset',0)+bv['byteLength']]
    imgs.append({'bufferView':addview(b),'mimeType':im['mimeType']}); imgmap[ii]=len(imgs)-1; return imgmap[ii]
def material(mi):
    if mi in matmap: return matmap[mi]
    m=json.loads(json.dumps(j['materials'][mi]))
    def fix(t):
        if t and 'index' in t:
            tex=j['textures'][t['index']]; texs.append({'source':image(tex['source']),**({'sampler':tex['sampler']} if 'sampler' in tex else {})}); t['index']=len(texs)-1
    pbr=m.get('pbrMetallicRoughness',{})
    for k in ('baseColorTexture','metallicRoughnessTexture'): fix(pbr.get(k))
    for k in ('normalTexture','occlusionTexture','emissiveTexture'): fix(m.get(k))
    m.pop('extensions',None); mats.append(m); matmap[mi]=len(mats)-1; return matmap[mi]
for nm in names:
    ni=byname[nm]; n=j['nodes'][ni]; Wm=W[ni]; R=Wm[:3,:3]; Ninv=np.linalg.inv(R).T
    prims=[]
    for p in j['meshes'][n['mesh']]['primitives']:
        attrs={}
        pos=acc(p['attributes']['POSITION']); pos=(np.c_[pos,np.ones(len(pos))]@Wm.T)[:,:3]; attrs['POSITION']=addacc(pos,'VEC3')
        if 'NORMAL' in p['attributes']:
            nr=acc(p['attributes']['NORMAL'])@Ninv.T; nr/=np.maximum(np.linalg.norm(nr,axis=1,keepdims=True),1e-9); attrs['NORMAL']=addacc(nr,'VEC3')
        if 'TEXCOORD_0' in p['attributes']: attrs['TEXCOORD_0']=addacc(acc(p['attributes']['TEXCOORD_0']),'VEC2')
        if 'COLOR_0' in p['attributes']:
            c=acc(p['attributes']['COLOR_0']); attrs['COLOR_0']=addacc(c,'VEC4' if c.shape[1]==4 else 'VEC3')
        prim={'attributes':attrs,'material':material(p['material'])}
        if 'indices' in p:
            ix=acc(p['indices']).astype(np.uint32).reshape(-1); v=addview(ix.tobytes(),34963); accs.append({'bufferView':v,'componentType':5125,'count':int(ix.size),'type':'SCALAR'}); prim['indices']=len(accs)-1
        prims.append(prim)
    meshes.append({'name':nm,'primitives':prims}); nodes.append({'name':nm,'mesh':len(meshes)-1})
g={'asset':{'version':'2.0','generator':'braxhorn extract'},'scene':0,'scenes':[{'nodes':list(range(len(nodes)))}],'nodes':nodes,'meshes':meshes,'materials':mats,'textures':texs,'images':imgs,'accessors':accs,'bufferViews':views,'buffers':[{'byteLength':len(blob)}]}
if samplers: g['samplers']=samplers
js=json.dumps(g).encode(); js+=b' '*((4-len(js)%4)%4)
while len(blob)%4: blob.append(0)
o=open(out,'wb'); o.write(struct.pack('<III',0x46546C67,2,12+8+len(js)+8+len(blob))); o.write(struct.pack('<II',len(js),0x4E4F534A)); o.write(js); o.write(struct.pack('<II',len(blob),0x004E4942)); o.write(blob); o.close()
print('OK',out,len(blob))
