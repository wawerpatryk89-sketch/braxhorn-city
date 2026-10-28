import * as T from 'three';
import {mergeGeometries} from '../three/examples/jsm/utils/BufferGeometryUtils.js';
import {MOVES} from '../src/fighter.js';
import {LEVEL} from './layout.js';

// Existing openings only. Local +Z is the approach; leaves swing into -Z.
export function entranceDefinitions(){
 const defs=[];
 for(const f of LEVEL.facilities)for(const rear of [false,true])defs.push({
  id:f.id+(rear?'-rear':'-front'),label:f.label,x:f.x,z:f.z+(rear?-1:1)*f.depth/2,
  width:4,height:3,base:.06,yaw:rear?Math.PI:0,glass:!rear,
  color:f.id==='hospital'?0xb9c9c5:f.id==='casino'?0x654c37:0x34443e,
 });
 defs.push(
  {id:'villa-front',label:'Wiśniowa 4',x:0,z:-30,width:2.6,height:2.52,base:.08,wood:true},
  {id:'villa-terrace',label:'Taras',x:0,z:-46,width:3,height:2.52,base:.08,yaw:Math.PI,glass:true},
  {id:'contact-front',label:'Poznańska 5',x:30,z:-32.5,width:2,height:2.3,base:.08,wood:true,single:true},
  {id:'contact-garden',label:'Ogród',x:30,z:-41.5,width:2,height:2.3,base:.08,yaw:Math.PI,wood:true,single:true},
  {id:'villa-bathroom',label:'Łazienka',x:7.2,z:-39.8,width:2,height:2.52,base:.08,wood:true,single:true},
  {id:'casino-hall',label:'Sala gier',x:-89.4,z:-34.55,width:2.6,height:3.1,base:.15,color:0x654c37,glass:true},
  {id:'police-office',label:'Komisariat',x:86,z:-32.5,width:2.6,height:2.5,base:.13,color:0x405866,glass:true},
  {id:'club-hall',label:'Sala klubowa',x:-86,z:16.05,width:4.2,height:3,base:.18,color:0x363c39,glass:true},
 );
 for(const side of [-1,1])for(const z of [6.5,16.5])defs.push({
  id:'hospital-room-'+side+'-'+z,label:'Gabinet',x:86+side*3.1,z,width:2,height:2.5,base:.06,
  yaw:side<0?Math.PI/2:-Math.PI/2,color:0xb9c9c5,single:true,
 });
 return defs;
}

