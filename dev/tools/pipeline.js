// Character pipeline: clean skinned models, analyse + process Mixamo clips,
// synthesise Run / Walk on the same skeleton, export compact GLB.
import * as THREE from 'three';
import { GLTFExporter } from 'three/addons/exporters/GLTFExporter.js';
import { mergeVertices } from 'three/addons/utils/BufferGeometryUtils.js';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';

const S = window.S;
const DEG = Math.PI / 180;
const X = new THREE.Vector3(1, 0, 0), Y = new THREE.Vector3(0, 1, 0), Z = new THREE.Vector3(0, 0, 1);
const FPS = 30;

export function boneMap(root) { const m = {}; root.traverse(o => { if (o.isBone) m[o.name.replace('mixamorig', '')] = o; }); return m; }
const wp = (o) => o.getWorldPosition(new THREE.Vector3());

function sampleClip(root, clip, fps, fn) {
  const mixer = new THREE.AnimationMixer(root);
  const a = mixer.clipAction(clip); a.setLoop(THREE.LoopOnce, 1); a.clampWhenFinished = true; a.play();
  const n = Math.max(2, Math.round(clip.duration * fps) + 1);
  for (let i = 0; i < n; i++) {
    const t = Math.min(clip.duration, i / fps);
    mixer.setTime(t); root.updateMatrixWorld(true); fn(t, i, n);
  }
  a.stop(); mixer.uncacheRoot(root);
}

// ---------------- analysis ----------------
export function analyze(name, key = 'mino') {
  const root = S.models[key]; const B = boneMap(root); const clip = S.clips[name];
  const rows = [];
  sampleClip(root, clip, FPS, (t) => {
    const h = wp(B.Hips);
    const r = { t: +t.toFixed(3), hips: h.toArray() };
    for (const [k, b] of [['LH', 'LeftHand'], ['RH', 'RightHand'], ['LF', 'LeftToeBase'], ['RF', 'RightToeBase'], ['HD', 'Head']]) r[k] = wp(B[b]).toArray();
    rows.push(r);
  });
  return rows;
}

// Find strike events: local maxima of horizontal reach of a limb from the hips.
export function strikes(name) {
  const rows = analyze(name);
  const out = [];
  for (const L of ['LH', 'RH', 'LF', 'RF']) {
    const reach = rows.map(r => Math.hypot(r[L][0] - r.hips[0], r[L][2] - r.hips[2]));
    const speed = reach.map((v, i) => i ? (v - reach[i - 1]) * FPS : 0);
    for (let i = 2; i < reach.length - 2; i++) {
      if (reach[i] >= reach[i - 1] && reach[i] >= reach[i + 1] && reach[i] >= reach[i - 2] && reach[i] >= reach[i + 2]) {
        const maxSpeed = Math.max(...speed.slice(Math.max(0, i - 6), i + 1));
        const thr = L[1] === 'H' ? 36 : 40;
        if (reach[i] > thr && maxSpeed > 60) out.push({ t: rows[i].t, limb: L, reach: +reach[i].toFixed(1), y: +rows[i][L][1].toFixed(1), speed: +maxSpeed.toFixed(0),
          dir: [+(rows[i][L][0] - rows[i].hips[0]).toFixed(1), +(rows[i][L][2] - rows[i].hips[2]).toFixed(1)] });
      }
    }
  }
  return out.sort((a, b) => a.t - b.t);
}

// ---------------- model cleanup ----------------
export function prepModel(key) {
  const src = S.models[key];
  let mesh; src.traverse(o => { if (o.isSkinnedMesh) mesh = o; });
  src.updateMatrixWorld(true);
  const g = mesh.geometry.clone();
  g.applyMatrix4(mesh.bindMatrix);
  // drop morph/unused attributes, merge duplicated vertices
  for (const k of Object.keys(g.attributes)) if (!['position', 'normal', 'uv', 'skinIndex', 'skinWeight'].includes(k)) g.deleteAttribute(k);
  g.clearGroups();
  const merged = mergeVertices(g, 1e-5);
  const root = new THREE.Group(); root.name = key;
  const hips = src.children.find(c => c.isBone);
  const mat = new THREE.MeshStandardMaterial({ name: 'skin_' + key });
  const sm = new THREE.SkinnedMesh(merged, mat); sm.name = 'Body';
  root.add(sm); root.add(hips);
  root.updateMatrixWorld(true);
  sm.bind(mesh.skeleton, new THREE.Matrix4());
  return { root, verts: merged.attributes.position.count, tris: merged.index.count / 3 };
}

