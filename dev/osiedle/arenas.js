import * as T from 'three';
import {mergeGeometries} from '../three/examples/jsm/utils/BufferGeometryUtils.js';
import {ringCanvas,apronSkirt} from '../src/textures.js';

export function arenaMaterials(logos){
 const matte=color=>new T.MeshStandardMaterial({color,roughness:.92,metalness:0});
 const canvas=ringCanvas('Arial, sans-serif',logos.badge.image,'#454a4b');
 const skirt=apronSkirt('Arial, sans-serif');skirt.repeat.set(1,1);
 const c=document.createElement('canvas');c.width=c.height=256;const g=c.getContext('2d');g.fillStyle='#292e2c';g.fillRect(0,0,256,256);
 for(let i=0;i<2600;i++){g.fillStyle=i%7?'#343a35':'#536343';g.fillRect((i*73)%256,(i*127+Math.floor(i/256)*19)%256,1,1);}
 g.strokeStyle='#181d1a';g.lineWidth=3;g.strokeRect(1,1,254,254);
 const rubber=new T.CanvasTexture(c);rubber.colorSpace=T.SRGBColorSpace;rubber.wrapS=rubber.wrapT=T.RepeatWrapping;rubber.repeat.set(24,22);rubber.anisotropy=4;
 const wireCanvas=document.createElement('canvas');wireCanvas.width=wireCanvas.height=64;const w=wireCanvas.getContext('2d');w.strokeStyle='#7e8780';w.lineWidth=2;w.beginPath();w.moveTo(0,32);w.lineTo(32,0);w.lineTo(64,32);w.lineTo(32,64);w.closePath();w.stroke();
 const wireTex=new T.CanvasTexture(wireCanvas);wireTex.wrapS=wireTex.wrapT=T.RepeatWrapping;wireTex.repeat.set(13,6);wireTex.anisotropy=4;
 return {black:matte(0x202622),steel:matte(0x515b54),green:matte(0x638c42),rope:matte(0xb8bcae),
  top:new T.MeshStandardMaterial({map:canvas,roughness:1}),skirt:new T.MeshStandardMaterial({map:skirt,roughness:1}),
  rubber:new T.MeshStandardMaterial({map:rubber,roughness:1}),logo:new T.MeshStandardMaterial({map:logos.wide,transparent:true,alphaTest:.1,roughness:1}),
  wire:new T.MeshStandardMaterial({map:wireTex,alphaTest:.06,alphaToCoverage:true,side:T.DoubleSide,roughness:1}),logoAspect:logos.wide.image.height/logos.wide.image.width};
}