export class EstateDoors{
 constructor(scene,opened=[]){
  this.items=[];this.active=new Set();this.material=new T.MeshStandardMaterial({vertexColors:true,roughness:.86,metalness:0});
  this.glass=new T.MeshStandardMaterial({color:0x829a94,roughness:.78,metalness:0,transparent:true,opacity:.28,depthWrite:false});
  const fixed=[];
  const box=(out,x,y,z,w,h,d,color)=>{
   const g=new T.BoxGeometry(w,h,d).translate(x,y,z),c=new T.Color(color),rgb=new Float32Array(g.attributes.position.count*3);
   for(let i=0;i<rgb.length;i+=3)c.toArray(rgb,i);
   g.setAttribute('color',new T.BufferAttribute(rgb,3));out.push(g);
  };
  const merged=(geos,parent)=>{const mesh=new T.Mesh(mergeGeometries(geos),this.material);for(const g of geos)g.dispose();mesh.receiveShadow=true;parent.add(mesh);return mesh;};
  for(const def of entranceDefinitions()){
   const saved=opened.find(v=>(typeof v==='string'?v:v.id)===def.id);
   const d={yaw:0,color:0x34443e,...def,swing:saved?.swing===-1?-1:1,state:'closed',angle:0,target:0,leaves:[]};
   const root=new T.Group();root.name='door-'+d.id;root.position.set(d.x,d.base,d.z);root.rotation.y=d.yaw;scene.add(root);root.updateMatrixWorld(true);d.root=root;
   const frame=[];
   for(const side of [-1,1])box(frame,side*(d.width/2-.05),d.height/2,0,.1,d.height,.2,0x303b36);
   box(frame,0,d.height-.045,0,d.width,.09,.2,0x303b36);
   box(frame,0,.012,0,d.width,.024,.22,0x81867e);
   for(const g of frame){g.applyMatrix4(root.matrixWorld);fixed.push(g);}
   const sides=d.single?[-1]:[-1,1],leafWidth=(d.width-.22)/sides.length;
   for(const side of sides){
    const pivot=new T.Group();pivot.position.x=side*(d.width/2-.11);root.add(pivot);
    const sign=-side,center=sign*leafWidth/2,parts=[],h=d.height-.13,col=d.wood?0x795a3b:d.color;
    if(d.glass){
     for(const xx of [sign*.055,sign*(leafWidth-.055)])box(parts,xx,h/2,0,.11,h,.095,col);
     for(const [y,hh]of [[.23,.46],[h-.055,.11],[1.05,.10]])box(parts,center,y,0,leafWidth,hh,.095,col);
     const pane=new T.Mesh(new T.BoxGeometry(leafWidth-.22,h-.57,.045),this.glass);pane.position.set(center,.46+(h-.57)/2,0);pivot.add(pane);
    }else{
     box(parts,center,h/2,0,leafWidth,h,.095,col);
     for(let j=1;j<6;j++)box(parts,sign*leafWidth*j/6,h/2,.051,.012,h-.24,.008,d.wood?0x4b392a:0x26332e);
     box(parts,center,.12,.057,leafWidth-.12,.17,.018,0x69736a);
    }
    for(const face of [-1,1]){
     const hx=sign*(leafWidth-.17);
     box(parts,hx,1.1,face*.065,.08,.29,.04,0x9b9e91);
     box(parts,hx-sign*.10,1.13,face*.13,.27,.035,.045,0xc1bba5);
     for(const y of [.4,h-.4])box(parts,sign*.025,y,face*.065,.045,.17,.06,0x747e73);
    }
    const mesh=merged(parts,pivot);mesh.name=d.id+'-leaf';mesh.castShadow=false;
    d.leaves.push({pivot,side,width:leafWidth,height:h,center,mesh});
   }
   this.items.push(d);this.setAngle(d,saved?1:0);d.state=d.angle?'open':'closed';d.target=d.angle;
  }
  const frames=merged(fixed,scene);frames.name='estate-door-frames';frames.castShadow=true;
 }
 setAngle(d,a){d.angle=a;for(const l of d.leaves){l.pivot.rotation.y=-l.side*a*Math.PI/2*d.swing;l.pivot.updateWorldMatrix(true,false);}}
 local(d,pos){return pos.clone().sub(d.root.position).applyAxisAngle(new T.Vector3(0,1,0),-d.yaw);}
 nearest(player){
  if(!player?.onGround||player.state!=='loco')return null;
  let best=null,score=Infinity;
  for(const d of this.items){const p=this.local(d,player.pos);
   if(Math.abs(p.y)>.65||Math.abs(p.z)>2.1||Math.abs(p.x)>d.width/2-.18)continue;
   const dist=p.x*p.x+p.z*p.z;if(dist<score){score=dist;best=d;}
  }
  return best;
 }
 label(player){const d=this.nearest(player);return !d?null:d.state==='closed'?'OTWÓRZ':d.state==='open'?'ZAMKNIJ':null;}
 use(player,sound,notify=()=>{}){
  const d=this.nearest(player);if(!d)return false;
  if(d.state==='open'){
   if(this.inPassage(d,player)){notify('Odsuń się od drzwi, żeby je zamknąć.');return true;}
   d.state='closing';d.target=0;this.active.add(d);sound.play('door',{gain:.45,pos:d.root.position});return true;
  }
  if(d.state!=='closed')return true;
  const outside=this.local(d,player.pos).z>=0;
  // Narrow single leaves use a two-way hinge, opening away from the actor.
  // This keeps the small rooms usable without trapping the player in the sweep.
  d.swing=d.single&&!outside?-1:1;
  const push=outside||d.swing===-1,m=push?MOVES.doorPush:MOVES.doorPull;
  player.heading=d.yaw+(outside?Math.PI:0);player.speed=0;player.vel.set(0,0);player.faceTarget=null;player.setState('door');
  d.use={overlay:player.play(m.clip,{rate:m.rate,fade:.2,onEnd:()=>player.backToLoco(.3)}),window:push?[1.25,3.1]:[1.9,3.7]};
  d.state='opening';this.active.add(d);sound.play('door',{gain:.45,pos:d.root.position});return true;
 }
 inPassage(d,player){const p=this.local(d,player.pos);return Math.abs(p.x)<d.width/2+player.radius&&Math.abs(p.z)<player.radius+.18;}
 // Exact rotated-leaf collision, also used to stop a leaf hitting the player.
 leafContact(d,l,pos,r,angle=d.angle){
  const p=this.local(d,pos),a=-l.side*angle*Math.PI/2*d.swing,c=Math.cos(a),s=Math.sin(a);
  const px=p.x-l.pivot.position.x,x=c*px-s*p.z-l.center,z=s*px+c*p.z;
  const half=l.width/2,cx=T.MathUtils.clamp(x,-half,half),cz=T.MathUtils.clamp(z,-.065,.065),dx=x-cx,dz=z-cz,dist=Math.hypot(dx,dz);
  if(dist>=r)return null;
  let rx=x,rz=z;
  if(dist>1e-7){rx=cx+dx/dist*r;rz=cz+dz/dist*r;}
  else{const gaps=[x+half,half-x,z+.065,.065-z],i=gaps.indexOf(Math.min(...gaps));if(i===0)rx=-half-r;else if(i===1)rx=half+r;else if(i===2)rz=-.065-r;else rz=.065+r;}
  return new T.Vector3(c*(rx+l.center)+s*rz+l.pivot.position.x,pos.y-d.base,-s*(rx+l.center)+c*rz).applyAxisAngle(new T.Vector3(0,1,0),d.yaw).add(d.root.position);
 }
 overlaps(d,player,angle){return d.leaves.some(l=>this.leafContact(d,l,player.pos,player.radius+.02,angle));}
 update(dt,player){
  for(const d of this.active){
   if(d.state==='opening'&&d.use){const [a,b]=d.use.window,k=T.MathUtils.clamp((d.use.overlay.t-a)/(b-a),0,1);d.target=k*k*(3-2*k);if(player.state!=='door'){d.target=1;d.use=null;}}
   const next=T.MathUtils.clamp(d.angle+T.MathUtils.clamp(d.target-d.angle,-dt*1.3,dt*1.3),0,1);
   if(next!==d.angle&&this.overlaps(d,player,next)){
    if(d.state==='closing'){d.state='opening';d.target=1;d.use=null;}continue;
   }
   this.setAngle(d,next);
   if(!d.use&&Math.abs(d.angle-d.target)<.0001){d.state=d.target?'open':'closed';this.active.delete(d);}
  }
 }
 resolve(pos,r,y,h){
  let hit=false;
  for(const d of this.items){if(y+h<d.base||y+.36>d.base+d.height||Math.hypot(pos.x-d.x,pos.z-d.z)>d.width+r+.3)continue;
   for(const l of d.leaves){const moved=this.leafContact(d,l,pos,r);if(moved){pos.x=moved.x;pos.z=moved.z;hit=true;}}
  }return hit;
 }
 cameraHit(raycaster){return raycaster.intersectObjects(this.items.flatMap(d=>d.leaves.map(l=>l.mesh)),false)[0];}
 snapshot(){return this.items.filter(d=>d.angle>.02||d.state==='opening').map(d=>({id:d.id,swing:d.swing}));}
}
