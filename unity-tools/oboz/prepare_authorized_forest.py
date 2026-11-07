#!/usr/bin/env python3
"""Prepare only the owner's two named Poly Haven trees, then install verified LODs.

Original .blend sources and raw exports stay outside Assets. The scene-only
workflow uses the already pinned Blender, without changing Unity or building an
APK. Optional source failures produce an explicit incomplete catalog and report.
"""
from __future__ import annotations
import argparse
import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import shutil
import subprocess
import sys
import time

REPO=Path(__file__).resolve().parents[2]
ASSET_ROOT='Assets/Source/Prywatne/Oboz/environment/vegetation/'
CATALOG_NAME='authorized-tree-catalog.json'
EXPECTED={
    'pine_tree_01_b':('pine_tree_01',[514980,267343],
        '788d5839aa1d256c20e50adc53ae830b35511d8bb2bc7c16d110dcfc0037651e'),
    'tree_small_02':('tree_small_02',[495533],
        '3c28e4ebded582d6539afeac7e5ccce6cb950830a94f1fe38135a45264145de5'),
}


def digest(path):
    value=hashlib.sha256()
    with Path(path).open('rb') as stream:
        for block in iter(lambda:stream.read(1024*1024),b''):value.update(block)
    return value.hexdigest()


def write_json(path,document):
    path=Path(path);path.parent.mkdir(parents=True,exist_ok=True)
    temporary=path.with_name(path.name+'.writing')
    temporary.write_text(json.dumps(document,ensure_ascii=False,indent=2)+'\n')
    temporary.replace(path)


def invalidate(destination,reason):
    # Preserve generated binaries and every GUID. An old complete catalog must
    # never make a failed current preparation look successful in Unity.
    write_json(destination/CATALOG_NAME,dict(schema=1,complete=False,assetRoot=ASSET_ROOT,
        title='OBÓZ TRENINGOWY — źródła wskazanego lasu nieprzygotowane',
        prototypes=[],warnings=[reason],apkBuilt=False))


def safe_relative(value):
    if not isinstance(value,str) or not value or '\\' in value or ':' in value:
        raise ValueError('Unsafe catalog path')
    path=PurePosixPath(value)
    if path.is_absolute() or '..' in path.parts:raise ValueError('Unsafe catalog path')
    return value


def catalog_files(document):
    if document.get('schema')!=1 or not document.get('complete') or document.get('assetRoot')!=ASSET_ROOT:
        raise ValueError('Incomplete or unrelated forest catalog')
    if document.get('lod0Imported') is not False or document.get('geometryDecimated') is not False:
        raise ValueError('LOD0 or altered source geometry is forbidden')
    prototypes=document.get('prototypes',[])
    if [p.get('name') for p in prototypes]!=list(EXPECTED):
        raise ValueError('Only pine b and small tree may follow the original six firs')
    files=[]
    for prototype in prototypes:
        name=prototype['name'];asset,counts,source_hash=EXPECTED[name]
        if (prototype.get('sourceAsset'),prototype.get('sourceBlendSha256'),prototype.get('license'),
            prototype.get('maxSceneInstances'))!=(asset,source_hash,'CC0-1.0',12):
            raise ValueError('Unexpected authorized source '+name)
        lods=prototype.get('lods',[])
        if len(lods)!=len(counts) or prototype.get('missingAuthoredLod2')!=(len(counts)==1):
            raise ValueError('Authored LOD inventory differs '+name)
        for level,(lod,triangles) in enumerate(zip(lods,counts),1):
            if (lod.get('level'),lod.get('triangles'),lod.get('sourceTriangles'),lod.get('file'))!=(
                level,triangles,triangles,f'{name}_LOD{level}__unity.glb'):
                raise ValueError('Wrong original LOD '+name)
            files.append(dict(lod,kind='model'))
        if prototype.get('nearFile')!=lods[0]['file'] or prototype.get('midFile')!=(lods[1]['file'] if len(lods)>1 else None):
            raise ValueError('LOD references disagree '+name)
        atlas=prototype.get('billboard',{})
        if not atlas.get('sourceBoundsMeasured') or not atlas.get('trunkCenteredSpan') or \
            atlas.get('file')!=name+'_Billboard_Color.png' or atlas.get('model')!=name+f'_LOD{len(lods)}.glb':
            raise ValueError('No measured original billboard '+name)
    textures=document.get('textures',[])
    if not textures:raise ValueError('Missing source maps')
    for texture in textures:
        relative=safe_relative(texture.get('file'))
        if not relative.startswith(('authorized_forest_textures/','billboards/')) or \
            not relative.lower().endswith(('.png','.jpg','.jpeg')) or \
            not 0<texture.get('width',0)<=2048 or not 0<texture.get('height',0)<=2048:
            raise ValueError('Unexpected source map '+relative)
        files.append(dict(texture,kind='billboard' if relative.startswith('billboards/') else 'texture'))
    by_name={entry['file']:entry for entry in files}
    if len(by_name)!=len(files):raise ValueError('Duplicate forest path')
    for prototype in prototypes:
        atlas=prototype['billboard'];entry=by_name.get('billboards/'+atlas['file'])
        if not entry or entry['sha256']!=atlas['sha256']:
            raise ValueError('Billboard texture not in verified map list')
    return files


