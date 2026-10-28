import * as T from 'three';
import {cityURL} from '../shared/district-travel.js';
import {GLTFLoader} from '../three/examples/jsm/loaders/GLTFLoader.js';
import {KTX2Loader} from '../three/examples/jsm/loaders/KTX2Loader.js';
import {MeshoptDecoder} from '../three/examples/jsm/libs/meshopt_decoder.module.js';
import {openDoorway} from './doors.js';
import {buildWorld} from './world.js';
import {addVegetation} from './vegetation.js';
import {LEVEL} from './layout.js';
import {mergeGeometries} from '../three/examples/jsm/utils/BufferGeometryUtils.js';
const $=id=>document.getElementById(id);
const renderer=new T.WebGLRenderer({canvas:$('scene'),antialias:true});renderer.setPixelRatio(Math.min(devicePixelRatio,1.75));renderer.setSize(innerWidth,innerHeight);renderer.outputColorSpace=T.SRGBColorSpace;renderer.toneMapping=T.ACESFilmicToneMapping;renderer.toneMappingExposure=1.05;renderer.shadowMap.enabled=true;renderer.shadowMap.type=T.PCFSoftShadowMap;
const scene=new T.Scene();scene.background=new T.Color('#bdccd0');scene.fog=new T.Fog('#bdccd0',160,330);
scene.add(new T.AmbientLight(0xdce5e0,.7));scene.add(new T.HemisphereLight(0xe9f1ed,0x827963,2));const sun=new T.DirectionalLight(0xfff0d2,2.6);sun.position.set(-55,110,45);sun.castShadow=true;sun.shadow.mapSize.set(2048,2048);Object.assign(sun.shadow.camera,{left:-135,right:135,top:135,bottom:-135,near:1,far:260});sun.shadow.normalBias=.035;sun.shadow.bias=-.0002;scene.add(sun);sun.target.position.set(0,0,-5);scene.add(sun.target);
const camera=new T.PerspectiveCamera(65,innerWidth/innerHeight,.08,400);camera.rotation.order='YXZ';camera.position.set(...LEVEL.spawn);let yaw=0,pitch=0,active=false,paused=false;
if(new URLSearchParams(location.search).get('from')==='city')camera.position.set(0,1.78,92);
const world=buildWorld(scene),imported=[],assetErrors=[];
const ktx=new KTX2Loader().setTranscoderPath('../assets/basis/').detectSupport(renderer);const loader=new GLTFLoader().setKTX2Loader(ktx).setMeshoptDecoder(MeshoptDecoder);
let importedReady=0;
const modelCache=new Map();
async function loadModel(name,x,z,width,rotation=0){
 if(!modelCache.has(name))modelCache.set(name,await loader.loadAsync('./assets/'+name+'.glb'));const root=modelCache.get(name).scene.clone(true);root.name=name;root.rotation.y=rotation;root.updateMatrixWorld(true);let b=new T.Box3().setFromObject(root);let s=b.getSize(new T.Vector3());root.scale.multiplyScalar(width/s.x);root.updateMatrixWorld(true);b.setFromObject(root);const c=b.getCenter(new T.Vector3());root.position.set(x-c.x,.08-b.min.y,z-c.z);
 root.traverse(o=>{if(!o.isMesh)return;o.castShadow=o.receiveShadow=true;for(const m of Array.isArray(o.material)?o.material:[o.material]){if(m.emissive)m.emissive.set(0);if('emissiveIntensity'in m)m.emissiveIntensity=0;if('metalness'in m)m.metalness=Math.min(m.metalness,.12);if('roughness'in m)m.roughness=Math.max(m.roughness,.65);if('transmission'in m&&m.transmission>0){m.transmission=0;m.transparent=true;m.opacity=.22;m.depthWrite=false;}m.envMapIntensity=0;}});
 // Cut actual door openings in the supplied closed interior walls.
 if(name==='gameready_casino_scene')openDoorway(root,{minX:-90.7,maxX:-88.1,minY:.15,maxY:3.3,minZ:-36.2,maxZ:-34.5});
 // The Hubs model's elevated entrance corridor has sub-human clearance at
 // this scale. Keep its complete dance hall/stage and connect our own foyer.
 if(name==='nightclub')openDoorway(root,{minX:-96,maxX:-76,minY:-1,maxY:8,minZ:15.8,maxZ:30});
 if(name==='policestation'){
  // Move complete small prop clusters from the new entrance into the foyer.
  // The source separates chair seats/legs into individual meshes, so transform
  // every component in the same spatial selection by the same world offset.
  root.updateMatrixWorld(true);const move=[];
  root.traverse(o=>{if(!o.isMesh)return;const b=new T.Box3().setFromObject(o),s=b.getSize(new T.Vector3());if(s.x<2.2&&s.z<2.2&&s.y<1.6&&b.max.y<1.7&&b.max.x>85.2&&b.min.x<86.8&&b.max.z>-37.2&&b.min.z<-34.1)move.push(o);});
  for(const o of move){const pos=o.getWorldPosition(new T.Vector3()).add(new T.Vector3(-12,0,5));o.position.copy(o.parent.worldToLocal(pos));}
  root.updateMatrixWorld(true);
 }
 if(name==='policestation')openDoorway(root,{minX:84.7,maxX:87.3,minY:.12,maxY:2.7,minZ:-34.3,maxZ:-32.4});
 // Retain small original mesh bounds for collision queries; batch rendering by material.
 root.updateMatrixWorld(true);
 let meshCount=0;root.traverse(o=>{if(o.isMesh)meshCount++;});
 if(meshCount>30){
  const original=root.clone(true);original.updateMatrixWorld(true);root.userData.collisionRoot=original;
  const batches=new Map(), grouped=[];
  root.traverse(o=>{
   if(!o.isMesh)return;
   const g=o.geometry.clone();
   // Quantized GLTF attributes must be decoded before baking world transforms.
   for(const [key,attr]of Object.entries(g.attributes))if(attr.normalized||!(attr.array instanceof Float32Array)){
    const values=new Float32Array(attr.count*attr.itemSize),get=['getX','getY','getZ','getW'];
    for(let i=0;i<attr.count;i++)for(let j=0;j<attr.itemSize;j++)values[i*attr.itemSize+j]=attr[get[j]](i);
    g.setAttribute(key,new T.BufferAttribute(values,attr.itemSize));
   }
   g.applyMatrix4(o.matrixWorld);
   if(Array.isArray(o.material)){grouped.push(new T.Mesh(g,o.material));return;}
   const geo=g.index?g.toNonIndexed():g,key=o.material.uuid+Object.keys(geo.attributes).sort().join();
   if(!batches.has(key))batches.set(key,{mat:o.material,geos:[]});batches.get(key).geos.push(geo);
  });
  root.clear();root.position.set(0,0,0);root.rotation.set(0,0,0);root.scale.set(1,1,1);
  for(const {mat,geos}of batches.values()){const m=new T.Mesh(mergeGeometries(geos),mat);m.castShadow=m.receiveShadow=true;root.add(m);for(const g of geos)g.dispose();}
  for(const m of grouped){m.castShadow=m.receiveShadow=true;root.add(m);}

 }
 scene.add(root);root.updateMatrixWorld(true);root.userData.collisionBounds=new T.Box3().setFromObject(root);root.userData.collisionMeshes=[];(root.userData.collisionRoot||root).traverse(o=>{if(o.isMesh)root.userData.collisionMeshes.push(o);});imported.push(root);importedReady++;$('progress').value=importedReady;return root;
}
const ready=async()=>{
 // Sequential loading bounds peak decoding memory and keeps progress accurate.
 for(const h of LEVEL.houses){$('status').textContent='Przygotowuję dom '+h.id.slice(-2)+'…';try{await loadModel(h.model,h.x,h.z,h.width,h.rotation);}catch(e){assetErrors.push(h.model+': '+e.message);}}
 $('status').textContent='Przygotowuję garaże…';try{await loadModel('garaze_sowieckie',-35,-39,23,Math.PI/2);}catch(e){assetErrors.push('garaze: '+e.message);}
 for(const [name,x,z,w,rot]of [
  ['gameready_casino_scene',-86,-43,34,Math.PI/2],
  ['policestation',86,-43,31,0],
  ['nightclub',-86,14,12.6,Math.PI],
  ['blackjack_table',-99,-31,3.1,0],
  ['roulette_table',-73,-31,3.8,0],
  ['stylized_poker_table_-_game_asset',-99,-54,4,0],
 ]){ $('status').textContent='Przygotowuję '+name+'…';try{await loadModel(name,x,z,w,rot);}catch(e){assetErrors.push(name+': '+e.message);} }
 for(const x of [75,97])for(const z of [6,16]){try{await loadModel('hospital_bed',x,z,1.15,0);}catch(e){assetErrors.push('łóżko: '+e.message);}}
 for(const x of [93,97,101]){try{await loadModel('hospital_chair',x,25,.65,Math.PI);}catch(e){assetErrors.push('krzesło: '+e.message);}}
 try{await loadModel('hospital_wheelchair',102,23,.75,-Math.PI/2);}catch(e){assetErrors.push('wózek: '+e.message);}
 $('status').textContent='Sadzę roślinność z paczki lasu…';
 try{window.__os.vegetation=await addVegetation(scene,loader,world.collision);}catch(e){assetErrors.push('las: '+e.message);}
 scene.updateMatrixWorld(true);renderer.compile(scene,camera);renderer.render(scene,camera);renderer.shadowMap.autoUpdate=false;
 if(assetErrors.length){$('status').textContent='Nie udało się załadować części modeli. Odśwież stronę.';console.error(assetErrors);return;}
 $('status').textContent='Osiedle · architektura i areny';$('enter').hidden=false;$('progress').hidden=true;window.__os.ready=true;
};
const keys=new Set(),joy={x:0,y:0},ray=new T.Raycaster(),down=new T.Vector3(0,-1,0);let drag=null,joyPointer=null;
function setCamera(){camera.rotation.set(pitch,yaw,0,'YXZ');}
function blocked(x,z,y=camera.position.y-1.7){const r=.26;return world.collision.some(b=>x+r>b.minX&&x-r<b.maxX&&z+r>b.minZ&&z-r<b.maxZ&&y+1.65>b.minY&&y+.2<b.maxY);}
function nearbyModels(x,z,padding=1){return imported.filter(o=>{const b=o.userData.collisionBounds;return x>=b.min.x-padding&&x<=b.max.x+padding&&z>=b.min.z-padding&&z<=b.max.z+padding;});}
// Radius-aware swept checks prevent clipping door jambs, even on a slow frame.
function importedBlocked(from,to){
 const d=new T.Vector3(to.x-from.x,0,to.z-from.z),len=d.length();if(len<.00001)return false;d.divideScalar(len);
 const candidates=nearbyModels(from.x,from.z,len+.3);if(!candidates.length)return false;
 const side=new T.Vector3(-d.z,0,d.x);
 for(const offset of [-.23,0,.23])for(const height of [.42,1,1.58]){
  ray.set(new T.Vector3(from.x+side.x*offset,from.y-1.7+height,from.z+side.z*offset),d);ray.near=0;ray.far=len+.26;
  if(ray.intersectObjects(candidates.flatMap(o=>o.userData.collisionMeshes),false).length)return true;
 }return false;
}
function step(dx,dz){
 const current=camera.position,count=Math.max(1,Math.ceil(Math.hypot(dx,dz)/.12));
 for(let i=0;i<count;i++){
  for(const [ax,az]of [[dx/count,0],[0,dz/count]]){
   const x=current.x+ax,z=current.z+az;
   if(!blocked(x,z)&&!importedBlocked(current,new T.Vector3(x,current.y,z)))current.set(x,current.y,z);
  }
  ray.set(new T.Vector3(current.x,current.y-1.7+.38,current.z),down);ray.near=0;ray.far=.9;
  const floor=ray.intersectObjects(nearbyModels(current.x,current.z,.1).flatMap(o=>o.userData.collisionMeshes),false).find(h=>h.face&&h.face.normal.clone().transformDirection(h.object.matrixWorld).y>.55);
  let floorY=floor&&floor.point.y>=-.1?floor.point.y:.08;
  for(const f of world.walkable){const inside=f.radius?Math.hypot(current.x-f.x,current.z-f.z)<=f.radius:current.x>=f.x0&&current.x<=f.x1&&current.z>=f.z0&&current.z<=f.z1;if(inside&&f.y<=current.y-1.7+.4)floorY=Math.max(floorY,f.y);}
  current.y=T.MathUtils.damp(current.y,floorY+1.7,18,1/60);
 }
}

