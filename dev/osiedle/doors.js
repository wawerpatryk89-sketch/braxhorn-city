import * as T from 'three';

// Boolean subtraction of a doorway from game-ready interior meshes. Attributes
// are interpolated, so the original UVs remain continuous right up to each jamb.
// The modified mesh is used for BOTH rendering and collision.
export function openDoorway(root,bounds){
 root.updateMatrixWorld(true);
 const planes=[['x',bounds.minX,-1],['x',bounds.maxX,1],['y',bounds.minY,-1],['y',bounds.maxY,1],['z',bounds.minZ,-1],['z',bounds.maxZ,1]];
 const v=new T.Vector3();
 root.traverse(o=>{
  if(!o.isMesh)return;
  const box=new T.Box3().setFromObject(o),cut=new T.Box3(new T.Vector3(bounds.minX,bounds.minY,bounds.minZ),new T.Vector3(bounds.maxX,bounds.maxY,bounds.maxZ));
  if(!box.intersectsBox(cut))return;
  const src=o.geometry.index?o.geometry.toNonIndexed():o.geometry,keys=Object.keys(src.attributes),data=Object.fromEntries(keys.map(k=>[k,[]])),groups=[];
  const get=['getX','getY','getZ','getW'];let count=0;
  const interpolate=(a,b,t)=>Object.fromEntries(Object.keys(a).map(k=>[k,a[k].map((n,i)=>n+(b[k][i]-n)*t)]));
  function split(poly,axis,at,sign){
   const inside=[],outside=[],k={x:0,y:1,z:2}[axis];
   for(let i=0;i<poly.length;i++){
    const a=poly[i],b=poly[(i+1)%poly.length],da=(a.world[k]-at)*sign,db=(b.world[k]-at)*sign;
    (da<=0?inside:outside).push(a);
    if((da<0&&db>0)||(da>0&&db<0)){const p=interpolate(a,b,da/(da-db));inside.push(p);outside.push(p);}
   }return [inside,outside];
  }
  function emit(poly,mat){
   for(let n=1;n<poly.length-1;n++)for(const p of [poly[0],poly[n],poly[n+1]]){for(const key of keys)data[key].push(...p[key]);count++;}
   const last=groups.at(-1);if(last?.materialIndex===mat)last.count=count-last.start;else groups.push({start:last?last.start+last.count:0,count:count-(last?last.start+last.count:0),materialIndex:mat});
  }
  for(let i=0;i<src.attributes.position.count;i+=3){
   const triangle=[];
   for(let j=0;j<3;j++){const p={};for(const key of keys){const a=src.attributes[key];p[key]=Array.from({length:a.itemSize},(_,n)=>a[get[n]](i+j));}v.fromArray(p.position).applyMatrix4(o.matrixWorld);p.world=v.toArray();triangle.push(p);}
   const mat=src.groups.find(g=>i>=g.start&&i<g.start+g.count)?.materialIndex||0;
   let poly=triangle;
   for(const [axis,at,sign]of planes){if(poly.length<3)break;const [inside,outside]=split(poly,axis,at,sign);if(outside.length>=3)emit(outside,mat);poly=inside;}
  }
  const geo=new T.BufferGeometry();for(const key of keys)geo.setAttribute(key,new T.Float32BufferAttribute(data[key],src.attributes[key].itemSize));
  for(const g of groups)if(g.count)geo.addGroup(g.start,g.count,g.materialIndex);
  geo.computeBoundingBox();geo.computeBoundingSphere();o.geometry=geo;
 });
}