// ---------------- procedural locomotion ----------------
const wrap = (d) => d - Math.round(d);
const bump = (x, c, w) => Math.exp(-0.5 * (wrap(x - c) / w) ** 2);

const GAITS = {
  Run: { period: 0.72, hipFlex: (p) => 12 + 30 * Math.cos(2 * Math.PI * (p - 0.85)),
    knee: (p) => 14 + 24 * bump(p, 0.15, 0.09) + 92 * bump(p, 0.63, 0.13),
    ankle: (p) => -4 - 12 * bump(p, 0.18, 0.1) + 30 * bump(p, 0.42, 0.07),
    toe: (p) => -22 * bump(p, 0.4, 0.06),
    yaw: 8, tilt: 9, lean: 13, armA: 30, armBias: -8, elbow: 68, elbowA: 26, abduct: 12, lift: 3.2, roll: 3 },
  Walk: { period: 1.1, hipFlex: (p) => 7 + 18 * Math.cos(2 * Math.PI * (p - 0.88)),
    knee: (p) => 6 + 12 * bump(p, 0.12, 0.08) + 56 * bump(p, 0.7, 0.11),
    ankle: (p) => -2 - 8 * bump(p, 0.25, 0.12) + 16 * bump(p, 0.56, 0.07),
    toe: (p) => -16 * bump(p, 0.56, 0.06),
    yaw: 5, tilt: 3, lean: 4, armA: 18, armBias: 2, elbow: 22, elbowA: 10, abduct: 9, lift: 0, roll: 2 },
};

