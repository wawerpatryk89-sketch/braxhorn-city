// Life on the estate: cars circling the main road (one police car), people on the sidewalks and two
// bosses who wait in their halls: Młot on the boxing ring and Asterion in the octagon. A boss fight starts
// only when you step into that ring; then you stay in until it is decided.
import * as T from 'three';
import * as A from '../src/assets.js';
import {Fighter} from '../src/fighter.js';
import {Traffic, loopPath} from '../src/traffic.js';
import {People} from '../src/people.js';
import {prepareCar} from '../src/street.js';
import {radial} from '../src/textures.js';

const BOSSES = [
 {id:'mlot', model:'musc', name:'Młot', title:'Pałkarz z Wiśniowej', hp:230, dmg:1.1, speed:.95, aggro:1.1, reward:700, size:1, weapon:'club',
  moves:['bat1','bat2','club4','bat3','clubKick'], place:'ring w sali bokserskiej',
  ring:{type:'square', x:-86, z:63, half:4.0, minY:.5}, spawn:[-84, 63],
  intro:'Na moim ringu nie ma sędziego. Jest tylko moja pałka.', outro:'Dobry… jesteś naprawdę dobry…'},
 {id:'asterion', model:'mino', name:'Asterion', title:'Szef gangu Czerwonego Rogu', hp:320, dmg:1.25, speed:1, aggro:1.25, reward:1500, size:1.08,
  moves:['jab','hook','kick','kicks','combo','elbow','martelo'], place:'oktagon w hali walk',
  ring:{type:'circle', x:86, z:63, r:4.6, minY:.3}, spawn:[88.2, 63],
  intro:'Klatka się zamknęła. Z niej wychodzi tylko jeden z nas.', outro:'Za mało… potrzebuję więcej serum…'},
];
const flat = (a, b) => Math.hypot(a.pos.x - b.pos.x, a.pos.z - b.pos.z);
const angDiff = (a, b) => { let d = (a - b) % (Math.PI * 2); if (d > Math.PI) d -= Math.PI * 2; if (d < -Math.PI) d += Math.PI * 2; return d; };
const segDist = (p, x, y0, y1, z) => { const cy = Math.max(y0, Math.min(p.y, y1)); return Math.hypot(p.x - x, p.y - cy, p.z - z); };
const v3 = (x, z) => new T.Vector3(x, 0, z);

