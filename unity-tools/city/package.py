import json,os,pathlib,runpy,shutil
R=pathlib.Path(__file__).resolve().parents[2]
runpy.run_path(str(R/'unity-tools/package_premium.py'),run_name='__main__')
out=R/'unity-preview';p=out/'index.html';html=p.read_text()
html=html.replace('BRAXHORN • Siłownia Unity','BRAXHORN • Miasto Unity').replace('ORYGINALNA SIŁOWNIA • UNITY','ORYGINALNE MIASTO • UNITY').replace('Twoje miejsce.<br>Twój trening.','Twoje miasto.<br>Twoje zasady.').replace('Brax. Ring. Worki. Swobodny ruch po siłowni.','Brax. Siłownia. Miasto. Osiedle.').replace('WEJDŹ NA SIŁOWNIĘ','WEJDŹ DO MIASTA').replace('Rozbudowana siłownia — oddzielna od Braxhorn City.','Oddzielna migracja Unity — oryginalna gra pozostaje bez zmian.').replace('Wczytuję siłownię i Braxa…','Wczytuję oryginalne miasto i Braxa…').replace('UNITY / TRENING 02','UNITY / MIASTO').replace('GYM<br>','CITY<br>').replace('<h2>SIŁOWNIA</h2>','<h2>BRAXHORN CITY</h2>').replace('Podejdź do worka i ćwicz ciosy. Kamerę obracasz sam.','Siłownia → ulica → osiedle. Kamerę obracasz sam.')
html=html.replace('</style>','#citymap{position:fixed;inset:0;background:#11180ff5;z-index:30;display:flex;align-items:center;justify-content:center}#citymap .panel{width:min(540px,90vw);max-height:82dvh;overflow:auto}#places{display:grid;grid-template-columns:1fr 1fr;gap:8px}#places button{font-size:11px;margin:0;min-height:44px}#citymap p{margin:7px 0 14px}.panel{max-height:82dvh;overflow:auto}</style>')
html=html.replace('<button id="reset">','<button id="mapbutton">MAPA DZIELNICY</button><button id="reset">')
html=html.replace('<script>','<div id="citymap" class="hidden"><div class="panel"><h2>MAPA DZIELNICY</h2><p id="areaname"></p><div id="places"></div><button id="mapclose">ZAMKNIJ MAPĘ</button></div></div><script>',1)
addon='''<script>
(function(){
 const original=window.onBraxhornState;let area='',points=[];
 const defaultHint='Siłownia → ulica → osiedle. Kamerę obracasz sam.';
 window.onBraxhornState=function(s){
  original(s);points=s.points||[];area=s.area||'city';
  document.querySelector('#hits').textContent=area==='estate'?'OSIEDLE · UNITY':'MIASTO · UNITY';
  const context=s.context||'',action=document.querySelector('#action');
  action.style.display=context.includes('DRZWI')||context.includes('OSIEDLE')||context.includes('WRÓĆ DO MIASTA')?'block':'none';action.textContent=context;
  document.querySelector('#hint').textContent=context||defaultHint;
  if(s.loadingArea){document.querySelector('#loading').classList.remove('hidden');document.querySelector('#loadtext').textContent='Wczytuję dzielnicę…';}
 };
 const close=()=>document.querySelector('#citymap').classList.add('hidden');
 document.querySelector('#mapclose').onclick=close;
 document.querySelector('#mapbutton').onclick=()=>{
  document.querySelector('#areaname').textContent=area==='estate'?'Osiedle: park, willa i budynki usługowe.':'Miasto: siłownia, ulica, nabrzeże i klub NOVA.';
  const list=document.querySelector('#places');list.replaceChildren();
  for(const p of points){const button=document.createElement('button');button.textContent=p.label||p.id;button.onclick=()=>{close();if(window.BraxhornState?.paused)window.BraxSend('Command','pause');window.BraxSend('TravelPoint',p.id);};list.append(button);}
  document.querySelector('#citymap').classList.remove('hidden');
 };
})();
</script>'''
html=html.replace('</body>',addon+'</body>');p.write_text(html)
for name in ['city-migration.json']:
 assert (out/name).is_file(),name
for name in ['city-manifest.json','estate-manifest.json','source-provenance.json','native-texture-preparation.json']:
 shutil.copyfile(R/'unity-city/Assets/Source/City'/name,out/name)
(out/'migration-scope.txt').write_text('Original city and estate environment migration, original Brax movement and six actions, original gym and estate doors. Estate NPC AI, boss combat, story/economy and second character selection are not included in this build. No production replacement.\n')
print('CITY_TEST_PACKAGE',os.environ.get('GITHUB_SHA'),sum(p.stat().st_size for p in out.rglob('*') if p.is_file()))
