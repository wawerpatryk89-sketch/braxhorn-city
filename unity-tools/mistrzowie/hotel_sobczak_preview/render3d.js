// Depth-buffered preview (three.js in headless Chromium via Playwright).
// node render3d.js <bin dir> <three.min.js> <views.json>   (views in Unity coordinates, nominal metres)
const { chromium } = require('playwright');
const fs = require('fs'); const path = require('path');
const [binDir, threePath, viewsPath] = process.argv.slice(2);
const views = JSON.parse(fs.readFileSync(viewsPath, 'utf8'));
const html = `<!doctype html><html><body style="margin:0;background:#9fc2e5">
<script src="http://preview.local/three.min.js"></script>
<script>
async function bin(n){const r=await fetch('http://preview.local/'+n);return new Float32Array(await r.arrayBuffer());}
window.setup=async function(w,h){
  const R=new THREE.WebGLRenderer({antialias:true,preserveDrawingBuffer:true});R.setSize(w,h);document.body.appendChild(R.domElement);
  const S=new THREE.Scene();S.background=new THREE.Color(0x9fc2e5);
  S.add(new THREE.HemisphereLight(0xffffff,0x6b665e,1.35));
  const sun=new THREE.DirectionalLight(0xfff4e0,1.6);sun.position.set(-45,78,42);S.add(sun);   // Unity sun (-.45,.78,-.42), z mirrored
  for(const [tag,opaque] of [['o',true],['t',false]]){
    const g=new THREE.BufferGeometry();g.setAttribute('position',new THREE.BufferAttribute(await bin('pos_'+tag+'.bin'),3));
    const c=await bin('col_'+tag+'.bin');const a=await bin('alpha_'+tag+'.bin');const rgba=new Float32Array(c.length/3*4);
    for(let i=0,j=0;i<c.length;i+=3,j+=4){rgba[j]=c[i];rgba[j+1]=c[i+1];rgba[j+2]=c[i+2];rgba[j+3]=a[i/3];}
    g.setAttribute('color',new THREE.BufferAttribute(rgba,4));g.computeVertexNormals();
    const m=new THREE.MeshLambertMaterial({vertexColors:true,transparent:!opaque,depthWrite:opaque,side:THREE.FrontSide});
    S.add(new THREE.Mesh(g,m));
  }
  window.R=R;window.S=S;
};
window.shot=function(v,w,h){
  const C=new THREE.PerspectiveCamera(v.fov,w/h,v.near||.05,2000);
  C.position.set(v.eye[0],v.eye[1],-v.eye[2]);C.lookAt(v.target[0],v.target[1],-v.target[2]);
  R.render(S,C);return R.domElement.toDataURL('image/png');
};
</script></body></html>`;
(async () => {
  const page_html = path.resolve(binDir, 'view.html'); fs.writeFileSync(page_html, html);
  const b = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
  const p = await b.newPage({ viewport: { width: 1600, height: 900 } });
  p.on('console', m => console.log('page:', m.text()));
  // everything is served from memory by Playwright (no web server, no file:// fetch restrictions)
  await p.route('http://preview.local/**', r => {
    const name = new URL(r.request().url()).pathname.slice(1);
    const file = name === 'three.min.js' ? threePath : path.resolve(binDir, name);
    r.fulfill({ status: 200, body: fs.readFileSync(file), contentType: name.endsWith('.html') ? 'text/html' : name.endsWith('.js') ? 'application/javascript' : 'application/octet-stream' });
  });
  await p.goto('http://preview.local/view.html');
  await p.evaluate(([w, h]) => window.setup(w, h), [1600, 900]);
  for (const v of views) {
    const url = await p.evaluate(([v, w, h]) => window.shot(v, w, h), [v, 1600, 900]);
    fs.writeFileSync(v.out, Buffer.from(url.split(',')[1], 'base64')); console.log('wrote', v.out);
  }
  await b.close();
})();