export function makeGait(name) {
  const G = GAITS[name];
  const obj = S.animObjs['Boxing'];
  const B = boneMap(obj);
  const names = Object.keys(B);
  const rest = {}; for (const n of names) rest[n] = { q: B[n].quaternion.clone(), p: B[n].position.clone() };
  // fist pose for fingers: sample Boxing clip once
  const fist = {};
  sampleClip(obj, S.clips['Boxing'], 2, (t, i) => { if (i === 1) for (const n of names) if (/Hand(Index|Middle|Ring|Pinky|Thumb)/.test(n)) fist[n] = B[n].quaternion.clone(); });
  // reference foot heights from the rest pose
  const resetPose = () => { for (const n of names) { B[n].quaternion.copy(rest[n].q); B[n].position.copy(rest[n].p); } for (const n in fist) B[n].quaternion.copy(fist[n]); obj.updateMatrixWorld(true); };
  resetPose();
  const restY = { Foot: wp(B.LeftFoot).y, Toe: wp(B.LeftToeBase).y, End: wp(B.LeftToe_End).y };

  const rotW = (bone, axis, deg) => {
    if (!deg) return;
    const pw = bone.parent.getWorldQuaternion(new THREE.Quaternion());
    const r = new THREE.Quaternion().setFromAxisAngle(axis, deg * DEG);
    bone.quaternion.copy(pw.clone().invert().multiply(r).multiply(pw).multiply(bone.quaternion));
    bone.updateMatrixWorld(true);
  };
  const rotQW = (bone, r) => {
    const pw = bone.parent.getWorldQuaternion(new THREE.Quaternion());
    bone.quaternion.copy(pw.clone().invert().multiply(r).multiply(pw).multiply(bone.quaternion));
    bone.updateMatrixWorld(true);
  };
  const aim = (bone, child, dir) => {
    const cur = wp(child).sub(wp(bone)).normalize();
    rotQW(bone, new THREE.Quaternion().setFromUnitVectors(cur, dir.clone().normalize()));
  };

  const pose = (phi) => {
    resetPose();
    B.Hips.position.set(0, rest.Hips.p.y, 0);
    obj.updateMatrixWorld(true);
    const yaw = -G.yaw * Math.cos(2 * Math.PI * (phi - 0.85));
    rotW(B.Hips, Y, yaw);
    rotW(B.Hips, X, G.tilt);
    rotW(B.Hips, Z, G.roll * Math.sin(2 * Math.PI * (phi - 0.15)));
    const sp = (G.lean - G.tilt) / 3;
    for (const s of ['Spine', 'Spine1', 'Spine2']) { rotW(B[s], X, sp); rotW(B[s], Y, -yaw * 0.45); }
    rotW(B.Neck, X, -G.lean * 0.35); rotW(B.Head, X, -G.lean * 0.45);
    // legs
    for (const [side, ph] of [['Left', phi], ['Right', (phi + 0.5) % 1]]) {
      rotW(B[side + 'UpLeg'], X, -(G.hipFlex(ph) + G.tilt));
      rotW(B[side + 'Leg'], X, G.knee(ph));
      rotW(B[side + 'Foot'], X, G.ankle(ph));
      rotW(B[side + 'ToeBase'], X, G.toe(ph));
    }
    // arms: swing opposite to the same-side leg
    for (const [side, sgn, ph] of [['Left', 1, (phi + 0.5) % 1], ['Right', -1, phi]]) {
      const sw = Math.cos(2 * Math.PI * (ph - 0.85)); // +1 when this arm is forward
      const upper = new THREE.Vector3(sgn * Math.sin(G.abduct * DEG), -Math.cos(G.abduct * DEG), 0)
        .applyAxisAngle(X, -(G.armA * sw + G.armBias) * DEG);
      aim(B[side + 'Arm'], B[side + 'ForeArm'], upper);
      const fore = upper.clone().applyAxisAngle(X, -(G.elbow + G.elbowA * (sw * 0.5 + 0.5)) * DEG)
        .add(new THREE.Vector3(-sgn * 0.18, 0, 0)).normalize();
      aim(B[side + 'ForeArm'], B[side + 'Hand'], fore);
    }
    // ground contact
    const contact = (side) => Math.min(wp(B[side + 'Foot']).y - restY.Foot, wp(B[side + 'ToeBase']).y - restY.Toe, wp(B[side + 'Toe_End']).y - restY.End);
    const low = Math.min(contact('Left'), contact('Right'));
    const lift = G.lift * (bump(phi, 0.45, 0.055) + bump(phi, 0.95, 0.055));
    B.Hips.position.y = rest.Hips.p.y - low + lift;
    obj.updateMatrixWorld(true);
  };

  const n = Math.round(G.period * FPS);
  const times = [], qv = {}, hp = [];
  for (const nm of names) qv[nm] = [];
  const feet = [];
  for (let i = 0; i <= n; i++) {
    const phi = (i / n) % 1;
    pose(phi);
    times.push(i / FPS * (G.period * FPS / n));
    for (const nm of names) qv[nm].push(...B[nm].quaternion.toArray());
    hp.push(...B.Hips.position.toArray());
    feet.push({ L: wp(B.LeftToeBase), R: wp(B.RightToeBase), Ly: wp(B.LeftFoot).y, Ry: wp(B.RightFoot).y });
  }
  // stance foot speed (rig units / s): the lower foot travels backwards in root space
  let dist = 0, tsum = 0;
  for (let i = 1; i < feet.length; i++) {
    const side = feet[i].Ly < feet[i].Ry ? 'L' : 'R';
    const dz = feet[i - 1][side].z - feet[i][side].z;
    if (dz > 0) { dist += dz; tsum += 1 / FPS; }
  }
  const speed = dist / (G.period);
  resetPose();
  const tracks = [new THREE.VectorKeyframeTrack('mixamorigHips.position', times, hp)];
  for (const nm of names) if (!nm.endsWith('_End') && !nm.endsWith('4')) tracks.push(new THREE.QuaternionKeyframeTrack('mixamorig' + nm + '.quaternion', times, qv[nm]));
  const clip = new THREE.AnimationClip(name, times[times.length - 1], tracks);
  S.clips[name] = clip;
  return { name, frames: times.length, speed: +speed.toFixed(1), duration: clip.duration };
}

// ---------------- proper retargeting (world-space, rest-relative) ----------------
// target_world(b,t) = src_world(b,t) * inverse(src_rest_world(b)) * C * model_bind_world(b)
// where C turns the model's Z-up bind pose upright. Then convert to locals along the model hierarchy.
const C_UP = new THREE.Quaternion().setFromAxisAngle(X, -Math.PI / 2);
export function srcRest(obj) {
  if (obj.userData.rest) return obj.userData.rest;
  const rest = {}; obj.traverse(o => { if (o.isBone) rest[o.name] = { q: o.quaternion.clone(), p: o.position.clone() }; });
  obj.userData.rest = rest; return rest;
}
function resetObj(obj) { const r = srcRest(obj); obj.traverse(o => { if (o.isBone && r[o.name]) { o.quaternion.copy(r[o.name].q); o.position.copy(r[o.name].p); } }); obj.updateMatrixWorld(true); }

