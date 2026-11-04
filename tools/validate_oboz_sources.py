"""Source integrity and C# grammar, explicitly NOT Unity compilation/Play Mode."""
import hashlib, json, sys
from pathlib import Path

ROOT=Path(__file__).resolve().parent.parent
PROJECT=ROOT/'unity-city'

def run():
    errors=[];checked=0;verified=set()
    manifest=json.loads((ROOT/'evidence/characters.json').read_text())
    expected={'monika','vorn','zyro','rakk','kade','gorr','mis'}
    roster=[c['id'] for c in manifest['characters']]
    if set(roster)!=expected or len(roster)!=7:errors.append('Wrong character roster')
    if manifest['worldScale']!=1.294 or manifest['unityVersion']!='6000.0.58f2':errors.append('Wrong scale/version')
    if next(c for c in manifest['characters'] if c['id']=='monika')['realHeight']!=1.68:errors.append('Wrong Monika height')
    signatures=set()
    def verify(record):
        nonlocal checked
        p=PROJECT/record['path']
        if p in verified:return
        if not p.is_file():errors.append('Missing '+str(p));return
        if hashlib.sha256(p.read_bytes()).hexdigest()!=record['sha256']:errors.append('Hash mismatch '+str(p))
        checked+=1;verified.add(p)
    for c in manifest['characters']:
        bones=c['sourceAudit']['bone_names'];signatures.add(tuple(sorted(bones)))
        if len(bones)!=65 or len(set(bones))!=65:errors.append(c['id']+' bone names')
        for f in c['files']:verify(f)
        if c.get('runtimeVerified'):errors.append('Unsupported runtimeVerified claim')
    if len(signatures)!=1:errors.append('Different source skeleton names')
    for a in manifest['animationSources']:verify(a)
    if len(manifest['animationSources'])!=19:errors.append('Wrong animation source count')
    for m in json.loads((ROOT/'evidence/materials.json').read_text())['materials']:
        for f in m['files']:verify(f)
    for e in json.loads((ROOT/'evidence/environment.json').read_text()):verify(e)
    for f in json.loads((ROOT/'evidence/gym-source.json').read_text())['files']:verify(f)
    glb_reports=json.loads((ROOT/'evidence/glb-mobile-import.json').read_text())
    vegetation=ROOT/'evidence/vegetation-mobile-import.json'
    if vegetation.exists():glb_reports+=json.loads(vegetation.read_text())
    for g in glb_reports:
        sources=list((PROJECT/'Assets/Source/Prywatne/Oboz/environment').rglob(g['source']))
        if len(sources)!=1:errors.append('Missing/ambiguous GLB '+g['source']);continue
        source=sources[0]
        verify({'path':source.relative_to(PROJECT).as_posix(),'sha256':g['sourceSha256']})
        verify({'path':(source.parent/g['derived']).relative_to(PROJECT).as_posix(),'sha256':g['derivedSha256']})
        for f in g['maps']:verify({'path':(source.parent/f['file']).relative_to(PROJECT).as_posix(),'sha256':f['sha256']})
    billboard_report=ROOT/'evidence/vegetation-billboards.json'
    if billboard_report.exists():
        from PIL import Image
        report=json.loads(billboard_report.read_text());folder=PROJECT/'Assets/Source/Prywatne/Oboz/environment/vegetation/billboards'
        if not report['complete'] or len(report['billboards'])!=6:errors.append('Incomplete billboard export')
        for f in report['billboards']:
            p=folder/f['file'];verify({'path':p.relative_to(PROJECT).as_posix(),'sha256':f['sha256']})
            try:
                with Image.open(p) as image:image.load();assert list(image.size)==f['size']
            except Exception as e:errors.append('Invalid billboard image '+str(e))
        if json.loads((folder/'metadata.json').read_text())!=report:errors.append('Stale Unity billboard metadata')
    if (PROJECT/'Assets/Editor/Oboz/CharacterManifest.json').read_bytes()!=(ROOT/'evidence/characters.json').read_bytes():errors.append('Character manifest copies differ')
    syntax={'executed':False,'files':0,'errors':[]}
    try:
        from tree_sitter import Language,Parser
        import tree_sitter_c_sharp
        parser=Parser(Language(tree_sitter_c_sharp.language()))
        files=[PROJECT/'Assets/BraxGymPlayer.cs',*sorted((PROJECT/'Assets/Oboz').glob('*.cs')),*sorted((PROJECT/'Assets/Editor/Oboz').glob('*.cs'))]
        for path in files:
            tree=parser.parse(path.read_bytes())
            def visit(n):
                if n.is_error or n.is_missing:syntax['errors'].append({'file':str(path.relative_to(ROOT)),'line':n.start_point.row+1,'kind':n.type})
                for child in n.children:visit(child)
            visit(tree.root_node)
        syntax.update(executed=True,files=len(files))
        if syntax['errors']:errors.append('C# syntax errors')
    except ImportError:syntax['limitation']='tree-sitter packages not available'
    result={'sourceIntegrityPassed':not errors,'checkedSourceFiles':checked,'errors':errors,'csharpGrammar':syntax,
      'unityCompilationExecuted':False,'sceneGenerationExecuted':False,'playModeExecuted':False,'apkBuilt':False,
      'characterSourceIssues':{c['id']:c['sourceIssues'] for c in manifest['characters'] if c['sourceIssues']},
      'blockers':['Unity 6000.0.58f2 execution unavailable locally','Unapproved Miś source deviations',
        'Fort, bunker architecture and service interiors not reconstructed',
        'Forest LOD assembly, near/far lighting and phone performance not verified in Unity',
        'Original gym copy needs reconciliation with current finishing and mural assets',
        'NPC real heights not agreed; source models normalized near one metre',
        'NPC daily schedule, Polish 3D voices and branding pending',
        'GORR hand correction verified in Blender preview only, not Unity',
        'Camp shader and geometry visual review pending','Previous save-schema migration not recovered']}
    (ROOT/'evidence/validation.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps(result,ensure_ascii=False));return bool(errors)
if __name__=='__main__':sys.exit(run())
