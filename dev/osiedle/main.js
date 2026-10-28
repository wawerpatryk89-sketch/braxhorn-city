// The estate is a playable district using the city's actual rig, Fighter and UI.
import * as T from 'three';
import * as A from '../src/assets.js';
import {Fighter,SCALE} from '../src/fighter.js';
import {HEROES} from '../src/story.js';
import {UI} from '../src/ui.js';
import {Sound} from '../src/sound.js';
import {view,applyOrientation} from '../src/view.js';
import {advanceFrame} from '../src/frame-time.js';
import {radial} from '../src/textures.js';
import {MOVE_LIST,INPUT_ACTIONS} from '../shared/player-controls.js';
import {cityURL,readCityVisit,saveCityVisit} from '../shared/district-travel.js';
import {loadEstate} from './level.js';
import {EstateWorld} from './player-world.js';
import {EstateDoors} from './entrances.js';
import {LEVEL} from './layout.js';

const $=id=>document.getElementById(id);
const store={get(k,d){try{return JSON.parse(localStorage.getItem('bx_'+k))??d;}catch{return d;}},set(k,v){try{localStorage.setItem('bx_'+k,JSON.stringify(v));}catch{}}};
const isTouch=matchMedia('(pointer:coarse)').matches||'ontouchstart'in window;
let landscape=store.get('land',true);applyOrientation(landscape,isTouch);
const resume=readCityVisit();
const heroKey=resume?.hero||(store.get('hero','brax')==='nyxa'?'nyxa':'brax');
const hero=HEROES[heroKey];
const state={hero:heroKey,explore:store.get('explore',true),money:store.get('money',0),specialIdx:0,...resume};
const upg={hp:0,dmg:0,armor:0,shakes:1,...store.get('upg',{})};
const QUALITY={high:{name:'Wysoka',dpr:1.75},medium:{name:'Średnia',dpr:1.5},low:{name:'Niska',dpr:1.25}};
let qKey=store.get('quality3','high');if(!QUALITY[qKey])qKey='high';
const renderer=new T.WebGLRenderer({canvas:$('c'),antialias:true});
renderer.setPixelRatio(Math.min(devicePixelRatio,QUALITY[qKey].dpr));renderer.setSize(view.w(),view.h(),false);
renderer.outputColorSpace=T.SRGBColorSpace;renderer.toneMapping=T.ACESFilmicToneMapping;renderer.toneMappingExposure=1.05;
renderer.shadowMap.enabled=true;renderer.shadowMap.type=T.PCFSoftShadowMap;
const scene=new T.Scene();scene.background=new T.Color('#bdccd0');scene.fog=new T.Fog('#bdccd0',160,330);
scene.add(new T.AmbientLight(0xdce5e0,.7),new T.HemisphereLight(0xe9f1ed,0x827963,2));
const sun=new T.DirectionalLight(0xfff0d2,2.6);sun.position.set(-55,110,45);sun.castShadow=true;sun.shadow.mapSize.set(2048,2048);
Object.assign(sun.shadow.camera,{left:-135,right:135,top:135,bottom:-135,near:1,far:260});sun.shadow.normalBias=.035;sun.shadow.bias=-.0002;
scene.add(sun,sun.target);sun.target.position.set(0,0,-5);
const camera=new T.PerspectiveCamera(55,view.w()/view.h(),.08,400);
// Keep the player camera across the boundary; only input changes its yaw.
const cam={mode:resume?.camMode||store.get('cam','3d'),yaw:resume?.yaw??0,pitch:resume?.pitch??.3,dist:resume?.dist??5.8,isoYaw:Math.PI/4,target:new T.Vector3(),snap:true};
const sound=new Sound(store.get('sound',true));sound.prepare().catch(console.warn);sound.setRoom('street');
const ui=new UI({isTouch,quality:QUALITY,qKey,sound,moves:MOVE_LIST});
const keys=new Set(),joy={x:0,y:0,active:false},held={block:false};
let player,physics,level,doors,blob,ready=false,paused=false,travelling=false,last=performance.now(),punchAlt=0,danceAlt=0,frameNumber=0;
const actions=[];
A.initLoaders(renderer,'../assets/');
A.net.onProgress=()=>{$('netMB').textContent=(A.net.bytes/1048576).toFixed(1)+' MB';};
ui.showTitle(false);ui.setQuality(qKey);ui.setSound(sound.on);ui.setCam(cam.mode);ui.setExplore(state.explore);ui.clock('13:30',false);
$('bTime').hidden=true;

