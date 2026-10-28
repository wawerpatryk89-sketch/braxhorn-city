import * as T from 'three';
import { mergeGeometries } from '../three/examples/jsm/utils/BufferGeometryUtils.js';
import { LEVEL } from './layout.js';
import {buildCivic} from './civic.js';

// All architecture is scoped to this level; no edits to city or gym modules.
export function buildWorld(scene) {
 const group=new T.Group();group.name='osiedle';scene.add(group);
 const collision=[],walkable=[], batches=new Map();let seed=41;
 const random=()=>{seed=(seed*1664525+1013904223)>>>0;return seed/4294967296;};
 function texture(kind,color){const c=document.createElement('canvas');c.width=c.height=256;const g=c.getContext('2d');g.fillStyle=color;g.fillRect(0,0,256,256);
  for(let i=0;i<13000;i++){g.fillStyle=`rgba(${random()>.5?'255,255,255':'0,0,0'},${random()*.035})`;g.fillRect(random()*256,random()*256,kind==='wood'?1:2,kind==='wood'?14:2);}
  if(kind==='stone'){g.strokeStyle='#00000015';g.lineWidth=2;for(let y=0;y<256;y+=64){g.beginPath();g.moveTo(0,y);g.lineTo(256,y);g.stroke();for(let x=(y%128?32:0);x<256;x+=96){g.beginPath();g.moveTo(x,y);g.lineTo(x,y+64);g.stroke();}}}
  if(kind==='wood'){g.strokeStyle='#17110c35';for(let x=0;x<256;x+=32){g.beginPath();g.moveTo(x,0);g.lineTo(x,256);g.stroke();}}
  const t=new T.CanvasTexture(c);t.wrapS=t.wrapT=T.RepeatWrapping;t.colorSpace=T.SRGBColorSpace;t.anisotropy=4;return t;
 }
 const material=(color,roughness=.9,map)=>new T.MeshStandardMaterial({color,roughness,metalness:0,map});
 const M={plaster:material(0xffffff,.95,texture('noise','#deded0')),stone:material(0xffffff,.95,texture('stone','#a4a293')),wood:material(0xffffff,.8,texture('wood','#8a6545')),asphalt:material(0xffffff,1,texture('noise','#484b48')),paving:material(0xffffff,.95,texture('stone','#b3b2a5')),grass:material(0xffffff,1,texture('noise','#65714a')),dark:material(0x252c2b),cream:material(0xbbb9a7),fabric:material(0x9b9e8d),black:material(0x141b1a),green:material(0x638847),leaf:material(0x485f38),trunk:material(0x665342),line:material(0xc2bc9d),water:material(0x517d7c,.55),white:material(0xd7d7c9),terra:material(0x7f6754)};
 function roundedBox(w,h,d,r){
 const g=new T.BoxGeometry(w,h,d,4,4,4),p=g.attributes.position,n=g.attributes.normal;
 const half=new T.Vector3(w/2-r,h/2-r,d/2-r),v=new T.Vector3(),core=new T.Vector3(),dir=new T.Vector3();
 for(let i=0;i<p.count;i++){v.fromBufferAttribute(p,i);core.set(T.MathUtils.clamp(v.x,-half.x,half.x),T.MathUtils.clamp(v.y,-half.y,half.y),T.MathUtils.clamp(v.z,-half.z,half.z));dir.copy(v).sub(core).normalize();v.copy(core).addScaledVector(dir,r);p.setXYZ(i,v.x,v.y,v.z);n.setXYZ(i,dir.x,dir.y,dir.z);}return g;
 }
 function box(x,y,z,w,h,d,mat,solid=true){const soft=[M.fabric,M.cream].includes(mat)&&w<6&&h<2&&d<6&&Math.min(w,h,d)>.1;
 const geo=soft?roundedBox(w,h,d,Math.min(.075,Math.min(w,h,d)*.2)):new T.BoxGeometry(w,h,d);
 if(mat.map){const uv=geo.attributes.uv,pos=geo.attributes.position,norm=geo.attributes.normal;const scale=mat===M.wood?2:4;
 for(let i=0;i<uv.count;i++){const nx=Math.abs(norm.getX(i)),ny=Math.abs(norm.getY(i));uv.setXY(i,(nx>.5?pos.getZ(i):pos.getX(i))/scale,(ny>.5?pos.getZ(i):pos.getY(i))/scale);}}
 geo.translate(x,y,z);if(!batches.has(mat))batches.set(mat,[]);batches.get(mat).push(geo);if(solid)collision.push({minX:x-w/2,maxX:x+w/2,minZ:z-d/2,maxZ:z+d/2,minY:y-h/2,maxY:y+h/2});return geo;}
 function mesh(geo,mat,x,y,z){const o=new T.Mesh(geo,mat);o.position.set(x,y,z);o.castShadow=o.receiveShadow=true;group.add(o);return o;}
 function sign(text,x,y,z,width=3,rotation=0){const c=document.createElement('canvas');c.width=1024;c.height=192;const g=c.getContext('2d');g.fillStyle='#27332d';g.fillRect(0,0,1024,192);g.fillStyle='#c7d2b9';g.font='600 65px sans-serif';g.textAlign='center';g.textBaseline='middle';g.fillText(text,512,96,960);const tx=new T.CanvasTexture(c);tx.colorSpace=T.SRGBColorSpace;const m=mesh(new T.PlaneGeometry(width,width*192/1024),new T.MeshStandardMaterial({map:tx,roughness:1}),x,y,z);m.rotation.y=rotation;return m;}
 function hedge(x,z,w,d){box(x,.6,z,w,1.2,d,M.leaf);box(x,.1,z,w+.2,.2,d+.2,M.stone);}
 function tree(x,z,h=6){mesh(new T.CylinderGeometry(.14,.24,h,7),M.trunk,x,h/2,z);collision.push({minX:x-.25,maxX:x+.25,minZ:z-.25,maxZ:z+.25,minY:0,maxY:h});for(let i=0;i<3;i++)mesh(new T.IcosahedronGeometry(h*.32,1),i%2?M.green:M.leaf,x+(i-1)*.65,h*.77+(i%2)*.9,z+(i%2)*.7);}
 function fence(x,z,w,d){box(x,.12,z,w,.24,d,M.stone);box(x,1.15,z,w,.13,d,M.dark);const along=w>d,n=Math.ceil(Math.max(w,d)/.24);for(let i=0;i<=n;i++)box(along?x-w/2+i*w/n:x,.69,along?z:z-d/2+i*d/n,along?.07:w,1.1,along?d:.07,M.dark,false);collision.push({minX:x-w/2,maxX:x+w/2,minZ:z-d/2,maxZ:z+d/2,minY:0,maxY:1.2});}
 function bench(x,z){box(x,.48,z,2,.14,.55,M.wood);box(x,.92,z-.25,2,.55,.1,M.wood);for(const dx of [-.7,.7])box(x+dx,.24,z,.12,.48,.46,M.dark);}
 // Continuous walkable district and roads, with deliberate physical boundaries.
 box(0,-.16,-1,232,.3,204,M.grass,false);walkable.push({x0:-116,x1:116,z0:-103,z1:101,y:0});
 box(0,.012,37,9,.025,122,M.asphalt,false);box(0,.014,-19,91,.03,8,M.asphalt,false);
 for(const x of [-6,6])box(x,.035,37,2.8,.07,122,M.paving,false);
 for(const z of [-25,-13])box(0,.035,z,94,.07,3.3,M.paving,false);
 for(const x of [-43,43]){box(x,.012,10,5,.025,53,M.asphalt,false);box(x+(x<0?-4:4),.025,10,2,.05,53,M.paving,false);}
 box(0,.012,39,91,.025,5,M.asphalt,false);
 for(let z=-10;z<54;z+=7)box(0,.031,z,.12,.014,3,M.line,false);
 for(const z of [-10,33])for(let x=-3;x<=3;x++)box(x,.034,z,.55,.02,2.4,M.white,false);
 for(const x of [-4.6,4.6])box(x,.07,37,.18,.14,122,M.stone,false);
 // Edges: actual walls/hedges, no open-looking invisible barriers.
 hedge(-114,-1,1.3,200);hedge(114,-1,1.3,200);hedge(0,-102,228,1.3);
 hedge(-60,98,108,1.3);hedge(60,98,108,1.3);fence(0,98,12,.18);sign('CENTRUM · WRÓĆ DO MIASTA',0,2.2,97.8,6,Math.PI);
 for(const x of [-52,52])for(let z=-56;z<60;z+=14)tree(x,z,5.5+random());
 for(const h of LEVEL.houses){const x=h.x,z=h.z;
  box(x,.02,z,23,.04,21,M.grass,false);box(x<0?-12:12,.04,z,13,.08,2.4,M.paving,false);
  const halfDepth=h.z===0?12:10;fence(x,z-halfDepth,23,.12);fence(x,z+halfDepth,23,.12);
  const fx=x<0?x+11.5:x-11.5;const fenceOffset=(halfDepth+1.8)/2;fence(fx,z-fenceOffset,.12,halfDepth-1.8);fence(fx,z+fenceOffset,.12,halfDepth-1.8);
  box(x<0?fx+1:fx-1,.8,z-2.1,.45,1.6,.4,M.stone);sign(h.id.slice(-2),x<0?fx+1.24:fx-1.24,1.22,z-2.1,.42,x<0?Math.PI/2:-Math.PI/2);
  tree(x+(x<0?-8:8),z+7,4.7);bench(x+(x<0?6:-6),z+7);
 }
 // Connections around villa and garage court.
 box(-30,.04,-35,25,.08,22,M.paving,false);box(0,.04,-28,18,.08,8,M.paving,false);
 box(30,.04,-35,16,.08,22,M.paving,false);box(43,.02,-43,5,.04,33,M.asphalt,false);
 fence(43,-58,6,.15);sign('LAS · TEREN ZAMKNIĘTY',43,2,-57.8,5);
 bench(-18,-12);bench(18,-12);tree(-18,-29,6);tree(18,-29,6);
 // Purpose-built villa: one accessible storey, open front portal, garden exit.
 const V=LEVEL.villa, X=V.x,Z=V.z;
 const b=(x,y,z,w,h,d,m,solid=true)=>box(X+x,y,Z+z,w,h,d,m,solid);
 b(0,.03,0,24,.06,16,M.paving,false);
 b(0,3.5,0,25,.3,17,M.plaster,false);b(0,3.22,0,24,.1,16,M.cream,false);
 // front entry clear width 2.6m; rear opening to terrace clear width 3m
 // Broad recessed windows keep the entrance readable from the street.
 for(const x of [-6.65,6.65]){
  b(x,.38,8,10.7,.76,.3,M.stone);b(x,3.02,8,10.7,.56,.3,M.plaster);
  for(const dx of [-5.15,5.15])b(x+dx,1.75,8,.4,2.05,.3,M.plaster);
 }
 b(0,2.95,8,2.6,.7,.3,M.plaster);
 b(-6.75,1.65,-8,10.5,3.3,.3,M.plaster);b(6.75,1.65,-8,10.5,3.3,.3,M.plaster);b(0,2.95,-8,3,.7,.3,M.plaster);
 // side walls have broad glazed windows (physical, matte shaded glass).
 const glass=new T.MeshStandardMaterial({color:0x9baea8,roughness:.85,transparent:true,opacity:.23,depthWrite:false});
 for(const x of [-6.65,6.65]){
  b(x,1.75,8,9.9,1.98,.08,glass);
  for(const dx of [-4.95,0,4.95])b(x+dx,1.75,8,.085,2.05,.18,M.dark);
  for(const y of [.76,2.74])b(x,y,8,10,.085,.18,M.dark);
 }
 for(const x of [-12,12]){b(x,.4,0,.3,.8,16,M.stone);b(x,3.05,0,.3,.5,16,M.plaster);for(const z of [-7.8,-2,3,7.8])b(x,1.8,z,.3,2,.14,M.dark);b(x,1.8,0,.12,2,15.5,glass);}
 // Partial room walls; openings connect all rooms on one level.
 b(4,1.65,4.6,.18,3.3,6.5,M.plaster);b(4,1.65,-5.3,.18,3.3,5.2,M.plaster);b(4,2.95,-.6,.18,.7,4.2,M.plaster);
 // Bedroom / bathroom partition with a genuine 2m doorway.
 b(5.1,1.65,-1.8,2.2,3.3,.18,M.plaster);b(10.1,1.65,-1.8,3.8,3.3,.18,M.plaster);b(7.2,2.95,-1.8,2,.7,.18,M.plaster);
 // Entrance accents, slatted timber wall and clear passage.
 b(-3.2,1.45,8.18,2.5,2.9,.12,M.wood);b(3.2,1.45,8.18,2.5,2.9,.12,M.wood);
 sign('WIŚNIOWA 4 · WAWER',X+5.9,1.85,Z+8.2,3.6);
 b(0,2.72,9.2,5,.15,2.5,M.dark,false);
 // Lounge: rug, generous sofa, armchair, coffee table, media wall.
 b(-5,.074,2,8,.025,6,M.fabric,false);
 b(-6,.45,3.7,5,.7,1.2,M.cream);b(-6,.95,4.2,5,.6,.25,M.fabric);
 for(const x of [-8.35,-3.65])b(x,.75,3.7,.3,.7,1.3,M.fabric);
 for(let x=-7.5;x<-3.5;x+=1.5)b(x,.84,3.65,1.35,.14,.88,M.fabric);
 b(-8.7,.45,1.8,1.3,.7,2.6,M.cream);b(-9.2,.96,1.8,.24,.6,2.7,M.fabric);
 b(-5,.37,.8,2.3,.12,1.2,M.wood);for(const x of [-5.9,-4.1])b(x,.18,.8,.12,.36,.9,M.dark);
 b(-6,.28,-1.6,5,.5,.5,M.wood);b(-6,1.55,-1.73,3.3,1.85,.09,M.black); // matte television, off
 // Kitchen & dining, left rear.
 b(-8.5,.5,-6.8,5.8,1,.65,M.dark);b(-8.5,1.04,-6.8,6,.08,.8,M.stone);
 for(let x=-10.8;x<-6;x+=1.15)b(x,.52,-6.43,.03,.8,.03,M.cream,false);
 b(-10.9,1.2,-5.5,1.2,2.4,1.1,M.dark);b(-6,1.85,-7.6,3.3,.8,.5,M.wood);
 b(-6.5,.5,-4.8,3.7,1,1.2,M.wood);b(-6.5,1.06,-4.8,3.9,.12,1.4,M.stone);
 b(-2.3,.8,-4.5,2.8,.12,1.5,M.wood);for(const x of [-3.3,-1.3])for(const z of [-5.7,-3.3]){b(x,.47,z,.58,.12,.58,M.cream);b(x,.82,z+(z<-4?-.24:.24),.58,.65,.1,M.fabric);for(const dx of [-.2,.2])b(x+dx,.23,z,.055,.46,.4,M.dark);}
 // Bedroom, not a fake exterior shell.
 b(8,.3,3,3,.55,4,M.wood);b(8,.65,3,2.9,.24,3.9,M.cream);b(8,.83,3.5,2.95,.12,2.8,M.fabric);b(8,1.1,1,3.2,1.6,.16,M.wood);
 for(const x of [5.7,10.3]){b(x,.35,1.4,.8,.7,.8,M.wood);b(x,.85,1.4,.15,.3,.15,M.dark);b(x,1.12,1.4,.42,.3,.42,M.cream,false);}
 b(10.9,1.4,6.2,1.4,2.8,2.8,M.wood);sign('BRAXHORN',X+8,2.4,Z+7.8,2.4,Math.PI);
 // Bathroom: stone tub, vanity and separate toilet.
 b(9,.45,-6.4,4,.9,1.8,M.stone);b(9,.91,-6.4,3.5,.04,1.35,M.cream,false);
 b(5.3,.45,-5,1.5,.9,2,M.wood);b(5.3,.95,-5,1.7,.1,2.1,M.stone);b(4.15,1.8,-5,.05,1.2,2,M.dark,false);
 b(10.7,.3,-3.3,.7,.6,.95,M.cream);b(10.7,.8,-3.8,.7,.9,.3,M.cream);
 // Finishing details are geometry, with no emissive lighting.
 for(const x of [7.25,8.75])b(x,.91,1.8,1.2,.22,.7,M.white,false);
 for(const x of [5.7,10.3])mesh(new T.CylinderGeometry(.28,.34,.35,20),M.cream,X+x,1.12,Z+1.4);
 // Sink, hob and mixer; all remain outside the circulation paths.
 b(-8.8,1.095,-6.8,1.15,.035,.5,M.dark,false);b(-8.8,1.11,-6.8,.91,.015,.32,M.stone,false);
 b(-8.8,1.31,-7,.05,.42,.05,M.dark,false);b(-8.8,1.51,-6.88,.05,.05,.29,M.dark,false);
 b(-7.2,1.13,-4.8,1.25,.035,.85,M.black,false);
 for(const x of [-7.5,-6.9])for(const z of [-5,-4.6])mesh(new T.CylinderGeometry(.16,.16,.018,20),M.dark,X+x,1.16,Z+z);
 for(let i=0;i<5;i++)b(-7.5+i*.21,.62,-1.55,.17,.22+random()*.13,.3,i%2?M.green:M.cream,false);
 b(-4.7,.46,.8,.55,.045,.4,M.cream,false);b(-4.7,.49,.8,.48,.025,.34,M.dark,false);
 // Framed graphic print and a reading corner.
 b(-11.77,1.7,5.7,.08,1.3,1.6,M.dark,false);b(-11.71,1.7,5.7,.025,1.1,1.4,M.cream,false);
 b(-11.68,1.8,5.5,.025,.6,.45,M.green,false);
 for(const [x,z]of [[-10,6],[2,-6]]){
  mesh(new T.CylinderGeometry(.34,.25,.6,16),M.terra,X+x,.36,Z+z);
  for(let i=0;i<5;i++){const leaf=mesh(new T.SphereGeometry(.35,8,6),M.leaf,X+x+Math.sin(i*2)*.22,1+i*.12,Z+z+Math.cos(i*2)*.22);leaf.scale.set(.5,1.6,.8);}
 }
 b(0,.09,8.9,2.1,.035,1.2,M.dark,false);
 for(const x of [-1.32,1.32])b(x,1.32,8,.08,2.64,.38,M.dark,false);
 // Terrace and garden: flat walkable deck, planters, sheltered seating.
 b(0,.03,-12,25,.06,8,M.wood,false);b(-8,.45,-12,4,.7,1.2,M.fabric);b(6,.4,-12,2.3,.16,1.4,M.stone);b(6,.2,-12,.7,.4,.7,M.dark);
 for(const x of [-11,11]){b(x,.5,-14,1.2,1,1.2,M.terra);mesh(new T.IcosahedronGeometry(.85,1),M.leaf,X+x,1.5,Z-14);}
 hedge(0,-58,31,1);hedge(-16,-45,1,25);hedge(16,-45,1,25);
 // A second genuinely accessible house: contact's home and garden route.
 // Imported houses keep their original geometry; this interior is built separately.
 const c=(x,y,z,w,h,d,m,solid=true)=>box(30+x,y,-37+z,w,h,d,m,solid);
 c(0,.04,0,10,.08,9,M.wood,false);c(0,3.1,0,10.8,.25,9.8,M.dark,false);
 for(const z of [-4.5,4.5]){c(-3,1.45,z,4,2.9,.24,M.plaster);c(3,1.45,z,4,2.9,.24,M.plaster);c(0,2.65,z,2,.5,.24,M.plaster);}
 for(const x of [-5,5]){c(x,.45,0,.24,.9,9,M.stone);c(x,2.75,0,.24,.3,9,M.plaster);for(const z of [-4.4,0,4.4])c(x,1.75,z,.24,1.8,.14,M.dark);c(x,1.75,0,.08,1.8,8.8,glass);}
 c(-3,.5,-3.7,3,1,.7,M.wood);c(-3,1.04,-3.7,3.1,.08,.85,M.stone);
 c(-3,.42,2,2.8,.7,1.1,M.fabric);c(-3,.87,2.48,2.8,.6,.18,M.fabric);
 c(-2.7,.4,.1,1.5,.1,.9,M.wood);c(-2.7,.2,.1,.6,.4,.5,M.dark);
 c(3,.32,-1.7,2.2,.5,3.3,M.wood);c(3,.66,-1.7,2.15,.2,3.25,M.cream);c(3,.83,-2.6,1.7,.2,.6,M.white,false);
 c(1.4,1.25,-2.2,.15,2.5,4.5,M.wood);c(4,1.3,3,1.5,2.6,1,M.wood);
 sign('POZNAŃSKA 5',33,2,-32.32,2.3);box(30,.04,-28,3,.08,9,M.paving,false);
 box(30,.04,-46.5,3,.08,10,M.paving,false);
 // Community space with clear fight area, future actors are not loaded.
 box(30,.04,-53,11,.08,8,M.stone,false);bench(34,-56);sign('POZNAŃSKA · GARAŻE',-26,2.2,-24.8,4);
 for(const z of [8,29])for(const x of [-8,8]){box(x,2.2,z,.12,4.4,.12,M.dark);box(x,4.4,z,.7,.16,.5,M.dark,false);}
 function flush(){for(const [mat,geos]of batches){const geo=mergeGeometries(geos);const m=new T.Mesh(geo,mat);m.castShadow=m.receiveShadow=true;group.add(m);for(const g of geos)g.dispose();}batches.clear();}
 const buildings=buildCivic({box,mesh,sign,bench,tree,fence,M,walkable,collision});
 flush();return {group,collision,walkable,sign,buildings,materials:M};
}
