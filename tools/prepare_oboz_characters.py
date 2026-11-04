"""Stage original camp character files without changing geometry or texture pixels.

This reports source topology, bone names and PNG dimensions. It does not prove
T-pose, avatar correctness, skinning, retarget quality or runtime performance.
"""
import argparse, array, collections, hashlib, io, json, struct, sys, zipfile, zlib
from pathlib import Path, PurePosixPath
from PIL import Image

ROSTER = [
    ('monika','Monika','Monika.zip','player'),
    ('vorn','VORN','Ochroniaz(1).zip','guard'),
    ('zyro','ZYRO','doberman(1).zip','fighter'),
    ('rakk','RAKK','pitbul.zip','fighter'),
    ('kade','KADE','boxer 3d model.zip','fighter'),
    ('gorr','GORR','gorilla 3d model.zip','fighter'),
    ('mis','Miś','anthropomorphic+bear+gi+3d+model.zip','boss'),
]

def sha256(data): return hashlib.sha256(data).hexdigest()

class Fbx:
    def __init__(self, data):
        if not data.startswith(b'Kaydara FBX Binary  \x00\x1a\x00'):
            raise ValueError('Only binary FBX is supported by this source audit')
        self.data=data; self.version=struct.unpack_from('<I',data,23)[0]
        self.wide=self.version>=7500; self.header=25 if self.wide else 13
        self.bones=[]; self.polygons=collections.Counter(); self.vertices=0
        self.animations=[]
    def scalar(self, pos, node):
        code=chr(self.data[pos]); pos+=1
        formats={'Y':'h','C':'?','I':'i','F':'f','D':'d','L':'q'}
        if code in formats:
            fmt='<'+formats[code];value=struct.unpack_from(fmt,self.data,pos)[0]
            return value,pos+struct.calcsize(fmt)
        if code in 'SR':
            count=struct.unpack_from('<I',self.data,pos)[0];pos+=4
            raw=self.data[pos:pos+count]
            return (raw.decode('utf-8',errors='replace') if code=='S' else None),pos+count
        if code in 'fdilbc':
            count,encoding,size=struct.unpack_from('<III',self.data,pos);pos+=12
            if node=='Vertices': self.vertices+=count//3
            if node=='PolygonVertexIndex':
                if code!='i':raise ValueError('Unexpected polygon array type')
                raw=self.data[pos:pos+size]
                if encoding==1:raw=zlib.decompress(raw)
                elif encoding!=0:raise ValueError('Unknown FBX array encoding')
                if len(raw)!=count*4:raise ValueError('Truncated FBX polygon array')
                indices=array.array('i');indices.frombytes(raw)
                if sys.byteorder!='little':indices.byteswap()
                n=0
                for idx in indices:
                    n+=1
                    if idx<0:self.polygons[n]+=1;n=0
                if n:raise ValueError('Unterminated polygon')
            return {'array_count':count},pos+size
        raise ValueError('Unsupported FBX property type '+repr(code))
    def node(self,pos):
        end,count,plen=struct.unpack_from('<QQQ' if self.wide else '<III',self.data,pos)
        if end==0:return None
        nlen=self.data[pos+self.header-1];p=pos+self.header
        name=self.data[p:p+nlen].decode('utf-8');p+=nlen
        prop_end=p+plen;props=[]
        for _ in range(count):value,p=self.scalar(p,name);props.append(value)
        if p!=prop_end or end>len(self.data) or end<=pos:raise ValueError('Invalid FBX node boundary')
        if name=='Model' and len(props)>=3 and props[2]=='LimbNode':
            self.bones.append(props[1].split('\x00')[0])
        if name=='AnimationStack' and len(props)>1:self.animations.append(props[1].split('\x00')[0])
        while p+self.header<=end:
            nxt=self.node(p)
            if nxt is None:break
            p=nxt
        return end
    def report(self):
        pos=27
        while pos+self.header<len(self.data):
            nxt=self.node(pos)
            if nxt is None:break
            pos=nxt
        return {'fbx_version':self.version,'bone_count':len(self.bones),'bone_names':sorted(self.bones),
                'vertices':self.vertices,'faces_by_size':dict(self.polygons),
                'quads':self.polygons[4],'triangles':self.polygons[3],
                'animation_stacks':self.animations,'t_pose_verified':False,'skinning_verified':False}

