// A rigged character: animation layering (locomotion blend + one-shot overlays),
// root motion from clip trajectories, capsule physics and fighting states.
import * as THREE from 'three';
import { STEP_UP } from './world.js';

export const SCALE = 0.024;          // rig units -> metres (a ~2.3 m minotaur)
export const WALK_SPEED = 2.0, RUN_SPEED = 5.0;
const WALK_STRIDE = 49.1 * SCALE * 1.1, RUN_STRIDE = 126.7 * SCALE * 0.72; // metres per gait cycle
const GRAVITY = 18, JUMP_V = 6.3;

export const MOVES = {
  jab: { clip: 'Jab Cross (1)', kind: 'attack', rate: 1.25, end: 1.55, hits: [{ t: 0.43, limb: 'LH', zone: 'head', dmg: 6, force: 1.2 }, { t: 0.70, limb: 'RH', zone: 'head', dmg: 9, force: 1.6 }] },
  hook: { clip: 'Hook', kind: 'attack', rate: 1.3, end: 1.75, hits: [{ t: 1.0, limb: 'LH', zone: 'head', dmg: 13, force: 2.0 }] },
  kick: { clip: 'Mma Kick (1)', kind: 'attack', rate: 1.2, end: 1.25, hits: [{ t: 0.6, limb: 'RF', zone: 'body', dmg: 12, force: 2.4 }] },
  kicks: { clip: 'Kicking', kind: 'attack', rate: 1.15, end: 2.0, hits: [{ t: 1.07, limb: 'RF', zone: 'body', dmg: 7, force: 1.8 }, { t: 1.27, limb: 'LF', zone: 'head', dmg: 14, force: 2.6 }] },
  combo: { clip: 'Kicking (2)', kind: 'attack', rate: 1.15, end: 3.1, hits: [{ t: 1.07, limb: 'RF', zone: 'body', dmg: 6, force: 1.6 }, { t: 1.83, limb: 'RF', zone: 'head', dmg: 16, force: 2.8 }] },
  taunt: { clip: 'Taunt', kind: 'emote', rate: 1 },
  warmup: { clip: 'Warming Up', kind: 'emote', rate: 1 },
  shadow: { clip: 'Boxing (2)', kind: 'emote', rate: 1 },
  guard: { clip: 'Boxing', kind: 'emote', rate: 1 },
  idle: { clip: 'Idle', kind: 'emote', rate: 1 },
  dance: { clip: "Rockin' Shackle A", kind: 'emote', rate: 1 },
  dance2: { clip: "Rockin' Shackle B", kind: 'emote', rate: 1 },
  stepf: { clip: 'Medium Step Forward (1)', kind: 'step', rate: 1.1 },
  stepl: { clip: 'Long Step Forward', kind: 'step', rate: 1.05 },
  stepb: { clip: 'Step Backward', kind: 'step', rate: 1.1 },
  hitHead: { clip: 'Head Hit', kind: 'hit', rate: 1.15 },
  hitHead2: { clip: 'Head Hit (2)', kind: 'hit', rate: 1.2, end: 1.25 },
  hitBody: { clip: 'Hit To Body', kind: 'hit', rate: 1.2, end: 1.2 },
  ko: { clip: 'Flying Back Death', kind: 'ko', rate: 1.0 },
  getup: { clip: 'Kip Up (1)', kind: 'getup', rate: 1.05 },
  dodge: { clip: 'Dodging', kind: 'dodge', rate: 1.55, end: 2.2 },
  bigHit: { clip: 'Big Hit To Head', kind: 'hit', rate: 1.15, end: 1.45 },
  doorPush: { clip: 'Opening (1)', kind: 'door', rate: 1.25 },
  doorPull: { clip: 'Opening', kind: 'door', rate: 1.25 },
  // specials (Brax: knee / elbow / body jab, Nyxa: martelo / double kick / breakdance)
  knee: { clip: 'Illegal Knee', kind: 'attack', rate: 1.25, end: 1.75, hits: [{ t: 0.6, limb: 'RK', zone: 'body', dmg: 17, force: 2.6 }] },
  elbow: { clip: 'Illegal Elbow Punch', kind: 'attack', rate: 1.25, end: 1.55, hits: [{ t: 0.57, limb: 'RE', zone: 'head', dmg: 16, force: 2.4 }] },
  bodyjab: { clip: 'Body Jab Cross', kind: 'attack', rate: 1.2, end: 1.2, hits: [{ t: 0.3, limb: 'LH', zone: 'body', dmg: 7, force: 1.2 }, { t: 0.53, limb: 'RH', zone: 'head', dmg: 11, force: 1.8 }] },
  martelo: { clip: 'Martelo Do Chau', kind: 'attack', rate: 1.3, end: 2.3, hits: [{ t: 1.23, limb: 'RF', zone: 'head', dmg: 19, force: 3.0 }] },
  mma2: { clip: 'Mma Kick (1) (2)', kind: 'attack', rate: 1.25, end: 1.5, hits: [{ t: 0.7, limb: 'RF', zone: 'head', dmg: 15, force: 2.6 }] },
  breakdance: { clip: 'Breakdance Freezes', kind: 'emote', rate: 1 },
  // weapon (bat / club): CLUB limb is the weapon tip
  bat1: { clip: 'Standing Melee Attack Downward', kind: 'attack', rate: 1.35, end: 1.9, hits: [{ t: 0.83, limb: 'CLUB', zone: 'head', dmg: 22, force: 3.0, weapon: true }] },
  bat2: { clip: 'Standing Melee Attack 360 High', kind: 'attack', rate: 1.5, end: 2.4, hits: [{ t: 1.07, limb: 'CLUB', zone: 'head', dmg: 20, force: 3.2, weapon: true }] },
  bat3: { clip: 'Standing Melee Combo Attack Ver. 3', kind: 'attack', rate: 1.35, end: 2.5, hits: [{ t: 1.03, limb: 'CLUB', zone: 'body', dmg: 14, force: 2.2, weapon: true }, { t: 1.77, limb: 'CLUB', zone: 'head', dmg: 22, force: 3.2, weapon: true }] },
  club4: { clip: 'Standing Melee Attack 360 Low', kind: 'attack', rate: 1.35, end: 2.1, hits: [{ t: 0.93, limb: 'CLUB', zone: 'body', dmg: 15, force: 2.6, weapon: true }] },
  clubKick: { clip: 'Standing Melee Attack Kick Ver. 2', kind: 'attack', rate: 1.2, end: 1.35, hits: [{ t: 0.62, limb: 'RF', zone: 'body', dmg: 12, force: 2.4 }] },
  hitHeavy: { clip: 'Standing React Large From Right', kind: 'hit', rate: 1.2, end: 1.5 },
  zhit: { clip: 'Zombie Reaction Hit', kind: 'hit', rate: 1.3, end: 1.6 },
  dying: { clip: 'Dying', kind: 'ko', rate: 1.0 },
};

