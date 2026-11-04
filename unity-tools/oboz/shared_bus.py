"""Restore only the existing bus and its dependencies from the owner's encrypted pack.

No other zone is changed. Git blob is immutable and verified before decryption.
The existing BRAXHORN_KEYSTORE_PASS binding is used only by openssl in CI.
"""
import base64
import hashlib
import json
import os
from pathlib import Path
import struct
import subprocess
import tarfile
import tempfile
from urllib.parse import unquote, urlparse
from assets import OPENSSL, safe, digest

SOURCE_COMMIT = '5ea83909a1aa58b74fb9fa54cbe23a5087a3e087'
BLOB = 'a4120490b834083e77a06ef5d7128b855bed6c3b'
SIZE = 6587504
DESTINATION = Path('unity-city/Assets/Source/Prywatne/Oboz/environment/vehicles')


def main():
    if not os.environ.get('KLUCZ'): raise ValueError('Missing CI encryption binding')
    with tempfile.TemporaryDirectory(prefix='camp-shared-bus-') as temporary:
        temp = Path(temporary)
        response = subprocess.run(['gh','api','repos/'+os.environ['GITHUB_REPOSITORY']+'/git/blobs/'+BLOB],
                                  check=True,capture_output=True,text=True)
        data = json.loads(response.stdout)
        if data['encoding']!='base64' or data['sha']!=BLOB: raise ValueError('Unexpected bus blob response')
        raw = base64.b64decode(data['content'])
        if len(raw)!=SIZE or hashlib.sha1(('blob '+str(len(raw))+'\0').encode()+raw).hexdigest()!=BLOB:
            raise ValueError('Encrypted bus package mismatch')
        encrypted=temp/'vehicles.enc'; encrypted.write_bytes(raw)
        plain=temp/'vehicles.tar.gz'
        subprocess.run(OPENSSL+['-d','-in',str(encrypted),'-out',str(plain),'-pass','env:KLUCZ'],
                       check=True,stdout=subprocess.DEVNULL,stderr=subprocess.PIPE)
        with tarfile.open(plain,'r:gz') as archive:
            members={m.name:m for m in archive.getmembers() if m.isfile()}
            model=members['autobus.glb']; content=archive.extractfile(model).read()
            magic,version,total,size,kind=struct.unpack('<4sIIII',content[:20])
            if magic!=b'glTF' or version!=2 or total!=len(content) or kind!=0x4e4f534a:
                raise ValueError('Invalid original bus GLB')
            gltf=json.loads(content[20:20+size])
            names={'autobus.glb'}
            for source in gltf.get('images',[])+gltf.get('buffers',[]):
                uri=source.get('uri','')
                if not uri or uri.startswith('data:'): continue
                if urlparse(uri).scheme or uri.startswith('//'): raise ValueError('Unexpected remote bus dependency')
                name=unquote(uri); safe(DESTINATION,name); names.add(name)
            metadata=gltf.get('asset',{}).get('extras',{})
            if not metadata.get('author') or not metadata.get('source') or not metadata.get('license'):
                raise ValueError('Original bus attribution is missing')
            records=[]
            for name in sorted(names):
                member=members[name]
                if not member.isfile() or member.size>100*1024*1024: raise ValueError('Invalid bus dependency')
                target=safe(DESTINATION,name); target.parent.mkdir(parents=True,exist_ok=True)
                payload=archive.extractfile(member).read()
                target.write_bytes(payload)
                records.append({'path':name,'bytes':len(payload),'sha256':digest(target)})
            credits='Autobus obozu — ten sam model co osiedle i miasto.\n'+ '\n'.join(str(metadata[k]) for k in ('author','source','license'))
            credits+='\nObóz: statyczna kopia, skala 12 m × 1.294, bez zmiany geometrii lub tekstur.\n'
            (DESTINATION/'CREDITS.md').write_text(credits)
            out=Path('unity-city/Assets/Claude/Resources');out.mkdir(parents=True,exist_ok=True)
            credit_file=out/'braxhorn_autorzy.txt'
            existing=credit_file.read_text() if credit_file.exists() else 'BRAXHORN CITY — AUTORZY\nTekstury obozu: Poly Haven, ambientCG — CC0.\n'
            if credits not in existing: credit_file.write_text(existing+'\n'+credits)
            report={'sourceCommit':SOURCE_COMMIT,'encryptedGitBlob':BLOB,'files':records,'attribution':metadata,'parkedOnly':True}
            (DESTINATION/'bus-source-receipt.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print('CAMP_SHARED_BUS source_verified=true files='+str(len(records))+' parked_only=true')


if __name__=='__main__': main()
