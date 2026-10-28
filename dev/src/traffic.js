// Cars driving along two lanes of the main street and the west cross street. They brake for people
// standing in the lane (most of the time), honk, and knock down anyone they hit at speed.
import * as THREE from 'three';
import { ST } from './street.js';

function pathEB() { // from the cross street (north) turning left into the eastbound lane
  const c = new THREE.CurvePath();
  const x = ST.crossX + 2.0, z = ST.laneEB;
  c.add(new THREE.LineCurve3(new THREE.Vector3(x, 0, 130), new THREE.Vector3(x, 0, z + 8)));
  c.add(new THREE.QuadraticBezierCurve3(new THREE.Vector3(x, 0, z + 8), new THREE.Vector3(x, 0, z), new THREE.Vector3(x + 8, 0, z)));
  c.add(new THREE.LineCurve3(new THREE.Vector3(x + 8, 0, z), new THREE.Vector3(150, 0, z)));
  return c;
}
function pathWB() { // from the east, under the viaduct, turning right into the cross street (north)
  const c = new THREE.CurvePath();
  const x = ST.crossX - 2.0, z = ST.laneWB;
  c.add(new THREE.LineCurve3(new THREE.Vector3(150, 0, z), new THREE.Vector3(x + 9, 0, z)));
  c.add(new THREE.QuadraticBezierCurve3(new THREE.Vector3(x + 9, 0, z), new THREE.Vector3(x, 0, z), new THREE.Vector3(x, 0, z + 9)));
  c.add(new THREE.LineCurve3(new THREE.Vector3(x, 0, z + 9), new THREE.Vector3(x, 0, 130)));
  return c;
}
function sample(curve, step = 0.5) {
  const L = curve.getLength(), n = Math.ceil(L / step), pts = [];
  for (let i = 0; i <= n; i++) { const u = i / n; const p = curve.getPointAt(u), t = curve.getTangentAt(u); pts.push([p.x, p.z, Math.atan2(t.x, t.z)]); }
  return { L, pts, at(s) { const f = Math.max(0, Math.min(1, s / L)) * (pts.length - 1), i = Math.floor(f), a = f - i, p = pts[i], q = pts[Math.min(i + 1, pts.length - 1)]; let dh = q[2] - p[2]; if (dh > Math.PI) dh -= Math.PI * 2; if (dh < -Math.PI) dh += Math.PI * 2; return [p[0] + (q[0] - p[0]) * a, p[1] + (q[1] - p[1]) * a, p[2] + dh * a]; } };
}