const LIMB_BONES = { LH: 'mixamorigLeftHand', RH: 'mixamorigRightHand', LF: 'mixamorigLeftToeBase', RF: 'mixamorigRightToeBase', HD: 'mixamorigHead',
  RK: 'mixamorigRightLeg', LK: 'mixamorigLeftLeg', RE: 'mixamorigRightForeArm', LE: 'mixamorigLeftForeArm' };
const tmpQ = new THREE.Quaternion();
export const angDiff = (a, b) => { let d = (b - a) % (Math.PI * 2); if (d > Math.PI) d -= Math.PI * 2; if (d < -Math.PI) d += Math.PI * 2; return d; };
const smooth = (a, b, x) => { const t = Math.max(0, Math.min(1, (x - a) / (b - a))); return t * t * (3 - 2 * t); };

export class Fighter {
  constructor({ model, clips, meta, name, world, radius = 0.6, height = 2.2, size = 1, moves = null, isPlayer = false }) {
    this.name = name; this.world = world; this.meta = meta; this.k = size; this.isPlayer = isPlayer;
    this.moves = moves ? { ...MOVES, ...moves } : MOVES;
    radius *= size; height *= size;
    this.root = new THREE.Group(); this.root.name = name;
    this.model = model; model.scale.setScalar(SCALE * size);
    this.root.add(model);
    model.traverse(o => { if (o.isSkinnedMesh) { this.mesh = o; o.castShadow = true; o.receiveShadow = true; o.frustumCulled = false; } });
    this.hips = model.getObjectByName('mixamorigHips');
    this.limbs = {}; for (const k in LIMB_BONES) this.limbs[k] = model.getObjectByName(LIMB_BONES[k]);
    this.mixer = new THREE.AnimationMixer(model);
    this.clips = {}; for (const c of clips) this.clips[c.name] = c;
    this.actions = {};
    const it = this.clips['Idle'].tracks.find(t => t.name === 'mixamorigHips.quaternion');
    this.qIdleInv = new THREE.Quaternion().fromArray(it.values, 0).invert();

    this.pos = this.root.position; this.y = 0; this.vy = 0; this.onGround = true;
    this.vel = new THREE.Vector2(); this.heading = 0; this.speed = 0;
    this.radius = radius; this.height = height;
    this.col = { x: 0, z: 0, r: radius, top: height, bottom: 0, mobile: true, owner: this, noStand: true, onPush: (dx, dz) => { this.pushX += dx; this.pushZ += dz; } };
    this.pushX = 0; this.pushZ = 0;
    world.dynamic.push(this.col);
    this.knock = new THREE.Vector2();
    this.idleW = { Idle: 1 }; this.idleClip = 'Idle';
    this.phase = 0;
    this.overlays = [];
    this.state = 'loco'; this.stateT = 0;
    this.hp = 100; this.maxHp = 100;
    this.input = { x: 0, z: 0, mag: 0, run: true };
    this.events = []; this.faceTarget = null; this.invuln = 0; this.queued = null;
    this.onState = null;
    // locomotion clips: prefer the Mixamo walk/run cycles when present
    this.walkClip = this.clips['Walking'] ? 'Walking' : 'Walk';
    this.runClip = this.clips['Running'] ? 'Running' : 'Run';
    this.walkStride = (meta.Walking && meta.Walking.stride && this.walkClip === 'Walking' ? meta.Walking.stride * SCALE : WALK_STRIDE) * size;
    this.runStride = (meta.Running && meta.Running.stride && this.runClip === 'Running' ? meta.Running.stride * SCALE : RUN_STRIDE) * size;
    this.speedMul = 1;
    this.walkPh = this.walkClip === 'Walking' ? (meta.Walking.phase || 0) : 0;
    this.runPh = this.runClip === 'Running' ? (meta.Running.phase || 0) : 0;
  }

