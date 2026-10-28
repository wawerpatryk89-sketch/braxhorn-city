import base64, subprocess, sys, os, json
W = '/home/claude/work'
r = subprocess.run(['/opt/npm-tools/node_modules/esbuild/bin/esbuild', 'src/main.js', '--bundle', '--format=iife', '--minify', '--target=es2020',
                    '--alias:three=./three/build/three.module.js', '--outfile=build/bundle.js', '--log-level=warning'], cwd=W, capture_output=True, text=True)
if r.returncode: print(r.stderr); sys.exit(1)
if r.stderr.strip(): print(r.stderr[:2000])
bundle = open(f'{W}/build/bundle.js').read().replace('</script', '<\\/script')
b64 = lambda p: base64.b64encode(open(p, 'rb').read()).decode()
meta = json.load(open(f'{W}/build/clips.json'))
meta = {k: {kk: v[kk] for kk in ('rm', 'traj', 'start', 'duration') if kk in v} for k, v in meta.items()}
data = [
  ('d-mino', 'text/plain', b64(f'{W}/build/mino.glb.gz')),
  ('d-brax', 'text/plain', b64(f'{W}/build/brax.glb.gz')),
  ('t-mino-c', 'text/plain', b64(f'{W}/tex/mino_color.webp')), ('t-mino-n', 'text/plain', b64(f'{W}/tex/mino_normal.webp')), ('t-mino-o', 'text/plain', b64(f'{W}/tex/mino_orm.webp')),
  ('t-brax-c', 'text/plain', b64(f'{W}/tex/brax_color.webp')), ('t-brax-n', 'text/plain', b64(f'{W}/tex/brax_normal.webp')), ('t-brax-o', 'text/plain', b64(f'{W}/tex/brax_orm.webp')),
  ('d-meta', 'application/json', json.dumps(meta, separators=(',', ':'))),
]
dh = '\n'.join(f'<script type="{t}" id="{i}">{c}</script>' for i, t, c in data)
tpl = open(f'{W}/src/template.html').read()
page = tpl.replace('{{DATA}}', dh).replace('{{BUNDLE}}', bundle)
os.makedirs(f'{W}/out/multi', exist_ok=True)
import gzip, shutil
# shared build: page + separate plain files
open(f'{W}/out/multi/game.js', 'w').write(open(f'{W}/build/bundle.js').read())
import struct
for k in ('mino', 'brax'):
  glb = gzip.decompress(open(f'{W}/build/{k}.glb.gz', 'rb').read())
  off = 12; js = None; binc = None
  while off < len(glb):
    ln, typ = struct.unpack_from('<II', glb, off); chunk = glb[off + 8: off + 8 + ln]; off += 8 + ln
    if typ == 0x4E4F534A: js = json.loads(chunk.decode('utf8'))
    elif typ == 0x004E4942: binc = chunk
  js['buffers'][0]['uri'] = 'data:application/octet-stream;base64,' + base64.b64encode(binc).decode()
  open(f'{W}/out/multi/{k}.gltf.json', 'w').write(json.dumps(js, separators=(',', ':')))
  if os.path.exists(f'{W}/out/multi/{k}.glb'): os.remove(f'{W}/out/multi/{k}.glb')
for k in ('mino', 'brax'):
  for t in ('color', 'normal', 'orm'): shutil.copy(f'{W}/tex/{k}_{t}.webp', f'{W}/out/multi/{k}_{t}.webp')
open(f'{W}/out/multi/clips.json', 'w').write(json.dumps(meta, separators=(',', ':')))
multi = tpl.replace('{{DATA}}', '').replace('<script>{{BUNDLE}}</script>', '<script src="game.js"></script>')
open(f'{W}/out/artifact.html', 'w').write(page)  # single-file page: the version that runs in the viewer
open(f'{W}/out/multi/index.html', 'w').write(multi)
head, body = page.split('<canvas', 1)
full = ('<!doctype html>\n<html lang="pl">\n<head>\n<meta charset="utf-8">\n<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">\n'
        + head + '</head>\n<body>\n<canvas' + body + '\n</body>\n</html>\n')
open(f'{W}/out/minotaur-gym.html', 'w').write(full)
print('bundle', len(bundle), 'page', os.path.getsize(f'{W}/out/artifact.html'), 'full', os.path.getsize(f'{W}/out/minotaur-gym.html'), {f: os.path.getsize(f'{W}/out/multi/'+f) for f in sorted(os.listdir(f'{W}/out/multi'))})