def safe_name(name):
    path=PurePosixPath(name)
    if path.is_absolute() or '..' in path.parts or '\\' in name:raise ValueError('Unsafe archive path')
    return path

def stage(archive_root, project_root, output):
    manifest={'schema':1,'worldScale':1.294,'unityVersion':'6000.0.58f2','characters':[],'runtimeVerified':False}
    for ident,label,filename,role in ROSTER:
        found=list(archive_root.rglob(filename))
        if len(found)!=1:raise ValueError('Need exactly one '+filename)
        archive=found[0];original=archive.read_bytes();entry={'id':ident,'name':label,'role':role,'archive':filename,
            'archiveSha256':sha256(original),'modelPath':'','colorPath':'','normalPath':'','roughnessPath':'','metallicPath':'',
            'realHeight':1.68 if ident=='monika' else 0,'hasGloves':False,'glovesVerified':False,'files':[]}
        root=project_root/'Assets/Source/Prywatne/Oboz/characters'/ident
        with zipfile.ZipFile(io.BytesIO(original)) as z:
            if z.testzip() is not None:raise ValueError('Corrupt archive: '+filename)
            for member in z.infolist():
                if member.is_dir():continue
                rel=safe_name(member.filename);data=z.read(member)
                dest=root/str(rel);dest.parent.mkdir(parents=True,exist_ok=True);dest.write_bytes(data)
                path=dest.relative_to(project_root).as_posix();record={'path':path,'sha256':sha256(data),'bytes':len(data)}
                if dest.suffix.lower()=='.fbx':
                    if entry['modelPath']:raise ValueError('Multiple FBX models in '+filename)
                    entry['modelPath']=path;entry['sourceAudit']=Fbx(data).report()
                elif dest.suffix.lower()=='.png':
                    with Image.open(io.BytesIO(data)) as im:record['width'],record['height']=im.size
                    n=dest.name.lower()
                    key='normalPath' if 'normal' in n else 'roughnessPath' if 'rough' in n else 'metallicPath' if 'metal' in n else 'colorPath' if 'color' in n else None
                    if key:entry[key]=path
                if dest.read_bytes()!=data:raise ValueError('Staging changed file bytes')
                entry['files'].append(record)
        issues=[];audit=entry['sourceAudit']
        if audit['bone_count']!=65:issues.append('Expected 65 source bones')
        if not 13500<=audit['quads']<=20500:issues.append('Quads outside 14000–20000 plus 500 tolerance')
        for key in ['colorPath','normalPath','roughnessPath','metallicPath']:
            info=next((x for x in entry['files'] if x['path']==entry[key]),None)
            if not info:issues.append('Missing '+key)
            elif min(info.get('width',0),info.get('height',0))<4096:issues.append(key+' source is below 4K')
        entry['sourceIssues']=issues
        entry['qualityExceptionApproved']=False
        entry['appearanceApproved']=False
        entry['runtimeVerified']=False
        review_path=Path(__file__).resolve().parents[1]/'evidence/character-visual-review.json'
        if review_path.exists():
            review=next((r for r in json.loads(review_path.read_text())['characters'] if r['id']==ident),None)
            model_hash=next(f['sha256'] for f in entry['files'] if f['path']==entry['modelPath'])
            if review and review['sourceModelSha256']==model_hash:
                entry['glovesVerified']=review['glovesVerifiedFromSourceRender'];entry['hasGloves']=review['hasGloves']
                entry['sourceAudit']['t_pose_verified']=review['sourceTpose'];entry['sourceAudit']['restArmAnglesDegrees']=review['restArmAnglesDegrees']
                if ident!='monika':
                    entry['previewHeight']=1.8
                    issues.append('FBX normalized near 1 metre: real NPC height not agreed; 1.80 m review-only baseline')
                if not review['sourceTpose']:issues.append('Source is not T-pose: upper arms about 64 degrees below horizontal')
        manifest['characters'].append(entry)
        print(label,audit['bone_count'],'bones;',audit['quads'],'quads;',issues,flush=True)
    signatures={tuple(x['sourceAudit']['bone_names']) for x in manifest['characters']}
    manifest['sameSourceBoneNames']=len(signatures)==1
    output.parent.mkdir(parents=True,exist_ok=True);output.write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
    return manifest

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--archives',type=Path,required=True);p.add_argument('--project',type=Path,required=True);p.add_argument('--output',type=Path,required=True)
    a=p.parse_args();stage(a.archives,a.project,a.output)