  act(name) {
    if (!this.actions[name]) {
      const a = this.mixer.clipAction(this.clips[name]);
      a.setLoop(THREE.LoopRepeat, Infinity); a.timeScale = 0; a.setEffectiveWeight(0); a.play();
      this.actions[name] = a;
    }
    return this.actions[name];
  }
  setIdle(clip) { if (this.clips[clip]) this.idleClip = clip; }
  setState(s) { this.state = s; this.stateT = 0; this.onState && this.onState(s); }

  // ---------------- overlays ----------------
  play(clipName, { rate = 1, fade = 0.15, t0 = 0, end, onEnd, hold = false, events = [], rm = true, manual = false } = {}) {
    for (const o of this.overlays) { o.target = 0; o.fade = Math.max(0.05, fade); o.ending = true; }
    const meta = this.meta[clipName] || {};
    const dur = this.clips[clipName].duration;
    const o = { name: clipName, action: this.act(clipName), t: t0, prevT: t0, rate, w: fade <= 0 ? 1 : 0, target: 1, fade, dur,
      end: Math.min(end ?? dur, dur - 1e-3), traj: rm && meta.rm ? meta.traj : null, events: events.map(e => ({ ...e, fired: false })),
      onEnd, hold, ending: false, manual, done: false };
    if (fade <= 0) for (const x of this.overlays) x.w = 0;
    this.overlays.push(o);
    return o;
  }
  stopOverlays(fade = 0.2) { for (const o of this.overlays) { o.target = 0; o.fade = fade; o.ending = true; } }
  get top() { for (let i = this.overlays.length - 1; i >= 0; i--) if (!this.overlays[i].ending) return this.overlays[i]; return null; }
  trajAt(traj, t) {
    const f = t * 30, i = Math.floor(f), n = traj.length / 2 - 1;
    if (i >= n) return [traj[n * 2], traj[n * 2 + 1]];
    if (i < 0) return [traj[0], traj[1]];
    const a = f - i;
    return [traj[i * 2] + (traj[i * 2 + 2] - traj[i * 2]) * a, traj[i * 2 + 1] + (traj[i * 2 + 3] - traj[i * 2 + 1]) * a];
  }