function resetInput(){keys.clear();joy.active=false;joy.x=joy.y=0;held.block=false;actions.length=0;if(player){player.input.mag=0;player.input.block=false;} $('joy').classList.remove('on');for(const el of document.querySelectorAll('.down'))el.classList.remove('down');}
function checkpoint(){if(!player)return;saveCityVisit({...state,hp:player.hp,yaw:cam.yaw,pitch:cam.pitch,dist:cam.dist,camMode:cam.mode,estateX:player.pos.x,estateY:player.y,estateZ:player.pos.z,estateHeading:player.heading});store.set('money',state.money);store.set('estate_doors',doors.snapshot());}
function returnToCity(){if(!ready||travelling)return;checkpoint();travelling=true;resetInput();ui.loading(true,'Wracam do miasta…');location.assign(cityURL());}
function openMenu(){if(!ready||travelling)return;paused=true;resetInput();ui.showPause(true,'Osiedle Wiśniowa');checkpoint();}
function closeMenu(){paused=false;ui.showMap(false);ui.showPause(false);last=performance.now();}
function resize(){applyOrientation(landscape,isTouch);camera.aspect=view.w()/view.h();camera.updateProjectionMatrix();renderer.setSize(view.w(),view.h(),false);ui.setOrient(view.w()>view.h());}
function fullscreen(){const el=document.documentElement;const fn=el.requestFullscreen||el.webkitRequestFullscreen;if(fn)Promise.resolve(fn.call(el)).catch(()=>{});}
function teleport(x,z,a=cam.yaw,p=cam.pitch,y=1.78){
 player.pos.set(x,y-1.7,z);player.y=y-1.7;player.vy=0;player.onGround=true;player.vel.set(0,0);player.knock.set(0,0);player.backToLoco(0);player.heading=a+Math.PI;
 physics.reset(player.pos);player.y=physics.groundAt(x,z,player.y+.01,player.radius);player.pos.y=player.y;
 cam.yaw=a;cam.pitch=p;cam.target.set(x,player.y+1.55,z);cam.snap=true;player.update(1/60);updateCamera(1/60);updateHud();
}
const destinations=[['Park',20,-83,0],['Boisko',-28,-72,0],['Wejście',0,52,0],['Przed willą',0,-24,0],['Salon',0,-33,.65],['Taras',0,-51,Math.PI],['Garaże',-24,-25,0],['Dom kontaktowy',30,-28,0],...LEVEL.facilities.map(f=>[f.label,f.x,f.z+19,0])];
function openMap(){paused=true;resetInput();ui.showPause(false);ui.showMap(true,[{key:'city',title:'Miasto BRAXHORN',sub:'Wróć swoją postacią do miasta i siłowni.',tag:'Przejście'},...destinations.map(([title],i)=>({key:'estate-'+i,title,sub:'Osiedle Wiśniowa',tag:'Miejsce'}))]);}
ui.onTravel=key=>{if(key==='city'){returnToCity();return;}const d=destinations[Number(key.replace('estate-',''))];if(d){ui.showMap(false);teleport(d[1],d[2],d[3]);paused=false;last=performance.now();}};
$('mapClose').addEventListener('click',()=>{paused=false;last=performance.now();});
$('map').addEventListener('click',e=>{if(e.target.id==='map'){paused=false;last=performance.now();}});
ui.onAction=a=>{sound.unlock();ui.hideKeysSoon();actions.push(a);};
ui.onHold=(name,value)=>{held[name]=value;sound.unlock();};
ui.onJoy=(x,y,active)=>{Object.assign(joy,{x,y,active});};
ui.onLook=(dx,dy)=>{if(!paused&&cam.mode==='3d'){cam.yaw-=dx*.006;cam.pitch=T.MathUtils.clamp(cam.pitch+dy*.004,.05,1.2);}};
ui.onZoom=d=>{cam.dist=T.MathUtils.clamp(cam.dist*(1+d),3,10);};
function menuAction(a){
 if(a==='menu'){paused?closeMenu():openMenu();}
 else if(a==='resume')closeMenu();
 else if(a==='map')openMap();
 else if(a==='moves')ui.toggleMoves();
 else if(a==='help')ui.toggleKeys();
 else if(a==='cam'){cam.mode=cam.mode==='3d'?'25d':'3d';store.set('cam',cam.mode);ui.setCam(cam.mode);}
 else if(a==='iso-left'){if(cam.mode==='25d')cam.isoYaw+=Math.PI/2;}
 else if(a==='sound'){sound.toggle();store.set('sound',sound.on);ui.setSound(sound.on);}
 else if(a==='orient'){landscape=!(view.rotated||view.w()>view.h());store.set('land',landscape);resize();}
 else if(a==='fullscreen')fullscreen();
 else if(a==='explore'){state.explore=!state.explore;store.set('explore',state.explore);ui.setExplore(state.explore);}
 else if(a.startsWith('q-')){qKey=a.slice(2);store.set('quality3',qKey);renderer.setPixelRatio(Math.min(devicePixelRatio,QUALITY[qKey].dpr));resize();ui.setQuality(qKey);}
 else return false;
 return true;
}
function doAction(a){
 if(menuAction(a)||!ready||paused||travelling)return;
 if(a==='act'){if(doors.use(player,sound,message=>ui.banner('',message,1.8))){updateHud();return;}if(Math.abs(player.pos.x)<7&&player.pos.z>90)returnToCity();return;}
 if(a==='jump'){player.jump();return;}
 if(a==='throw'){ui.banner('','Podejdź bliżej przeciwnika, żeby rzucić.',1.4);return;}
 if(a==='shake'){if(upg.shakes>0&&player.hp<player.maxHp-1){upg.shakes--;player.hp=Math.min(player.maxHp,player.hp+player.maxHp*.6);store.set('upg',upg);updateHud();checkpoint();}return;}
 if(a==='block'){player.startBlock();player.blockTimer=.5;return;}
 let move=a;
 if(a==='punch')move=state.bat?['bat1','bat2','bat3'][punchAlt++%3]:punchAlt++%3===2?'hook':'jab';
 if(a==='special')move=hero.specials[state.specialIdx++%hero.specials.length];
 if(a==='dance')move=danceAlt++%2?'dance2':'dance';
 player.faceTarget=null;if(player.doMove(move,{force:player.state==='emote'}))sound.whoosh(!['jab','idle'].includes(move),player.pos);
}
function updateCamera(dt){
 const hips=player.hipsWorld(),target=new T.Vector3(hips.x,player.y+(cam.mode==='3d'?1.55:1)*player.k,hips.z);
 if(cam.snap)cam.target.copy(target);else cam.target.lerp(target,1-Math.exp(-dt*9));
 const yaw=cam.mode==='3d'?cam.yaw:cam.isoYaw,pitch=cam.mode==='3d'?cam.pitch:.68,dist=cam.mode==='3d'?cam.dist:17;
 let pos=new T.Vector3(Math.sin(yaw)*Math.cos(pitch),Math.sin(pitch),Math.cos(yaw)*Math.cos(pitch)).multiplyScalar(dist).add(cam.target);
 pos=physics.cameraPosition(cam.target,pos);pos.y=Math.max(player.y+.35,pos.y);
 if(cam.snap||camera.position.distanceTo(pos)>8){camera.position.copy(pos);cam.snap=false;}else camera.position.lerp(pos,1-Math.exp(-dt*16));
 const fov=cam.mode==='3d'?55:28;if(camera.fov!==fov){camera.fov=fov;camera.updateProjectionMatrix();}
 camera.lookAt(cam.target);sound.setListener(camera.position.x,camera.position.z,yaw);
}
function updateHud(){
 ui.hp('me',player.hp/player.maxHp);ui.money(state.money);ui.shakes(upg.shakes);ui.shakeNeeded(player.hp<player.maxHp-1);
 ui.action(doors.label(player)||(Math.abs(player.pos.x)<7&&player.pos.z>90?'MIASTO':null));
 const facility=LEVEL.facilities.find(f=>Math.abs(player.pos.x-f.x)<f.width/2+3&&Math.abs(player.pos.z-f.z)<f.depth/2+4);
 $('meTag').textContent=facility?.label||(player.pos.z<-66?'Park Wiśniowy':'Osiedle Wiśniowa');
}
function simulate(dt){
 let ix=(keys.has('d')||keys.has('arrowright')?1:0)-(keys.has('a')||keys.has('arrowleft')?1:0),iy=(keys.has('w')||keys.has('arrowup')?1:0)-(keys.has('s')||keys.has('arrowdown')?1:0);
 if(joy.active){ix=joy.x;iy=joy.y;}
 const mag=Math.min(1,Math.hypot(ix,iy)),yaw=cam.mode==='3d'?cam.yaw:cam.isoYaw;
 const wx=-Math.sin(yaw)*iy+Math.cos(yaw)*ix,wz=-Math.cos(yaw)*iy-Math.sin(yaw)*ix,length=Math.hypot(wx,wz)||1;
 Object.assign(player.input,{x:wx/length,z:wz/length,mag,run:joy.active||!keys.has('shift'),block:held.block||keys.has('c')});
 if(player.state==='emote'&&mag>.25)player.backToLoco(.25);
 if(player.input.block&&['loco','emote'].includes(player.state))player.startBlock();
 player.setIdle('Idle');player.update(dt);doors.update(dt,player);player.events.length=0;ui.tick(dt);
 const h=player.hipsWorld();blob.position.set(h.x,physics.groundAt(h.x,h.z,player.y+.1)+.014,h.z);
 updateCamera(dt);
}
function render(){renderer.render(scene,camera);}
function frame(now){
 requestAnimationFrame(frame);const elapsed=(now-last)/1000;last=now;
 const changed=actions.length>0;while(actions.length)doAction(actions.shift());
 if(!ready||travelling||document.hidden)return;
 if(paused){if(changed){updateCamera(1/60);render();}return;}
 advanceFrame(elapsed,simulate);if(frameNumber++%15===0)updateHud();render();
}
async function boot(){
 $('loadMsg').textContent='Ładuję '+hero.name+' i osiedle…';
 const heroAssets=Promise.all([A.loadRig('chars/'+heroKey+'.glb.gz'),A.loadTex('tex/'+heroKey+'_color.webp'),A.loadTex('tex/'+heroKey+'_normal.webp',false),A.loadJSON('chars/clips.json')]);
 level=await loadEstate(scene,renderer,(message,count)=>{$('loadMsg').textContent=message;const bar=document.querySelector('#loader .bar i');bar.style.width=Math.min(96,10+count*3.8)+'%';});
 doors=new EstateDoors(scene,store.get('estate_doors',[]));level.doors=doors;physics=new EstateWorld(level);
 const [rig,map,normalMap,meta]=await heroAssets;
 const mat=new T.MeshStandardMaterial({map,normalMap,metalness:0,roughness:.6,envMapIntensity:.12});
 rig.scene.traverse(o=>{if(o.isSkinnedMesh)o.material=mat;});
 player=new Fighter({model:rig.scene,clips:rig.animations,meta,name:hero.name,world:physics,isPlayer:true});
 player.key=heroKey;player.team='hero';player.maxHp=hero.hp+15*upg.hp;player.hp=T.MathUtils.clamp(state.hp??player.maxHp,1,player.maxHp);player.speedMul=hero.speed;player.dmgMul=hero.dmg*(1+.08*upg.dmg);player.armor=1-.08*upg.armor;
 // Freeze only static architecture shadows; moving fighters retain a ground shadow.
 player.model.traverse(o=>{if(o.isMesh)o.castShadow=false;});scene.add(player.root);
 blob=new T.Mesh(new T.PlaneGeometry(1.6,1.6).rotateX(-Math.PI/2),new T.MeshBasicMaterial({map:radial('rgba(0,0,0,0.75)','rgba(0,0,0,0)'),transparent:true,depthWrite:false}));scene.add(blob);
 if(state.bat){const g=await A.loadGLTF('bat.glb');const m=g.scene.clone();m.scale.setScalar(.95/(SCALE*player.k));m.rotation.set(Math.PI/2,0,0);m.position.set(0,9,2.5);player.attach(m,'mixamorigRightHand');ui.weapon('PAŁKA',state.batHits??16);}
 teleport(resume?.estateX??0,resume?.estateZ??92,cam.yaw,cam.pitch,(resume?.estateY??.08)+1.7);
 if(Number.isFinite(resume?.estateHeading))player.heading=resume.estateHeading;
 ui.setHero(hero,HEROES[heroKey==='brax'?'nyxa':'brax']);ui.showOpp(false);ui.clock('13:30',false);ui.objective('Podejdź do drzwi i naciśnij OTWÓRZ. Miasto i siłownia są w mapie.');updateHud();
 scene.updateMatrixWorld(true);renderer.compile(scene,camera);render();renderer.shadowMap.autoUpdate=false;
 ready=true;window.__os.ready=true;$('loader').classList.add('done');checkpoint();last=performance.now();
}
addEventListener('keydown',e=>{const k=e.key.toLowerCase();if([' ','arrowup','arrowdown','arrowleft','arrowright'].includes(k))e.preventDefault();keys.add(k);if(!e.repeat&&INPUT_ACTIONS[k])actions.push(INPUT_ACTIONS[k]);sound.unlock();});
addEventListener('keyup',e=>keys.delete(e.key.toLowerCase()));
for(const event of ['pointerup','touchend','click'])addEventListener(event,()=>sound.unlock(),{passive:true,capture:true});
for(const event of ['gesturestart','gesturechange','gestureend','dblclick'])document.addEventListener(event,e=>e.preventDefault(),{passive:false});
addEventListener('blur',()=>{resetInput();if(ready)openMenu();});
addEventListener('resize',resize);addEventListener('orientationchange',()=>setTimeout(resize,250));
addEventListener('pagehide',checkpoint);addEventListener('pageshow',e=>{if(e.persisted){travelling=false;ui.loading(false);openMenu();last=performance.now();}});
addEventListener('visibilitychange',()=>{if(document.hidden){resetInput();checkpoint();if(ready)openMenu();}});
window.__os={ready:false,THREE:T,LEVEL,scene,camera,renderer,cam,joy,keys,state,assetErrors:[],get player(){return player;},get doors(){return doors;},get world(){return level?.world;},get imported(){return level?.imported||[];},get vegetation(){return level?.vegetation;},teleport,start(){paused=false;},pause(v){paused=v;resetInput();},step(dx,dz){const count=Math.max(1,Math.ceil(Math.hypot(dx,dz)/.12));for(let i=0;i<count;i++){player.pos.x+=dx/count;player.pos.z+=dz/count;physics.resolve(player.pos,player.radius,player.y,player.height,player.col);player.y=physics.groundAt(player.pos.x,player.pos.z,player.y,player.radius);player.pos.y=player.y;}updateCamera(1/60);},sim(seconds){for(let t=0;t<seconds;t+=1/60)simulate(Math.min(1/60,seconds-t));},act:doAction,returnToCity,checkpoint,render,stats(){return{...renderer.info.render,assets:level?.imported.length,errors:[],position:player?.pos.toArray(),hero:heroKey,yaw:cam.yaw};}};
resize();requestAnimationFrame(frame);boot().catch(e=>{window.__os.assetErrors.push(e.message);$('loadMsg').textContent='Nie udało się uruchomić osiedla: '+e.message;console.error(e);});
