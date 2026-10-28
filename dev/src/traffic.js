// Cars driving along two lanes of the main street and the west cross street. They brake for people
// standing in the lane (most of the time), honk, and knock down anyone they hit at speed.
import * as THREE from 'three';
import { ST } from './street.js';
import * as TX from './textures.js';

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
    this.halo = TX.radial('rgba(255,255,255,1)', 'rgba(255,255,255,0)');
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
    // night lights
    const mk = (color, s) => { const sp = new THREE.Sprite(new THREE.SpriteMaterial({ map: this.halo, color, blending: THREE.AdditiveBlending, depthWrite: false, transparent: true })); sp.scale.setScalar(s); sp.renderOrder = 6; obj.add(sp); return sp; };
    const hl = [-1, 1].map(sx => { const s = mk(new THREE.Color(1, 0.95, 0.85), 1.1); s.position.set(sx * size.x * 0.36, size.y * 0.42, size.z / 2 + 0.05); return s; });
    const tl = [-1, 1].map(sx => { const s = mk(new THREE.Color(1, 0.08, 0.05), 0.7); s.position.set(sx * size.x * 0.38, size.y * 0.48, -size.z / 2 - 0.05); return s; });
    const beam = new THREE.Mesh(new THREE.PlaneGeometry(size.x * 1.1, 9).rotateX(Math.PI / 2).translate(0, 0.03, size.z / 2 + 4.5), new THREE.MeshBasicMaterial({ map: beamTex(), transparent: true, blending: THREE.AdditiveBlending, depthWrite: false, side: THREE.DoubleSide, color: new THREE.Color(0.5, 0.48, 0.4) }));
    beam.renderOrder = 4; obj.add(beam);
    const car = { obj, key, path: this.paths[pi], pi, s: 0, v: sp, cruise: sp, size, hl, tl, beam, police: src.userData.def.police, honkT: 0, brakeChance: Math.random() < 0.85, id: this.n++, siren: src.userData.def.police && Math.random() < 0.6 };
    car.lamp = { pos: new THREE.Vector3(), col: new THREE.Color(1, 0.92, 0.78), r: 10, kind: 'car', dyn: true, w: 1 };
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
    car.lamp.pos.set(x + car.fx * (car.size.z / 2 + 4.5), 0.9, z + car.fz * (car.size.z / 2 + 4.5));
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
      for (const s of car.hl) s.material.opacity = 0.15 + 0.85 * night;
      for (const s of car.tl) s.material.opacity = 0.2 + 0.8 * night;
      car.beam.visible = night > 0.1; car.beam.material.opacity = night;
      car.lamp.w = night;
      if (car.flash) { const ph = Math.floor(t * 6 + car.id) % 2; car.flash.col.setRGB(ph ? 1.6 : 0, 0, ph ? 0 : 2.0); car.flash.w = 1; }
    }
    // retire cars at the end of the path
    this.cars = this.cars.filter(c => { if (c.s >= c.path.L - 0.5) { this.scene.remove(c.obj); return false; } return true; });
    this.lampSources.length = 0;
    for (const c of this.cars) { if (night > 0.05) this.lampSources.push(c.lamp); if (c.flash) this.lampSources.push(c.flash); }
  }
  // the closest car for the engine sound
  nearest(x, z) { let best = null, bd = 1e9; for (const c of this.cars) { const d = Math.hypot(c.x - x, c.z - z); if (d < bd) { bd = d; best = c; } } return best ? { car: best, d: bd } : null; }
  clear() { for (const c of this.cars) this.scene.remove(c.obj); this.cars = []; }
}

let _beam = null;
function beamTex() {
  if (_beam) return _beam;
  const c = document.createElement('canvas'); c.width = 64; c.height = 256; const x = c.getContext('2d');
  const g = x.createLinearGradient(0, 256, 0, 0); g.addColorStop(0, 'rgba(255,255,255,0.9)'); g.addColorStop(1, 'rgba(255,255,255,0)');
  x.fillStyle = g; x.beginPath(); x.moveTo(24, 256); x.lineTo(40, 256); x.lineTo(64, 0); x.lineTo(0, 0); x.fill();
  const t = new THREE.CanvasTexture(c); _beam = t; return t;
}