  // ---------------- actions ----------------
  get busy() { return !['loco'].includes(this.state); }
  backToLoco(fade = 0.25) { this.stopOverlays(fade); this.setState('loco'); }

  has(clip) { return !!this.clips[clip]; }
  doMove(key, opts = {}) {
    const m = this.moves[key];
    if (!m || !this.clips[m.clip]) return false;
    if (this.state === 'attack' && m.kind === 'attack') {
      const top = this.top;
      if (top && top.t > top.end * 0.4) { this.queued = key; }
      return false;
    }
    if (this.state !== 'loco' && !(opts.force)) {
      if (!(this.state === 'emote' && (m.kind === 'attack' || m.kind === 'emote'))) return false;
    }
    if (!this.onGround) return false;
    const kind = m.kind === 'step' ? 'emote' : m.kind;
    this.setState(kind === 'attack' ? 'attack' : kind === 'hit' ? 'hit' : kind === 'ko' ? 'ko' : kind === 'getup' ? 'getup' : kind === 'dodge' ? 'dodge' : kind === 'door' ? 'door' : 'emote');
    this.speed *= 0.3;
    this.play(m.clip, { rate: m.rate, end: m.end, fade: m.kind === 'attack' ? 0.1 : 0.22, events: m.hits || [], onEnd: () => {
      if (this.queued) { const q = this.queued; this.queued = null; this.setState('loco'); this.doMove(q); return; }
      if (key === 'ko') { this.setState('down'); return; }
      this.backToLoco(m.kind === 'attack' ? 0.2 : 0.3);
    }, hold: key === 'ko' });
    this.lastMove = key;
    return true;
  }

  hit(zone, dmg, dir, attacker) {
    if (['ko', 'down', 'getup', 'pair', 'door'].includes(this.state) || this.invuln > 0) return false;
    if (this.state === 'dodge') { const t = this.top ? this.top.t : 0; if (t > 0.08 && t < 1.4) return 'dodged'; }
    if (this.state === 'block' && attacker) {
      const toA = Math.atan2(attacker.pos.x - this.pos.x, attacker.pos.z - this.pos.z);
      if (Math.abs(angDiff(this.heading, toA)) < 1.6) {
        this.hp = Math.max(1, this.hp - dmg * 0.12);
        this.knock.set(dir.x, dir.z).multiplyScalar(0.6 + dmg * 0.05);
        if (this.blockO) this.blockO.t = Math.max(0.35, this.blockO.t - 0.12);
        return 'blocked';
      }
    }
    this.hp = Math.max(0, this.hp - dmg);
    this.queued = null;
    if (attacker) this.heading = Math.atan2(attacker.pos.x - this.pos.x, attacker.pos.z - this.pos.z);
    if (this.hp <= 0) { this.ko(dir); return 'ko'; }
    this.knock.set(dir.x, dir.z).multiplyScalar(1.2 + dmg * 0.12);
    this.blockO = null;
    let key = zone === 'body' ? 'hitBody' : dmg >= 14 ? 'bigHit' : dmg >= 10 ? 'hitHead2' : 'hitHead';
    if (this.reactKey) key = this.reactKey(zone, dmg, key) || key;
    if (!this.clips[this.moves[key].clip]) key = this.clips[this.moves.hitHead.clip] ? 'hitHead' : 'hitBody';
    this.stopOverlays(0.06);
    this.setState('hit');
    const m = this.moves[key];
    this.play(m.clip, { rate: m.rate, end: m.end, fade: 0.06, onEnd: () => this.backToLoco(0.25) });
    this.invuln = 0.12;
    return true;
  }