const MB_CACHE = {};
export function modelBind(key) {
  if (MB_CACHE[key]) return MB_CACHE[key];
  const m = S.models[key]; let mesh; m.traverse(o => { if (o.isSkinnedMesh) mesh = o; });
  const sk = mesh.skeleton; const up = {}; const parent = {};
  sk.bones.forEach((b, i) => {
    const bw = sk.boneInverses[i].clone().invert(); // bone world at bind (mesh bind space)
    const q = new THREE.Quaternion(), p = new THREE.Vector3(), s = new THREE.Vector3();
    bw.decompose(p, q, s);
    up[b.name] = { q: C_UP.clone().multiply(q), p: p.applyQuaternion(C_UP) };
    parent[b.name] = b.parent && b.parent.isBone ? b.parent.name : null;
  });
  // The model is bound in an A-pose, the Mixamo clips assume a T-pose. Rotate every bone (top-down) so its
  // direction to its main child matches the source rig's rest pose: this gives the model's own T-pose.
  const src = S.animObjs['Idle']; srcRest(src); resetObj(src);
  const sp = {}; src.traverse(o => { if (o.isBone) sp[o.name] = o.getWorldPosition(new THREE.Vector3()); });
  const kids = {}; for (const n of Object.keys(up)) { const p = parent[n]; if (p) (kids[p] ||= []).push(n); }
  const primary = (n) => {
    const k = kids[n] || []; if (!k.length) return null;
    const pick = (re) => k.find(c => re.test(c));
    if (/Hips$/.test(n)) return null;
    if (/Spine2$/.test(n)) return pick(/Neck$/);
    if (/Hand$/.test(n)) return pick(/HandMiddle1$/);
    return k[0];
  };
  const acc = {}, posT = {}, tq = {};
  const order = []; const visit = (n) => { order.push(n); for (const c of kids[n] || []) visit(c); };
  for (const n of Object.keys(up)) if (!parent[n]) visit(n);
  for (const n of order) {
    const par = parent[n];
    const accP = par ? acc[par] : new THREE.Quaternion();
    posT[n] = par ? posT[par].clone().add(up[n].p.clone().sub(up[par].p).applyQuaternion(accP)) : up[n].p.clone();
    let r = new THREE.Quaternion();
    const c = primary(n);
    if (c && sp[n] && sp[c]) {
      const cur = up[c].p.clone().sub(up[n].p).applyQuaternion(accP).normalize();
      const tgt = sp[c].clone().sub(sp[n]).normalize();
      r.setFromUnitVectors(cur, tgt);
    }
    acc[n] = r.multiply(accP);
    tq[n] = acc[n].clone().multiply(up[n].q);
  }
  for (const n of Object.keys(up)) up[n].tq = tq[n];
  return (MB_CACHE[key] = { bones: sk.bones.map(b => b.name), up, parent });
}

export function retarget(name, key, srcName) {
  const clip = S.clips[name];
  const src = S.animObjs[srcName || name] || S.animObjs['Boxing'];
  srcRest(src); resetObj(src);
  const srcB = {}; src.traverse(o => { if (o.isBone) srcB[o.name] = o; });
  const restW = {}; for (const n in srcB) restW[n] = srcB[n].getWorldQuaternion(new THREE.Quaternion());
  const MB = modelBind(key);
  const times = [], qv = {}, hp = [];
  for (const n of MB.bones) qv[n] = [];
  const mixer = new THREE.AnimationMixer(src);
  const a = mixer.clipAction(clip); a.setLoop(THREE.LoopOnce, 1); a.clampWhenFinished = true; a.play();
  const n = Math.max(2, Math.round(clip.duration * FPS) + 1);
  const T = {};
  for (let i = 0; i < n; i++) {
    const t = Math.min(clip.duration, i / FPS);
    mixer.setTime(t); src.updateMatrixWorld(true);
    times.push(t);
    for (const b of MB.bones) {
      const sb = srcB[b];
      const par = MB.parent[b];
      if (sb) T[b] = sb.getWorldQuaternion(new THREE.Quaternion()).multiply(restW[b].clone().invert()).multiply(MB.up[b].tq);
      else T[b] = (par ? T[par].clone() : new THREE.Quaternion()).multiply(par ? MB.up[par].tq.clone().invert() : new THREE.Quaternion()).multiply(MB.up[b].tq);
      const local = par ? T[par].clone().invert().multiply(T[b]) : T[b].clone();
      qv[b].push(...local.toArray());
    }
    const h = srcB['mixamorigHips'].getWorldPosition(new THREE.Vector3());
    hp.push(h.x, h.y, h.z);
  }
  a.stop(); mixer.uncacheRoot(src); resetObj(src);
  const tracks = [new THREE.VectorKeyframeTrack('mixamorigHips.position', times, hp)];
  for (const b of MB.bones) if (!b.endsWith('_End') && !/Hand\w+4$/.test(b)) tracks.push(new THREE.QuaternionKeyframeTrack(b + '.quaternion', times, qv[b]));
  const rc = new THREE.AnimationClip(name, clip.duration, tracks);
  S.rt ||= {}; (S.rt[key] ||= {})[name] = rc;
  return rc;
}

