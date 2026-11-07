"""Verify exported source trees and source-rendered atlases before installation.

The report names source-file bytes, geometry payload and estimated ASTC mip
payload separately. None is an APK size or a measured Unity/phone memory value.
"""
from __future__ import annotations
import argparse
import hashlib
import json
import math
from pathlib import Path
import struct


def digest(path):
    result=hashlib.sha256()
    with path.open('rb') as stream:
        for chunk in iter(lambda:stream.read(1024*1024),b''):result.update(chunk)
    return result.hexdigest()


def geometry(path):
    with path.open('rb') as stream:
        header=stream.read(20)
        if header[:4]!=b'glTF' or header[16:20]!=b'JSON':raise ValueError('Not a GLB '+str(path))
        document=json.loads(stream.read(struct.unpack_from('<I',header,12)[0]))
    views=set();positions=set();triangles=0
    for mesh in document.get('meshes',[]):
        for primitive in mesh['primitives']:
            if primitive.get('mode',4)!=4:raise ValueError('Unexpected source primitive')
            positions.add(primitive['attributes']['POSITION'])
            triangles+=document['accessors'][primitive['indices']]['count']//3
            for index in list(primitive['attributes'].values())+[primitive['indices']]:
                views.add(document['accessors'][index]['bufferView'])
    return dict(triangles=triangles,vertices=sum(document['accessors'][i]['count'] for i in positions),
                gltfGeometryBufferViewBytes=sum(document['bufferViews'][i]['byteLength'] for i in views),
                meshCount=len(document.get('meshes',[])))