export function createLife({scene, physics, player, ui, sound, meta, store, state, isTouch, camera}) {
 const down = new Set(store.get('estateBossDown', []));
 const life = {traffic:null, people:null, boss:null, arena:null, spawning:false, bosses:BOSSES, down};
 const blobMat = new T.MeshBasicMaterial({map:radial('rgba(0,0,0,0.6)','rgba(0,0,0,0)'), transparent:true, depthWrite:false});
 const hint = () => {
  const left = BOSSES.filter(b => !down.has(b.id));
  return left.length ? 'Bossowie osiedla: ' + left.map(b => b.name + ' — ' + b.place).join('; ') + '. Mapa w pauzie.' : 'Osiedle wolne: obaj bossowie pokonani.';
 };
 life.hint = hint;
 const bars = () => { ui.hp('me', player.hp / player.maxHp); if (life.boss) ui.bossHp(life.boss.hp / life.boss.maxHp); };

 // ---- traffic: the main road, southbound at x = +2, northbound at x = -2, U-turns at the junction and before the city gate
 (async () => {
  const keys = ['car_mar', 'car_zuk', 'car_gmc'], models = {}; // police + two light models (few draw calls)
  await Promise.all(keys.map(async k => { try { models[k] = prepareCar(k, await A.loadGLTF(k + '.glb')); } catch (e) { console.warn('car', k, e); } }));
  if (!Object.keys(models).length) return;
  const t = new Traffic({scene, models, sound, loops:[{curve:loopPath(v3(2, 80), v3(2, -14), v3(-2, -14), v3(-2, 80), 8.5), cars:3, police:1}]});
  t.camPos = camera.position;
  for (const c of t.cars) { const s = c.size, b = new T.Mesh(new T.PlaneGeometry(s.x + .7, s.z + .9).rotateX(-Math.PI / 2), blobMat); b.position.y = .03; b.renderOrder = 2; c.obj.add(b); }
  life.traffic = t;
 })().catch(e => console.warn('estate traffic', e));

 // ---- people on the sidewalks along the main road and on the paths in front of the houses
 (async () => {
  const p = new People({scene, world:physics, A});
  p.routeFar = isTouch ? 40 : 55; p.routeNoShadow = true; // estate shadows are frozen after loading anyway
  const routes = {west:[[-6.2, -9], [-6.2, 34], [-6.2, 90]], east:[[6.2, -9], [6.2, 34], [6.2, 90]], pathE:[[8, -13.6], [40, -13.6]], pathW:[[-8, -13.6], [-40, -13.6]]};
  await p.populateRoutes(routes, isTouch ? [['west', 1], ['east', 2], ['pathW', 1]] : [['west', 2], ['east', 2], ['pathE', 1], ['pathW', 1]], () => .07);
  life.people = p;
 })().catch(e => console.warn('estate people', e));

 // ---- bosses
 const chars = {};
 const loadChar = k => chars[k] ||= (async () => {
  const [g, map, nrm] = await Promise.all([A.loadRig(`chars/${k}.glb.gz`), A.loadTex(`tex/${k}_color.webp`, true), A.loadTex(`tex/${k}_normal.webp`, false)]);
  const mat = new T.MeshStandardMaterial({map, normalMap:nrm, metalness:0, roughness:.6, envMapIntensity:.12});
  g.scene.traverse(o => { if (o.isSkinnedMesh) o.material = mat; });
  return g;
 })();
 if (!state.explore) for (const b of BOSSES) if (!down.has(b.id)) loadChar(b.model).catch(e => console.warn('boss', e));
 const inRing = (R, x, z, pad = 0) => R.type === 'square' ? Math.abs(x - R.x) < R.half - pad && Math.abs(z - R.z) < R.half - pad : Math.hypot(x - R.x, z - R.z) < R.r - pad;

 async function spawnBoss(def) {
  const g = await loadChar(def.model);
  const w = Object.create(physics); w.previous = new T.Vector3(def.spawn[0], 0, def.spawn[1]); w.groundCache = null; // own collision memory
  const f = new Fighter({model:g.scene, clips:g.animations, meta, name:def.name, world:w, size:def.size || 1, isPlayer:false});
  f.team = 'enemy'; f.boss = true; f.def = def; f.hp = f.maxHp = def.hp; f.dmgMul = def.dmg; f.speedMul = def.speed;
  f.pos.set(def.spawn[0], 0, def.spawn[1]); f.y = physics.groundAt(def.spawn[0], def.spawn[1], 3, f.radius);
  f.heading = Math.atan2(player.pos.x - f.pos.x, player.pos.z - f.pos.z);
  f.model.traverse(o => { if (o.isMesh) o.castShadow = false; });
  scene.add(f.root); f.root.position.set(f.pos.x, f.y, f.pos.z); f.root.rotation.y = f.heading;
  f.blob = new T.Mesh(new T.PlaneGeometry(1.6, 1.6).rotateX(-Math.PI / 2), blobMat); f.blob.renderOrder = 2; scene.add(f.blob);
  if (def.weapon === 'club' && meta.__club) { const tip = new T.Object3D(); tip.position.fromArray(meta.__club.tipLocal); f.attach(tip, 'mixamorigRightHand'); f.addLimb('CLUB', tip); f.weapon = true; }
  f.ai = {cd:1.4, stepCd:0}; f.update(1 / 60);
  return f;
 }
 function remove(f) {
  scene.remove(f.root); scene.remove(f.blob); f.mixer.stopAllAction();
  const i = physics.dynamic.indexOf(f.col); if (i >= 0) physics.dynamic.splice(i, 1);
 }
 // the boss stays where it is if you leave by the map; it waits for the next visit
 life.abort = () => { if (life.boss) { remove(life.boss); life.boss = null; } life.arena = null; ui.boss(null); };

 function onKO(t) {
  try { sound.thump(1, t.pos); } catch {}
  if (t === player) {
   ui.banner('LEŻYSZ', 'Wstajesz z resztką sił…', 2.2); player.downTime = 1.6;
   player.onState = s => { if (s === 'loco' && player.hp <= player.maxHp * .5) { player.hp = player.maxHp * .6; bars(); player.onState = null; } };
   return;
  }
  const def = t.def; t.downTime = Infinity; t.dead = true; t.fadeT = 6;
  down.add(def.id); store.set('estateBossDown', [...down]);
  state.money += def.reward; store.set('money', state.money);
  ui.banner('POKONANY!', def.name + ' leży. +$' + def.reward, 2.6); sound.bell(1);
  if (def.outro) setTimeout(() => ui.say(def.name, def.outro, 3), 1400);
  life.boss = null; life.arena = null; ui.boss(null); ui.objective(hint()); life.fading = t;
  try { sound.playMusic('street'); } catch {}
 }
 function resolve(ev) {
  const a = ev.by; if (!a.limbs[ev.limb]) return;
  const p = a.limbWorld(ev.limb), reach = ev.weapon ? .55 : .34;
  for (const t of [player, life.boss]) {
   if (!t || t === a || t.team === a.team || ['ko', 'down', 'pair', 'getup'].includes(t.state)) continue;
   const d = segDist(p, t.pos.x, t.y + .35 * t.k, t.y + 2.05 * t.k, t.pos.z) - t.radius; if (d > reach) continue;
   const dir = new T.Vector3(t.pos.x - a.pos.x, 0, t.pos.z - a.pos.z).normalize();
   const dmg = ev.dmg * (a.dmgMul || 1) * (t.team === 'hero' && t.armor ? t.armor : 1);
   const res = t.hit(p.y - t.y > 1.45 * t.k ? 'head' : 'body', dmg, dir, a); if (!res) continue;
   if (res === 'dodged') { sound.whoosh(false, t.pos); continue; }
   if (res === 'blocked') { sound.block(t.pos); bars(); continue; }
   sound.punch(dmg / 16, t.pos); bars();
   if (res === 'ko') onKO(t);
  }
 }
 function brawl(o, dt) {
  const p = player, ai = o.ai, b = o.def; o.input.mag = 0; ai.cd -= dt; ai.stepCd -= dt;
  if (o.state !== 'loco' || o.dead) return;
  if (['ko', 'down'].includes(p.state)) { o.setIdle('Idle'); return; }
  o.setIdle(o.has('Boxing') ? 'Boxing' : 'Idle'); o.faceTarget = p.pos;
  const d = flat(o, p);
  if (p.state === 'attack' && p.stateT < .15 && d < 2.6) {
   if (!ai.reacted) { ai.reacted = true; const r = Math.random(); if (r < .22 * b.aggro && o.startBlock()) { o.blockTimer = .9; return; } if (r < .22 * b.aggro + .1 && o.doMove('dodge')) return; }
  } else if (p.state !== 'attack') ai.reacted = false;
  o.heading += angDiff(Math.atan2(p.pos.x - o.pos.x, p.pos.z - o.pos.z), o.heading) * Math.min(1, dt * 6);
  const reach = (o.weapon ? 2.5 : 2.15) * o.k;
  if (d > reach) { const k = 1 / d; o.input.x = (p.pos.x - o.pos.x) * k; o.input.z = (p.pos.z - o.pos.z) * k; o.input.mag = d > 6 ? .9 : .4; o.input.run = d > 6; return; }
  if (ai.cd < 0) { const mv = b.moves[Math.floor(Math.random() * b.moves.length)]; if (o.doMove(mv)) { ai.cd = 1.5 / b.aggro * (.6 + Math.random() * .9); if (Math.random() < .3) sound.play('grunt', {gain:.35, rate:.75, pos:o.pos}); } else ai.cd = .3; }
 }

 life.update = (dt, t) => {
  // traffic and people
  if (life.traffic) life.traffic.update(dt, [player, life.boss].filter(Boolean), {lamps:0}, (f, car, dir) => {
   if (f.state === 'ko' || f.state === 'down') return;
   f.hp = Math.max(0, f.hp - (f.isPlayer ? 18 : 40)); f.stopOverlays(.05); f.ko(dir); f.knock.set(dir.x, dir.z).multiplyScalar(9);
   sound.play('crash', {gain:.8, pos:f.pos});
   if (f.isPlayer) { ui.banner('', 'Uważaj na auta!', 1.4); f.downTime = 1.6; if (f.hp <= 0) f.hp = f.maxHp * .4; }
   bars();
  }, t);
  if (life.people) {
   const threats = life.boss && !life.boss.dead ? [{x:player.pos.x, z:player.pos.z, r:8}] : player.state === 'attack' ? [{x:player.pos.x, z:player.pos.z, r:5}] : [];
   life.people.update(dt, {threats, camPos:camera.position});
  }
  if (life.traffic) {
   const nc = life.traffic.nearest(player.pos.x, player.pos.z);
   sound.loop('engine', nc ? Math.max(0, 1 - nc.d / 25) * .6 : 0, nc ? .6 + nc.car.v / 14 : 1);
   const pol = life.traffic.cars.find(c => c.siren);
   sound.loop('siren', pol ? Math.max(0, 1 - Math.hypot(pol.x - player.pos.x, pol.z - player.pos.z) / 50) * .25 : 0);
  }
  // boss fights
  for (const ev of player.events) resolve(ev);
  const f = life.boss;
  if (f) {
   brawl(f, dt); f.update(dt);
   for (const ev of f.events) resolve(ev); f.events.length = 0;
   const h = f.hipsWorld(); f.blob.position.set(h.x, f.y + .02, h.z);
  }
  if (life.fading) {
   const o = life.fading; o.update(dt); o.fadeT -= dt; if (o.fadeT < 1.2) o.root.position.y = o.y - (1.2 - o.fadeT) * .8;
   if (o.fadeT <= 0) { remove(o); life.fading = null; }
  }
  const R = life.arena;
  if (R) { // stay in the ring until it is decided
   if (R.type === 'square') { player.pos.x = Math.max(R.x - R.half, Math.min(R.x + R.half, player.pos.x)); player.pos.z = Math.max(R.z - R.half, Math.min(R.z + R.half, player.pos.z)); }
   else { const dx = player.pos.x - R.x, dz = player.pos.z - R.z, d = Math.hypot(dx, dz); if (d > R.r) { player.pos.x = R.x + dx / d * R.r; player.pos.z = R.z + dz / d * R.r; } }
  }
  if (!life.boss && !life.spawning && !state.explore) for (const def of BOSSES) {
   if (down.has(def.id) || !inRing(def.ring, player.pos.x, player.pos.z, .3) || player.y < def.ring.minY) continue;
   life.spawning = true;
   spawnBoss(def).then(f => {
    life.spawning = false;
    if (state.explore || down.has(def.id) || !inRing(def.ring, player.pos.x, player.pos.z, -1)) { remove(f); return; }
    life.boss = f; life.arena = def.ring;
    ui.boss(def.name, def.title); ui.bossHp(1); ui.say(def.name, def.intro, 4.2);
    try { sound.playMusic('boss'); } catch {}
    sound.play('grunt', {gain:.5, rate:.7, pos:f.pos});
   }).catch(e => { life.spawning = false; console.warn('boss', e); });
   break;
  }
 };
 return life;
}
