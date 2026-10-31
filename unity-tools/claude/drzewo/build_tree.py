# Drzewo Verdant w wersji do gry: pień + liście w 3 gęstościach (karty liści losowo przerzedzone
# i powiększone, żeby korona nie robiła się łysa). Jeden GLB, wspólne tekstury.
import json,struct,sys,numpy as np
def load(f):
    d=open(f,'rb').read(); l=struct.unpack('<I',d[12:16])[0]; j=json.loads(d[20:20+l]); b0=20+l+8
    p=j['meshes'][0]['primitives'][0]
    def acc(i,nc):
        a=j['accessors'][i]; bv=j['bufferViews'][a['bufferView']]
        return np.frombuffer(d,dtype=np.float32,count=a['count']*nc,offset=b0+bv.get('byteOffset',0)).reshape(-1,nc).copy()
    at={k:acc(v,{'POSITION':3,'NORMAL':3,'TEXCOORD_0':2,'COLOR_0':j['accessors'][v]['type']=='VEC4' and 4 or 3}[k]) for k,v in p['attributes'].items()}
    imgs=[]
    for im in j['images']:
        bv=j['bufferViews'][im['bufferView']]; imgs.append((d[b0+bv.get('byteOffset',0):b0+bv.get('byteOffset',0)+bv['byteLength']],im['mimeType']))
    return at,j['materials'][p['material']],j.get('textures',[]),imgs,j.get('samplers',[])
def thin(at,keep,scale,seed):
    pos=at['POSITION']; ntri=len(pos)//3
    key=np.round(pos*1e4).astype(np.int64); _,inv=np.unique(key,axis=0,return_inverse=True); inv=inv.reshape(-1,3)
    parent=np.arange(inv.max()+1)
    def find(x):
        r=x
        while parent[r]!=r: r=parent[r]
        while parent[x]!=r: parent[x],x=r,parent[x]
        return r
    for t in inv:
        a=find(t[0]); parent[find(t[1])]=a; parent[find(t[2])]=a
    roots=np.array([find(t[0]) for t in inv])
    ids,cl=np.unique(roots,return_inverse=True)
    rng=np.random.default_rng(seed); chosen=rng.random(len(ids))<keep
    keept=chosen[cl]
    out={k:v.reshape(ntri,3,-1)[keept].reshape(-1,v.shape[1]).copy() for k,v in at.items()}
    p=out['POSITION'].reshape(-1,3,3); c=cl[keept]
    # środek każdej karty i powiększenie wokół niego
    sums=np.zeros((len(ids),3)); cnt=np.zeros(len(ids))
    np.add.at(sums,c,p.mean(1)); np.add.at(cnt,c,1)
    cen=sums[c]/cnt[c][:,None]
    p=cen[:,None,:]+(p-cen[:,None,:])*scale
    out['POSITION']=p.reshape(-1,3).astype(np.float32)
    return out
trunk,tmat,ttex,timg,tsam=load(sys.argv[1]); leaves,lmat,ltex,limg,lsam=load(sys.argv[2]); out=sys.argv[3]
levels=[(float(x.split(':')[0]),float(x.split(':')[1])) for x in sys.argv[4].split(',')]
blob=bytearray(); views=[]; accs=[]; meshes=[]; nodes=[]; images=[]; textures=[]; materials=[]
def view(b,target=None):
    while len(blob)%4: blob.append(0)
    views.append({'buffer':0,'byteOffset':len(blob),'byteLength':len(b),**({'target':target} if target else {})}); blob.extend(b); return len(views)-1
def acc(arr,typ):
    arr=np.ascontiguousarray(arr.astype(np.float32)); a={'bufferView':view(arr.tobytes(),34962),'componentType':5126,'count':len(arr),'type':typ}
    if typ=='VEC3': a['min']=arr.min(0).tolist(); a['max']=arr.max(0).tolist()
    accs.append(a); return len(accs)-1
def addmat(m,texs,imgs):
    m=json.loads(json.dumps(m)); base=len(images)
    for b,mt in imgs: images.append({'bufferView':view(b),'mimeType':mt})
    tb=len(textures)
    for t in texs: textures.append({'source':t['source']+base,**({'sampler':0} if 'sampler' in t else {})})
    def fix(t):
        if t and 'index' in t: t['index']+=tb
    pbr=m.get('pbrMetallicRoughness',{})
    for k in ('baseColorTexture','metallicRoughnessTexture'): fix(pbr.get(k))
    for k in ('normalTexture','occlusionTexture'): fix(m.get(k))
    materials.append(m); return len(materials)-1
def mesh(name,at,mi):
    attrs={k:acc(v,{2:'VEC2',3:'VEC3',4:'VEC4'}[v.shape[1]]) for k,v in at.items()}
    meshes.append({'name':name,'primitives':[{'attributes':attrs,'material':mi}]}); nodes.append({'name':name,'mesh':len(meshes)-1})
tm=addmat(tmat,ttex,timg); lm=addmat(lmat,ltex,limg)
mesh('pien',trunk,tm)
tris=[len(trunk['POSITION'])//3]
for i,(k,s) in enumerate(levels):
    lv=leaves if k>=.999 else thin(leaves,k,s,17+i)
    mesh('liscie_%d'%i,lv,lm); tris.append(len(lv['POSITION'])//3)
g={'asset':{'version':'2.0'},'scene':0,'scenes':[{'nodes':list(range(len(nodes)))}],'nodes':nodes,'meshes':meshes,'materials':materials,'textures':textures,'images':images,'accessors':accs,'bufferViews':views,'buffers':[{'byteLength':len(blob)}],'samplers':lsam or tsam or [{}]}
js=json.dumps(g).encode(); js+=b' '*((4-len(js)%4)%4)
while len(blob)%4: blob.append(0)
o=open(out,'wb'); o.write(struct.pack('<III',0x46546C67,2,12+8+len(js)+8+len(blob))); o.write(struct.pack('<II',len(js),0x4E4F534A)); o.write(js); o.write(struct.pack('<II',len(blob),0x004E4942)); o.write(blob); o.close()
print('OK',out,'tris',tris,'MB',round(len(blob)/1e6,2))
