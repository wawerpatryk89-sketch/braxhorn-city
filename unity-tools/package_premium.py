import pathlib,os,runpy,json,shutil
root=pathlib.Path(__file__).resolve().parents[1]
runpy.run_path(str(root/'unity-tools/package_preview.py'),run_name='__main__')
out=root/'unity-preview';html=(out/'index.html').read_text();version=os.environ.get('GITHUB_SHA','local')[:12]
for f in (out/'Build').iterdir():
 if f.is_file():
  new=f.with_name(version+'_'+f.name);html=html.replace('Build/'+f.name,'Build/'+new.name);f.rename(new)
html=html.replace('<strong>✊</strong>','<strong><svg width="25" height="25" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round"><path d="M5 11V7a2 2 0 0 1 4 0V5a2 2 0 0 1 4 0v1a2 2 0 0 1 4 0v2a2 2 0 0 1 4 0v7l-4 6H9l-5-6a3 3 0 0 1 1-4Z"/><path d="M9 7v5m4-6v6m4-4v5M5 11l4 3"/></svg></strong>')
html=html.replace('<strong>↗</strong>','<strong><svg width="25" height="25" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linejoin="round"><path d="m12 3 4 2-4 8 6 3 3 1-1 4-8-2-6-5 2-6Z"/><path d="m11 13-4-2m7 6 2-3"/></svg></strong>')
html=html.replace('TRAFIENIA W WOREK: ','WOREK · ')
html=html.replace('UNITY / TRENING','UNITY / TRENING 02')
html=html.replace('Wersja testowa — oddzielna od Braxhorn City.','Rozbudowana siłownia — oddzielna od Braxhorn City.')
html=html.replace("productVersion:'2026.10.29'","productVersion:'2026.10.29-"+version+"'")
(out/'index.html').write_text(html)
for name in ['unity-premium-report.json','material-review.json','geometry-review.json','source-refinements.json']:
 shutil.copyfile(root/'unity-city/Assets/Source/Premium'/name,out/name)
(out/'library-credits.txt').write_text('Sofa 02: Kirill Sannikov / Poly Haven, CC0. Existing owner library. Original source 4K retained unchanged; runtime textures 2K.\nBrax model, rig, animations, UVs and brand marks remain original.\n')
shutil.copyfile(root/'unity-city/Assets/Source/source-integrity.json',out/'source-integrity.json')
print('PREMIUM_WEBGL_ASSETS_VERSIONED',version,sum(p.stat().st_size for p in out.rglob('*') if p.is_file()))
