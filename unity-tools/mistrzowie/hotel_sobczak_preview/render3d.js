// Depth-buffered preview with the real Poly Haven maps and models (three.js in headless Chromium via Playwright).
// node render3d.js <bin dir> <three package dir> <views.json> <models dir> <textures dir>
// views in Unity coordinates, nominal metres; z is mirrored for three.js (right-handed).
const { chromium } = require('playwright');
const fs = require('fs'); const path = require('path');
const [binDir, threeDir, viewsPath, modelsDir, texDir] = process.argv.slice(2);
const views = JSON.parse(fs.readFileSync(viewsPath, 'utf8'));
const PALMS_ON = process.env.PREVIEW_PALMS === '1';
const html = `<!doctype html><html><body style="margin:0;background:#9fc2e5">
<script type="importmap">{"imports":{"three":"http://preview.local/three/build/three.module.js","three/addons/":"http://preview.local/three/examples/jsm/"}}</script>
<script type="module">
import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
const PALMS=${PALMS_ON};
async function bin(n){const r=await fetch('http://preview.local/bin/'+n);return new Float32Array(await r.arrayBuffer());}
const tl=new THREE.TextureLoader();
function tex(url,srgb){return new Promise(res=>tl.load(url,t=>{t.wrapS=t.wrapT=THREE.RepeatWrapping;t.anisotropy=8;if(srgb)t.colorSpace=THREE.SRGBColorSpace;res(t);},undefined,()=>res(null)));}
// Lighting = CityStaticPBR (the shader every W surface uses in Unity): fixed city sun without shadows,
// per-material ambient lerp(ground,sky,n.y*.5+.5)*_AmbientIntensity (albedo x ambient, no GI), broad sky/ground
// reflection x .4, up to 4 vertex lamps per object with Unity's attenuation, no tone mapping.
const S2L=c=>new THREE.Color().setRGB(c[0],c[1],c[2],THREE.SRGBColorSpace);
function cityAmbient(mat,a){
  a=a||[.60,.68,.78,.42,.40,.36,.65];
  const uSky={value:S2L(a.slice(0,3))},uGround={value:S2L(a.slice(3,6))},uAmb={value:a[6]};
  mat.onBeforeCompile=sh=>{
    Object.assign(sh.uniforms,{uSky,uGround,uAmb});
    const NL=String.fromCharCode(10);
    const inject=['#include <lights_fragment_begin>',
      '{ vec3 wn=inverseTransformDirection(normal,viewMatrix); irradiance+=mix(uGround,uSky,wn.y*.5+.5)*uAmb*PI; }',
      '#if defined( RE_IndirectSpecular )',
      '{ vec3 wr=inverseTransformDirection(reflect(-geometryViewDir,normal),viewMatrix); radiance+=mix(uGround,uSky,wr.y*.5+.5)*.4*(1.-.65*material.roughness); }',
      '#endif'].join(NL);
    sh.fragmentShader='uniform vec3 uSky;uniform vec3 uGround;uniform float uAmb;'+NL+sh.fragmentShader.replace('#include <lights_fragment_begin>',inject);
  };
  mat.customProgramCacheKey=()=>a.join(',');
}
window.setup=async function(w,h){
  const R=new THREE.WebGLRenderer({antialias:true,preserveDrawingBuffer:true});R.setSize(w,h);R.toneMapping=THREE.NoToneMapping;
  document.body.appendChild(R.domElement);
  const S=new THREE.Scene();S.background=new THREE.Color(0x9fc2e5);
  const sun=new THREE.DirectionalLight(S2L([1,.98,.94]),2.4);sun.position.set(-.45,.78,.42);S.add(sun);   // _SunDirection (z mirrored)
  const scene=await (await fetch('http://preview.local/bin/scene.json')).json();
  for(const m of scene.materials){
    const g=new THREE.BufferGeometry();
    g.setAttribute('position',new THREE.BufferAttribute(await bin('m'+m.id+'_pos.bin'),3));
    g.setAttribute('uv',new THREE.BufferAttribute(await bin('m'+m.id+'_uv.bin'),2));
    g.computeVertexNormals();
    const c=m.colour, n=m.name.toLowerCase();
    const p={color:S2L([Math.min(c[0],1.6),Math.min(c[1],1.6),Math.min(c[2],1.6)]),side:THREE.FrontSide,roughness:.75,metalness:0};
    if(m.slug){p.map=await tex('http://preview.local/tex/'+m.slug+'_diff_1k.jpg',true);p.normalMap=await tex('http://preview.local/tex/'+m.slug+'_nor_gl_1k.jpg',false);
      if(/ceiling|wall_white/.test(n))p.normalScale=new THREE.Vector2(.18,.18);}   // same _BumpScale as the Unity ceiling / white wall materials
    if(/brass|chrome|stainless|bronze|mirror/.test(n)){p.metalness=.9;p.roughness=/mirror|chrome/.test(n)?.08:.3;}
    if(m.pbr){   // plain-mesh generators (casino, arena) record the real _Metallic / _Roughness / _EmissionColor
      p.metalness=m.pbr.metallic;p.roughness=Math.max(m.pbr.roughness,.04);
      const e=m.pbr.emission;if(e[0]+e[1]+e[2]>0){p.emissive=S2L(e.map(x=>Math.min(x,1)));p.emissiveIntensity=Math.max(1,Math.max(...e));}
      if(m.pbr.picture){p.map=await tex('http://preview.local/pic/'+encodeURIComponent(m.pbr.picture),true);if(p.map){p.map.wrapS=p.map.wrapT=THREE.ClampToEdgeWrapping;p.alphaTest=.08;if(e[0]+e[1]+e[2]>0)p.emissiveMap=p.map;}}
    }else if(/light|led|frosted/.test(n)){p.emissive=p.color.clone();p.emissiveIntensity=1.0;}
    const transparent=c[3]<.99;
    if(transparent){p.transparent=true;p.opacity=Math.max(c[3],.18);p.depthWrite=false;p.roughness=.05;}
    const mat=new THREE.MeshStandardMaterial(p);cityAmbient(mat,m.ambient);
    S.add(new THREE.Mesh(g,mat));
  }
  if(scene.ground!=null){const gm=new THREE.MeshStandardMaterial({color:S2L([.36,.37,.38]),roughness:.9});cityAmbient(gm);
    const gp=new THREE.Mesh(new THREE.PlaneGeometry(1600,1600),gm);gp.rotation.x=-Math.PI/2;gp.position.y=scene.ground;S.add(gp);}
  // glTF models use glTFast's shader in Unity: RenderSettings trilight ambient (BraxChampionsExplorer), not CityStaticPBR's
  const loader=new GLTFLoader();const cache={};
  for(const mo of scene.models){
    if(!(mo.path in cache)){
      cache[mo.path]=await new Promise(res=>loader.load('http://preview.local/models/'+mo.path,gl=>res(gl.scene),undefined,()=>res(null)));
      if(cache[mo.path])cache[mo.path].traverse(o=>{if(o.isMesh)(Array.isArray(o.material)?o.material:[o.material]).forEach(mt=>cityAmbient(mt,[.55,.63,.71,.23,.27,.22,1]));});
    }
    const src=cache[mo.path];if(!src)continue;
    const o=src.clone(true);o.position.set(mo.pos[0],mo.pos[1],mo.pos[2]);o.rotation.y=mo.rotY;if(mo.scale&&mo.scale!==1)o.scale.setScalar(mo.scale);
    if(mo.material){const e=mo.material.emission;const mm=new THREE.MeshStandardMaterial({color:S2L(mo.material.colour),metalness:mo.material.metallic,roughness:mo.material.roughness,
      emissive:S2L(e.map(x=>Math.min(x,1))),emissiveIntensity:1});cityAmbient(mm);o.traverse(n=>{if(n.isMesh)n.material=mm;});}
    S.add(o);
  }
  // City palms: the game places its own palm model pack; the preview draws a plain stand-in (trunk + fronds) there.
  // Off unless PREVIEW_PALMS=1: a stand-in is not what the game shows (owner 10.11: "żadnych sztucznych drzew").
  if(PALMS&&scene.palms&&scene.palms.length){
    const tm=new THREE.MeshStandardMaterial({color:S2L([.42,.33,.24]),roughness:.9});cityAmbient(tm);
    const fm=new THREE.MeshStandardMaterial({color:S2L([.20,.42,.16]),roughness:.8,side:THREE.DoubleSide});cityAmbient(fm);
    const palm=new THREE.Group();const trunk=new THREE.Mesh(new THREE.CylinderGeometry(.16,.24,7.2,10),tm);trunk.position.y=3.6;palm.add(trunk);
    for(let i=0;i<11;i++){const f=new THREE.Mesh(new THREE.PlaneGeometry(.7,3.4),fm);const a=i*Math.PI*2/11;
      f.position.set(Math.cos(a)*1.35,7.0-(i%2)*.25,Math.sin(a)*1.35);f.rotation.set(0,-a+Math.PI/2,0);f.rotateX(-1.05-(i%2)*.2);palm.add(f);}
    for(const p of scene.palms){const o=palm.clone(true);o.position.set(p[0],p[1],p[2]);o.rotation.y=p[0]*.7+p[2]*.3;S.add(o);}
  }
  // W.Lampa: colour (1,.95,.86), ForceVertex. Unity per-vertex-light attenuation ~ 1/(1+25 d^2/r^2): decay 1, I = 2.5 x intensity
  window.lights=scene.lights.map(l=>{const L=new THREE.PointLight(S2L([1,.95,.86]),l[4]*2.5,l[3],1);L.position.set(l[0],l[1],l[2]);L.visible=false;S.add(L);return L;});
  window.R=R;window.S=S;return {materials:scene.materials.length,models:scene.models.length};
};
window.shot=function(v,w,h){
  const eye=new THREE.Vector3(v.eye[0],v.eye[1],-v.eye[2]);
  window.lights.forEach(L=>L.visible=false);
  window.lights.map(L=>[L,L.position.distanceTo(eye)]).sort((a,b)=>a[1]-b[1]).slice(0,v.lights??4).forEach(([L,d])=>{if(d<L.distance+2)L.visible=true;});
  const C=new THREE.PerspectiveCamera(v.fov,w/h,v.near||.05,2000);
  C.position.copy(eye);C.lookAt(v.target[0],v.target[1],-v.target[2]);
  R.render(S,C);return R.domElement.toDataURL('image/png');
};
window.ready=true;
</script></body></html>`;
(async () => {
  const b = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
  const p = await b.newPage({ viewport: { width: 1600, height: 900 } });
  p.on('console', m => { if (!/GPU stall|deprecated/.test(m.text())) console.log('page:', m.text()); });
  await p.route('http://preview.local/**', r => {
    const u = new URL(r.request().url()).pathname.slice(1);
    let file = null;
    if (u === 'view.html') { return r.fulfill({ status: 200, body: html, contentType: 'text/html' }); }
    if (u.startsWith('three/')) file = path.join(threeDir, u.slice(6));
    else if (u.startsWith('bin/')) file = path.join(binDir, u.slice(4));
    else if (u.startsWith('models/')) file = path.join(modelsDir, decodeURIComponent(u.slice(7)));
    else if (u.startsWith('tex/')) file = path.join(texDir, u.slice(4));
    else if (u.startsWith('pic/')) file = path.join(process.env.PREVIEW_PICTURES || texDir, decodeURIComponent(u.slice(4)));
    if (!file || !fs.existsSync(file)) return r.fulfill({ status: 404, body: '' });
    const ext = path.extname(file);
    const type = { '.js': 'application/javascript', '.json': 'application/json', '.gltf': 'model/gltf+json', '.jpg': 'image/jpeg', '.png': 'image/png' }[ext] || 'application/octet-stream';
    r.fulfill({ status: 200, body: fs.readFileSync(file), contentType: type });
  });
  await p.goto('http://preview.local/view.html');
  await p.waitForFunction(() => window.ready === true);
  const info = await p.evaluate(([w, h]) => window.setup(w, h), [1600, 900]);
  console.log('scene', JSON.stringify(info));
  for (const v of views) {
    const url = await p.evaluate(([v, w, h]) => window.shot(v, w, h), [v, 1600, 900]);
    fs.writeFileSync(v.out, Buffer.from(url.split(',')[1], 'base64')); console.log('wrote', v.out);
  }
  await b.close();
})();