function updateLocation(){const p=camera.position;let title='Ulica Wiśniowa',sub='Zwiedzanie osiedla';if(Math.abs(p.x)<12&&p.z<-30&&p.z>-46){title='Willa Wawera';sub=p.x>4?(p.z<-39.8?'Łazienka':'Sypialnia'):(p.z<-40?'Kuchnia i jadalnia':'Salon');}else if(Math.abs(p.x)<16&&p.z<=-46){title='Willa Wawera';sub='Taras i ogród';}else if(p.x<-24&&p.z<-24){title='Zaplecze garaży';sub='Miejsce przyszłego zlecenia';}else if(p.x>25&&p.x<35&&p.z<-32&&p.z>-42){title='Dom kontaktowy';sub='Poznańska 5';}else if(p.x>35&&p.z<-35){title='Droga do lasu';sub='Dalsza plansza — zamknięta';}else if(p.x>17&&Math.abs(p.z)<10){title='Dom kontaktowy';sub='Wiśniowa';}if(p.z<-66){title=p.x<-8?'Boisko':'Park Wiśniowy';sub='Alejki · odpoczynek · ćwiczenia';}
 const facility=LEVEL.facilities.find(f=>Math.abs(p.x-f.x)<f.width/2+3&&Math.abs(p.z-f.z)<f.depth/2+4);if(facility){title=facility.label;sub=['boxing','arena'].includes(facility.id)?'Arena · przygotowane miejsce przyszłych walk':'Zwiedzanie · wnętrze i zaplecze';}else if(Math.abs(p.z-39)<6){title='Ulica Bydgoska';}else if(Math.abs(p.z+19)<6){title='Ulica Poznańska';}
 $('action').hidden=!(Math.abs(p.x)<7&&p.z>90);
 $('place').replaceChildren(document.createTextNode(title),Object.assign(document.createElement('span'),{textContent:sub}));}
