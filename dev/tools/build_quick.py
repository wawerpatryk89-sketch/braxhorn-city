#!/usr/bin/env python3
# Quick build for a fresh session (no /home/claude/work needed):
#   bundles dev/src -> public/game.js and writes public/index.html from dev/src/template.html.
#   All processed assets already live in public/assets (characters, textures, scans, bake, logos).
#   Usage (repo root):  python3 dev/tools/build_quick.py [--dev]
import os, sys, subprocess, datetime
R = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ESB = os.environ.get('ESBUILD', '/opt/npm-tools/node_modules/esbuild/bin/esbuild')   # preinstalled in the session image
dev = '--dev' in sys.argv
args = [ESB, 'dev/src/main.js', '--bundle', '--format=iife', '--target=es2020', '--alias:three=./dev/three/build/three.module.js',
        '--outfile=public/game.js', '--log-level=warning'] + ([] if dev else ['--minify']) + (['--sourcemap'] if dev else [])
r = subprocess.run(args, cwd=R, capture_output=True, text=True)
if r.returncode: print(r.stderr); sys.exit(1)
tpl = open(f'{R}/dev/src/template.html').read()
head, body = tpl.split('<div id="app">', 1)
html = ('<!doctype html>\n<html lang="pl">\n<head>\n<meta charset="utf-8">\n'
        '<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover, user-scalable=no">\n'
        '<link rel="manifest" href="manifest.webmanifest">\n<link rel="icon" href="icon-192.png">\n<link rel="apple-touch-icon" href="icon-192.png">\n'
        '<meta name="mobile-web-app-capable" content="yes">\n<meta name="apple-mobile-web-app-capable" content="yes">\n'
        '<meta name="apple-mobile-web-app-status-bar-style" content="black-translucent">\n<meta name="apple-mobile-web-app-title" content="Braxhorn">\n'
        + head + '</head>\n<body>\n<div id="app">' + body + '\n</body>\n</html>\n')
stamp = datetime.datetime.now(datetime.timezone.utc) + datetime.timedelta(hours=1)  # Swiss time (winter)
html = html.replace('__BUILD__', stamp.strftime('%d.%m %H:%M')).replace('src="game.js"', 'src="game.js?v=' + stamp.strftime('%m%d%H%M%S') + '"')
open(f'{R}/public/index.html', 'w').write(html)
print('ok', os.path.getsize(f'{R}/public/game.js') // 1024, 'KB', stamp.strftime('%d.%m %H:%M'))
