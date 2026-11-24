// THE QUAD TRACK'S FOREST: the trees' cards baked from the real models (owner 24.11.2026 14:20: "więcej lasu").
// A phone cannot draw thousands of scanned trees (Poly Haven pine_tree_01 is 17.4 M triangles, fir_tree_01 7.85 M): each tree
// is drawn as two crossed cards (BraxChampionsLasKladow), and their pictures are rendered here from the scans themselves -
// each from the side at 0 and 90 degrees, orthographic, centred on its trunk, on a transparent background, lit as by the
// sun (a hemisphere sky + a key light from the front above), the needles' cut-out kept (pobierz_drzewo.py gives the glTF
// its alpha), rendered SS times over for atlas_lasu.py to average down.
//   node wypal_drzewa.js <three dir> <out dir> <model.gltf>:<node name|*>:<frame name> ...
// Each frame: <out>/<frame>_0.png, <frame>_90.png (512 x 1024 x SS, env SS = 2) and <frame>.json (the tree's height, its
// reach from the trunk, its foot, the picture's height; metres).
// The atlas of 24.11 (las_atlas.png):
//   pine_tree_01_las.gltf:pine_tree_01_{a,b,c}_LOD0:sosna_{a,b,c}  fir_tree_01_las.gltf:fir_tree_01_{a,b,c}_LOD0:jodla_{a,b,c}
//   fir_sapling_las.gltf:fir_sapling_a:mloda_jodla  pine_sapling_small_las.gltf:pine_sapling_small_a:mloda_sosna
const fs = require('fs'), path = require('path'), http = require('http');
const { chromium } = require('playwright');
const [threeDir, outDir, ...jobs] = process.argv.slice(2);
const SS = +(process.env.SS || 2), W = 512 * SS, H = 1024 * SS;   // rendered SS times over (the atlas averages it down)
const html = `<!doctype html><html><body style="margin:0;background:transparent">
<script type="importmap">{"imports":{"three":"/three/build/three.module.js","three/addons/":"/three/examples/jsm/"}}</script>
<script type="module">
import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
const R = new THREE.WebGLRenderer({ antialias: true, alpha: true, preserveDrawingBuffer: true });
R.setSize(${W}, ${H}); R.setClearColor(0x000000, 0); R.outputColorSpace = THREE.SRGBColorSpace;
document.body.appendChild(R.domElement);
window.bake = async (url, nodeName) => {
  const g = await new GLTFLoader().loadAsync(url);
  let root = g.scene;
  if (nodeName !== '*') { const n = g.scene.getObjectByName(nodeName); if (!n) return { error: 'no node ' + nodeName }; root = n; root.parent.remove(root); root.position.set(0, 0, 0); }
  const S = new THREE.Scene(); S.add(root);
  root.traverse(o => { if (o.isMesh && o.visible) { const ms = Array.isArray(o.material) ? o.material : [o.material];
    for (const m of ms) { m.transparent = false; m.alphaTest = .5; m.side = THREE.DoubleSide; m.depthWrite = true;
      // the needles are thin lines on a black sheet: no mipmaps (their averages run to black at this distance)
      for (const k of ['map', 'normalMap', 'roughnessMap', 'aoMap']) { const t = m[k]; if (t) { t.generateMipmaps = false; t.minFilter = THREE.LinearFilter; t.needsUpdate = true; } } } } });
  S.add(new THREE.HemisphereLight(0xe4ecf4, 0x6a5a45, 2.4));
  const sun = new THREE.DirectionalLight(0xfff2dd, 2.6); sun.position.set(.4, 1.0, .8); S.add(sun);
  const box = new THREE.Box3(); root.traverse(o => { if (o.isMesh && o.visible && (!o.parent || o.parent.visible)) box.expandByObject(o); });
  const size = box.getSize(new THREE.Vector3());
  // the frame centred on the trunk (the model's origin: both crossed cards then show it at their crossing), the tree filling
  // it (its height, or its widest reach from the trunk for a broad one; a 2 % margin), its foot at the bottom
  const reach = Math.max(-box.min.x, box.max.x, -box.min.z, box.max.z);
  const h = Math.max(size.y, 2 * reach * ${H} / ${W}) * 1.02, w = h * ${W} / ${H};
  const out = { height: size.y, width: Math.max(size.x, size.z), reach, minY: box.min.y, frameH: h, views: {} };
  for (const deg of [0, 90]) {
    const a = deg * Math.PI / 180, d = reach * 4 + 10;
    const cam = new THREE.OrthographicCamera(-w / 2, w / 2, h / 2, -h / 2, .1, d * 3);
    cam.position.set(Math.sin(a) * d, box.min.y + h / 2, Math.cos(a) * d); cam.lookAt(0, box.min.y + h / 2, 0);
    sun.position.set(Math.sin(a + .5) * d, box.max.y + d, Math.cos(a + .5) * d);
    R.render(S, cam); out.views[deg] = R.domElement.toDataURL('image/png');
  }
  return out;
};
window.ready = true;
</script></body></html>`;
(async () => {
  const b = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--disable-dev-shm-usage', '--js-flags=--max-old-space-size=8192'] });
  const p = await b.newPage({ viewport: { width: W, height: H } });
  p.on('console', m => console.log('page:', m.text()));
  // a local server streams the files (the scanned fir's 478 MB of geometry: too big for a routed reply)
  const srv = http.createServer((q, r) => {
    const u = decodeURIComponent(new URL(q.url, 'http://x').pathname.slice(1));
    if (u === 'bake.html') { r.writeHead(200, { 'Content-Type': 'text/html' }); return r.end(html); }
    let file = null;
    if (u.startsWith('three/')) file = path.join(threeDir, u.slice(6));
    else if (u.startsWith('m/')) file = '/' + u.slice(2);
    if (!file || !fs.existsSync(file)) { r.writeHead(404); return r.end(); }
    const type = { '.js': 'application/javascript', '.gltf': 'model/gltf+json', '.glb': 'model/gltf-binary', '.jpg': 'image/jpeg', '.png': 'image/png' }[path.extname(file)] || 'application/octet-stream';
    r.writeHead(200, { 'Content-Type': type, 'Content-Length': fs.statSync(file).size });
    fs.createReadStream(file).pipe(r);
  });
  await new Promise(ok => srv.listen(0, '127.0.0.1', ok));
  const base = 'http://127.0.0.1:' + srv.address().port;
  await p.goto(base + '/bake.html');
  await p.waitForFunction(() => window.ready === true);
  fs.mkdirSync(outDir, { recursive: true });
  for (const job of jobs) {
    const [model, node, name] = job.split(':');
    const t0 = Date.now();
    const res = await p.evaluate(([u, n]) => window.bake(u, n), [base + '/m' + path.resolve(model), node]);
    if (res.error) { console.log('ERROR', name, res.error); continue; }
    for (const deg of [0, 90]) fs.writeFileSync(path.join(outDir, name + '_' + deg + '.png'), Buffer.from(res.views[deg].split(',')[1], 'base64'));
    fs.writeFileSync(path.join(outDir, name + '.json'), JSON.stringify({ height: res.height, width: res.width, reach: res.reach, minY: res.minY, frameH: res.frameH }));
    console.log('baked', name, 'h=' + res.height.toFixed(2), 'w=' + res.width.toFixed(2), ((Date.now() - t0) / 1000).toFixed(0) + ' s');
  }
  await b.close(); srv.close();
})();
