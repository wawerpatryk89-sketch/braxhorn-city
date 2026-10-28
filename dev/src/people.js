// Background people (Mixamo characters): pedestrians walking on the sidewalks (they run away from fights)
// and gym members training (barbell lifts, bag work, warm-ups).
import * as THREE from 'three';
import { clone as cloneSkinned } from '../three/examples/jsm/utils/SkeletonUtils.js';
import { addLampField } from './env.js';

export const PEOPLE = ['Arissa', 'Big_Vegas', 'Ch06_nonPBR', 'Ch08_nonPBR', 'Ch12_nonPBR', 'Ch13_nonPBR', 'Ch16_nonPBR', 'Ch17_nonPBR', 'Ch18_nonPBR', 'Ch22_nonPBR', 'Ch26_nonPBR',
  'Ch27_nonPBR', 'Ch31_nonPBR', 'Ch37_nonPBR', 'Ch38_nonPBR', 'Ch39_nonPBR', 'Ch47_nonPBR', 'Kachujin_G_Rosales', 'Lola_B_Styperek', 'Remy', 'The_Boss'];
const GYM_PEOPLE = ['Ch16_nonPBR', 'Ch31_nonPBR', 'Kachujin_G_Rosales', 'Ch22_nonPBR', 'Ch47_nonPBR', 'Ch12_nonPBR', 'Ch26_nonPBR'];