// ---------------- clip processing ----------------
function fixQuatSigns(v) {
  for (let i = 4; i < v.length; i += 4) {
    const d = v[i] * v[i - 4] + v[i + 1] * v[i - 3] + v[i + 2] * v[i - 2] + v[i + 3] * v[i - 1];
    if (d < 0) for (let k = 0; k < 4; k++) v[i + k] = -v[i + k];
  }
}
function reduceTrack(track, tolRad, isQuat, tolPos) {
  const t = track.times, v = track.values, s = isQuat ? 4 : 3, n = t.length;
  if (n <= 2) return track;
  const qa = new THREE.Quaternion(), qb = new THREE.Quaternion(), qi = new THREE.Quaternion(), qx = new THREE.Quaternion();
  const keep = [0];
  let k = 0;
  const err = (a, b, i) => {
    const al = (t[i] - t[a]) / (t[b] - t[a]);
    if (isQuat) {
      qa.fromArray(v, a * 4); qb.fromArray(v, b * 4); qi.slerpQuaternions(qa, qb, al); qx.fromArray(v, i * 4);
      return qi.angleTo(qx) / tolRad;
    }
    let e = 0; for (let c = 0; c < 3; c++) { const lin = v[a * 3 + c] + (v[b * 3 + c] - v[a * 3 + c]) * al; e = Math.max(e, Math.abs(lin - v[i * 3 + c])); }
    return e / tolPos;
  };
  for (let j = 2; j < n; j++) {
    let ok = true;
    for (let i = k + 1; i < j; i++) if (err(k, j, i) > 1) { ok = false; break; }
    if (!ok) { keep.push(j - 1); k = j - 1; }
  }
  keep.push(n - 1);
  const nt = new Float32Array(keep.length), nv = new Float32Array(keep.length * s);
  keep.forEach((idx, o) => { nt[o] = t[idx]; for (let c = 0; c < s; c++) nv[o * s + c] = v[idx * s + c]; });
  const Ctor = track.constructor;
  return new Ctor(track.name, nt, nv);
}

export const PAIRED = ['Shoulder Throw, Aggressor', 'Shoulder Throw, Victim'];
export function processClip(name, opts = {}, clipOverride) {
  const clip = clipOverride || S.clips[name];
  const out = { name, duration: +clip.duration.toFixed(3) };
  const tracks = [];
  for (let tr of clip.tracks) {
    tr = tr.clone();
    if (tr.name === 'mixamorigHips.position') {
      const v = tr.values, n = tr.times.length;
      const x0 = v[0], z0 = v[2];
      let maxD = 0; for (let i = 0; i < n; i++) maxD = Math.max(maxD, Math.hypot(v[i * 3] - x0, v[i * 3 + 2] - z0));
      const rm = maxD > 15 && !opts.inPlace && !opts.loop;
      out.rm = rm; out.maxDisp = +maxD.toFixed(1);
      if (opts.loop) {
        // looping locomotion: remove the steady forward drift, keep the natural sway
        const T = tr.times, dur = clip.duration, x1 = v[(n - 1) * 3], z1 = v[(n - 1) * 3 + 2];
        out.stride = +Math.hypot(x1 - x0, z1 - z0).toFixed(2);
        for (let i = 0; i < n; i++) { const a = T[i] / dur; v[i * 3] -= x0 + (x1 - x0) * a; v[i * 3 + 2] -= z0 + (z1 - z0) * a; }
      }
      if (rm) {
        const paired = PAIRED.includes(name);
        // trajectory sampled at FPS
        const traj = [];
        const interp = tr.createInterpolant();
        for (let f = 0; f <= Math.round(clip.duration * FPS); f++) {
          const r = interp.evaluate(Math.min(clip.duration, f / FPS));
          traj.push(+(r[0] - x0).toFixed(2), +(r[2] - z0).toFixed(2));
        }
        out.traj = traj;
        out.start = [+x0.toFixed(2), +z0.toFixed(2)];
        for (let i = 0; i < n; i++) { v[i * 3] = paired ? x0 : 0; v[i * 3 + 2] = paired ? z0 : 0; }
      }
      tr = reduceTrack(tr, 0, false, 0.12);
    } else if (tr.name.endsWith('.quaternion')) {
      fixQuatSigns(tr.values);
      const finger = /Hand(Index|Middle|Ring|Pinky|Thumb)/.test(tr.name);
      tr = reduceTrack(tr, (finger ? 1.6 : 0.5) * DEG, true);
    } else continue; // drop scale tracks etc.
    tracks.push(tr);
  }
  const nc = new THREE.AnimationClip(name, clip.duration, tracks);
  S.processed ||= {}; S.processed[name] = nc;
  out.keys = tracks.reduce((a, t) => a + t.times.length, 0);
  return out;
}

