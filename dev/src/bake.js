// Baked lighting for static interiors (the gym). Light from the lamps with soft shadows plus ambient occlusion is
// computed once, offline (tools: __mg.bakeGym in the play-test harness), per vertex of finely split geometry, and
// stored as half floats (assets/bake/gym.bin.gz). In the game the static meshes switch to unlit materials that
// multiply their texture by that stored light: the look of expensive lighting for almost no cost on a phone.
// Only people and moving props stay lit live.
import * as THREE from 'three';

// ---------------- geometry: cut triangles along a world-aligned grid (cell x cell) so light can vary across big
// faces; thin strips are only cut along their length. Shared edges are cut at the same points (no cracks).
export function tessellate(geo, cell) {
  const g = geo.index ? geo.toNonIndexed() : geo;
  const P = g.attributes.position.array, N = g.attributes.normal ? g.attributes.normal.array : null, U = g.attributes.uv ? g.attributes.uv.array : null;
  const nv = P.length / 3;
  const groups = g.groups.length ? g.groups : [{ start: 0, count: nv, materialIndex: 0 }];
  const oP = [], oN = [], oU = [], outGroups = [];
  const V = (i) => [P[i * 3], P[i * 3 + 1], P[i * 3 + 2], N ? N[i * 3] : 0, N ? N[i * 3 + 1] : 1, N ? N[i * 3 + 2] : 0, U ? U[i * 2] : 0, U ? U[i * 2 + 1] : 0];
  const lerp = (a, b, t) => { const m = a.map((v, k) => v + (b[k] - v) * t); const l = Math.hypot(m[3], m[4], m[5]) || 1; m[3] /= l; m[4] /= l; m[5] /= l; return m; };
  const split = (poly, ax, c) => {
    const L = [], R = [];
    for (let i = 0; i < poly.length; i++) {
      const a = poly[i], b = poly[(i + 1) % poly.length], da = a[ax] - c, db = b[ax] - c;
      if (da <= 1e-7) L.push(a); if (da >= -1e-7) R.push(a);
      if ((da < -1e-7 && db > 1e-7) || (da > 1e-7 && db < -1e-7)) { const m = lerp(a, b, da / (da - db)); L.push(m); R.push(m); }
    }
    return [L, R];
  };
  const emit = (v) => { oP.push(v[0], v[1], v[2]); oN.push(v[3], v[4], v[5]); oU.push(v[6], v[7]); };
  for (const gr of groups) {
    const s0 = oP.length / 3;
    for (let t = gr.start; t < gr.start + gr.count; t += 3) {
      const a = V(t), b = V(t + 1), c = V(t + 2);
      const ux = b[0] - a[0], uy = b[1] - a[1], uz = b[2] - a[2], vx = c[0] - a[0], vy = c[1] - a[1], vz = c[2] - a[2];
      const fn = [Math.abs(uy * vz - uz * vy), Math.abs(uz * vx - ux * vz), Math.abs(ux * vy - uy * vx)];
      const k = fn[0] >= fn[1] && fn[0] >= fn[2] ? 0 : fn[1] >= fn[2] ? 1 : 2;
      let polys = [[a, b, c]];
      for (const ax of [0, 1, 2]) {
        if (ax === k) continue;
        const lo = Math.min(a[ax], b[ax], c[ax]), hi = Math.max(a[ax], b[ax], c[ax]);
        if (hi - lo <= cell) continue;
        for (let line = Math.ceil((lo + 1e-5) / cell) * cell; line < hi - 1e-5; line += cell) {
          const next = [];
          for (const p of polys) {
            let pl = Infinity, ph = -Infinity; for (const q of p) { pl = Math.min(pl, q[ax]); ph = Math.max(ph, q[ax]); }
            if (line <= pl + 1e-7 || line >= ph - 1e-7) { next.push(p); continue; }
            const [L, R] = split(p, ax, line); if (L.length >= 3) next.push(L); if (R.length >= 3) next.push(R);
          }
          polys = next;
        }
      }
      for (const p of polys) for (let i = 1; i + 1 < p.length; i++) { emit(p[0]); emit(p[i]); emit(p[i + 1]); }
    }
    outGroups.push({ start: s0, count: oP.length / 3 - s0, materialIndex: gr.materialIndex || 0 });
  }
  const out = new THREE.BufferGeometry();
  out.setAttribute('position', new THREE.Float32BufferAttribute(oP, 3));
  out.setAttribute('normal', new THREE.Float32BufferAttribute(oN, 3));
  if (U) out.setAttribute('uv', new THREE.Float32BufferAttribute(oU, 2));
  if (g.groups.length) for (const gr of outGroups) out.addGroup(gr.start, gr.count, gr.materialIndex);
  return out;
}