export class People {
  constructor({ scene, world, A }) {
    this.scene = scene; this.world = world; this.A = A;
    this.templates = {}; this.clips = null; this.list = []; this.tmp = new THREE.Vector3(); this.mats = []; this.envK = 0.35;
  }
  async loadClips() {
    if (this.clips) return this.clips;
    const [buf, fb] = await Promise.all([this.A.loadBin('people/clips.json.gz'), this.A.loadBin('people/fight.json.gz')]);
    const base = JSON.parse(new TextDecoder().decode(buf)), fight = JSON.parse(new TextDecoder().decode(fb));
    this.fightNames = Object.keys(fight);
    this.clipData = { ...fight, ...base };
    this.clips = {};
    return this.clips;
  }
  // a clip adapted to a body: hip height track scaled to that skeleton
  clip(name, hipsY) {
    const key = name + '@' + Math.round(hipsY);
    if (this.clips[key]) return this.clips[key];
    const c = this.clipData[name]; const k = hipsY / (c.rest || hipsY);
    const tracks = c.tracks.map(t => t.n.endsWith('position') ? new THREE.VectorKeyframeTrack(t.n, t.t, t.v.map(v => v * k)) : new THREE.QuaternionKeyframeTrack(t.n, t.t, t.v));
    return (this.clips[key] = new THREE.AnimationClip(name, c.d, tracks));
  }
  async template(name) {
    if (!this.templates[name]) this.templates[name] = (async () => {
      const g = await this.A.loadGLTF('people/' + name + '.glb');
      const root = g.scene; root.updateMatrixWorld(true);
      let hips = null, head = null;
      root.traverse(o => { if (o.isBone && o.name === 'mixamorigHips') hips = o; if (o.isBone && o.name === 'mixamorigHead') head = o; if (o.isMesh) { o.castShadow = true; o.frustumCulled = false; for (const m of (Array.isArray(o.material) ? o.material : [o.material])) { addLampField(m, 'lit'); if (!this.mats.includes(m)) this.mats.push(m); m.envMapIntensity = this.envK; } } });
      const hy = hips.getWorldPosition(new THREE.Vector3()).y, top = head.getWorldPosition(new THREE.Vector3()).y * 1.09;
      return { root, hipsY: hips.position.y, hipsWorld: hy, height: top };
    })();
    return this.templates[name];
  }
  async spawn(name, { pos, heading = 0, height = 1.75, mode = 'walk', clip = 'Walking', lane = null, prop = null }) {
    await this.loadClips();
    const T = await this.template(name);
    const obj = cloneSkinned(T.root);
    const s = height / T.height; obj.scale.setScalar(s);
    const g = new THREE.Group(); g.add(obj); g.position.copy(pos); g.rotation.y = heading; this.scene.add(g);
    const mixer = new THREE.AnimationMixer(obj);
    const act = (n) => { const a = mixer.clipAction(this.clip(n, T.hipsY)); return a; };
    const p = { name, g, obj, mixer, act, s, hipsY: T.hipsY, mode, heading, lane, state: mode, t: Math.random() * 10, speed: 0, col: this.world.circle(pos.x, pos.z, 0.32, 1.8) };
    p.col.mobile = false;
    const wd = this.clipData.Walking, rd = this.clipData.Running;
    p.walkSpeed = wd.stride * (T.hipsY / wd.rest) * s / wd.d;
    p.runSpeed = rd.stride * (T.hipsY / rd.rest) * s / rd.d;
    if (mode === 'walk') { p.cur = act('Walking'); p.cur.play(); p.cur.time = Math.random() * wd.d; p.speed = p.walkSpeed; }
    else { p.cur = act(clip); p.cur.play(); p.cur.time = Math.random() * this.clipData[clip].d; }
    mixer.update(0);
    if (prop === 'barbell') p.prop = makeBarbell(this.scene);
    this.list.push(p);
    return p;
  }
  fade(p, to, name, dur = 0.3) { const n = p.act(name); if (p.cur === n) return; n.reset().play(); p.cur.crossFadeTo(n, dur, false); p.cur = n; }
  // threats: [{x,z,r}], street bounds for walkers
  update(dt, { threats = [], camPos, night = 0, bounds, hideWalkers = false }) {
    for (const p of this.list) {
      const far = camPos && Math.hypot(p.g.position.x - camPos.x, p.g.position.z - camPos.z) > (p.route ? this.routeFar ?? 55 : 55);
      p.g.visible = !far && !(p.mode === 'walk' && (hideWalkers || (night > 0.6 && p.nightHide)));
      p.col.top = p.g.visible ? 1.8 : -10;
      if (!p.g.visible) continue;
      p.mixer.update(dt);
      if (p.route) this.walkRoute(p, dt, threats);
      else if (p.mode === 'walk') this.walk(p, dt, threats, bounds);
      else if (p.prop) updateBarbell(p, this.tmp);
      p.col.x = p.g.position.x; p.col.z = p.g.position.z;
    }
  }
  walk(p, dt, threats, B) {
    const pos = p.g.position;
    let threat = null;
    for (const t of threats) { const d = Math.hypot(t.x - pos.x, t.z - pos.z); if (d < (t.r || 7)) { threat = t; break; } }
    if (threat) { p.fleeT = 4; p.fleeDir = Math.sign(pos.x - threat.x) || 1; }
    if (p.fleeT > 0) {
      p.fleeT -= dt;
      if (p.state !== 'flee') { p.state = 'flee'; this.fade(p, 'flee', 'Running', 0.2); }
      p.heading = p.fleeDir > 0 ? Math.PI / 2 : -Math.PI / 2;
      p.speed = p.runSpeed;
    } else if (p.state === 'flee') { p.state = 'walk'; this.fade(p, 'walk', 'Walking', 0.4); p.speed = p.walkSpeed; }
    if (p.state === 'walk') { p.heading = p.dir > 0 ? Math.PI / 2 : -Math.PI / 2; p.speed = p.walkSpeed; }
    // turn around at the ends of the street
    if (pos.x > B.x1 - 1 && Math.sin(p.heading) > 0) { p.dir = -1; p.fleeT = 0; }
    if (pos.x < B.x0 + 1 && Math.sin(p.heading) < 0) { p.dir = 1; p.fleeT = 0; }
    p.g.rotation.y += ((p.heading - p.g.rotation.y + Math.PI * 3) % (Math.PI * 2) - Math.PI) * Math.min(1, dt * 6);
    pos.x += Math.sin(p.g.rotation.y) * p.speed * dt;
    // stay in the lane, step around the player/others a little
    if (p.lane) pos.z += (p.lane - pos.z) * Math.min(1, dt * 1.5);
  }
  // walking along a polyline (closed when the first and last points are equal), turning back at open ends
  walkRoute(p, dt, threats) {
    const pos = p.g.position, R = p.route, loop = R.loop;
    let threat = null;
    for (const t of threats) { const d = Math.hypot(t.x - pos.x, t.z - pos.z); if (d < (t.r || 7)) { threat = t; break; } }
    const target = () => R.pts[p.wp];
    if (threat && !(p.fleeT > 0)) {
      // run towards whichever neighbouring waypoint leads away from the fight
      const next = R.pts[p.wp], prevI = p.wp - p.dir, prev = R.pts[loop ? (prevI + R.pts.length) % R.pts.length : Math.max(0, Math.min(R.pts.length - 1, prevI))];
      if (Math.hypot(prev[0] - threat.x, prev[1] - threat.z) > Math.hypot(next[0] - threat.x, next[1] - threat.z)) { p.dir = -p.dir; p.wp = loop ? (prevI + R.pts.length) % R.pts.length : Math.max(0, Math.min(R.pts.length - 1, prevI)); }
      p.fleeT = 4;
    }
    if (p.fleeT > 0) { p.fleeT -= dt; if (p.state !== 'flee') { p.state = 'flee'; this.fade(p, 'flee', 'Running', 0.2); } p.speed = p.runSpeed; }
    else if (p.state !== 'walk') { p.state = 'walk'; this.fade(p, 'walk', 'Walking', 0.4); p.speed = p.walkSpeed; }
    let [tx, tz] = target();
    if (Math.hypot(tx - pos.x, tz - pos.z) < 0.6) {
      let n = p.wp + p.dir;
      if (loop) n = (n + R.pts.length) % R.pts.length;
      else if (n < 0 || n >= R.pts.length) { p.dir = -p.dir; n = p.wp + p.dir; }
      p.wp = n; [tx, tz] = target();
    }
    p.heading = Math.atan2(tx - pos.x, tz - pos.z);
    p.g.rotation.y += ((p.heading - p.g.rotation.y + Math.PI * 3) % (Math.PI * 2) - Math.PI) * Math.min(1, dt * 5);
    const step = Math.min(p.speed * dt, Math.hypot(tx - pos.x, tz - pos.z));
    pos.x += Math.sin(p.g.rotation.y) * step; pos.z += Math.cos(p.g.rotation.y) * step;
    if (p.groundY) pos.y += (p.groundY(pos.x, pos.z, pos.y) - pos.y) * Math.min(1, dt * 10);
  }
  // people on named routes: routes = { name: [[x, z], ...] }, plan = [[routeName, count], ...]
  async populateRoutes(routes, plan, groundY = null) {
    const names = PEOPLE.filter(x => !GYM_PEOPLE.includes(x));
    const ps = []; let i = 0;
    for (const [rn, count] of plan) {
      const pts = routes[rn]; if (!pts) continue;
      const loop = pts.length > 2 && pts[0][0] === pts[pts.length - 1][0] && pts[0][1] === pts[pts.length - 1][1];
      const route = { pts: loop ? pts.slice(0, -1) : pts, loop };
      for (let k = 0; k < count; k++, i++) {
        const name = names[(i * 5 + 3) % names.length];
        const seg = Math.min(route.pts.length - 2, Math.floor(((k + 0.5) / count) * (route.pts.length - 1)));
        const a = route.pts[seg], b = route.pts[(seg + 1) % route.pts.length], u = 0.2 + 0.6 * ((i * 0.37) % 1);
        const x = a[0] + (b[0] - a[0]) * u, z = a[1] + (b[1] - a[1]) * u;
        const dir = loop ? 1 : (k % 2 ? -1 : 1);
        const y = groundY ? groundY(x, z, 1) : 0.15;
        ps.push(this.spawn(name, { pos: new THREE.Vector3(x, y, z), heading: Math.atan2(b[0] - a[0], b[1] - a[1]) + (dir < 0 ? Math.PI : 0), height: 1.62 + ((i * 13) % 7) * 0.04 }).then(p => {
          p.route = route; p.dir = dir; p.wp = dir > 0 ? (seg + 1) % route.pts.length : seg; p.groundY = groundY; if (this.routeNoShadow) p.obj.traverse(o => { o.castShadow = false; }); return p;
        }));
      }
    }
    return Promise.all(ps);
  }
  async populateStreet(ST, n = 8) {
    const names = PEOPLE.filter(x => !GYM_PEOPLE.includes(x));
    const ps = [];
    for (let i = 0; i < n; i++) {
      const name = names[(i * 7 + 3) % names.length];
      const far = i % 2 === 1;
      const lane = far ? ST.road1 + 1.0 + (i % 3) * 0.35 : ST.near + 1.0 + (i % 3) * 0.6;
      const x = ST.x0 + 6 + ((i * 37) % 90);
      const dir = i % 3 === 0 ? -1 : 1;
      ps.push(this.spawn(name, { pos: new THREE.Vector3(x, 0.15, lane), heading: dir > 0 ? Math.PI / 2 : -Math.PI / 2, height: 1.62 + ((i * 13) % 7) * 0.04, lane }).then(p => { p.dir = dir; p.nightHide = i > 1; return p; }));
    }
    return Promise.all(ps);
  }
  async populateGym(max = 7) {
    const spots = [
      ['Ch16_nonPBR', 'Back Squat', [12, 0, 7.3], Math.PI, 'barbell'],
      ['Ch31_nonPBR', 'Clean And Jerk', [5.6, 0, 8.4], Math.PI, 'barbell'],
      ['Kachujin_G_Rosales', 'Snatch', [-2.4, 0, 7.8], Math.PI * 0.9, 'barbell'],
      ['Ch22_nonPBR', 'Sumo High Pull', [14.6, 0, 1.0], -Math.PI / 2, 'barbell'],
      ['Ch47_nonPBR', 'Boxing (2)', [-12.55, 0, -5.4], -Math.PI / 2, null],
      ['Ch12_nonPBR', 'Jab Cross (1)', [-12.55, 0, 0.6], -Math.PI / 2, null],
      ['Ch26_nonPBR', 'Warming Up', [-6.5, 0, -7.2], 0.3, null],
    ];
    return Promise.all(spots.slice(0, max).map(([n, c, p, h, prop]) => this.spawn(n, { pos: new THREE.Vector3(...p), heading: h, mode: 'still', clip: c, prop, height: 1.72 })));
  }
}