export function buildArena(f,{mesh,box,collision,walkable,sign,arena:M}){
 const {x,z,id}=f,boxing=id==='boxing',cy=z-1,height=boxing?.84:.6;
 const batches=new Map();
 function add(geo,mat){if(!batches.has(mat))batches.set(mat,[]);batches.get(mat).push(geo);}
 function tube(a,b,r,mat,sag=0){
  const mid=a.clone().add(b).multiplyScalar(.5);mid.y-=sag;
  add(new T.TubeGeometry(new T.QuadraticBezierCurve3(a,mid,b),sag?18:1,r,7,false),mat);
 }
 const P=(dx,y,dz)=>new T.Vector3(x+dx,y,cy+dz);
 const panel=(width,h,mat,px,py,pz,rot=0)=>{const m=mesh(new T.PlaneGeometry(width,h),mat,px,py,pz);m.rotation.y=rot;return m;};
 const floor=mesh(new T.PlaneGeometry(25,23),M.rubber,x,.061,cy);floor.rotation.x=-Math.PI/2;
 if(boxing){
  box(x,height/2,cy,10.6,height,10.6,M.black);
  const top=mesh(new T.PlaneGeometry(10.56,10.56),M.top,x,height+.008,cy);top.rotation.x=-Math.PI/2;top.name='boxing-branded-canvas';
  walkable.push({x0:x-5.3,x1:x+5.3,z0:cy-5.3,z1:cy+5.3,y:height+.01});
  for(let side=0;side<4;side++){const a=side*Math.PI/2;panel(10.6,height-.05,M.skirt,x+Math.sin(a)*5.31,height/2,cy+Math.cos(a)*5.31,a);}
  for(const sx of [-1,1])for(const sz of [-1,1]){
   tube(P(sx*4.4,height,sz*4.4),P(sx*4.4,height+1.55,sz*4.4),.09,M.steel);
   tube(P(sx*4.28,height+.32,sz*4.28),P(sx*4.28,height+1.43,sz*4.28),.16,sx===sz?M.green:M.black);
   collision.push({minX:x+sx*4.4-.2,maxX:x+sx*4.4+.2,minZ:cy+sz*4.4-.2,maxZ:cy+sz*4.4+.2,minY:height,maxY:height+1.55});
  }
  for(const rh of [.38,.72,1.06,1.4]){
   const y=height+rh,mat=rh===1.4?M.green:M.rope;
   for(const sx of [-1,1])tube(P(sx*4.3,y,-4.3),P(sx*4.3,y,4.3),.034,mat,.055);
   tube(P(-4.3,y,-4.3),P(4.3,y,-4.3),.034,mat,.055);
   tube(P(-4.3,y,4.3),P(-.95,y,4.3),.034,mat,.035);tube(P(.95,y,4.3),P(4.3,y,4.3),.034,mat,.035);
  }
  for(const sx of [-1,1])for(const zz of [-2,2])tube(P(sx*4.3,height+.38,zz),P(sx*4.3,height+1.4,zz),.018,M.black);
  for(const xx of [-2,2])for(const sz of [-1,1])tube(P(xx,height+.38,sz*4.3),P(xx,height+1.4,sz*4.3),.018,M.black);
  for(const dx of [-.95,.95])tube(P(dx,height,4.3),P(dx,height+1.4,4.3),.06,M.black);
  for(const [xa,xb,za,zb]of [[-4.4,-4.2,-4.4,4.4],[4.2,4.4,-4.4,4.4],[-4.4,4.4,-4.4,-4.2],[-4.4,-.95,4.2,4.4],[.95,4.4,4.2,4.4]])collision.push({minX:x+xa,maxX:x+xb,minZ:cy+za,maxZ:cy+zb,minY:height+.22,maxY:height+1.5});
 }else{
  const r=5.5,ap=r*Math.cos(Math.PI/8),len=2*r*Math.sin(Math.PI/8);
  const base=mesh(new T.CylinderGeometry(r,r,height,8),M.black,x,height/2,cy);base.rotation.y=Math.PI/8;
  // Plane UVs give the canvas its proper circular brand mark without pinching.
  const shape=new T.Shape();for(let i=0;i<8;i++){const a=Math.PI/8+i*Math.PI/4;const xx=r*Math.cos(a),zz=r*Math.sin(a);i?shape.lineTo(xx,zz):shape.moveTo(xx,zz);}shape.closePath();
  const geo=new T.ShapeGeometry(shape),uv=geo.attributes.uv,pos=geo.attributes.position;
  for(let i=0;i<uv.count;i++)uv.setXY(i,pos.getX(i)/(2*r)+.5,pos.getY(i)/(2*r)+.5);
  geo.rotateX(-Math.PI/2);mesh(geo,M.top,x,height+.008,cy).name='arena-branded-canvas';
  walkable.push({x,z:cy,radius:r-.4,y:height+.01});
  for(let i=0;i<8;i++){
   const a=i*Math.PI/4,px=x+ap*Math.cos(a),pz=cy+ap*Math.sin(a),rot=Math.PI/2-a;
   panel(len,height-.04,M.skirt,px,height/2,pz,rot);
   const va=a+Math.PI/8;const post=P(r*Math.cos(va),height,r*Math.sin(va));
   tube(post,post.clone().add(new T.Vector3(0,1.92,0)),.13,i%2?M.black:M.green);
   for(let j=0;j<12;j++){const t=(j/11-.5)*len,xx=px-Math.sin(a)*t,zz=pz+Math.cos(a)*t;collision.push({minX:xx-.16,maxX:xx+.16,minZ:zz-.16,maxZ:zz+.16,minY:0,maxY:height});}
   if(i===2)continue;
   panel(len-.1,1.8,M.wire,px,height+.95,pz,rot);
   for(const yy of [height+.08,height+1.86]){
    const edge=new T.Vector3(-Math.sin(a),0,Math.cos(a)).multiplyScalar(len/2);
    tube(new T.Vector3(px,yy,pz).sub(edge),new T.Vector3(px,yy,pz).add(edge),.075,M.black);
   }
   for(let j=0;j<12;j++){const t=(j/11-.5)*len,xx=px-Math.sin(a)*t,zz=pz+Math.cos(a)*t;collision.push({minX:xx-.16,maxX:xx+.16,minZ:zz-.16,maxZ:zz+.16,minY:height,maxY:height+1.9});}
  }
 }
 // Three real, low-rise steps meet the apron, with non-slip painted edges.
 const edge=boxing?5.3:5.5*Math.cos(Math.PI/8);
 for(let i=0;i<3;i++){
  const y=height*(i+1)/3,zz=cy+edge+(2-i)*.6+.3;
  box(x,y/2,zz,2,y,.6,M.steel);box(x,y+.008,zz+.24,1.94,.016,.06,M.green,false);
  walkable.push({x0:x-1,x1:x+1,z0:zz-.3,z1:zz+.3,y:y+.016});
 }
 const aspect=M.logoAspect;
 panel(11,11*aspect,M.logo,x,4.6,z-14.72);
 for(const side of [-1,1]){
  const a=side>0?-Math.PI/2:Math.PI/2;
  panel(7,7*aspect,M.logo,x+side*19.7,3.7,z-4,a);
 }
 for(const [mat,geos]of batches){mesh(mergeGeometries(geos),mat,0,0,0);for(const g of geos)g.dispose();}
 return {id,x,z:cy,height,logo:true};
}