def installation_plan(catalog_path,models,billboards,destination):
    document=json.loads(Path(catalog_path).read_text());files=catalog_files(document)
    guids=set();plan=[]
    for row in files:
        relative=safe_relative(row['file'])
        source=billboards/Path(relative).name if row['kind']=='billboard' else models/relative
        target=destination/relative
        if not source.is_file() or source.stat().st_size!=row['bytes'] or digest(source)!=row['sha256']:
            raise ValueError('Missing or changed measured source '+relative)
        meta=target.with_name(target.name+'.meta')
        text=meta.read_text() if meta.is_file() else ''
        match=re.search(r'^guid: ([0-9a-f]{32})$',text,re.M)
        if not match or match[1] in guids:raise ValueError('Missing/duplicate committed GUID '+relative)
        guids.add(match[1])
        if row['kind']!='model' and not re.search(r'^\s*textureShape:\s*1\s*$',text,re.M):
            raise ValueError('Source texture metadata is not Texture2D '+relative)
        plan.append((source,target,row))
    meta=destination/(CATALOG_NAME+'.meta')
    match=re.search(r'^guid: ([0-9a-f]{32})$',meta.read_text() if meta.is_file() else '',re.M)
    if not match or match[1] in guids:raise ValueError('Missing/duplicate committed catalog GUID')
    return document,plan


def install(catalog_path,models,billboards,destination,report_path):
    document,plan=installation_plan(catalog_path,models,billboards,destination)
    invalidate(destination,'Verified files are being installed; Unity scene has not run')
    installed=[]
    for source,target,row in plan:
        target.parent.mkdir(parents=True,exist_ok=True)
        if not target.exists() or digest(target)!=row['sha256']:shutil.copyfile(source,target)
        if target.stat().st_size!=row['bytes'] or digest(target)!=row['sha256']:
            raise ValueError('Installed source does not match measured export '+row['file'])
        installed.append(dict(file=row['file'],bytes=row['bytes'],sha256=row['sha256']))
    # Catalog is the final commit marker. Originals/metadata are never altered.
    write_json(destination/CATALOG_NAME,document)
    result=dict(title='OBÓZ TRENINGOWY — instalacja wskazanych autorskich LOD',complete=True,
        files=installed,sourceFileBytes=sum(row['bytes'] for row in installed),
        originalBlendsInstalled=False,lod0Installed=False,geometryDecimated=False,
        originalSixPrototypesChanged=False,unitySceneVerified=False,phoneMeasured=False,apkBuilt=False,
        metric='Source bytes copied; not an APK size, Unity native memory or phone frame rate')
    write_json(report_path,result)
    return result


