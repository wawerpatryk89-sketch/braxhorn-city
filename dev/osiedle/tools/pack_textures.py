"""Generate runtime GLBs with 1K textures; preserve mesh data and source assets."""
import io,json,pathlib,struct,sys
from PIL import Image

def pack(src,dest,limit=1024):
 raw=src.read_bytes();n,typ=struct.unpack_from('<II',raw,12);doc=json.loads(raw[20:20+n]);bstart=20+n+8;blob=raw[bstart:];views=doc.get('bufferViews',[])
 images={im['bufferView']:im for im in doc.get('images',[]) if 'bufferView' in im};out=bytearray();changed=0
 for i,view in enumerate(views):
  chunk=blob[view.get('byteOffset',0):view.get('byteOffset',0)+view['byteLength']]
  if i in images:
   im=Image.open(io.BytesIO(chunk));im.load()
   if max(im.size)>limit:
    im.thumbnail((limit,limit),Image.Resampling.LANCZOS);buf=io.BytesIO()
    if im.mode in ['RGBA','LA'] or 'transparency' in im.info:
     im.convert('RGBA').save(buf,format='PNG',compress_level=6);mime='image/png'
    else:im.convert('RGB').save(buf,format='JPEG',quality=90,subsampling=0);mime='image/jpeg'
    chunk=buf.getvalue();images[i]['mimeType']=mime;changed+=1
  while len(out)%4:out.append(0)
  view['byteOffset']=len(out);view['byteLength']=len(chunk);out.extend(chunk)
 for m in doc.get('materials',[]):
  if src.name.startswith('park_') and m.get('alphaMode')=='BLEND':m['alphaMode']='MASK';m['alphaCutoff']=.42;m['doubleSided']=True
 while len(out)%4:out.append(0)
 doc['buffers'][0]['byteLength']=len(out)
 j=json.dumps(doc,separators=(',',':'),ensure_ascii=True).encode();j+=b' '*((-len(j))%4)
 result=struct.pack('<III',0x46546c67,2,28+len(j)+len(out))+struct.pack('<II',len(j),0x4e4f534a)+j+struct.pack('<II',len(out),0x004e4942)+out
 tmp=dest.with_suffix('.tmp');tmp.write_bytes(result);tmp.replace(dest)
 print(src.name,len(raw),'->',len(result),'textures resized',changed,flush=True)
 return {'file':src.name,'sourceBytes':len(raw),'runtimeBytes':len(result),'textureLimit':limit,'resizedTextures':changed}
if __name__=='__main__':pack(pathlib.Path(sys.argv[1]),pathlib.Path(sys.argv[2]),int(sys.argv[3]) if len(sys.argv)>3 else 1024)