  ko(dir) {
    this.stopOverlays(0.08);
    this.heading = Math.atan2(-dir.x, -dir.z);
    this.setState('ko');
    this.play(this.moves[this.koMove || 'ko'].clip, { rate: 1, fade: 0.08, hold: true, onEnd: () => this.setState('down') });
  }

  getUp() {
    this.setState('getup');
    if (!this.clips[this.moves.getup.clip]) { this.hp = this.maxHp; this.backToLoco(0.3); return; }
    this.play(this.moves.getup.clip, { rate: this.moves.getup.rate, fade: 0.35, onEnd: () => { if (!this.noRefill) this.hp = this.maxHp; this.invuln = 1.0; this.backToLoco(0.3); } });
  }

  startBlock() {
    if (this.state === 'block') return true;
    if (!['loco', 'emote'].includes(this.state) || !this.onGround || !this.clips['Center Block']) return false;
    this.setState('block');
    this.speed *= 0.2;
    this.blockO = this.play('Center Block', { fade: 0.1, t0: 0.18, manual: true, rm: false });
    return true;
  }
  updateBlock(dt) {
    const o = this.blockO; if (!o) { this.backToLoco(0.2); return; }
    o.prevT = o.t;
    const hold = this.input.block || (this.blockTimer || 0) > 0;
    if (this.blockTimer > 0) this.blockTimer -= dt;
    if (hold) o.t = Math.min(0.62, o.t + dt * 1.8);
    else { o.t += dt * 1.8; if (o.t >= 1.25) { this.blockO = null; this.backToLoco(0.2); } }
  }

  jump() {
    if (this.state !== 'loco' || !this.onGround) return false;
    this.setState('jump');
    this.jumpPhase = 'crouch';
    this.jumpO = this.play('Jump', { fade: 0.08, t0: 0.34, manual: true, rm: false });
    return true;
  }
  updateJump(dt) {
    const o = this.jumpO; if (!o) return;
    o.prevT = o.t;
    if (this.jumpPhase === 'crouch') {
      o.t += dt * 2.0;
      if (o.t >= 0.78) { this.jumpPhase = 'air'; this.vy = JUMP_V; this.onGround = false; this.airT = 0; this.y += 0.02; }
    } else if (this.jumpPhase === 'air') {
      this.airT += dt;
      o.t = 0.78 + 0.5 * Math.min(1, this.airT / (2 * JUMP_V / GRAVITY));
      if (this.onGround && this.airT > 0.05) { this.jumpPhase = 'land'; o.t = Math.max(o.t, 1.3); }
    } else {
      o.t += dt * 1.8;
      const moving = this.input.mag > 0.1;
      if (o.t >= 1.78 || (moving && o.t > 1.42)) { this.jumpO = null; this.backToLoco(moving ? 0.15 : 0.25); }
    }
  }

  // world-space facing derived from the hips (used when re-rooting after big clips)
  facingFromHips() {
    tmpQ.copy(this.hips.quaternion).multiply(this.qIdleInv);
    const f = new THREE.Vector3(0, 0, 1).applyQuaternion(tmpQ);
    return this.heading + Math.atan2(f.x, f.z);
  }
  hipsWorld(out = new THREE.Vector3()) { return this.hips.getWorldPosition(out); }
  attach(obj, bone = 'mixamorigRightHand') { const b = this.model.getObjectByName(bone); b.add(obj); return obj; }
  addLimb(key, obj) { this.limbs[key] = obj; }
  limbWorld(k, out = new THREE.Vector3()) { return this.limbs[k].getWorldPosition(out); }

