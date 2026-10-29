"""Keep every mesh/UV/texture pixel; externalize images for native mobile GPU compression."""
import collections,hashlib,json,pathlib,struct
from PIL import Image
ROOT=pathlib.Path(__file__).resolve().parents[2];S=ROOT/'unity-city/Assets/Source/City'
images=S/'Images';images.mkdir(parents=True,exist_ok=True)
reports=[]
for area in ['city','estate']:
    path=S/f'braxhorn-{area}.glb';raw=path.read_bytes();assert raw[:4]==b'glTF'
    n=struct.unpack_from('<I',raw,12)[0];g=json.loads(raw[20:20+n]);data=memoryview(raw)[28+n:]
    image_views={i['bufferView'] for i in g.get('images',[])}
    color=set()
    for m in g['materials']:
        p=m.get('pbrMetallicRoughness',{})
        for slot in [p.get('baseColorTexture'),m.get('emissiveTexture')]:
            if slot:color.add(g['textures'][slot['index']]['source'])
    image_records=[]
    for index,i in enumerate(g.get('images',[])):
        view=g['bufferViews'][i['bufferView']];start=view.get('byteOffset',0);content=data[start:start+view['byteLength']].tobytes()
        digest=hashlib.sha256(content).hexdigest();ext='png' if i.get('mimeType')=='image/png' else 'jpg'
        filename=('s_' if index in color else 'l_')+digest+'.'+ext;dest=images/filename
        if dest.exists():assert dest.read_bytes()==content
        else:dest.write_bytes(content)
        with Image.open(dest) as image:width,height=image.size
        g['images'][index]={'uri':'Images/'+filename}
        image_records.append({'uri':filename,'sha256':digest,'width':width,'height':height,'srgb':index in color})
    views=[];remap={};packed=bytearray();fingerprint=hashlib.sha256()
    for index,v in enumerate(g['bufferViews']):
        if index in image_views:continue
        while len(packed)%4:packed.append(0)
        start=v.get('byteOffset',0);content=data[start:start+v['byteLength']];fingerprint.update(content)
        new=dict(v);new['byteOffset']=len(packed);new['buffer']=0;packed.extend(content);remap[index]=len(views);views.append(new)
    def references(o):
        if isinstance(o,dict):
            for k,v in o.items():
                if k=='bufferView':assert v in remap;o[k]=remap[v]
                else:references(v)
        elif isinstance(o,list):
            for v in o:references(v)
    g.pop('bufferViews');references(g);g['bufferViews']=views
    binname=f'braxhorn-{area}.bin';g['buffers']=[{'uri':binname,'byteLength':len(packed)}]
    (S/binname).write_bytes(packed);(S/f'braxhorn-{area}.gltf').write_text(json.dumps(g,separators=(',',':')))
    verify=hashlib.sha256()
    for v in views:verify.update(packed[v['byteOffset']:v['byteOffset']+v['byteLength']])
    assert verify.hexdigest()==fingerprint.hexdigest()
    report={'area':area,'originalExportSha256':hashlib.sha256(raw).hexdigest(),'geometryByteHash':fingerprint.hexdigest(),'geometryUnchanged':True,'meshCount':len(g['meshes']),'imageCount':len(image_records),'uniqueImages':len(set(i['uri'] for i in image_records)),'imagePixelsChanged':False,'resolutionChanged':False,'geometryBytes':len(packed),'textures':image_records}
    reports.append(report);path.unlink()
    print('SOURCE_GEOMETRY_AND_IMAGE_PIXELS_PRESERVED',area,report['meshCount'],report['uniqueImages'],len(packed))
(S/'native-texture-preparation.json').write_text(json.dumps(reports,indent=2))