function makeBarbell(scene) {
  const g = new THREE.Group();
  const steel = addLampField(new THREE.MeshStandardMaterial({ color: 0x9aa0a6, metalness: 0.9, roughness: 0.3 }));
  const rubber = addLampField(new THREE.MeshStandardMaterial({ color: 0x151515, roughness: 0.8 }));
  const bar = new THREE.Mesh(new THREE.CylinderGeometry(0.014, 0.014, 2.0, 8).rotateZ(Math.PI / 2), steel); g.add(bar);
  for (const sx of [-1, 1]) for (const [o, r] of [[0.72, 0.22], [0.8, 0.18]]) { const pl = new THREE.Mesh(new THREE.CylinderGeometry(r, r, 0.05, 20).rotateZ(Math.PI / 2), rubber); pl.position.x = sx * o; pl.castShadow = true; g.add(pl); }
  scene.add(g);
  return g;
}
const _a = new THREE.Vector3(), _b = new THREE.Vector3(), _x = new THREE.Vector3(1, 0, 0);
function updateBarbell(p) {
  if (!p.hands) { p.hands = [p.obj.getObjectByName('mixamorigLeftHand'), p.obj.getObjectByName('mixamorigRightHand')]; }
  const [l, r] = p.hands; if (!l || !r) return;
  l.getWorldPosition(_a); r.getWorldPosition(_b);
  p.prop.position.copy(_a).add(_b).multiplyScalar(0.5);
  const d = _b.sub(_a); if (d.lengthSq() < 1e-6) return;
  p.prop.quaternion.setFromUnitVectors(_x, d.normalize());
}