def texture(path):
    from PIL import Image
    with Image.open(path) as image:width,height=image.size
    if max(width,height)>2048:raise ValueError('Finished forest source image exceeds 2K '+str(path))
    payload={}
    for block in (4,6):
        w,h=width,height;total=0
        while True:
            total+=math.ceil(w/block)*math.ceil(h/block)*16
            if w==1 and h==1:break
            w=max(1,w//2);h=max(1,h//2)
        payload['astc'+str(block)+'x'+str(block)+'FullMipPayloadBytes']=total
    return dict(width=width,height=height,bytes=path.stat().st_size,sha256=digest(path),**payload)


def catalog(manifest_path,models,finished_path,billboards,billboard_reports,report_path):
    manifest=json.loads(manifest_path.read_text());finished=json.loads(finished_path.read_text())
    if not finished.get('complete'):raise ValueError('Finished authored LOD report is incomplete')
    atlases={}
    for path in billboard_reports:
        report=json.loads(path.read_text())
        if not report.get('complete'):raise ValueError('Atlas report incomplete '+str(path))
        for row in report['billboards']:
            if row['model'] in atlases:raise ValueError('Duplicate source billboard')
            atlases[row['model']]=row
    result=dict(title='OBÓZ TRENINGOWY — zweryfikowany katalog sosny b i małego drzewa',schema=1,
        assetRoot='Assets/Source/Prywatne/Oboz/environment/vegetation/',
        unityImported=False,unitySceneVerified=False,phoneMemoryMeasured=False,apkBuilt=False,
        originalSourcesOutsideAssets=True,lod0Imported=False,geometryDecimated=False,
        requiresPreservingOldSixPrototypeIndices=True,prototypes=[],textures=[],knownLimitations=[],warnings=[])
    unique_textures={}
    for asset in manifest['assets']:
        for name in asset['selectedPrototypes']:
            rows=[r for r in finished['models'] if r['sourceAsset']==asset['id'] and r['prototype']==name and r['installable']]
            if {r['authoredLod'] for r in rows}!=set(asset['requiredLodLevels']):
                raise ValueError('Required authored source LOD missing '+name)
            rows.sort(key=lambda r:r['authoredLod'])
            item=dict(name=name,sourceAsset=asset['id'],sourceUrl=asset['sourceUrl'],license=asset['license'],
                sourceBlendSha256=rows[0]['sourceBlendSha256'],maxSceneInstances=asset['maxSceneInstances'],
                geometryDistanceMetres=asset['geometryDistanceMetres'],billboardCullDistanceMetres=300,
                referenceVerticalFov=60,referenceLodBias=1,lods=[],
                missingAuthoredLod2=2 not in asset['requiredLodLevels'],knownLimitations=asset.get('knownLimitations',[]))
            result['knownLimitations'].extend(item['knownLimitations'])
            for row in rows:
                mobile=row['mobile'];path=models/mobile['derived']
                if digest(path)!=mobile['derivedSha256']:raise ValueError('Derived GLB hash mismatch '+str(path))
                measured=geometry(path)
                if measured['triangles']!=row['triangles']:raise ValueError('Derived source mesh changed '+str(path))
                item['lods'].append(dict(level=row['authoredLod'],file=path.name,bytes=path.stat().st_size,
                    sha256=mobile['derivedSha256'],sourceObject=row['sourceObject'],sourceTriangles=row['triangles'],
                    **measured,sourceAlphaCoverageRestored=row.get('sourceAlphaCoverageRestored',[])))
                for image in mobile['maps']:
                    path=models/image['file'];measured=texture(path)
                    if measured['sha256']!=image['sha256']:raise ValueError('Original source map changed '+str(path))
                    unique_textures[image['file']]=dict(file=image['file'],color=image['color'],**measured)
            last=rows[-1];atlas=atlases.get(last['file'])
            if not atlas or not atlas.get('sourceBoundsMeasured') or atlas['sourceSha256']!=last['colorRepairedSha256']:
                raise ValueError('Atlas does not belong to the exact finished source LOD '+name)
            atlas_path=billboards/atlas['file'];measured=texture(atlas_path)
            if measured['sha256']!=atlas['sha256']:raise ValueError('Billboard hash mismatch '+name)
            for view in atlas['viewAlphaBoundsPixels']:
                box=view['pixels']
                if min(box[0],box[1])<=1 or max(box[2],box[3])>=511:
                    raise ValueError('Actual source silhouette clipped by billboard '+name)
            item['nearFile']=item['lods'][0]['file'];item['midFile']=item['lods'][1]['file'] if len(item['lods'])>1 else None
            item['billboard']=dict(atlas,relativeFile='billboards/'+atlas['file'],bytes=measured['bytes'])
            unique_textures['billboards/'+atlas['file']]=dict(file='billboards/'+atlas['file'],color=True,**measured)
            result['prototypes'].append(item)
    result['textures']=list(unique_textures.values())
    lods=[lod for p in result['prototypes'] for lod in p['lods']]
    result['sourceInstallFileBytes']=sum(x['bytes'] for x in lods)+sum(x['bytes'] for x in result['textures'])
    result['gltfGeometryBufferViewBytes']=sum(x['gltfGeometryBufferViewBytes'] for x in lods)
    result['textureAstc6x6FullMipPayloadBytesEstimate']=sum(x['astc6x6FullMipPayloadBytes'] for x in result['textures'])
    result['textureAstc4x4FullMipPayloadBytesEstimate']=sum(x['astc4x4FullMipPayloadBytes'] for x in result['textures'])
    result['memoryMetricNote']='GLB source files contain retained source image BIN regions. Geometry view bytes and computed ASTC mip payload are separate source-derived measurements. Unity vertex layouts, native overhead, streaming residency, APK compressed contribution and phone FPS remain unmeasured.'
    result['complete']=True
    report_path.parent.mkdir(parents=True,exist_ok=True);report_path.write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
    return result


if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--manifest',type=Path,required=True)
    p.add_argument('--models',type=Path,required=True);p.add_argument('--finished-report',type=Path,required=True)
    p.add_argument('--billboards',type=Path,required=True);p.add_argument('--billboard-report',type=Path,action='append',required=True)
    p.add_argument('--report',type=Path,required=True);a=p.parse_args()
    try:catalog(a.manifest,a.models,a.finished_report,a.billboards,a.billboard_report,a.report)
    except Exception as error:
        issue='OBÓZ TRENINGOWY — katalog drzew: '+str(error);print('::warning::'+issue)
        a.report.parent.mkdir(parents=True,exist_ok=True);a.report.write_text(json.dumps(dict(title=issue,complete=False,warnings=[str(error)],prototypes=[],apkBuilt=False),ensure_ascii=False,indent=2)+'\n')