const isLitMat = (m) => m && m.isMeshStandardMaterial;
function dynamicAncestor(o) { for (let p = o; p; p = p.parent) if (p.userData && (p.userData.dynamic || p.userData.outside)) return true; return false; }

// static, lit meshes of a root, in a fixed order; their geometry is split to ~maxEdge so light can vary across it
export function prepareBake(root, maxEdge = 0.5) {
  const list = [];
  root.updateMatrixWorld(true);
  root.traverse(o => {
    if (!o.isMesh || o.isSkinnedMesh || o.isInstancedMesh || o.userData.noBake || dynamicAncestor(o)) return;
    const mats = Array.isArray(o.material) ? o.material : [o.material];
    if (!mats.every(isLitMat)) return;
    o.geometry = tessellate(o.geometry, maxEdge);
    list.push(o);
  });
  return list;
}

// ---------------- ray casting: BVH over world-space triangles ----------------
class BVH {
  constructor(tri) {
    const n = tri.length / 9; this.tri = tri;
    const cen = new Float32Array(n * 3), bb = new Float32Array(n * 6);
    for (let i = 0; i < n; i++) {
      for (let k = 0; k < 3; k++) {
        const a = tri[i * 9 + k], b = tri[i * 9 + 3 + k], c = tri[i * 9 + 6 + k];
        bb[i * 6 + k] = Math.min(a, b, c); bb[i * 6 + 3 + k] = Math.max(a, b, c); cen[i * 3 + k] = (a + b + c) / 3;
      }
    }
    this.idx = new Uint32Array(n); for (let i = 0; i < n; i++) this.idx[i] = i;
    this.nodes = []; // [minx,miny,minz,maxx,maxy,maxz,left,right,start,count]
    const build = (s, e) => {
      let mn = [Infinity, Infinity, Infinity], mx = [-Infinity, -Infinity, -Infinity], cmn = [Infinity, Infinity, Infinity], cmx = [-Infinity, -Infinity, -Infinity];
      for (let i = s; i < e; i++) { const t = this.idx[i]; for (let k = 0; k < 3; k++) { mn[k] = Math.min(mn[k], bb[t * 6 + k]); mx[k] = Math.max(mx[k], bb[t * 6 + 3 + k]); cmn[k] = Math.min(cmn[k], cen[t * 3 + k]); cmx[k] = Math.max(cmx[k], cen[t * 3 + k]); } }
      const id = this.nodes.length; const node = [...mn, ...mx, -1, -1, s, e - s]; this.nodes.push(node);
      if (e - s <= 4) return id;
      let ax = 0; for (let k = 1; k < 3; k++) if (cmx[k] - cmn[k] > cmx[ax] - cmn[ax]) ax = k;
      const sp = (cmn[ax] + cmx[ax]) / 2;
      let i = s, j = e - 1;
      while (i <= j) { if (cen[this.idx[i] * 3 + ax] < sp) i++; else { const t = this.idx[i]; this.idx[i] = this.idx[j]; this.idx[j] = t; j--; } }
      let m = i; if (m === s || m === e) m = (s + e) >> 1;
      node[6] = build(s, m); node[7] = build(m, e); node[9] = 0;
      return id;
    };
    build(0, n);
    // flatten
    const N = this.nodes.length; this.nb = new Float32Array(N * 6); this.nl = new Int32Array(N * 4);
    this.nodes.forEach((nd, i) => { for (let k = 0; k < 6; k++) this.nb[i * 6 + k] = nd[k]; this.nl[i * 4] = nd[6]; this.nl[i * 4 + 1] = nd[7]; this.nl[i * 4 + 2] = nd[8]; this.nl[i * 4 + 3] = nd[9]; });
    this.nodes = null; this.stack = new Int32Array(128);
  }
  // any hit with 1e-3 < t < tmax?
  occluded(ox, oy, oz, dx, dy, dz, tmax) {
    const nb = this.nb, nl = this.nl, T = this.tri, idx = this.idx, st = this.stack;
    const ix = 1 / dx, iy = 1 / dy, iz = 1 / dz;
    let sp = 0; st[sp++] = 0;
    while (sp) {
      const n = st[--sp], b = n * 6;
      let t0 = ((ix >= 0 ? nb[b] : nb[b + 3]) - ox) * ix, t1 = ((ix >= 0 ? nb[b + 3] : nb[b]) - ox) * ix;
      const ty0 = ((iy >= 0 ? nb[b + 1] : nb[b + 4]) - oy) * iy, ty1 = ((iy >= 0 ? nb[b + 4] : nb[b + 1]) - oy) * iy;
      if (ty0 > t0) t0 = ty0; if (ty1 < t1) t1 = ty1;
      const tz0 = ((iz >= 0 ? nb[b + 2] : nb[b + 5]) - oz) * iz, tz1 = ((iz >= 0 ? nb[b + 5] : nb[b + 2]) - oz) * iz;
      if (tz0 > t0) t0 = tz0; if (tz1 < t1) t1 = tz1;
      if (t0 > t1 || t1 < 0 || t0 > tmax) continue;
      const cnt = nl[n * 4 + 3];
      if (cnt === 0) { st[sp++] = nl[n * 4]; st[sp++] = nl[n * 4 + 1]; continue; }
      const s = nl[n * 4 + 2];
      for (let i = s; i < s + cnt; i++) {
        const o = idx[i] * 9;
        const e1x = T[o + 3] - T[o], e1y = T[o + 4] - T[o + 1], e1z = T[o + 5] - T[o + 2];
        const e2x = T[o + 6] - T[o], e2y = T[o + 7] - T[o + 1], e2z = T[o + 8] - T[o + 2];
        const px = dy * e2z - dz * e2y, py = dz * e2x - dx * e2z, pz = dx * e2y - dy * e2x;
        const det = e1x * px + e1y * py + e1z * pz; if (det > -1e-9 && det < 1e-9) continue;
        const inv = 1 / det, tx = ox - T[o], ty = oy - T[o + 1], tz = oz - T[o + 2];
        const u = (tx * px + ty * py + tz * pz) * inv; if (u < 0 || u > 1) continue;
        const qx = ty * e1z - tz * e1y, qy = tz * e1x - tx * e1z, qz = tx * e1y - ty * e1x;
        const v = (dx * qx + dy * qy + dz * qz) * inv; if (v < 0 || u + v > 1) continue;
        const t = (e2x * qx + e2y * qy + e2z * qz) * inv;
        if (t > 1e-3 && t < tmax) return true;
      }
    }
    return false;
  }
}