export async function exportGLB(key, clipNames, clipObjs) {
  const { root } = prepModel(key);
  const clips = clipObjs || (clipNames || []).map(n => S.processed[n]);
  const exporter = new GLTFExporter();
  const glb = await new Promise((res, rej) => exporter.parse(root, res, rej, { binary: true, animations: clips, onlyVisible: false }));
  const gz = await new Response(new Blob([glb]).stream().pipeThrough(new CompressionStream('gzip'))).arrayBuffer();
  const b64 = (buf) => { const u = new Uint8Array(buf); let s = ''; for (let i = 0; i < u.length; i += 0x8000) s += String.fromCharCode.apply(null, u.subarray(i, i + 0x8000)); return btoa(s); };
  return { raw: glb.byteLength, gz: gz.byteLength, b64: b64(gz) };
}

// quick preview of a processed/generated clip on a model (side + front)
export async function previewClip(key, clipName, times, yaw = 0, useProcessed = false) {
  const clip = useProcessed ? S.processed[clipName] : S.clips[clipName];
  const keep = S.clips[clipName]; S.clips['__tmp'] = clip;
  const imgs = await window.preview(key, '__tmp', times, yaw);
  S.clips[clipName] = keep;
  return imgs;
}

export async function closeup(key, clipName, t, { yaw = 0, dist = 190, y = 75, w = 420, h = 560, useRt = true } = {}) {
  const clip = useRt ? S.rt[key][clipName] : S.clips[clipName];
  const r = window.__cuR ||= new THREE.WebGLRenderer({ antialias: true, preserveDrawingBuffer: true });
  r.setSize(w, h); r.outputColorSpace = THREE.SRGBColorSpace;
  const scene = new THREE.Scene(); scene.background = new THREE.Color(0x2d2a28);
  scene.add(new THREE.HemisphereLight(0xffffff, 0x444444, 1.6));
  const dl = new THREE.DirectionalLight(0xffffff, 2.2); dl.position.set(1, 2, 3); scene.add(dl);
  const src = S.models[key];
  const tl = window.__tl ||= new THREE.TextureLoader();
  const tex = (window.__tx ||= {})[key] ||= (() => { const tt = tl.load('/tex/' + key + '_color.jpg'); tt.colorSpace = THREE.SRGBColorSpace; return tt; })();
  src.traverse(o => { if (o.isMesh) { o.material = new THREE.MeshStandardMaterial({ map: tex, roughness: 0.7 }); o.frustumCulled = false; } });
  scene.add(src); src.rotation.y = yaw;
  const mixer = new THREE.AnimationMixer(src); const a = mixer.clipAction(clip); a.play(); mixer.setTime(t);
  await new Promise(res => setTimeout(res, 400));
  mixer.setTime(t); src.updateMatrixWorld(true);
  let hips; src.traverse(o => { if (o.name === 'mixamorigHips') hips = o; });
  const hp = hips.getWorldPosition(new THREE.Vector3());
  const cam = new THREE.PerspectiveCamera(30, w / h, 1, 5000); cam.position.set(hp.x, y + 8, hp.z + dist); cam.lookAt(hp.x, y, hp.z);
  r.render(scene, cam);
  const img = r.domElement.toDataURL('image/jpeg', 0.85);
  a.stop(); mixer.uncacheRoot(src); scene.remove(src); src.rotation.y = 0;
  return img;
}

export async function loadBuiltGLB(url, key) {
  const buf = await (await fetch(url)).arrayBuffer();
  const raw = await new Response(new Blob([buf]).stream().pipeThrough(new DecompressionStream('gzip'))).arrayBuffer();
  const g = await new Promise((res, rej) => new GLTFLoader().parse(raw, '', res, rej));
  S.models[key] = g.scene; S.rt ||= {}; S.rt[key] = {}; for (const c of g.animations) S.rt[key][c.name] = c;
  return g.animations.map(c => c.name);
}

export function gaitPhase(key, name) {
  const clip = S.processed[name]; const root = S.models[key]; const B = boneMap(root);
  let best = -1e9, bt = 0;
  sampleClip(root, clip, 60, (t) => { const d = wp(B.LeftFoot).z - wp(B.Hips).z; if (d > best) { best = d; bt = t; } });
  return +(bt / clip.duration).toFixed(3);
}

