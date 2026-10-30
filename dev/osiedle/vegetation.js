import * as T from 'three';
import {mergeGeometries} from '../three/examples/jsm/utils/BufferGeometryUtils.js';
import {LEVEL} from './layout.js';

// Source foliage rendered as shared instances. Collision follows trunks/rocks,
// never the canopy bounding box, so paths remain walkable under the leaves.
export async function addVegetation(scene,loader,collision,extra=[]){
 if(!Array.isArray(extra)||extra.some(p=>!['tree','shrub','rock'].includes(p.kind)||!Number.isFinite(p.x)||!Number.isFinite(p.z)))throw Error('Invalid source foliage request');
 const result=[];
 for(const kind of ['tree','shrub','rock']){
  const gltf=await loader.loadAsync(`./assets/park_${kind}.glb`),root=gltf.scene;
  root.updateMatrixWorld(true);
  const bounds=new T.Box3().setFromObject(root),size=bounds.getSize(new T.Vector3()),anchor=bounds.getCenter(new T.Vector3());anchor.y=bounds.min.y;
  if(kind==='tree')root.traverse(o=>{if(o.isMesh&&o.name.includes('jakaranda')){const b=new T.Box3().setFromObject(o);anchor.x=(b.min.x+b.max.x)/2;anchor.z=(b.min.z+b.max.z)/2;}});
  const groups=new Map();
  root.traverse(o=>{if(!o.isMesh)return;const mat=o.material;if(Array.isArray(mat))throw new Error('Vegetation exporter must split materials');
   if(mat.emissive)mat.emissive.set(0);mat.roughness=Math.max(.8,mat.roughness);mat.metalness=0;mat.envMapIntensity=0;
   if(kind!=='rock'&&(mat.transparent||mat.alphaTest>0)){mat.transparent=false;mat.alphaTest=.42;mat.side=T.DoubleSide;mat.depthWrite=true;}
   let g=o.geometry.clone().applyMatrix4(o.matrixWorld);g.translate(-anchor.x,-anchor.y,-anchor.z);if(g.index)g=g.toNonIndexed();
   if(!groups.has(mat))groups.set(mat,[]);groups.get(mat).push(g);
  });
  const places=[...LEVEL.vegetation,...extra].filter(p=>p.kind===kind),dummy=new T.Object3D();
  for(const [mat,geos]of groups){const geo=mergeGeometries(geos),mesh=new T.InstancedMesh(geo,mat,places.length);mesh.name='Verdant_'+kind;mesh.castShadow=mesh.receiveShadow=true;
   places.forEach((p,i)=>{dummy.position.set(p.x,p.y??.07,p.z);dummy.rotation.y=p.rotation||0;dummy.scale.setScalar(p.height/size.y);dummy.updateMatrix();mesh.setMatrixAt(i,dummy.matrix);});mesh.instanceMatrix.needsUpdate=true;mesh.computeBoundingSphere();scene.add(mesh);for(const g of geos)g.dispose();
  }
  for(const p of places){const r=kind==='tree'?.22:kind==='rock'?p.height*.75:0;if(r)collision.push({minX:p.x-r,maxX:p.x+r,minZ:p.z-r,maxZ:p.z+r,minY:p.y??0,maxY:(p.y??0)+(kind==='tree'?3:p.height)});}
  result.push({kind,count:places.length,source:`park_${kind}.glb`});
 }
 return result;
}