function worldTriangles(meshes) {
  const out = []; const v = new THREE.Vector3();
  for (const m of meshes) {
    const g = m.geometry, p = g.attributes.position, idx = g.index;
    const n = idx ? idx.count : p.count;
    for (let i = 0; i < n; i++) { v.fromBufferAttribute(p, idx ? idx.getX(i) : i).applyMatrix4(m.matrixWorld); out.push(v.x, v.y, v.z); }
  }
  return new Float32Array(out);
}

// ---------------- the bake ----------------
// lights: [{ p: Vector3, col: Color (already times intensity), r: range, lobe: 'down' | 'omni' | Vector3 (facing dir), size }]
export function bakeLighting({ receivers, occluders, lights, ambient = new THREE.Color(0.22, 0.22, 0.21), aoRays = 24, aoDist = 2.6, falloff = 1.6, onProgress = null }) {
  const bvh = new BVH(worldTriangles(occluders));
  // fixed, evenly spread cosine-weighted directions (same result every run)
  const dirs = [];
  for (let i = 0; i < aoRays; i++) { const u = (i + 0.5) / aoRays, v = ((i * 0.618034) % 1); const r = Math.sqrt(u), a = 2 * Math.PI * v; dirs.push([r * Math.cos(a), r * Math.sin(a), Math.sqrt(1 - u)]); }
  const cache = new Map();
  const P = new THREE.Vector3(), Nn = new THREE.Vector3(), nm = new THREE.Matrix3();
  const T1 = new THREE.Vector3(), T2 = new THREE.Vector3();
  const res = [];
  let done = 0; const total = receivers.reduce((s, m) => s + m.geometry.attributes.position.count, 0);
  for (const m of receivers) {
    const g = m.geometry, p = g.attributes.position, n = g.attributes.normal, cnt = p.count;
    nm.getNormalMatrix(m.matrixWorld);
    const out = new Float32Array(cnt * 3);
    for (let i = 0; i < cnt; i++) {
      P.fromBufferAttribute(p, i).applyMatrix4(m.matrixWorld); Nn.fromBufferAttribute(n, i).applyMatrix3(nm).normalize();
      const key = `${Math.round(P.x * 200)},${Math.round(P.y * 200)},${Math.round(P.z * 200)},${Math.round(Nn.x * 20)},${Math.round(Nn.y * 20)},${Math.round(Nn.z * 20)}`;
      let e = cache.get(key);
      if (!e) {
        e = [0, 0, 0];
        const ox = P.x + Nn.x * 0.03, oy = P.y + Nn.y * 0.03, oz = P.z + Nn.z * 0.03;
        // direct light
        for (const L of lights) {
          let dx = L.p.x - ox, dy = L.p.y - oy, dz = L.p.z - oz; const d = Math.hypot(dx, dy, dz);
          if (d > L.r || d < 1e-4) continue;
          dx /= d; dy /= d; dz /= d;
          const ndl = Nn.x * dx + Nn.y * dy + Nn.z * dz; if (ndl <= 0) continue;
          let lobe = 1;
          if (L.lobe === 'down') lobe = 0.1 + 0.9 * Math.pow(Math.max(0, dy), 1.4);
          else if (L.lobe && L.lobe.isVector3) lobe = Math.max(0, -(dx * L.lobe.x + dy * L.lobe.y + dz * L.lobe.z));
          if (lobe <= 0) continue;
          const w = Math.max(0, 1 - Math.pow(d / L.r, 4)); const att = w * w / (1 + (d / falloff) * (d / falloff));
          // soft shadow: a few points on the lamp
          const S = L.size > 0 && d < 9 ? 3 : 1; let vis = 0;
          for (let s = 0; s < S; s++) {
            const jx = S > 1 ? Math.cos(s * 2.094) * L.size : 0, jz = S > 1 ? Math.sin(s * 2.094) * L.size : 0;
            let ex = L.p.x + jx - ox, ey = L.p.y - oy, ez = L.p.z + jz - oz; const dd = Math.hypot(ex, ey, ez);
            if (!bvh.occluded(ox, oy, oz, ex / dd, ey / dd, ez / dd, dd - 0.08)) vis++;
          }
          const k = ndl * lobe * att * vis / S;
          e[0] += L.col.r * k; e[1] += L.col.g * k; e[2] += L.col.b * k;
        }
        // ambient occlusion
        const up = Math.abs(Nn.y) < 0.9 ? T1.set(0, 1, 0) : T1.set(1, 0, 0);
        T2.crossVectors(up, Nn).normalize(); T1.crossVectors(Nn, T2);
        let ao = 0;
        for (const [a, b, c] of dirs) {
          const rx = T2.x * a + T1.x * b + Nn.x * c, ry = T2.y * a + T1.y * b + Nn.y * c, rz = T2.z * a + T1.z * b + Nn.z * c;
          ao += bvh.occluded(ox, oy, oz, rx, ry, rz, aoDist) ? 0 : 1;
        }
        ao /= dirs.length;
        const amb = 0.25 + 0.75 * ao;
        e[0] += ambient.r * amb; e[1] += ambient.g * amb; e[2] += ambient.b * amb;
        cache.set(key, e);
      }
      out[i * 3] = e[0]; out[i * 3 + 1] = e[1]; out[i * 3 + 2] = e[2];
    }
    res.push(out);
    done += cnt; if (onProgress) onProgress(done / total);
  }
  return res;
}