// speed peaks of several joints (for knees, elbows and weapon swings)
export function speedPeaks(name, key = 'mino') {
  const root = S.models[key]; const B = boneMap(root); const clip = S.clips[name];
  const J = { RH: 'RightHand', LH: 'LeftHand', RF: 'RightToeBase', LF: 'LeftToeBase', RK: 'RightLeg', LK: 'LeftLeg', RE: 'RightForeArm', LE: 'LeftForeArm' };
  const rows = [];
  sampleClip(root, clip, 30, (t) => { const h = wp(B.Hips); const r = { t }; for (const k in J) { const p = wp(B[J[k]]); r[k] = [p.x - h.x, p.y, p.z - h.z]; } rows.push(r); });
  const out = [];
  for (const k in J) {
    const sp = rows.map((r, i) => i ? Math.hypot(...r[k].map((v, c) => v - rows[i - 1][k][c])) * 30 : 0);
    for (let i = 2; i < sp.length - 2; i++) if (sp[i] > 120 && sp[i] >= sp[i - 1] && sp[i] >= sp[i + 1] && sp[i] >= sp[i - 2] && sp[i] >= sp[i + 2])
      out.push([+rows[i].t.toFixed(2), k, Math.round(sp[i]), +rows[i][k][1].toFixed(0), +rows[i][k][2].toFixed(0)]);
  }
  return out.sort((a, b) => a[0] - b[0]);
}

// split a mesh into connected pieces (by shared vertex positions); report size + dominant bones
export function components(key) {
  const src = S.models[key]; let mesh; src.traverse(o => { if (o.isSkinnedMesh) mesh = o; });
  const g = mesh.geometry, P = g.attributes.position, n = P.count;
  const keyOf = (i) => `${Math.round(P.getX(i) * 200)},${Math.round(P.getY(i) * 200)},${Math.round(P.getZ(i) * 200)}`;
  const id = new Map(), parent = [];
  const vid = new Int32Array(n);
  for (let i = 0; i < n; i++) { const k = keyOf(i); if (!id.has(k)) { id.set(k, parent.length); parent.push(parent.length); } vid[i] = id.get(k); }
  const find = (a) => { while (parent[a] !== a) { parent[a] = parent[parent[a]]; a = parent[a]; } return a; };
  const idx = g.index ? g.index.array : null, triCount = idx ? idx.length / 3 : n / 3;
  const tv = (t, c) => idx ? idx[t * 3 + c] : t * 3 + c;
  for (let t = 0; t < triCount; t++) { const a = find(vid[tv(t, 0)]), b = find(vid[tv(t, 1)]), c = find(vid[tv(t, 2)]); parent[b] = a; parent[find(c)] = a; }
  const comps = new Map();
  const SI = g.attributes.skinIndex, SW = g.attributes.skinWeight;
  for (let i = 0; i < n; i++) {
    const r = find(vid[i]); if (!comps.has(r)) comps.set(r, { verts: 0, min: [1e9, 1e9, 1e9], max: [-1e9, -1e9, -1e9], bones: {} });
    const c = comps.get(r); c.verts++;
    const p = [P.getX(i), P.getY(i), P.getZ(i)]; for (let k = 0; k < 3; k++) { c.min[k] = Math.min(c.min[k], p[k]); c.max[k] = Math.max(c.max[k], p[k]); }
    for (let k = 0; k < 4; k++) { const w = SW.getComponent(i, k); if (w > 0.05) { const b = mesh.skeleton.bones[SI.getComponent(i, k)].name.replace('mixamorig', ''); c.bones[b] = (c.bones[b] || 0) + w; } }
  }
  S.comp ||= {}; S.comp[key] = { vid, find, mesh };
  return [...comps.entries()].map(([r, c]) => ({ root: r, verts: c.verts, size: c.max.map((v, k) => +(v - c.min[k]).toFixed(1)), min: c.min.map(v => +v.toFixed(1)), bones: Object.entries(c.bones).sort((a, b) => b[1] - a[1]).slice(0, 5).map(([b, w]) => b + ':' + Math.round(w)) }))
    .sort((a, b) => b.verts - a.verts).slice(0, 12);
}

