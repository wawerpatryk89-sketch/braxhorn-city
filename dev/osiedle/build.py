#!/usr/bin/env python3
"""Build only the isolated neighbourhood. Never rewrites public/game.js or index.html."""
import os, pathlib, shutil, subprocess, hashlib
root=pathlib.Path(__file__).resolve().parents[2]
out=root/'public/osiedle';out.mkdir(parents=True,exist_ok=True)
esbuild=os.environ.get('ESBUILD')
cmd=[esbuild] if esbuild else ['npm','exec','--yes','--package=esbuild@0.28.2','--','esbuild']
subprocess.run(cmd+['dev/osiedle/main.js','--bundle','--format=esm','--target=es2020','--alias:three=./dev/three/build/three.module.js','--outfile=public/osiedle/game.js','--minify'],cwd=root,check=True)
# Use the game's actual HUD and touch controls for this playable district.
version=hashlib.sha256((out/'game.js').read_bytes()).hexdigest()[:10]
index=out/'index.html'
template=(root/'dev/src/template.html').read_text()
head,body=template.split('<div id="app">',1)
html='<!doctype html><html lang="pl"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1,viewport-fit=cover,user-scalable=no">'+head+'</head><body><div id="app">'+body+'</body></html>'
html=html.replace('src="assets/', 'src="../assets/').replace('src="game.js"', f'type="module" src="./game.js?v={version}"').replace('__BUILD__', 'Osiedle · '+version)
index.write_text(html)
(out/'assets').mkdir(exist_ok=True)
for name in ['dom_suburban','dom_nowoczesny','willa_nowoczesna','dom_z_wnetrzem','garaze_sowieckie','dom_wnetrze_zewn']:
 src=root/f'dev/models/{name}.glb'; dest=out/f'assets/{name}.glb'
 if not dest.exists() or not os.path.samefile(src,dest): shutil.copyfile(src,dest)
print('Neighbourhood ready: /osiedle/ — city build unchanged.')

# Original GLBs stay in dev; the browser receives optimized texture copies.
import sys, json
sys.path.insert(0,str(root/'dev/osiedle/tools'))
from pack_textures import pack
manifest_path=out/'texture-budget.json'
old={v['file']:v for v in json.loads(manifest_path.read_text())} if manifest_path.exists() else {}
records=[]
for src in sorted((root/'dev/osiedle/assets').glob('*.glb')):
 dest=out/'assets'/src.name
 digest=hashlib.sha256(src.read_bytes()).hexdigest()
 if src.name in old and old[src.name].get('sha256')==digest and dest.exists():record=old[src.name]
 else:record=pack(src,dest,1024);record['sha256']=digest
 records.append(record)
manifest_path.write_text(json.dumps(records,indent=2)+'\n')