export class Traffic {
  constructor({ scene, models, sound }) {
    this.scene = scene; this.models = models; this.sound = sound;
    this.paths = [sample(pathEB()), sample(pathWB())];
    this.cars = []; this.spawnT = [1.0, 3.5]; this.lampSources = [];
    this.flare = flareTex(); this.camPos = new THREE.Vector3();
    this.enabled = true;
    const keys = Object.keys(models);
    this.civil = keys.filter(k => !models[k].userData.def.police);
    this.police = keys.filter(k => models[k].userData.def.police);
    this.n = 0;
  }
  spawn(pi) {
    const pool = Math.random() < 0.14 && this.police.length ? this.police : this.civil;
    const key = pool[Math.floor(Math.random() * pool.length)];
    const src = this.models[key];
    const obj = src.clone(); this.scene.add(obj);
    const size = src.userData.size;
    const sp = 9 + Math.random() * 5;
    // night lights: small flares at the real lamp positions + a headlight spot and a red tail glow in the lamp field
    const A = src.userData.anchors;
    const mk = (color, s, p) => { const sp = new THREE.Sprite(new THREE.SpriteMaterial({ map: this.flare, color, blending: THREE.AdditiveBlending, depthWrite: false, transparent: true, opacity: 0 })); sp.scale.setScalar(s); sp.renderOrder = 6; sp.position.copy(p); obj.add(sp); return sp; };
    const hl = A.hl.map(p => mk(new THREE.Color(1, 0.94, 0.82), 0.95, p));
    const tl = A.tl.map(p => mk(new THREE.Color(1, 0.06, 0.03), 0.55, p));
    const car = { obj, key, path: this.paths[pi], pi, s: 0, v: sp, cruise: sp, size, hl, tl, police: src.userData.def.police, honkT: 0, brakeChance: Math.random() < 0.85, id: this.n++, siren: src.userData.def.police && Math.random() < 0.6, hlY: (A.hl[0].y + A.hl[1].y) / 2, front: Math.max(A.hl[0].z, size.z / 2) };
    car.lamp = { pos: new THREE.Vector3(), dir: new THREE.Vector3(), cos: 0.8, col: new THREE.Color(1, 0.93, 0.8).multiplyScalar(4.2), r: 24, kind: 'car', dyn: true, w: 1 };
    car.tail = { pos: new THREE.Vector3(), col: new THREE.Color(1, 0.05, 0.02).multiplyScalar(0.9), r: 4.2, kind: 'tail', dyn: true, w: 1 };
    car.flash = car.police ? { pos: new THREE.Vector3(), col: new THREE.Color(1, 0, 0), r: 9, kind: 'flash', dyn: true, w: 1 } : null;
    this.cars.push(car);
    this.place(car);
    return car;
  }
  place(car) {
    const [x, z, h] = car.path.at(car.s);
    car.x = x; car.z = z; car.h = h;
    car.obj.position.set(x, 0, z); car.obj.rotation.y = h;
    car.fx = Math.sin(h); car.fz = Math.cos(h);
    car.lamp.pos.set(x + car.fx * (car.front - 0.3), car.hlY + 0.05, z + car.fz * (car.front - 0.3)); car.lamp.dir.set(car.fx, -0.13, car.fz).normalize();
    car.tail.pos.set(x - car.fx * (car.size.z / 2 + 0.6), 0.6, z - car.fz * (car.size.z / 2 + 0.6));
    if (car.flash) car.flash.pos.set(x, car.size.y + 0.6, z);
  }
  // fighters: [{pos, y, radius, state, ...}], onHit(fighter, car, dir)
  update(dt, fighters, env, onHit, t) {
    if (!this.enabled) return;
    for (let i = 0; i < 2; i++) {
      this.spawnT[i] -= dt;
      const onPath = this.cars.filter(c => c.pi === i);
      if (this.spawnT[i] <= 0 && onPath.length < 3 && !onPath.some(c => c.s < 14)) { this.spawn(i); this.spawnT[i] = 5 + Math.random() * 9; }
    }
    const night = env.lamps;
    for (const car of this.cars) {
      // look ahead: other cars and people in the lane
      let want = car.cruise, obstacle = null;
      for (const o of this.cars) if (o !== car && o.pi === car.pi && o.s > car.s && o.s - car.s < 16) want = Math.min(want, Math.max(0, (o.s - car.s - car.size.z - 2.5) * 0.9));
      for (const f of fighters) {
        if (!f.root.visible) continue;
        const dx = f.pos.x - car.x, dz = f.pos.z - car.z;
        const ahead = dx * car.fx + dz * car.fz, side = Math.abs(-dx * car.fz + dz * car.fx);
        if (ahead > 0 && ahead < 15 && side < car.size.x / 2 + 0.9) { if (!obstacle || ahead < obstacle.ahead) obstacle = { f, ahead }; }
      }
      if (obstacle) {
        const stop = car.brakeChance || obstacle.f.isPlayer;
        if (stop) want = Math.min(want, Math.max(0, (obstacle.ahead - car.size.z / 2 - 1.6) * 1.2));
        if (car.honkT <= 0 && obstacle.ahead < 12) { this.sound && this.sound.play('horn', { gain: 0.55, pos: { x: car.x, z: car.z }, variant: car.v < 2 ? 1 : 0 }); car.honkT = 2.5 + Math.random() * 2; }
      }
      car.honkT -= dt;
      const acc = want < car.v ? 14 : 3.5;
      car.v += Math.sign(want - car.v) * Math.min(Math.abs(want - car.v), acc * dt);
      car.s += car.v * dt;
      this.place(car);
      // collisions with fighters
      for (const f of fighters) {
        if (!f.root.visible || f.state === 'ko' || f.state === 'down' || f.ghost) continue;
        const dx = f.pos.x - car.x, dz = f.pos.z - car.z;
        const lz = dx * car.fx + dz * car.fz, lx = -dx * car.fz + dz * car.fx;
        const hx = car.size.x / 2 + f.radius * 0.7, hz = car.size.z / 2 + f.radius * 0.7;
        if (Math.abs(lx) < hx && Math.abs(lz) < hz) {
          if (car.v > 3.2 && f.y < 0.8) { onHit && onHit(f, car, new THREE.Vector3(car.fx, 0, car.fz)); }
          // push out sideways
          const push = lx >= 0 ? hx - lx : -hx - lx;
          f.pos.x += -car.fz * push; f.pos.z += car.fx * push;
        }
      }
      // lights
      // flares: bright when seen from the front (head) / behind (tail), almost nothing side-on
      const cdx = this.camPos.x - car.x, cdz = this.camPos.z - car.z, cl = Math.hypot(cdx, cdz) || 1, facing = (cdx * car.fx + cdz * car.fz) / cl;
      const lit = Math.max(night, 0.0), braking = car.v < car.cruise - 1 && car.v > 0.1 ? 1 : 0;
      for (const s of car.hl) { s.material.opacity = lit * (0.12 + 0.88 * Math.pow(Math.max(0, facing), 1.5)); s.scale.setScalar(0.5 + 0.9 * Math.max(0, facing)); }
      for (const s of car.tl) { s.material.opacity = (0.35 + 0.65 * lit) * (braking ? 1 : 0.55) * (0.15 + 0.85 * Math.pow(Math.max(0, -facing), 1.2)); s.scale.setScalar(0.35 + 0.35 * Math.max(0, -facing) + braking * 0.15); }
      car.lamp.w = night; car.tail.w = night * (braking ? 1.6 : 0.8);
      if (car.flash) { const ph = Math.floor(t * 6 + car.id) % 2; car.flash.col.setRGB(ph ? 1.6 : 0, 0, ph ? 0 : 2.0); car.flash.w = 1; }
    }
    // retire cars at the end of the path
    this.cars = this.cars.filter(c => { if (c.s >= c.path.L - 0.5) { this.scene.remove(c.obj); return false; } return true; });
    this.lampSources.length = 0;
    for (const c of this.cars) { if (night > 0.05) this.lampSources.push(c.lamp, c.tail); if (c.flash) this.lampSources.push(c.flash); }
  }
  // the closest car for the engine sound
  nearest(x, z) { let best = null, bd = 1e9; for (const c of this.cars) { const d = Math.hypot(c.x - x, c.z - z); if (d < bd) { bd = d; best = c; } } return best ? { car: best, d: bd } : null; }
  clear() { for (const c of this.cars) this.scene.remove(c.obj); this.cars = []; }
}

let _flare = null;
function flareTex() {
  if (_flare) return _flare;
  const c = document.createElement('canvas'); c.width = c.height = 128; const x = c.getContext('2d');
  const g = x.createRadialGradient(64, 64, 0, 64, 64, 64);
  for (const [o, a] of [[0, 1], [0.07, 0.95], [0.16, 0.42], [0.32, 0.12], [0.6, 0.03], [1, 0]]) g.addColorStop(o, `rgba(255,255,255,${a})`);
  x.fillStyle = g; x.fillRect(0, 0, 128, 128);
  // faint horizontal streak
  const h = x.createLinearGradient(0, 0, 128, 0); h.addColorStop(0, 'rgba(255,255,255,0)'); h.addColorStop(0.5, 'rgba(255,255,255,0.35)'); h.addColorStop(1, 'rgba(255,255,255,0)');
  x.fillStyle = h; x.fillRect(0, 62, 128, 4);
  _flare = new THREE.CanvasTexture(c); return _flare;
}