// render the bind pose upright with vertices coloured by a per-vertex value (0..1)
export async function bindView(key, values, { yaw = 0, w = 420, h = 520, dist = 260, y = 50 } = {}) {
  let mesh; S.models[key].traverse(o => { if (o.isSkinnedMesh) mesh = o; });
  const g = mesh.geometry.clone(); g.applyMatrix4(mesh.bindMatrix); g.applyQuaternion(new THREE.Quaternion().setFromAxisAngle(new THREE.Vector3(1, 0, 0), -Math.PI / 2));
  const col = new Float32Array(g.attributes.position.count * 3);
  for (let i = 0; i < values.length; i++) { const v = values[i]; col[i * 3] = 0.3 + 0.7 * v; col[i * 3 + 1] = 0.3 * (1 - v); col[i * 3 + 2] = 0.35 * (1 - v); }
  g.setAttribute('color', new THREE.BufferAttribute(col, 3));
  const m = new THREE.Mesh(g, new THREE.MeshStandardMaterial({ vertexColors: true, roughness: 0.8 }));
  m.rotation.y = yaw;
  const r = window.__cuR ||= new THREE.WebGLRenderer({ antialias: true, preserveDrawingBuffer: true }); r.setSize(w, h);
  const sc = new THREE.Scene(); sc.background = new THREE.Color(0x222222); sc.add(new THREE.HemisphereLight(0xffffff, 0x444444, 2)); const dl = new THREE.DirectionalLight(0xffffff, 2); dl.position.set(1, 2, 3); sc.add(dl); sc.add(m);
  const cam = new THREE.PerspectiveCamera(35, w / h, 1, 5000); cam.position.set(0, y + 10, dist); cam.lookAt(0, y, 0);
  r.render(sc, cam); return r.domElement.toDataURL('image/jpeg', 0.85);
}
export function boneJoints(key) {
  let mesh; S.models[key].traverse(o => { if (o.isSkinnedMesh) mesh = o; });
  const sk = mesh.skeleton, J = {};
  sk.bones.forEach((b, i) => { const p = new THREE.Vector3(); sk.boneInverses[i].clone().invert().decompose(p, new THREE.Quaternion(), new THREE.Vector3()); J[b.name.replace('mixamorig', '')] = p; });
  return { J, mesh };
}

// make a hand-held prop rigid: vertices along the axis from the hand to the far tip get 100% hand weight
export function fixClub(key) {
  const { J, mesh } = boneJoints(key);
  const g = mesh.geometry, P = g.attributes.position, n = P.count, bm = mesh.bindMatrix;
  const SI = g.attributes.skinIndex, SW = g.attributes.skinWeight;
  const hand = J['RightHand'];
  const v = new THREE.Vector3();
  const pts = new Array(n);
  for (let i = 0; i < n; i++) pts[i] = v.fromBufferAttribute(P, i).applyMatrix4(bm).clone();
  // tip = farthest point from the hand among points that are far from the body
  const dist = S.muscDist;
  let tip = null, best = 0;
  // the bind pose is Z-up and the club hangs below the hand: look only below the hand
  for (let i = 0; i < n; i++) if (dist[i] > 10 && pts[i].z < hand.z - 10) { const d = pts[i].distanceTo(hand); if (d > best) { best = d; tip = pts[i]; } }
  const axis = new THREE.Line3(hand.clone(), tip.clone()), cp = new THREE.Vector3();
  // club radius from the clearly separated part
  const rs = [];
  for (let i = 0; i < n; i++) { if (pts[i].distanceTo(tip) > best * 0.45) continue; const tp = axis.closestPointToPointParameter(pts[i], true); if (tp < 0.55) continue; axis.at(tp, cp); const r = cp.distanceTo(pts[i]); if (r < 10) rs.push(r); }
  rs.sort((a, b) => a - b); const R = Math.min(6, rs[Math.floor(rs.length * 0.95)] * 1.15);
  const handIdx = mesh.skeleton.bones.findIndex(b => b.name === 'mixamorigRightHand');
  let count = 0; const sel = new Float32Array(n);
  for (let i = 0; i < n; i++) {
    const tParam = axis.closestPointToPointParameter(pts[i], true);
    axis.at(tParam, cp);
    const r = cp.distanceTo(pts[i]);
    const lim = tParam < 0.12 ? R * 0.85 : R;   // tighter inside the fist
    if (r <= lim && tParam > 0.02) { SI.setXYZW(i, handIdx, 0, 0, 0); SW.setXYZW(i, 1, 0, 0, 0); sel[i] = 1; count++; }
  }
  SI.needsUpdate = SW.needsUpdate = true;
  // tip and grip in the hand bone's local space (bind)
  const hb = mesh.skeleton.boneInverses[handIdx];
  const tipL = tip.clone().applyMatrix4(hb), gripL = hand.clone().lerp(tip, 0.08).applyMatrix4(hb);
  return { count, R: +R.toFixed(2), len: +best.toFixed(1), tipLocal: tipL.toArray().map(x => +x.toFixed(2)), gripLocal: gripL.toArray().map(x => +x.toFixed(2)), sel };
}