export function packBake(arrays) {
  const n = arrays.reduce((s, a) => s + a.length, 0), h = new Uint16Array(n); let o = 0;
  for (const a of arrays) for (let i = 0; i < a.length; i++) h[o++] = THREE.DataUtils.toHalfFloat(Math.min(a[i], 60000));
  return h;
}

// swap the baked meshes to unlit materials that multiply texture x stored light; returns false if the data does not fit
export function applyBake(receivers, half, gain = 1) {
  const need = receivers.reduce((s, m) => s + m.geometry.attributes.position.count * 3, 0);
  if (half.length !== need) { console.warn('bake: vertex count changed', half.length, need); return false; }
  const conv = new Map();
  const toBasic = (m) => {
    if (conv.has(m)) return conv.get(m);
    const b = new THREE.MeshBasicMaterial({ map: m.map, color: m.color.clone().multiplyScalar(gain), vertexColors: true, transparent: m.transparent, opacity: m.opacity, alphaTest: m.alphaTest, side: m.side, depthWrite: m.depthWrite, polygonOffset: m.polygonOffset, polygonOffsetFactor: m.polygonOffsetFactor, polygonOffsetUnits: m.polygonOffsetUnits, fog: true });
    b.name = (m.name || '') + '_baked'; conv.set(m, b); return b;
  };
  let o = 0;
  for (const m of receivers) {
    const cnt = m.geometry.attributes.position.count * 3;
    m.geometry.setAttribute('color', new THREE.Float16BufferAttribute(half.subarray(o, o + cnt), 3)); o += cnt;
    m.material = Array.isArray(m.material) ? m.material.map(toBasic) : toBasic(m.material);
    m.receiveShadow = false; m.castShadow = false; // their shadows are in the bake; the shadow pass now only draws people
  }
  return conv;
}
