import * as T from 'three';
import {GLTFLoader} from '../three/examples/jsm/loaders/GLTFLoader.js';
import {KTX2Loader} from '../three/examples/jsm/loaders/KTX2Loader.js';
import {MeshoptDecoder} from '../three/examples/jsm/libs/meshopt_decoder.module.js';
import {openDoorway} from './doors.js';
import {buildWorld} from './world.js';
import {addVegetation} from './vegetation.js';
import {LEVEL} from './layout.js';
import {mergeGeometries} from '../three/examples/jsm/utils/BufferGeometryUtils.js';

export async function loadEstate(scene,renderer,progress=()=>{}) {
let vegetation;
const world=buildWorld(scene),imported=[],assetErrors=[],surfaceSources=new Map(),surfaceAudit=[];
// Reuse ONLY maps from delivered real house models; never fabricate a PBR map.
const sourceSurfaceNames={
 plaster:['stucco_indoor','stucco_yellow_outdoor','stucco_dark'],
 wood:['Parquetlight_1K','MI_Wooden_Planks_ulyobjsn_2K'],
 stone:['Concrete_obruby','MI_Marble_Floor_th5jfjufw_4K']
};
function collectSourceSurfaces(root,source){
 root.traverse(o=>{if(!o.isMesh)return;for(const m of Array.isArray(o.material)?o.material:[o.material]){
  if(!m||!m.map)return;
  const name=m.name.replace(/\\s*\\(Instance\\)$/,'');
  for(const [role,names]of Object.entries(sourceSurfaceNames))if(names.includes(name)){
   const current=surfaceSources.get(role),rank=names.indexOf(name);
   if(!current||rank<current.rank)surfaceSources.set(role,{name,source,map:m.map,normalMap:m.normalMap||null,roughnessMap:m.roughnessMap||null,rank});
  }
 }});
}
function applySourceSurfaces(){
 for(const [role,names]of Object.entries(sourceSurfaceNames)){
  const found=surfaceSources.get(role),target=world.materials[role];
  if(!found){surfaceAudit.push({role,status:'BLOCKED_NO_VERIFIED_SOURCE_MAP',candidates:names});continue;}
  const repeat=t=>{if(!t)return null;const c=t.clone();c.wrapS=c.wrapT=T.RepeatWrapping;c.needsUpdate=true;return c;};
  target.map=repeat(found.map);target.normalMap=repeat(found.normalMap);target.roughnessMap=repeat(found.roughnessMap);
  target.color.set(0xffffff);target.roughness=role==='wood'?.78:.92;target.metalness=0;target.needsUpdate=true;
  surfaceAudit.push({role,status:'SOURCE_TEXTURE_APPLIED',source:found.source,material:found.name,normal:!!found.normalMap});
 }
}

const ktx=new KTX2Loader().setTranscoderPath('../assets/basis/').detectSupport(renderer);const loader=new GLTFLoader().setKTX2Loader(ktx).setMeshoptDecoder(MeshoptDecoder);
let importedReady=0;
const modelCache=new Map();
async function loadModel(name,x,z,width,rotation=0){
 if(!modelCache.has(name))modelCache.set(name,await loader.loadAsync('./assets/'+name+'.glb'));const root=modelCache.get(name).scene.clone(true);root.name=name;root.rotation.y=rotation;root.updateMatrixWorld(true);let b=new T.Box3().setFromObject(root);let s=b.getSize(new T.Vector3());root.scale.multiplyScalar(width/s.x);root.updateMatrixWorld(true);b.setFromObject(root);const c=b.getCenter(new T.Vector3());root.position.set(x-c.x,.08-b.min.y,z-c.z);
 collectSourceSurfaces(root,name);
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
 scene.add(root);root.updateMatrixWorld(true);root.userData.collisionBounds=new T.Box3().setFromObject(root);root.userData.collisionMeshes=[];(root.userData.collisionRoot||root).traverse(o=>{if(o.isMesh)root.userData.collisionMeshes.push(o);});imported.push(root);importedReady++;progress('Przygotowuję osiedle…',importedReady);return root;
}
const ready=async()=>{
 // Sequential loading bounds peak decoding memory and keeps progress accurate.
 for(const h of LEVEL.houses){progress('Przygotowuję dom '+h.id.slice(-2)+'…',importedReady);try{await loadModel(h.model,h.x,h.z,h.width,h.rotation);}catch(e){assetErrors.push(h.model+': '+e.message);}}
 applySourceSurfaces();
 progress('Przygotowuję garaże…',importedReady);try{await loadModel('garaze_sowieckie',-35,-39,23,Math.PI/2);}catch(e){assetErrors.push('garaze: '+e.message);}
 for(const [name,x,z,w,rot]of [
  ['gameready_casino_scene',-86,-43,34,Math.PI/2],
  ['policestation',86,-43,31,0],
  ['nightclub',-86,14,12.6,Math.PI],
  ['blackjack_table',-99,-31,3.1,0],
  ['roulette_table',-73,-31,3.8,0],
  ['stylized_poker_table_-_game_asset',-99,-54,4,0],
 ]){ progress('Przygotowuję '+name+'…',importedReady);try{await loadModel(name,x,z,w,rot);}catch(e){assetErrors.push(name+': '+e.message);} }
 for(const x of [75,97])for(const z of [6,16]){try{await loadModel('hospital_bed',x,z,1.15,0);}catch(e){assetErrors.push('łóżko: '+e.message);}}
 for(const x of [93,97,101]){try{await loadModel('hospital_chair',x,25,.65,Math.PI);}catch(e){assetErrors.push('krzesło: '+e.message);}}
 try{await loadModel('hospital_wheelchair',102,23,.75,-Math.PI/2);}catch(e){assetErrors.push('wózek: '+e.message);}
 // The supplied police office floor is ~0.49m above the foyer. The old
 // spectator camera crossed it without feet; the Fighter needs real steps.
 const stepMaterial=new T.MeshStandardMaterial({color:0x70695a,roughness:1,metalness:0});
 for(const [z0,z1,y]of [[-34.9,-34.1,.17],[-36.5,-34.9,.34]]){
  const step=new T.Mesh(new T.BoxGeometry(2.4,y,z1-z0),stepMaterial);
  step.position.set(86,y/2,(z0+z1)/2);step.castShadow=step.receiveShadow=true;
  world.group.add(step);
  world.collision.push({minX:84.8,maxX:87.2,minZ:z0,maxZ:z1,minY:0,maxY:y});
  world.walkable.push({x0:84.8,x1:87.2,z0,z1,y});
 }
 progress('Sadzę roślinność z paczki lasu…',importedReady);
 try{vegetation=await addVegetation(scene,loader,world.collision,world.foliageRequests);}catch(e){assetErrors.push('las: '+e.message);}
 scene.updateMatrixWorld(true);
 if(assetErrors.length)throw new Error(assetErrors.join('; '));
 return {world,imported,assetErrors,vegetation,surfaceAudit};
};
return ready();
}
