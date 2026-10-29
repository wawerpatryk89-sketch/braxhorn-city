import json,pathlib,shutil
root=pathlib.Path(__file__).resolve().parents[1];source=root/'build/WebGL';out=root/'unity-preview'
if out.exists():shutil.rmtree(out)
shutil.copytree(source,out)
html=(root/'unity-tools/site.html').read_text()
for token,pattern in [('LOADER','*.loader.js'),('DATA','*.data*'),('FRAMEWORK','*.framework.js*'),('WASM','*.wasm*')]:
    matches=list((out/'Build').glob(pattern))
    if len(matches)!=1:raise RuntimeError((token,[p.name for p in matches]))
    html=html.replace('__'+token+'__',matches[0].relative_to(out).as_posix())
(out/'index.html').write_text(html)
shutil.copyfile(root/'public/assets/logo/wide_a.webp',out/'logo.webp')
(out/'robots.txt').write_text('User-agent: *\nDisallow: /\n')
(out/'vercel.json').write_text(json.dumps({'headers':[{'source':'/(.*)','headers':[{'key':'X-Robots-Tag','value':'noindex, nofollow'},{'key':'X-Content-Type-Options','value':'nosniff'}]},{'source':'/Build/(.*)','headers':[{'key':'Cache-Control','value':'public, max-age=86400, immutable'}]}]},indent=2)+'\n')
print('ISOLATED_PREVIEW_READY',sum(p.stat().st_size for p in out.rglob('*') if p.is_file()))