def prepare(repo,work,reports,blender):
    destination=repo/'unity-city'/ASSET_ROOT
    invalidate(destination,'Current scene preparation not yet complete')
    report=dict(title='OBÓZ TRENINGOWY — przygotowanie źródeł F bez APK',complete=False,
        steps=[],warnings=[],apkBuilt=False,originalsOutsideAssets=True)
    reports.mkdir(parents=True,exist_ok=True);work.mkdir(parents=True,exist_ok=True)
    manifest=repo/'unity-tools/oboz/AUTHORIZED_TREES_20261106.json'
    source=work/'source';models=work/'selected-export';billboards=work/'billboards'
    def run(label,command):
        start=time.monotonic();log=reports/('forest-'+label+'.log')
        with log.open('w') as output:
            completed=subprocess.run([str(arg) for arg in command],cwd=repo,stdout=output,stderr=subprocess.STDOUT)
        report['steps'].append(dict(name=label,returncode=completed.returncode,seconds=round(time.monotonic()-start,3),log=log.name))
        if completed.returncode:raise RuntimeError(label+' failed; see '+log.name)
    def require(path):
        if not path.is_file() or not json.loads(path.read_text()).get('complete'):
            raise RuntimeError(path.name+' is incomplete; original six firs preserved')
    try:
        if any(p.lower()=='assets' for p in work.resolve().parts):raise ValueError('Source work must stay outside Unity Assets')
        run('source',[sys.executable,repo/'unity-tools/oboz/authorized_tree_sources.py','--manifest',manifest,
            '--sources',source,'--report',reports/'forest-source.json']);require(reports/'forest-source.json')
        run('export',[blender,'--background','--factory-startup','--disable-autoexec','--threads','2',
            '--python',repo/'tools/export_oboz_vegetation.py','--','--authorized-manifest',manifest,
            '--sources',source,'--output',models,'--report',reports/'forest-export.json']);require(reports/'forest-export.json')
        run('finish',[sys.executable,repo/'tools/finish_oboz_authorized_trees.py','--sources',source,'--models',models,
            '--manifest',manifest,'--export-report',reports/'forest-export.json','--report',reports/'forest-finished.json'])
        require(reports/'forest-finished.json')
        atlas_reports=[]
        for name,(_,counts,_) in EXPECTED.items():
            atlas_report=reports/('forest-billboard-'+name+'.json');atlas_reports.append(atlas_report)
            run('billboard-'+name,[blender,'--background','--factory-startup','--disable-autoexec','--threads','2',
                '--python',repo/'tools/render_oboz_tree_billboards.py','--','--sources',models,'--output',billboards,
                '--model-pattern',f'{name}_LOD{len(counts)}.glb','--report',atlas_report]);require(atlas_report)
        catalog_path=reports/'forest-catalog.json'
        command=[sys.executable,repo/'tools/catalog_oboz_authorized_trees.py','--manifest',manifest,'--models',models,
            '--finished-report',reports/'forest-finished.json','--billboards',billboards,'--report',catalog_path]
        for path in atlas_reports:command.extend(['--billboard-report',path])
        run('catalog',command);require(catalog_path)
        install(catalog_path,models,billboards,destination,reports/'forest-installation.json')
        report['complete']=True
    except Exception as error:
        reason=str(error);report['warnings'].append(reason)
        invalidate(destination,reason)
        print('::warning::OBÓZ TRENINGOWY — wskazane drzewa nieprzygotowane: '+reason,flush=True)
    write_json(reports/'forest-preparation.json',report)
    return report


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repo',type=Path,default=REPO)
    parser.add_argument('--work',type=Path)
    parser.add_argument('--reports',type=Path)
    parser.add_argument('--blender',type=Path)
    parser.add_argument('--invalidate-only',action='store_true')
    args=parser.parse_args()
    if args.invalidate_only:
        invalidate(args.repo/'unity-city'/ASSET_ROOT,'Scene source preparation has started')
    elif args.work and args.reports and args.blender:
        result=prepare(args.repo.resolve(),args.work.resolve(),args.reports.resolve(),args.blender.resolve())
        print('BRAXHORN_OBOZ_FOREST_PREPARATION complete='+str(result['complete'])+' apk=false')
    else:parser.error('--work, --reports and --blender required unless --invalidate-only')