  // ---------------- per-frame ----------------
  update(dt) {
    this.stateT += dt;
    if (this.invuln > 0) this.invuln -= dt;
    if (this.state === 'jump') this.updateJump(dt);
    if (this.state === 'block') this.updateBlock(dt);
    if (this.state === 'down' && this.stateT > (this.downTime ?? 1.3)) this.getUp();
    const inp = this.input;
    let rmX = 0, rmZ = 0;

    let ovSum = 0;
    for (const o of this.overlays) {
      o.w += Math.sign(o.target - o.w) * (o.fade > 0 ? dt / o.fade : 1);
      o.w = Math.max(0, Math.min(1, o.w));
      if (!o.manual) { o.prevT = o.t; o.t = Math.min(o.end, o.t + dt * o.rate); }
      if (o.traj) {
        const a = this.trajAt(o.traj, o.prevT), b = this.trajAt(o.traj, o.t);
        const wgt = o.ending ? o.w : 1;
        rmX += (b[0] - a[0]) * SCALE * this.k * wgt; rmZ += (b[1] - a[1]) * SCALE * this.k * wgt;
      }
      if (!o.ending) for (const e of o.events) if (!e.fired && o.t >= e.t) { e.fired = true; this.events.push({ ...e, by: this, clip: o.name }); }
      if (!o.ending && !o.manual && o.t >= o.end - 1e-4 && !o.done) {
        o.done = true;
        if (o.onEnd) o.onEnd(o);
        else if (!o.hold) { o.target = 0; o.ending = true; }
      }
      ovSum += o.w;
    }
    this.overlays = this.overlays.filter(o => !(o.ending && o.w <= 0));
    if (ovSum > 1) { for (const o of this.overlays) o.w /= ovSum; ovSum = 1; }

    // locomotion intent
    const locoFree = this.state === 'loco';
    const moving = locoFree && inp.mag > 0.08;
    const tSpeed = (inp.mag > 0.08 ? (inp.run ? (inp.mag > 0.62 ? RUN_SPEED : WALK_SPEED * 1.1) : WALK_SPEED * Math.min(1, 0.35 + inp.mag)) : 0) * this.speedMul * this.k;
    if (this.state === 'jump') {
      const want = inp.mag > 0.08 ? Math.max(tSpeed, 0) : 0;
      this.vel.x += (inp.x / (inp.mag || 1) * want - this.vel.x) * Math.min(1, dt * (this.onGround ? 6 : 1.8));
      this.vel.y += (inp.z / (inp.mag || 1) * want - this.vel.y) * Math.min(1, dt * (this.onGround ? 6 : 1.8));
      if (inp.mag > 0.1) this.heading += angDiff(this.heading, Math.atan2(inp.x, inp.z)) * Math.min(1, dt * 5);
    } else if (locoFree) {
      const target = moving ? tSpeed : 0;
      const accel = target > this.speed ? 12 : 16;
      this.speed += Math.sign(target - this.speed) * Math.min(Math.abs(target - this.speed), accel * dt);
      if (moving) this.heading += angDiff(this.heading, Math.atan2(inp.x, inp.z)) * Math.min(1, dt * 10);
      this.vel.set(Math.sin(this.heading) * this.speed, Math.cos(this.heading) * this.speed);
    } else {
      this.speed = Math.max(0, this.speed - 20 * dt);
      this.vel.multiplyScalar(Math.max(0, 1 - dt * 10));
    }
    if (this.faceTarget && ['attack', 'emote', 'block', 'dodge'].includes(this.state)) {
      const want = Math.atan2(this.faceTarget.x - this.pos.x, this.faceTarget.z - this.pos.z);
      this.heading += angDiff(this.heading, want) * Math.min(1, dt * 12);
    }

    // base layer
    const v = locoFree ? this.speed : 0;
    const wIdle = 1 - smooth(0.12, 0.7, v);
    const wRun = smooth(2.6, 4.2, v);
    const wWalk = Math.max(0, 1 - wIdle - wRun);
    const stride = this.walkStride * (1 - wRun) + this.runStride * wRun;
    if (v > 0.05) this.phase = (this.phase + dt * v / stride) % 1;
    if (!(this.idleClip in this.idleW)) this.idleW[this.idleClip] = 0;
    let iws = 0;
    for (const k in this.idleW) { this.idleW[k] += ((k === this.idleClip ? 1 : 0) - this.idleW[k]) * Math.min(1, dt * 5); iws += this.idleW[k]; }
    const baseW = 1 - ovSum;
    for (const k in this.idleW) { const a = this.act(k); a.timeScale = 1; a.setEffectiveWeight(baseW * wIdle * this.idleW[k] / iws); }
    const aw = this.act(this.walkClip), ar = this.act(this.runClip);
    aw.time = ((this.phase + this.walkPh) % 1) * aw.getClip().duration; ar.time = ((this.phase + this.runPh) % 1) * ar.getClip().duration;
    aw.setEffectiveWeight(baseW * wWalk); ar.setEffectiveWeight(baseW * wRun);
    const live = new Set(this.overlays.map(o => o.action));
    for (const o of this.overlays) { o.action.time = Math.min(o.t, o.dur - 1e-3); }
    for (const [k, a] of Object.entries(this.actions)) {
      if (k in this.idleW || k === this.walkClip || k === this.runClip) continue;
      if (!live.has(a)) a.setEffectiveWeight(0);
    }
    // the same clip can be listed twice (re-triggered); sum its weights
    const wsum = new Map();
    for (const o of this.overlays) wsum.set(o.action, (wsum.get(o.action) || 0) + o.w);
    for (const [a, w] of wsum) a.setEffectiveWeight(w);
    // if base idle clip is also an overlay clip, the overlay wins
    this.mixer.update(dt);

    // ---------------- movement + collision ----------------
    const c = Math.cos(this.heading), s = Math.sin(this.heading);
    const dx = this.vel.x * dt + (rmX * c + rmZ * s) + this.knock.x * dt + this.pushX;
    const dz = this.vel.y * dt + (-rmX * s + rmZ * c) + this.knock.y * dt + this.pushZ;
    this.pushX = this.pushZ = 0;
    this.knock.multiplyScalar(Math.max(0, 1 - dt * 6));
    const steps = Math.ceil(Math.hypot(dx, dz) / 0.2) || 1;
    for (let i = 0; i < steps; i++) {
      this.pos.x += dx / steps; this.pos.z += dz / steps;
      if (!this.ghost) this.world.resolve(this.pos, this.radius, this.y, this.height, this.col, this.partner);
    }
    const g = this.ghost ? this.y : this.world.groundAt(this.pos.x, this.pos.z, this.y, this.radius);
    if (this.ghost) { /* paired clip: root is offset from the body, keep the floor height fixed */ }
    else if (this.onGround) {
      if (g > this.y) this.y = Math.min(g, this.y + Math.max(dt * 4, (g - this.y) * Math.min(1, dt * 18)));
      else if (g < this.y - 0.02) {
        if (this.y - g < STEP_UP + 0.05) this.y = Math.max(g, this.y - dt * 4);
        else { this.onGround = false; this.vy = 0; }
      }
    } else {
      this.vy -= GRAVITY * dt; this.y += this.vy * dt;
      if (this.y <= g) { this.y = g; this.onGround = true; this.landed = this.vy; this.vy = 0; }
    }
    this.root.position.y = this.y;
    this.root.rotation.y = this.heading;
    this.col.x = this.pos.x; this.col.z = this.pos.z; this.col.bottom = this.y; this.col.top = this.y + this.height;
  }
}
