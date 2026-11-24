// Depth-buffered preview with the real Poly Haven maps and models (three.js in headless Chromium via Playwright).
// node render3d.js <bin dir> <three package dir> <views.json> <models dir> <textures dir>
// views in Unity coordinates, nominal metres; z is mirrored for three.js (right-handed).
const { chromium } = require('playwright');
const fs = require('fs'); const path = require('path');
const [binDir, threeDir, viewsPath, modelsDir, texDir] = process.argv.slice(2);
const views = JSON.parse(fs.readFileSync(viewsPath, 'utf8'));
const PALMS_ON = process.env.PREVIEW_PALMS === '1';
// PREVIEW_PALM_GLB=<path under the models dir>: the owner's own palm pack (meshopt + KTX2 glTF) drawn at every
// palm the generator places - the real model, never a stand-in.
const PALM_GLB = process.env.PREVIEW_PALM_GLB || '';
const MODELS_NEAR = process.env.PREVIEW_MODELS_NEAR || '';
// PREVIEW_PALM_SCALE: 1/K when the scene is in nominal metres (the game places the real-metre palm in K-scaled world)
const PALM_SCALE = parseFloat(process.env.PREVIEW_PALM_SCALE || '1');
// PREVIEW_PALM_HALF=1: the preview palm file is a cluster of palms; keep one (the game plants one per bed)
const PALM_HALF = process.env.PREVIEW_PALM_HALF === '1';
// PREVIEW_FLAT=1: flat facets (every triangle its own normal) instead of the game-like smooth shading
const FLAT = process.env.PREVIEW_FLAT === '1';
// PREVIEW_COLOUR_CAP: the highest tint (gamma) drawn as it is (default 2.2). The game takes any tint (linear colour space,
// the tint to the power 2.2): a dark photo-scan brought to white carries a tint of 3 and more (Plastered Wall 05 to ivory:
// 3.1, 2.8, 2.4) - capped at 2.2 its hue is lost and the wall reads blue-grey (owner 23.11 21:02, the island villa's walls)
const COLOUR_CAP = parseFloat(process.env.PREVIEW_COLOUR_CAP || '2.2');
// PREVIEW_GROUND_TEX=<texture slug in the textures dir>: the ground plane is that real map (e.g. the region's Grass004
// lawn) instead of flat grey; PREVIEW_GROUND_BANDS=JSON [[x0,x1,z0,z1,tex,tileMetres],..] draws the region's roads and
// sidewalks over it (Unity coordinates, nominal metres).
const GROUND_TEX = process.env.PREVIEW_GROUND_TEX || '';
const GROUND_BANDS = process.env.PREVIEW_GROUND_BANDS || '[]';
// PREVIEW_SKY_HDR=<.hdr>: the region's real sky panorama (BraxChampionsSky: Poly Haven, Skybox/Panoramic turned
// PREVIEW_SKY_ROT degrees, exposure PREVIEW_SKY_EXPO) as the background, and in the tower glass (materials whose pbr has
// "glass": Braxhorn/ChampionsPBR _SKY_GLASS, the same formula as the game's shader)
const SKY_HDR = process.env.PREVIEW_SKY_HDR || '';
const SKY_ROT = parseFloat(process.env.PREVIEW_SKY_ROT || '19.0'), SKY_EXPO = parseFloat(process.env.PREVIEW_SKY_EXPO || String(.89 * .9834));   // sunflowers_puresky (owner 17.11)
const html = `<!doctype html><html><body style="margin:0;background:#9fc2e5">
<script type="importmap">{"imports":{"three":"http://preview.local/three/build/three.module.js","three/addons/":"http://preview.local/three/examples/jsm/"}}</script>
<script type="module">
import * as THREE from 'three';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
import { KTX2Loader } from 'three/addons/loaders/KTX2Loader.js';
import { MeshoptDecoder } from 'three/addons/libs/meshopt_decoder.module.js';
import { toCreasedNormals } from 'three/addons/utils/BufferGeometryUtils.js';
import { Reflector } from 'three/addons/objects/Reflector.js';
import { RGBELoader } from 'three/addons/loaders/RGBELoader.js';
const process_flat=${FLAT};const CAP=${COLOUR_CAP};const MODELS_NEAR=${JSON.stringify(MODELS_NEAR)};const PALMS=${PALMS_ON};const PALM_GLB=${JSON.stringify(PALM_GLB)};const PALM_SCALE=${PALM_SCALE};const PALM_HALF=${PALM_HALF};const GROUND_TEX=${JSON.stringify(GROUND_TEX)};const GROUND_BANDS=${GROUND_BANDS};const SKY=${JSON.stringify(SKY_HDR?'yes':'')};const SKY_ROT=${SKY_ROT};const SKY_EXPO=${SKY_EXPO};
// Unity's Skybox/Panoramic lookup of a Unity-space direction (three.js z mirrored), turned by the sky's rotation; v = 1 -
// acos(y)/pi as BraxChampionsPBR _SKY_GLASS: RGBELoader uploads the .hdr with flipY, the zenith row at v 1 (until 17.11 the
// preview read v = acos(y)/pi - the panorama upside down, the ground fill of a puresky smeared over the sky)
const PANO='vec2 panoUV(vec3 d){float s=sin(radians(skyRot)),c=cos(radians(skyRot));vec3 r=vec3(c*d.x-s*d.z,d.y,s*d.x+c*d.z);'+
  'return vec2(.5-atan(r.z,r.x)/(2.*3.14159265),1.-acos(clamp(r.y,-1.,1.))/3.14159265);}';
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
  let pano=null;
  if(SKY){pano=await new Promise(res=>new RGBELoader().load('http://preview.local/sky/sky.hdr',t=>res(t),undefined,()=>res(null)));
    if(pano){pano.generateMipmaps=true;pano.minFilter=THREE.LinearMipmapLinearFilter;pano.magFilter=THREE.LinearFilter;pano.wrapS=THREE.RepeatWrapping;pano.needsUpdate=true;
      const dm=new THREE.ShaderMaterial({uniforms:{pano:{value:pano},skyRot:{value:SKY_ROT},expo:{value:SKY_EXPO}},side:THREE.BackSide,depthWrite:false,
        vertexShader:'varying vec3 D;void main(){D=position;gl_Position=projectionMatrix*modelViewMatrix*vec4(position,1.);}',
        // under the horizon the game has the region's sea, coast and hills hazed into the sky (BraxChampionsHorizon); the
        // panorama itself is sky only - the preview draws that sea in the haze there instead of the panorama's lower half
        fragmentShader:'uniform sampler2D pano;uniform float skyRot,expo;varying vec3 D;'+PANO+'void main(){vec3 d=normalize(D);d.z=-d.z;'+
          'vec3 c=textureLod(pano,panoUV(vec3(d.x,max(d.y,.002),d.z)),0.).rgb*expo;'+
          'if(d.y<0.){vec3 haze=textureLod(pano,panoUV(vec3(d.x,.002,d.z)),3.).rgb*expo;c=mix(haze,vec3(.035,.085,.11),smoothstep(0.,.18,-d.y));}'+
          'gl_FragColor=vec4(c,1.);'+String.fromCharCode(10)+'#include <colorspace_fragment>'+String.fromCharCode(10)+'}'});
      window.skyDome=new THREE.Mesh(new THREE.SphereGeometry(1900,64,32),dm);window.skyDome.renderOrder=-10;window.skyDome.frustumCulled=false;S.add(window.skyDome);}}
  // live mirror quads (two triangles each, Unity front = into the wall): a three.js Reflector of the same size, turned to
  // the side the camera is on at each shot
  window.mirrors=[];
  const addMirrors=a=>{
    for(let i=0;i+17<a.length;i+=18){
      const P=[];for(let k=0;k<6;k++)P.push(new THREE.Vector3(a[i+3*k],a[i+3*k+1],a[i+3*k+2]));
      const n=new THREE.Vector3().subVectors(P[1],P[0]).cross(new THREE.Vector3().subVectors(P[2],P[0])).normalize();
      const box=new THREE.Box3();P.forEach(p=>box.expandByPoint(p));const c=box.getCenter(new THREE.Vector3());
      const u=new THREE.Vector3(0,1,0).cross(n).normalize();let u0=1e9,u1=-1e9;
      P.forEach(p=>{const d=p.clone().sub(c).dot(u);u0=Math.min(u0,d);u1=Math.max(u1,d);});
      const w=u1-u0,hh=box.max.y-box.min.y;if(w<.05||hh<.05)continue;
      const tw=Math.min(4096,Math.max(512,Math.round(w*140))),th=Math.min(1024,Math.max(256,Math.round(tw*hh/w)));
      const r=new THREE.Group();const rf=new Reflector(new THREE.PlaneGeometry(w,hh),{textureWidth:tw,textureHeight:th,color:0xc4c8cc,clipBias:.003});
      r.add(rf);r.position.copy(c);r.userData={c,n};r.visible=false;S.add(r);window.mirrors.push(r);
    }
  };
  const sun=new THREE.DirectionalLight(S2L([1,.98,.94]),2.4);sun.position.set(-.45,.78,.42);S.add(sun);   // _SunDirection (z mirrored)
  const scene=await (await fetch('http://preview.local/bin/scene.json')).json();
  for(const m of scene.materials){
    const g=new THREE.BufferGeometry();
    g.setAttribute('position',new THREE.BufferAttribute(await bin('m'+m.id+'_pos.bin'),3));
    g.setAttribute('uv',new THREE.BufferAttribute(await bin('m'+m.id+'_uv.bin'),2));
    const c=m.colour, n=m.name.toLowerCase();
    // the game's live mirrors (BraxGymPlanarMirror, materials "... live mirror ..."): a real planar reflection here too
    if(/live mirror/.test(n)){addMirrors(g.getAttribute('position').array);continue;}
    // smooth shading like the game (the batches carry smooth normals on round parts): faces under 35 deg apart share
    // their normals, sharper edges stay hard (PREVIEW_FLAT=1: the old flat facets)
    let gg=g;if(process_flat)g.computeVertexNormals();else{gg=toCreasedNormals(g,35*Math.PI/180);}
    const g2=gg;
    // the Mistrzowie pools' water body (Braxhorn/ChampionsPoolVolume, 16.11): the same light-path fog and caustics as the
    // game's shader; the surface level per pool (nominal metres, the aquapark's DY .16 minus .12 / .07 / .10)
    if(/water_body/.test(n)){
      const kids=/kids/.test(n),spa=/jacuzzi/.test(n);
      const sm=new THREE.ShaderMaterial({uniforms:{deep:{value:S2L(kids?[.03,.26,.42]:spa?[.02,.20,.36]:[.012,.15,.31])},
        shallow:{value:S2L(kids?[.12,.62,.78]:spa?[.10,.56,.74]:[.07,.52,.70])},dens:{value:kids?.20:spa?.30:.24},
        caus:{value:kids?.7:spa?.5:.9},surf:{value:kids?.09:spa?.06:.04},t:{value:3.0}},
        vertexShader:'varying vec3 W;varying vec3 N;void main(){vec4 w=modelMatrix*vec4(position,1.);W=w.xyz;N=normalize(mat3(modelMatrix)*normal);gl_Position=projectionMatrix*viewMatrix*w;}',
        fragmentShader:'uniform vec3 deep,shallow;uniform float dens,caus,surf,t;varying vec3 W;varying vec3 N;void main(){'+
          'vec3 C=cameraPosition,P=W;float len=length(P-C);float under=C.y<=surf?len:len*(1.-clamp((C.y-surf)/max(C.y-P.y,1e-3),0.,1.));'+
          'float depth=max(0.,surf-P.y);float fog=1.-exp(-under*dens);vec3 water=mix(shallow,deep,clamp(under/7.+depth/9.,0.,1.));'+
          'vec2 q=vec2(P.x,-P.z)*2.7;float a1=sin(q.x*1.7+sin(q.y*1.3+t*.8)*1.6+t*.5);float a2=sin(q.y*1.9+sin(q.x*1.1-t*.7)*1.7-t*.6);'+
          'float a3=sin((q.x+q.y)*1.2+sin(q.x*.7-q.y*.9+t*.4)*2.+t*.3);vec2 r=vec2(q.x*.8-q.y*.6,q.x*.6+q.y*.8)*1.37;'+
          'float b1=sin(r.x*1.5+sin(r.y*1.7-t*.6)*1.4-t*.4);float b2=sin(r.y*1.6+sin(r.x*1.2+t*.55)*1.5+t*.45);'+
          'float ridge=clamp(1.-abs(a1+a2+a3)*.45,0.,1.)*clamp(1.-abs(b1+b2)*.3,0.,1.);float up=clamp(normalize(N).y,0.,1.);'+
          'float cs=pow(ridge,6.)*caus*.45*(.3+.7*up)*exp(-depth*.35)*clamp(1.-len/16.,0.,1.)*(1.-fog*.7);'+
          'gl_FragColor=vec4(water*fog+cs*vec3(.62,.70,.66),fog);'+String.fromCharCode(10)+'#include <colorspace_fragment>'+String.fromCharCode(10)+'}',
        transparent:true,depthWrite:false,blending:THREE.CustomBlending,blendSrc:THREE.OneFactor,blendDst:THREE.OneMinusSrcAlphaFactor,blendEquation:THREE.AddEquation,side:THREE.FrontSide});
      const vm=new THREE.Mesh(g2,sm);vm.renderOrder=-1;S.add(vm);continue;
    }
    // tower glass: the game's _SKY_GLASS formula (sun GGX + the region's ambient + the real sky panorama by the
    // reflected direction, the unit's frame from its UV like the mip-mapped frame texture)
    if(m.pbr&&m.pbr.glass&&pano){
      const amb=m.ambient||[.60,.68,.78,.42,.40,.36,.65];
      const gm=new THREE.ShaderMaterial({uniforms:{pano:{value:pano},skyRot:{value:SKY_ROT},expo:{value:SKY_EXPO},f0g:{value:new THREE.Vector3(...m.pbr.glass)},
          body:{value:S2L(c)},rough:{value:Math.max(m.pbr.roughness,.085)},sunDir:{value:new THREE.Vector3(-.45,.78,-.42).normalize()},
          sunCol:{value:S2L([1,.98,.94])},skyC:{value:S2L(amb.slice(0,3))},groundC:{value:S2L(amb.slice(3,6))},ambI:{value:amb[6]}},
        vertexShader:'varying vec3 W;varying vec3 N;varying vec2 U;void main(){vec4 w=modelMatrix*vec4(position,1.);W=w.xyz;N=normalize(mat3(modelMatrix)*normal);U=uv;gl_Position=projectionMatrix*viewMatrix*w;}',
        fragmentShader:'uniform sampler2D pano;uniform float skyRot,expo,rough,ambI;uniform vec3 f0g,body,sunDir,sunCol,skyC,groundC;varying vec3 W;varying vec3 N;varying vec2 U;'+PANO+
          'float P5(float v){float s=v*v;return s*s*v;}'+
          'void main(){vec3 n=normalize(N);vec3 v=normalize(cameraPosition-W);n.z=-n.z;v.z=-v.z;'+
          'float bx=2./128.;vec2 d=min(U,1.-U);vec2 w=fwidth(U);'+
          'float gx=mix(clamp((d.x-bx)/max(w.x,1e-5)+.5,0.,1.),1.-2.*bx,clamp(w.x/(2.*bx)-.5,0.,1.));'+
          'float gy=mix(clamp((d.y-bx)/max(w.y,1e-5)+.5,0.,1.),1.-2.*bx,clamp(w.y/(2.*bx)-.5,0.,1.));float g=gx*gy;'+
          'vec3 alb=mix(vec3(.006,.006,.007),vec3(1.),g)*body;float r=clamp(mix(.45,rough,g),.085,1.);vec3 f0=mix(vec3(.035,.035,.04),f0g,g);'+
          'vec3 L=sunDir;vec3 h=normalize(v+L);float nl=clamp(dot(n,L),0.,1.),nv=max(clamp(dot(n,v),0.,1.),.001),nh=clamp(dot(n,h),0.,1.),vh=clamp(dot(v,h),0.,1.);'+
          'vec3 F=f0+(1.-f0)*P5(1.-vh);float a=r*r,a2=a*a;float dn=nh*nh*(a2-1.)+1.;float D=a2/max(3.14159265*dn*dn,1e-6);float k=(r+1.)*(r+1.)*.125;'+
          'float G=(nv/(nv*(1.-k)+k))*(nl/(nl*(1.-k)+k));vec3 spec=F*(D*G/max(4.*nv*nl,.001));vec3 dif=(1.-F)*alb/3.14159265;'+
          'vec3 col=(dif+spec)*sunCol*(2.4*nl)+alb*mix(groundC,skyC,n.y*.5+.5)*ambI;'+
          'vec3 R=reflect(-v,n);vec3 pn=min(textureLod(pano,panoUV(R),r*6.).rgb*expo,vec3(16.));float sr=sin(radians(skyRot)),cr=cos(radians(skyRot));'+
          'float ry=R.y;vec3 env=mix(pn,groundC*ambI*1.4,clamp(-ry*12.,0.,1.));col+=env*(f0+(1.-f0)*P5(1.-nv))*(1.-.65*r);'+
          'gl_FragColor=vec4(col,1.);'+String.fromCharCode(10)+'#include <colorspace_fragment>'+String.fromCharCode(10)+'}'});
      g.computeVertexNormals();S.add(new THREE.Mesh(g,gm));continue;
    }
    const p={color:S2L([Math.min(c[0],CAP),Math.min(c[1],CAP),Math.min(c[2],CAP)]),side:THREE.FrontSide,roughness:.75,metalness:0};
    if(m.slug){p.map=await tex('http://preview.local/tex/'+m.slug+'_diff_1k.jpg',true);p.normalMap=await tex('http://preview.local/tex/'+m.slug+'_nor_gl_1k.jpg',false);
      if(/ceiling|wall_white/.test(n))p.normalScale=new THREE.Vector2(.18,.18);}   // same _BumpScale as the Unity ceiling / white wall materials
    if(/brass|chrome|stainless|bronze|mirror/.test(n)){p.metalness=.9;p.roughness=/mirror|chrome/.test(n)?.08:.3;}
    if(m.pbr){   // plain-mesh generators (casino, arena) record the real _Metallic / _Roughness / _EmissionColor
      p.metalness=m.pbr.metallic;p.roughness=Math.max(m.pbr.roughness,.04);
      const e=m.pbr.emission;if(e[0]+e[1]+e[2]>0){p.emissive=S2L(e.map(x=>Math.min(x,1)));p.emissiveIntensity=Math.max(1,Math.max(...e));}
      if(m.pbr.picture){p.map=await tex('http://preview.local/pic/'+encodeURIComponent(m.pbr.picture),true);if(p.map){const net=/siatka|tasma/.test(m.pbr.picture),karta=/las_atlas/.test(m.pbr.picture);p.map.wrapS=p.map.wrapT=net?THREE.RepeatWrapping:THREE.ClampToEdgeWrapping;p.alphaTest=net||karta?.5:.08;if(net||karta)p.side=THREE.DoubleSide;if(e[0]+e[1]+e[2]>0)p.emissiveMap=p.map;}}
    }else if(/light|led|frosted/.test(n)){p.emissive=p.color.clone();p.emissiveIntensity=1.0;}
    const transparent=c[3]<.99;
    if(transparent){p.transparent=true;p.opacity=Math.max(c[3],.18);p.depthWrite=false;p.roughness=.05;}
    const mat=new THREE.MeshStandardMaterial(p);cityAmbient(mat,m.ambient);
    S.add(new THREE.Mesh(g2,mat));
  }
  if(scene.ground!=null){const gm=new THREE.MeshStandardMaterial({color:S2L([.36,.37,.38]),roughness:.9});
    if(GROUND_TEX){const t=await tex('http://preview.local/tex/'+GROUND_TEX+'_diff_1k.jpg',true);if(t){t.repeat.set(1600/1.4,1600/1.4);gm.map=t;gm.color=new THREE.Color(1,1,1);}}
    cityAmbient(gm);
    const gp=new THREE.Mesh(new THREE.PlaneGeometry(1600,1600),gm);gp.rotation.x=-Math.PI/2;gp.position.y=scene.ground;S.add(gp);
    for(const b of GROUND_BANDS){const w=b[1]-b[0],d=b[3]-b[2];const bm=new THREE.MeshStandardMaterial({roughness:.9});
      const t=await tex('http://preview.local/tex/'+b[4]+'_diff_1k.jpg',true);if(t){t.repeat.set(w/b[5],d/b[5]);bm.map=t;}cityAmbient(bm);
      const bp=new THREE.Mesh(new THREE.PlaneGeometry(w,d),bm);bp.rotation.x=-Math.PI/2;bp.position.set((b[0]+b[1])/2,scene.ground+.02,-(b[2]+b[3])/2);S.add(bp);}}
  // glTF models use glTFast's shader in Unity: RenderSettings trilight ambient (BraxChampionsExplorer), not CityStaticPBR's
  const loader=new GLTFLoader();const cache={};
  // PREVIEW_MODELS_NEAR="x,y,z,r" (three.js space: z = -Unity z): only the models within r of that point (a gallery
  // with 5 000 goods models does not fit the page's memory at once)
  const near=MODELS_NEAR?MODELS_NEAR.split(',').map(Number):null;
  for(const mo of scene.models){
    if(near&&Math.hypot(mo.pos[0]-near[0],mo.pos[1]-near[1],mo.pos[2]-near[2])>near[3])continue;
    if(!(mo.path in cache)){
      cache[mo.path]=await new Promise(res=>loader.load('http://preview.local/models/'+mo.path,gl=>res(gl.scene),undefined,()=>res(null)));
      if(cache[mo.path])cache[mo.path].traverse(o=>{if(o.isMesh)(Array.isArray(o.material)?o.material:[o.material]).forEach(mt=>cityAmbient(mt,[.55,.63,.71,.23,.27,.22,1]));});
      // a model with its LODs as nodes (drzewo_verdant: pien, liscie_0/1/2 - the game's LODGroup): draw LOD 0 only
      if(cache[mo.path])cache[mo.path].traverse(o=>{if(/^liscie_[1-9]$/.test(o.name))o.visible=false;});
    }
    const src=cache[mo.path];if(!src)continue;
    const o=src.clone(true);o.position.set(mo.pos[0],mo.pos[1],mo.pos[2]);if(mo.quat)o.quaternion.set(mo.quat[0],mo.quat[1],mo.quat[2],mo.quat[3]);else o.rotation.y=mo.rotY;if(mo.scale&&mo.scale!==1)o.scale.setScalar(mo.scale);
    if(mo.map){const t=await tex('http://preview.local/pic/'+encodeURIComponent(mo.map),true);
      // clone() drops the city ambient hook: put it back, or the unlit sides of the repainted model render black
      if(t){t.flipY=false;o.traverse(n=>{if(n.isMesh){n.material=n.material.clone();cityAmbient(n.material,[.55,.63,.71,.23,.27,.22,1]);n.material.map=t;n.material.needsUpdate=true;}});}}
    if(mo.material){const e=mo.material.emission;const mm=new THREE.MeshStandardMaterial({color:S2L(mo.material.colour),metalness:mo.material.metallic,roughness:mo.material.roughness,
      emissive:S2L(e.map(x=>Math.min(x,1))),emissiveIntensity:1});cityAmbient(mm);o.traverse(n=>{if(n.isMesh)n.material=mm;});}
    S.add(o);
  }
  if(PALM_GLB&&scene.palms&&scene.palms.length){
    const pl=new GLTFLoader();pl.setMeshoptDecoder(MeshoptDecoder);
    pl.setKTX2Loader(new KTX2Loader().setTranscoderPath('http://preview.local/three/examples/jsm/libs/basis/').detectSupport(R));
    const palm=await new Promise(res=>pl.load('http://preview.local/models/'+PALM_GLB,gl=>res(gl.scene),undefined,e=>{console.log('palm glb failed '+e);res(null);}));
    if(palm){palm.traverse(o=>{if(o.isMesh)(Array.isArray(o.material)?o.material:[o.material]).forEach(mt=>{mt.side=THREE.DoubleSide;cityAmbient(mt,[.55,.63,.71,.23,.27,.22,1]);});});
      // PREVIEW_PALM_HALF=1: the preview file is a CLUSTER of palms; keep ONE palm (the game plants one per bed): split
      // every primitive into connected parts, take one trunk, the fronds nearest its crown and the base nearest its foot
      let trunkFoot=null;
      if(PALM_HALF){palm.updateMatrixWorld(true);const comps=[];const v=new THREE.Vector3();
        palm.traverse(o=>{if(!o.isMesh||!o.geometry.index)return;const g=o.geometry,p=g.attributes.position,ix=g.index.array,n=p.count;
          const par=new Int32Array(n);for(let i=0;i<n;i++)par[i]=i;const f=i=>{while(par[i]!==i){par[i]=par[par[i]];i=par[i];}return i;};
          for(let t=0;t<ix.length;t+=3){const a=f(ix[t]),b=f(ix[t+1]);if(a!==b)par[b]=a;const c=f(ix[t+2]),a2=f(ix[t]);if(c!==a2)par[c]=a2;}
          const map=new Map();
          for(let t=0;t<ix.length;t+=3){const r=f(ix[t]);if(!map.has(r))map.set(r,{mesh:o,kind:(o.material.name||''),tris:[],pts:[]});const C=map.get(r);C.tris.push(t);
            for(let k=0;k<3;k++){v.fromBufferAttribute(p,ix[t+k]).applyMatrix4(o.matrixWorld);C.pts.push([v.x,v.y,v.z]);}}
          for(const C of map.values()){let mn=1e9,mx=-1e9;for(const q of C.pts){mn=Math.min(mn,q[1]);mx=Math.max(mx,q[1]);}
            const avg=sel=>{let x=0,z=0,k=0;for(const q of C.pts)if(sel(q)){x+=q[0];z+=q[2];k++;}return k?[x/k,z/k]:[0,0];};
            C.minY=mn;C.maxY=mx;C.foot=avg(q=>q[1]<mn+.5);C.top=avg(q=>q[1]>mx-.5);C.mid=avg(q=>true);comps.push(C);}});
        const trunks=comps.filter(C=>/trunk/i.test(C.kind)&&C.maxY-C.minY>3).sort((a,b)=>(b.maxY-b.minY)-(a.maxY-a.minY));
        console.log('palm parts '+comps.length+' trunks '+trunks.length);
        if(trunks.length>1){const T=trunks[0];trunkFoot=T.foot;const d2=(a,b)=>(a[0]-b[0])**2+(a[1]-b[1])**2;
          const keep=new Set([T]);
          for(const C of comps){if(C===T||trunks.includes(C))continue;
            const ref=/base/i.test(C.kind)?'foot':'top';const own=d2(C.mid,T[ref]);
            if(trunks.every(O=>O===T||own<=d2(C.mid,O[ref])))keep.add(C);}
          const per=new Map();for(const C of keep){if(!per.has(C.mesh))per.set(C.mesh,[]);const ix=C.mesh.geometry.index.array;for(const t of C.tris)per.get(C.mesh).push(ix[t],ix[t+1],ix[t+2]);}
          palm.traverse(o=>{if(!o.isMesh||!o.geometry.index)return;o.geometry.setIndex(per.get(o)||[]);o.geometry.computeBoundingBox();o.geometry.computeBoundingSphere();});}}
      const bb=new THREE.Box3().setFromObject(palm);const c=bb.getCenter(new THREE.Vector3());
      // anchor = the trunk foot (centroid of the vertices in the lowest 0.6 m), as BraxChampionsPalmTrunk does in the game -
      // the bounding-box centre of a leaning palm is beside its trunk
      {palm.updateMatrixWorld(true);let sx=0,sz=0,n=0;const v=new THREE.Vector3();
       palm.traverse(o=>{if(!o.isMesh)return;const p=o.geometry.attributes.position,ix=o.geometry.index?o.geometry.index.array:null;
         const used=ix?new Set(ix):null;for(let i=0;i<p.count;i++){if(used&&!used.has(i))continue;v.fromBufferAttribute(p,i).applyMatrix4(o.matrixWorld);
           if(v.y<bb.min.y+.6){sx+=v.x;sz+=v.z;n++;}}});
       const bc=bb.getCenter(new THREE.Vector3());if(trunkFoot){c.x=trunkFoot[0];c.z=trunkFoot[1];}else if(n){c.x=sx/n;c.z=sz/n;}
       console.log('palm foot n='+n+' foot='+c.x.toFixed(2)+','+c.z.toFixed(2)+' bboxCentre='+bc.x.toFixed(2)+','+bc.z.toFixed(2)+' size='+(bb.max.x-bb.min.x).toFixed(2)+'x'+(bb.max.z-bb.min.z).toFixed(2)+' minY='+bb.min.y.toFixed(2));}
      const g=new THREE.Group();palm.position.set(-c.x,-bb.min.y,-c.z);g.add(palm);g.scale.setScalar(PALM_SCALE);
      console.log('palm model height '+(bb.max.y-bb.min.y).toFixed(2));
      for(const p of scene.palms){const o=g.clone(true);o.position.set(p[0],p[1],p[2]);o.rotation.y=p[0]*.7+p[2]*.3;if(p.length>3&&p[3]>0&&p[3]!==1)o.scale.multiplyScalar(p[3]);S.add(o);}}
  }
  // City palms: the game places its own palm model pack; the preview draws a plain stand-in (trunk + fronds) there.
  // Off unless PREVIEW_PALMS=1: a stand-in is not what the game shows (owner 10.11: "żadnych sztucznych drzew").
  if(PALMS&&scene.palms&&scene.palms.length){
    const tm=new THREE.MeshStandardMaterial({color:S2L([.42,.33,.24]),roughness:.9});cityAmbient(tm);
    const fm=new THREE.MeshStandardMaterial({color:S2L([.20,.42,.16]),roughness:.8,side:THREE.DoubleSide});cityAmbient(fm);
    const palm=new THREE.Group();const trunk=new THREE.Mesh(new THREE.CylinderGeometry(.16,.24,7.2,10),tm);trunk.position.y=3.6;palm.add(trunk);
    for(let i=0;i<11;i++){const f=new THREE.Mesh(new THREE.PlaneGeometry(.7,3.4),fm);const a=i*Math.PI*2/11;
      f.position.set(Math.cos(a)*1.35,7.0-(i%2)*.25,Math.sin(a)*1.35);f.rotation.set(0,-a+Math.PI/2,0);f.rotateX(-1.05-(i%2)*.2);palm.add(f);}
    for(const p of scene.palms){const o=palm.clone(true);o.position.set(p[0],p[1],p[2]);o.rotation.y=p[0]*.7+p[2]*.3;if(p.length>3&&p[3]>0&&p[3]!==1)o.scale.multiplyScalar(p[3]*11/7.6);S.add(o);}   // [3]: the palm's scale (11 m = 1)
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
  if(window.skyDome)window.skyDome.position.copy(eye);
  // the mirrors within 40 m show the room: in the game one renders live and the others keep the picture they took
  // (BraxChampionsWydajnosc, 12.11: at most 40 copies), so the preview draws up to 24 of the nearest as reflections
  // (a view's "mirrors": n draws fewer: each reflection draws the scene once more)
  const near=window.mirrors.map(r=>[r,r.userData.c.distanceTo(eye)]).sort((a,b)=>a[1]-b[1]);
  near.forEach(([r,d],i)=>{const {c,n}=r.userData;const toEye=eye.clone().sub(c);
    r.visible=i<(v.mirrors??24)&&d<40;const side=toEye.dot(n)>=0?1:-1;r.lookAt(c.clone().add(n.clone().multiplyScalar(side)));});
  R.render(S,C);return R.domElement.toDataURL('image/png');
};
window.ready=true;
</script></body></html>`;
(async () => {
  // big scenes (the gallery with 5 000 goods models) need the page's memory off /dev/shm and a larger JS heap
  const b = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader', '--disable-dev-shm-usage', '--js-flags=--max-old-space-size=6144'] });
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
    else if (u === 'sky/sky.hdr') file = SKY_HDR;
    else if (u.startsWith('pic/')) file = path.join(process.env.PREVIEW_PICTURES || texDir, decodeURIComponent(u.slice(4)));
    if (!file || !fs.existsSync(file)) { if (process.env.PREVIEW_DEBUG404) console.log('404 ' + u); return r.fulfill({ status: 404, body: '' }); }
    const ext = path.extname(file);
    const type = { '.js': 'application/javascript', '.json': 'application/json', '.gltf': 'model/gltf+json', '.jpg': 'image/jpeg', '.png': 'image/png', '.wasm': 'application/wasm' }[ext] || 'application/octet-stream';
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
