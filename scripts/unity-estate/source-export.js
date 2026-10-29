// READ-ONLY source capture. We invoke the exact existing Three.js builders.
// Do not simplify the procedural world, colors, details, doors or geometry.
import * as T from '/dev/three/build/three.module.js';
import {GLTFExporter} from '/dev/three/examples/jsm/exporters/GLTFExporter.js';
import {buildWorld} from '/dev/osiedle/world.js';
import {LEVEL} from '/dev/osiedle/layout.js';
import {EstateDoors} from '/dev/osiedle/entrances.js';

const state=window.__estateExport={ready:false,error:null,manifest:null,download:null};

async function init(){
 const loader=new T.TextureLoader();
 const [badge,wide]=await Promise.all([
  loader.loadAsync('/public/assets/logo/badge.webp'),
  loader.loadAsync('/public/assets/logo/wide_a.webp')
 ]);
 for(const tex of [badge,wide]){tex.colorSpace=T.SRGBColorSpace;tex.anisotropy=4;}
 const scene=new T.Scene();
 const world=buildWorld(scene,{badge,wide});
 // Identical two real entrance steps added in original level.js after the model import.
 // Static source geometry is captured, not a designed approximation.
 const stepMaterial=new T.MeshStandardMaterial({color:0x70695a,roughness:1,metalness:0});
 for(const [z0,z1,y] of [[-34.9,-34.1,.17],[-36.5,-34.9,.34]]){
  const step=new T.Mesh(new T.BoxGeometry(2.4,y,z1-z0),stepMaterial);
  step.position.set(86,y/2,(z0+z1)/2);step.castShadow=step.receiveShadow=true;
  step.name='SourcePoliceStep_'+z0;world.group.add(step);
  world.collision.push({minX:84.8,maxX:87.2,minZ:z0,maxZ:z1,minY:0,maxY:y});
  world.walkable.push({x0:84.8,x1:87.2,z0,z1,y});
 }
 const doors=new EstateDoors(scene,[]);
 const doorDefs=doors.items.map(d=>{
  d.leaves.forEach((leaf,i)=>{leaf.pivot.name='HINGE_'+d.id+'_'+i;});
  return {id:d.id,label:d.label,x:d.x,z:d.z,yaw:d.yaw,base:d.base,width:d.width,
   height:d.height,single:!!d.single,glass:!!d.glass,wood:!!d.wood,
   initialSwing:d.swing,leafCount:d.leaves.length};
 });
 // Only procedural geometry and source-built doors are exported here.
 // Imported artist GLBs remain separate ORIGINAL binary assets for glTFast.
 const exportScene=new T.Scene();
 const houseWorld=world.group.clone(true);houseWorld.name='EstateProceduralOriginal';exportScene.add(houseWorld);
 const fixed=scene.getObjectByName('estate-door-frames');
 if(!fixed)throw new Error('Source door frames missing');
 exportScene.add(fixed.clone(true));
 for(const d of doors.items)exportScene.add(d.root.clone(true));
 const doorCount=doorDefs.length;
 if(LEVEL.houses.length!==6||LEVEL.facilities.length!==6||doorCount!==24)
  throw new Error('Source layout changed; migration manifest requires review');
 const gltfExporter=new GLTFExporter();
 state.manifest={
  schema:'braxhorn-estate-source-1',
  provenance:'dev/osiedle/layout.js,world.js,civic.js,arenas.js,entrances.js,level.js',
  coordinateRule:'Three.js (x,y,z) -> Unity (x,y,-z); yaw sign reversed',
  level:{id:LEVEL.id,version:LEVEL.version,bounds:LEVEL.bounds,spawn:LEVEL.spawn,
   streets:LEVEL.streets,villa:LEVEL.villa,houses:LEVEL.houses,facilities:LEVEL.facilities,
   points:LEVEL.points,arenas:LEVEL.arenas,missionSockets:LEVEL.missionSockets},
  collision:world.collision,walkable:world.walkable,doors:doorDefs,
  vegetation:LEVEL.vegetation,doorCount,sourceModelInstancesExpected:21,
  importedMeshesInThisGLB:false,originalPlayerInThisGLB:false,
  visualAcceptance:false
 };
 state.download=async()=>{
  const buf=await gltfExporter.parseAsync(exportScene,{
   binary:true,onlyVisible:true,embedImages:true,includeCustomExtensions:false,maxTextureSize:2048
  });
  if(!(buf instanceof ArrayBuffer)||buf.byteLength<1024)throw new Error('GLB export failed/empty');
  const url=URL.createObjectURL(new Blob([buf],{type:'model/gltf-binary'}));
  const link=document.createElement('a');link.href=url;link.download='EstateProceduralAndDoors.glb';
  document.body.appendChild(link);link.click();link.remove();
  setTimeout(()=>URL.revokeObjectURL(url),30000);
 };
 state.ready=true;
}
init().catch(e=>{state.error=String(e&&e.stack||e);console.error('Estate export failed',e);});
