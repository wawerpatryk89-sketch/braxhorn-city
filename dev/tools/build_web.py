#!/usr/bin/env python3
# Build the hosted version of BRAXHORN CITY into dist/ (static files for Vercel).
import json, os, shutil, subprocess, sys
from PIL import Image

W = '/home/claude/work'
D = f'{W}/dist'
os.makedirs(D, exist_ok=True)
minify = '--minify' if '--dev' not in sys.argv else '--sourcemap'
r = subprocess.run(['/opt/npm-tools/node_modules/esbuild/bin/esbuild', 'src/main.js', '--bundle', '--format=iife', minify, '--target=es2020',
                    '--alias:three=./three/build/three.module.js', f'--outfile={D}/game.js', '--log-level=warning'], cwd=W, capture_output=True, text=True)
if r.returncode: print(r.stderr); sys.exit(1)
if r.stderr.strip(): print(r.stderr[:3000])

tpl = open(f'{W}/src/template.html').read()
head, body = tpl.split('<div id="app">', 1)
html = ('<!doctype html>\n<html lang="pl">\n<head>\n<meta charset="utf-8">\n'
        '<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover, user-scalable=no">\n'
        '<link rel="manifest" href="manifest.webmanifest">\n<link rel="icon" href="icon-192.png">\n<link rel="apple-touch-icon" href="icon-192.png">\n'
        '<meta name="mobile-web-app-capable" content="yes">\n<meta name="apple-mobile-web-app-capable" content="yes">\n'
        '<meta name="apple-mobile-web-app-status-bar-style" content="black-translucent">\n<meta name="apple-mobile-web-app-title" content="Braxhorn">\n'
        + head + '</head>\n<body>\n<div id="app">' + body + '\n</body>\n</html>\n')
open(f'{D}/index.html', 'w').write(html)

AS = f'{D}/assets'
for sub in ('basis', 'chars', 'tex', 'logo'): os.makedirs(f'{AS}/{sub}', exist_ok=True)
for f in ('basis_transcoder.js', 'basis_transcoder.wasm'): shutil.copy(f'{W}/three/examples/jsm/libs/basis/{f}', f'{AS}/basis/{f}')
CH = ['brax', 'nyxa', 'tiger', 'tiger2', 'fant', 'musc', 'mino', 'zombie', 'demon']
for k in CH:
    shutil.copy(f'{W}/build2/{k}.glb.gz', f'{AS}/chars/{k}.glb.gz')
    for t in ('color', 'normal', 'orm'): shutil.copy(f'{W}/tex/{k}_{t}.webp', f'{AS}/tex/{k}_{t}.webp')
meta = json.load(open(f'{W}/build2/clips.json'))
keep = ('rm', 'traj', 'start', 'duration', 'stride', 'phase')
out = {k: ({kk: v[kk] for kk in keep if kk in v} if not k.startswith('__') else v) for k, v in meta.items()}
json.dump(out, open(f'{AS}/chars/clips.json', 'w'), separators=(',', ':'))
for f in os.listdir(f'{W}/assets/src'):
    if f.endswith('.glb') and not f.startswith('test'): shutil.copy(f'{W}/assets/src/{f}', f'{AS}/{f}')
for f in ('wide', 'badge', 'poster', 'mark'):
    src = f'{W}/logos/{f}_t.webp'
    if f == 'poster': Image.open(f'{W}/logos/poster.png').convert('RGB').resize((720, int(720 * 2556 / 1179)), Image.LANCZOS).save(f'{AS}/logo/poster.webp', quality=86)
    else: shutil.copy(src, f'{AS}/logo/{f}.webp')
badge = Image.open(f'{W}/logos/badge.png').convert('RGB')
badge.resize((192, 192), Image.LANCZOS).save(f'{D}/icon-192.png')
badge.resize((512, 512), Image.LANCZOS).save(f'{D}/icon-512.png')
json.dump({
    'name': 'Braxhorn City', 'short_name': 'Braxhorn', 'start_url': './', 'scope': './', 'display': 'fullscreen', 'orientation': 'landscape',
    'background_color': '#050605', 'theme_color': '#050605',
    'icons': [{'src': 'icon-192.png', 'sizes': '192x192', 'type': 'image/png'}, {'src': 'icon-512.png', 'sizes': '512x512', 'type': 'image/png', 'purpose': 'any maskable'}],
}, open(f'{D}/manifest.webmanifest', 'w'), indent=1)
json.dump({'cleanUrls': True, 'headers': [
    {'source': '/assets/(.*)', 'headers': [{'key': 'Cache-Control', 'value': 'public, max-age=86400'}]},
    {'source': '/(.*)\\.wasm', 'headers': [{'key': 'Content-Type', 'value': 'application/wasm'}]},
]}, open(f'{D}/vercel.json', 'w'), indent=1)
tot = 0
for root, _, files in os.walk(D):
    for f in files: tot += os.path.getsize(os.path.join(root, f))
print('dist MB', round(tot / 1048576, 1), 'game.js KB', os.path.getsize(f'{D}/game.js') // 1024)
