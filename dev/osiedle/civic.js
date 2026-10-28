import * as T from 'three';
import {LEVEL} from './layout.js';
import {arenaMaterials,buildArena} from './arenas.js';

// Public buildings and fight venues. Interaction sockets contain no mission logic.
export function buildCivic({box,mesh,sign,bench,tree,fence,M,walkable,collision,logos}) {
 const mat=(color)=>new T.MeshStandardMaterial({color,roughness:.92});
 const brick=mat(0x79594d), ivory=mat(0xd5d6cb), blue=mat(0x405866), burgundy=mat(0x533637);
 const pane=mat(0x738784), steel=mat(0x414a49);
 const buildings=[],arena=arenaMaterials(logos);
 for(const f of LEVEL.facilities){
  const {x,z,width:w,depth:d,id}=f, h=['casino','club','arena','boxing'].includes(id)?7:4.5;
  const skin=id==='hospital'?ivory:id==='police'?M.stone:id==='casino'?brick:M.dark;
  const b=(a,y,c,ww,hh,dd,m=skin,solid=true)=>box(x+a,y,z+c,ww,hh,dd,m,solid);
  // Wide, genuinely open front and rear doors; low floors have no impassable steps.
  b(0,.025,0,w,.05,d,id==='club'?M.dark:M.paving,false);
  b(0,h+.16,0,w+1,.32,d+1,skin,false);
  b(0,h-.06,0,w,.1,d,M.cream,false);
  for(const side of [-1,1]){
   b(side*w/2,h/2,0,.35,h,d);
   for(const dz of [-d/2,d/2]){
    b(side*(w/4+1),h/2,dz,w/2-2,h,.35);
   }
  }
  for(const dz of [-d/2,d/2])b(0,(h+3.1)/2,dz,4,h-3.1,.35);
  // Front plinth, recessed window bays, vertical pilasters and roof coping.
  for(const side of [-1,1])for(const a of [5.5,11,16.5]){
   b(side*a,2.2,d/2+.2,3.8,2.45,.08,pane);
   for(const dx of [-1.95,1.95])b(side*a+dx,2.2,d/2+.28,.10,2.65,.12,steel,false);
   for(const y of [.92,3.48])b(side*a,y,d/2+.28,4,.1,.12,steel,false);
   b(side*a,.7,d/2+.2,4.25,.24,.45,M.stone);
  }
  for(let a=-w/2;a<=w/2;a+=w/8)if(Math.abs(a)>2)b(a,h/2,d/2+.3,.32,h,.45,skin);
  b(0,3.22,d/2+1.6,7,.25,3.5,M.dark,false);
  for(const a of [-3.1,3.1])b(a,1.55,d/2+2.8,.15,3.1,.15,steel);
  sign(f.label.toLocaleUpperCase('pl'),x,4.05,z+d/2+.4,Math.min(13,w-6));
  sign('WEJŚCIE',x,3.6,z+d/2+.35,1.6);
  sign('WYJŚCIE',x,3.4,z-d/2+.24,1.5);
  sign('WYJŚCIE',x,3.4,z+d/2-.24,1.5,Math.PI);
  // Hard landscaping and rear service access connected around both sides.
  b(0,.025,d/2+3,w+4,.05,5,M.paving,false);
  for(const a of [-w/2-2,w/2+2])b(a,.025,0,2.7,.05,d+10,M.paving,false);
  b(0,.025,-d/2-2,w+4,.05,3,M.paving,false);
  for(const a of [-8,8]){bench(x+a,z+d/2+3);b(a,.22,-d/2-3,1.8,.44,1.1,M.stone);}
  for(const a of [-w/2+2,w/2-2]){b(a,.4,d/2+3,1.3,.8,1.3,M.stone);tree(x+a,z+d/2+3,4.4);}
  // HVAC and screened plant on roofs; no luminous materials.
  for(const a of [-12,12]){b(a,h+.55,-6,3.2,.8,2.4,steel,false);for(let j=-1;j<=1;j++)b(a+j,h+1,-6,.15,.04,2,M.black,false);}
  if(id==='casino'||id==='police'){
   const doorX=id==='casino'?x-3.4:x,dz=id==='casino'?-35.2:-32.5,half=id==='casino'?1.38:1.38,height=id==='casino'?3.35:2.75;
   for(const side of [-1,1])box(doorX+side*half,height/2,dz,.16,height,.22,steel);
   box(doorX,height,dz,half*2+.16,.16,.22,steel,false);
  }
  if(id==='casino'){
   // Finish the outside face of the imported room as a proper foyer wall.
   const door=x-3.4,zWall=-34.64,edgeL=x-18,edgeR=x+18;
   for(const [left,right]of [[edgeL,door-1.4],[door+1.4,edgeR]]){
    box((left+right)/2,3.4,zWall,right-left,6.8,.14,ivory);
    box((left+right)/2,.55,zWall+.1,right-left,1.1,.12,M.wood);
   }
   box(door,5.1,zWall,2.8,3.4,.14,ivory);sign('SALA GIER',door,3.8,zWall+.12,3.5);
  }
  if(id==='club'){
   b(-12,.6,10,7,1.2,1.1,M.wood);sign('SZATNIA · WSTĘP',x-12,2.5,z+11,4);
   for(let c=7;c<=11;c+=2)bench(x+12,z+c);
   for(const side of [-1,1])box(x+side*2.2,1.6,16.05,.22,3.2,.3,steel);
   box(x,3.25,16.05,4.6,.2,.3,steel,false);sign('SALA KLUBOWA',x,3.55,16.25,4);
  }
  // Lobby is always clear down its centre; the registration desk sits to one side.
  if(!['casino','club','police'].includes(id)){
   b(-13,.6,10,8,1.2,1.1,M.wood);b(-13,1.24,10,8.2,.08,1.3,M.stone);
   b(-13,1.55,9.8,1,.6,.1,M.black,false);
   sign(id==='hospital'?'REJESTRACJA':'ZAPISY · KASA',x-13,2.3,z+11,4);
  }
  if(id==='hospital'){
   // Reception, waiting area and four treatment bays off a 4m-wide corridor.
   for(const a of [-1,1])for(const c of [-8,2]){
    const bx=a*11;b(bx,1.7,c-4.5,17,3.4,.18,ivory);
    b(a*3.1,1.7,c-2.5,.18,3.4,4,ivory);
    b(a*3.1,1.7,c+3,.18,3.4,3,ivory);
    // Detailed bed mesh is loaded from the supplied hospital pack.
    for(const cc of [c-2.5,c+3])b(a*3.22,.7,cc,.06,1.4,cc===c-2.5?4:3,blue,false);
    sign('GABINET '+(a<0?(c<0?'01':'02'):(c<0?'03':'04')),x+a*3.2,2.6,z+c+.5,1.7,a<0?Math.PI/2:-Math.PI/2);
    b(bx+2,1.3,c-1.1,.65,.5,.1,M.black);b(bx+2,.6,c-1.1,.1,1.2,.1,steel);
    b(bx-2,.5,c-1.2,.8,1,.8,M.white);
   }
   // Three source chairs and wheelchair waiting space are loaded separately.
   sign('GABINETY 01—04',x,3,z+5,3,Math.PI);
  }
  if(['boxing','arena'].includes(id)){
   // Roof structure, wall panelling and spectator seating give the hall a readable scale.
   for(let c=-12;c<=12;c+=6){b(0,6.1,c,w-.8,.24,.25,steel,false);for(const a of [-18,18])b(a,3,c,.24,6,.24,steel);}
   for(const a of [-19.7,19.7])b(a,1,0,.08,2,d-.8,M.stone,false);
   for(const a of [-10,10])for(let c=-5;c<=3;c+=1.6){b(a,.5,c,1,.16,.85,M.dark);b(a,1,c-.38,1,.9,.12,M.dark);b(a,.25,c,.12,.5,.55,steel);}

   // Separate locker bays, with a clear circulation route around the platform.
   for(const a of [-16,16]){
    for(let c=-10;c<=2;c+=1.3){b(a,1,c,1.15,2,1,steel);b(a,1,c+.51,.7,.04,.04,M.white,false);}
    b(a>0?13:-13,.45,-4,.7,.18,8,M.wood);
   }
   for(const c of [-11,-9])for(const a of [-9,9])b(a,.45,c,5,.9,.8,M.stone);
   sign('SZATNIA',x-16,2.7,z+4,2.5);sign('ZAPLECZE',x+16,2.7,z+4,2.5);
   buildArena(f,{mesh,box,collision,walkable,sign,arena});
   for(const a of [-1,1]){b(a*9,.85,9,3,1.7,.7,M.dark);sign('REGULAMIN · WALKI',x+a*9,1.6,z+9.38,2.6);}
  }
  buildings.push({id,entry:[x,z+d/2+2],rear:[x,z-d/2-2]});
 }
 // Three connected boulevards, pedestrian crossings and furniture.
 for(const z of [-19,39,86]){
  box(0,.012,z,224,.025,7,M.asphalt,false);
  for(const off of [-5,5])box(0,.03,z+off,224,.06,2.8,M.paving,false);
  for(let x=-109;x<=109;x+=8)box(x,.032,z,3,.012,.12,M.line,false);
  for(const x of [-58,0,58])for(let a=-2;a<=2;a++)box(x+a,.034,z,.5,.012,2.4,M.white,false);
 }
 for(const x of [-58,58]){box(x,.014,16,6,.028,140,M.asphalt,false);for(const a of [-4.5,4.5])box(x+a,.03,16,2,.06,140,M.paving,false);}
 for(const [text,x,z]of [['WIŚNIOWA',6,82],['BYDGOSKA',-51,36],['POZNAŃSKA',51,-22]]){
  box(x,1.5,z,.09,3,.09,steel);sign(text,x,2.75,z+.08,3.2);sign(text,x,2.75,z-.08,3.2,Math.PI);
 }
 for(const x of [-58,58])for(const z of [-7,16,60,80]){
  box(x-4,2.7,z,.1,5.4,.1,steel);box(x-3.5,5.4,z,1.1,.12,.4,steel,false);
  box(x+4,.45,z,.6,.9,.6,M.dark);box(x+4,.95,z,.7,.1,.7,steel,false);
 }
 // Neighbourhood park: a continuous public route behind the private gardens.
 const court=mat(0x526961), courtKey=mat(0x878675);
 box(0,.018,-83,110,.036,34,M.grass,false);
 for(const z of [-67,-99])box(0,.035,z,116,.07,2.5,M.paving,false);
 for(const x of [-58,58]){box(x,.02,-73,6,.04,38,M.asphalt,false);box(x+(x<0?4.5:-4.5),.03,-83,2,.06,33,M.paving,false);}
 box(0,.035,-83,3,.07,32,M.paving,false);
 box(28,.035,-83,54,.07,2.5,M.paving,false);
 box(-28,.04,-83,28,.08,15,court,false);
 for(const x of [-41.9,-14.1])box(x,.085,-83,.08,.012,14.8,M.white,false);
 for(const z of [-90.4,-75.6])box(-28,.085,z,27.8,.012,.08,M.white,false);
 box(-28,.085,-83,.08,.012,14.8,M.white,false);
 const arc=(cx,cz,r,start,end)=>{const pts=[];for(let i=0;i<=80;i++){const a=start+(end-start)*i/80;pts.push(new T.Vector3(cx+Math.cos(a)*r,.09,cz+Math.sin(a)*r));}const line=new T.Line(new T.BufferGeometry().setFromPoints(pts),new T.LineBasicMaterial({color:0xe0ded1}));mesh(new T.BufferGeometry(),M.white,0,0,0).add(line);};
 arc(-28,-83,1.8,0,Math.PI*2);
 for(const side of [-1,1]){
  const cx=-28+side*12.5;box(cx-side*1.8,.083,-83,4.7,.012,4.9,courtKey,false);
  arc(cx-side*4.4,-83,1.8,-Math.PI/2,Math.PI/2);
  box(cx+side*1.7,1.8,-83,.17,3.6,.17,steel);
  box(cx+side*.7,3.5,-83,2,.15,.15,steel,false);
  box(cx,3.5,-83,.12,1.05,1.8,M.white,false);
  const hoop=mesh(new T.TorusGeometry(.23,.025,8,24),brick,cx-side*.32,3.05,-83);hoop.rotation.x=Math.PI/2;
 }
 fence(-28,-92,32,.1);fence(-28,-74,32,.1);
 for(const x of [-44,-12])for(const z of [-88.5,-77.5])fence(x,z,.1,7);
 sign('BOISKO · BRAXHORN',-28,1.65,-73.85,5);
 // Park vegetation is loaded from the supplied Verdant Trail assets.
 for(const [x,z]of [[12,-80],[28,-80],[42,-80],[18,-96],[36,-96]])bench(x,z);
 // Calisthenics corner, outside the path and future mission gathering area.
 for(const x of [8,11,14]){box(x,1.3,-89,.09,2.6,.09,steel);box(x,1.3,-92,.09,2.6,.09,steel);box(x,2.6,-90.5,.09,.09,3,steel,false);}
 sign('PARK WIŚNIOWY',4,2,-68,4);
 return buildings;
}