function resetInput(){keys.clear();joy.x=joy.y=0;joyPointer=null;drag=null;$('joystick').firstElementChild.style.transform='';}
$('enter').onclick=()=>{active=true;$('loading').hidden=true;};
function menu(open){paused=open;$('map').hidden=!open;resetInput();}
const returnToCity=()=>{resetInput();paused=true;window.location.assign(cityURL());};
$('action').textContent='WRÓĆ DO MIASTA';$('action').onclick=returnToCity;
$('return-city').onclick=returnToCity;
$('menu').onclick=()=>menu(true);$('close').onclick=()=>menu(false);
let high=true;$('quality').onclick=()=>{high=!high;renderer.setPixelRatio(high?Math.min(devicePixelRatio,1.75):1);$('quality').textContent='Jakość: '+(high?'wysoka':'standardowa');renderer.shadowMap.needsUpdate=true;};
const destinations=[['Park',20,-83,0],['Boisko',-28,-72,0],['Wejście',0,52,0],['Przed willą',0,-24,0],['Salon',0,-33,.65],['Taras',0,-51,Math.PI],['Garaże',-24,-25,0],['Dom kontaktowy',30,-28,0]];
for(const f of LEVEL.facilities)destinations.push([f.label,f.x,f.z+19,0]);
for(const [label,x,z,a]of destinations){const btn=document.createElement('button');btn.textContent=label;btn.onclick=()=>{camera.position.set(x,1.78,z);yaw=a;pitch=0;setCamera();menu(false);};$('destinations').append(btn);}
addEventListener('keydown',e=>{if(['ArrowUp','ArrowDown','ArrowLeft','ArrowRight',' '].includes(e.key))e.preventDefault();keys.add(e.key.toLowerCase());if(e.key==='Escape')menu(!paused);});addEventListener('keyup',e=>keys.delete(e.key.toLowerCase()));addEventListener('blur',resetInput);document.addEventListener('visibilitychange',()=>{resetInput();});
$('scene').addEventListener('pointerdown',e=>{if(!active||paused)return;drag={id:e.pointerId,x:e.clientX,y:e.clientY};$('scene').setPointerCapture(e.pointerId);});
$('scene').addEventListener('pointermove',e=>{if(drag?.id!==e.pointerId)return;yaw-=(e.clientX-drag.x)*.003;pitch=T.MathUtils.clamp(pitch-(e.clientY-drag.y)*.003,-1.15,1.15);drag.x=e.clientX;drag.y=e.clientY;setCamera();});
for(const name of ['pointerup','pointercancel'])$('scene').addEventListener(name,()=>drag=null);
$('joystick').addEventListener('pointerdown',e=>{if(!active||paused)return;joyPointer=e.pointerId;$('joystick').setPointerCapture(e.pointerId);updateJoy(e);});
function updateJoy(e){if(e.pointerId!==joyPointer)return;const b=$('joystick').getBoundingClientRect();let x=(e.clientX-b.left-b.width/2)/38,y=(e.clientY-b.top-b.height/2)/38;const n=Math.max(1,Math.hypot(x,y));joy.x=x/n;joy.y=y/n;$('joystick').firstElementChild.style.transform=`translate(${joy.x*32}px,${joy.y*32}px)`;}
$('joystick').addEventListener('pointermove',updateJoy);for(const name of ['pointerup','pointercancel'])$('joystick').addEventListener(name,()=>{joyPointer=null;joy.x=joy.y=0;$('joystick').firstElementChild.style.transform='';});
addEventListener('resize',()=>{camera.aspect=innerWidth/innerHeight;camera.updateProjectionMatrix();renderer.setSize(innerWidth,innerHeight);});
let last=performance.now(),frame=0;function tick(now){requestAnimationFrame(tick);const dt=Math.min((now-last)/1000,.2);last=now;if(active&&!paused&&!document.hidden){let f=(keys.has('w')||keys.has('arrowup')?1:0)-(keys.has('s')||keys.has('arrowdown')?1:0)-joy.y;let r=(keys.has('d')||keys.has('arrowright')?1:0)-(keys.has('a')||keys.has('arrowleft')?1:0)+joy.x;const norm=Math.max(1,Math.hypot(f,r));f/=norm;r/=norm;const speed=(keys.has('shift')?5.2:3.2)*dt;if(f||r)step((-Math.sin(yaw)*f+Math.cos(yaw)*r)*speed,(-Math.cos(yaw)*f-Math.sin(yaw)*r)*speed);if(frame++%15===0)updateLocation();}if(window.__os.ready&&active&&!paused)renderer.render(scene,camera);}
window.__os={ready:false,THREE:T,ray,LEVEL,scene,camera,renderer,world,imported,assetErrors,blocked,step,teleport(x,z,a=0,p=0,y=1.78){camera.position.set(x,y,z);yaw=a;pitch=p;setCamera();updateLocation();},start(){active=true;$('loading').hidden=true;},pause(v){paused=v;},stats(){return {calls:renderer.info.render.calls,triangles:renderer.info.render.triangles,geometries:renderer.info.memory.geometries,textures:renderer.info.memory.textures,assets:importedReady,errors:assetErrors,position:camera.position.toArray(),yaw};}};
setCamera();requestAnimationFrame(tick);ready().catch(e=>{$('status').textContent='Błąd uruchomienia: '+e.message;console.error(e);});
