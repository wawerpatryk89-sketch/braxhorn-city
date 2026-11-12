// BRAXHORN — przegląd modeli sklepowych ze wszystkich stron (owner 12.11.2026 13:58: "przejrzeć te modele, czy wszystko się
// zgadza, żeby nie było lipy"; buty zrośnięte od tyłu, zegarki bez tyłu). Every .glb of a decrypted pack folder is drawn
// from six sides (front, back, left, right, top, bottom) in one row with its name: a contact sheet per pack to judge what
// is whole, what is good from one side only and what is not usable.
// node przeglad_modeli.js <pack folder with *.glb> <node_modules/three> <out.png>     (needs playwright; nothing is uploaded)
// PRZEGLAD_KAFEL=<px> sets the tile size (220 by default; 560 for one model looked at closely)
// PRZEGLAD_KLIENT=<json: name -> {tx, tz}> adds two columns: the model seen by a customer standing where (tx, tz) points
// (the toes of a pair of shoes put heels to the wall), at eye level and from above
const { chromium } = require('playwright');
const fs = require('fs'); const path = require('path');
const [dir, threeDir, out] = process.argv.slice(2);
const KLIENT = process.env.PRZEGLAD_KLIENT ? JSON.parse(fs.readFileSync(process.env.PRZEGLAD_KLIENT, 'utf8')) : null;
const KOLUMNY = KLIENT ? 8 : 6;
const KAFEL = Math.max(120, parseInt(process.env.PRZEGLAD_KAFEL || '220', 10) || 220);
const files = fs.readdirSync(dir).filter(f => f.endsWith('.glb')).sort();
const html = `<!doctype html><html><body style="margin:0;background:#888"><canvas id=c></canvas>
<script type="importmap">{"imports":{"three":"http://audit.local/three/build/three.module.js","three/addons/":"http://audit.local/three/examples/jsm/"}}</script>
<script type="module">
import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
const T=${KAFEL}, files=${JSON.stringify(files)}, klient=${JSON.stringify(KLIENT)}, NK=${KOLUMNY};
const cv=document.getElementById('c'); cv.width=T*NK+170; cv.height=T*files.length;
const R=new THREE.WebGLRenderer({canvas:cv,antialias:true,preserveDrawingBuffer:true}); R.setSize(cv.width,cv.height,false); R.setScissorTest(true);
R.setClearColor(0x8a8f96,1); R.outputColorSpace=THREE.SRGBColorSpace;
const S=new THREE.Scene(); S.add(new THREE.HemisphereLight(0xffffff,0x777777,2.2)); const D=new THREE.DirectionalLight(0xffffff,1.6); S.add(D);
const C=new THREE.OrthographicCamera(-1,1,1,-1,.001,100);
const dirs=[[0,0,1],[0,0,-1],[-1,0,0],[1,0,0],[0,1,.001],[0,-1,.001]];
const loader=new GLTFLoader(); window.rows=[];
window.run=async function(){
  for(let i=0;i<files.length;i++){
    const gl=await new Promise(res=>loader.load('http://audit.local/models/'+encodeURIComponent(files[i]),g=>res(g),undefined,()=>res(null)));
    if(!gl){window.rows.push([files[i],null]);continue;}
    const o=gl.scene; S.add(o); const b=new THREE.Box3().setFromObject(o); const c=b.getCenter(new THREE.Vector3()); const s=b.getSize(new THREE.Vector3());
    const r=Math.max(s.x,s.y,s.z)*.58; let tris=0; o.traverse(n=>{if(n.isMesh){tris+=(n.geometry.index?n.geometry.index.count:n.geometry.attributes.position.count)/3;n.material.side=THREE.FrontSide;}});
    const kl=klient&&klient[files[i].replace('.glb','')]; const my=dirs.slice(); if(klient){const t=kl||{tx:0,tz:1}; my.push([t.tx,.42,t.tz],[t.tx,1.3,t.tz]);}
    for(let k=0;k<NK;k++){
      const d=new THREE.Vector3(...my[k]).normalize(); C.left=-r;C.right=r;C.top=r;C.bottom=-r;C.updateProjectionMatrix();
      C.position.copy(c).addScaledVector(d,r*4); C.up.set(0,(k==4||k==5)?0:1,(k==4||k==5)?-1:0); C.lookAt(c); D.position.copy(C.position);
      const x=170+k*T, y=cv.height-(i+1)*T; R.setViewport(x,y,T,T); R.setScissor(x,y,T,T); R.render(S,C);
    }
    S.remove(o); window.rows.push([files[i],[s.x,s.y,s.z].map(v=>+v.toFixed(3)),tris]);
  }
  return window.rows;
};
window.ready=true;
</script></body></html>`;
(async () => {
  const b = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--disable-dev-shm-usage'] });
  const p = await b.newPage({ viewport: { width: 1500, height: 900 } });
  await p.route('http://audit.local/**', r => {
    const u = decodeURIComponent(new URL(r.request().url()).pathname.slice(1));
    if (u === 'view.html') return r.fulfill({ status: 200, body: html, contentType: 'text/html' });
    const file = u.startsWith('three/') ? path.join(threeDir, u.slice(6)) : path.join(dir, u.slice(7));
    if (!fs.existsSync(file)) return r.fulfill({ status: 404, body: '' });
    const ext = path.extname(file);
    r.fulfill({ status: 200, body: fs.readFileSync(file), contentType: { '.js': 'application/javascript', '.jpg': 'image/jpeg', '.png': 'image/png' }[ext] || 'application/octet-stream' });
  });
  await p.goto('http://audit.local/view.html');
  await p.waitForFunction(() => window.ready === true);
  const rows = await p.evaluate(() => window.run());
  // labels drawn on a 2D canvas over the picture
  const url = await p.evaluate(([rows, T, NK]) => {
    const cv = document.getElementById('c'); const o = document.createElement('canvas'); o.width = cv.width; o.height = cv.height + 24;
    const g = o.getContext('2d'); g.fillStyle = '#222'; g.fillRect(0, 0, o.width, o.height); g.drawImage(cv, 0, 24);
    g.fillStyle = '#fff'; g.font = '13px sans-serif';
    ['przód', 'tył', 'lewy bok', 'prawy bok', 'góra', 'spód', 'od klienta', 'od klienta z góry'].slice(0, NK).forEach((t, k) => g.fillText(t, 170 + k * T + 8, 16));
    rows.forEach((r, i) => { const y = 24 + i * T; g.fillStyle = '#222'; g.fillRect(0, y, 170, T); g.fillStyle = '#fff';
      const n = r[0].replace('.glb', ''); for (let k = 0; k < n.length; k += 20) g.fillText(n.slice(k, k + 20), 6, y + 20 + (k / 20) * 16);
      if (r[1]) { g.fillText(r[1].join(' x ') + ' m', 6, y + 110); g.fillText(r[2] + ' trójk.', 6, y + 128); } else g.fillText('NIE WCZYTANO', 6, y + 110); });
    return o.toDataURL('image/png');
  }, [rows, KAFEL, KOLUMNY]);
  fs.writeFileSync(out, Buffer.from(url.split(',')[1], 'base64'));
  console.log('wrote', out, rows.length);
  await b.close();
})();
