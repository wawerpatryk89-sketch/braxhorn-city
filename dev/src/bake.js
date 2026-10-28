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

// glTF models packed with quantized attributes (gltfpack): copy position/normal/uv to plain floats before any transform
export function floatGeo(geo, keep = ['position', 'normal', 'uv']) {
  const g = new THREE.BufferGeometry();
  for (const k of keep) {
    const a = geo.attributes[k]; if (!a) continue;
    const n = a.count, s = a.itemSize, f = new Float32Array(n * s);
    for (let i = 0; i < n; i++) { f[i * s] = a.getX(i); if (s > 1) f[i * s + 1] = a.getY(i); if (s > 2) f[i * s + 2] = a.getZ(i); }
    g.setAttribute(k, new THREE.BufferAttribute(f, s));
  }
  if (geo.index) g.setIndex(Array.from(geo.index.array));
  for (const gr of geo.groups) g.addGroup(gr.start, gr.count, gr.materialIndex);
  if (!g.attributes.normal && g.attributes.position) g.computeVertexNormals();
  return g;
}
const isLitMat = (m) => m && m.isMeshStandardMaterial;
function dynamicAncestor(o) { for (let p = o; p; p = p.parent) if (p.userData && (p.userData.dynamic || p.userData.outside)) return true; return false; }

// static, lit meshes of a root, in a fixed order; their geometry is split to ~maxEdge so light can vary across it
export function prepareBake(root, maxEdge = 0.5) {
  const list = [];
  root.updateMatrixWorld(true);
  root.traverse(o => {
    if (!o.isMesh || o.isSkinnedMesh || o.isInstancedMesh || o.userData.noBake || dynamicAncestor(o)) return;
    if (o.parent && o.parent.userData && o.parent.userData.bakeCell && !o.userData.bakeCell) o.userData.bakeCell = o.parent.userData.bakeCell;
    const mats = Array.isArray(o.material) ? o.material : [o.material];
    if (!mats.every(isLitMat)) return;
    // split in world space (cells in metres), then back to the mesh's own space
    const inv = new THREE.Matrix4().copy(o.matrixWorld).invert();
    const wg = floatGeo(o.geometry).applyMatrix4(o.matrixWorld);
    o.geometry = tessellate(wg, o.userData.bakeCell || maxEdge).applyMatrix4(inv);
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

// ---------------- ground lightmap: top-down texture over the outdoor floors (crisp sun shadows, few vertices) ----------------
export const LM = { x0: -120, x1: 112, z0: -12, z1: 118, res: 0.3 };
export function lmSize() { return [Math.round((LM.x1 - LM.x0) / LM.res), Math.round((LM.z1 - LM.z0) / LM.res)]; }
export function setLightmapUV(mesh) {
  const g = mesh.geometry.index ? mesh.geometry.toNonIndexed() : mesh.geometry; mesh.geometry = g;
  const p = g.attributes.position, uv = new Float32Array(p.count * 2), v = new THREE.Vector3();
  mesh.updateMatrixWorld(true);
  for (let i = 0; i < p.count; i++) { v.fromBufferAttribute(p, i).applyMatrix4(mesh.matrixWorld); uv[i * 2] = (v.x - LM.x0) / (LM.x1 - LM.x0); uv[i * 2 + 1] = (v.z - LM.z0) / (LM.z1 - LM.z0); }
  g.setAttribute('uv1', new THREE.BufferAttribute(uv, 2));
}
// bake the ground texture: samples at the floor height (heightAt) facing up; returns Uint8 RGB (value / scale)
export function bakeGroundMap({ occluders, heightAt, sun, sky, rays = 20, scale = 2, solidAt = null }) {
  const bvh = new BVH(worldTriangles(occluders));
  const [W, H] = lmSize(), out = new Uint8Array(W * H * 3);
  const dirs = []; for (let i = 0; i < rays; i++) { const u = (i + 0.5) / rays, v = ((i * 0.618034) % 1); const r = Math.sqrt(u), a = 2 * Math.PI * v; dirs.push([r * Math.cos(a), Math.sqrt(1 - u), r * Math.sin(a)]); }
  const sd = sun.dir.clone().normalize(), t1 = new THREE.Vector3(0, 1, 0).cross(sd).normalize(), t2 = new THREE.Vector3().crossVectors(sd, t1), sunDirs = [];
  for (let i = 0; i < (sun.rays || 3); i++) { const a = i / (sun.rays || 3) * Math.PI * 2, r = i ? sun.spread || 0.02 : 0; sunDirs.push(sd.clone().addScaledVector(t1, Math.cos(a) * r).addScaledVector(t2, Math.sin(a) * r).normalize()); }
  for (let j = 0; j < H; j++) for (let i = 0; i < W; i++) {
    const x = LM.x0 + (i + 0.5) * LM.res, z = LM.z0 + (j + 0.5) * LM.res, o = (j * W + i) * 3;
    if (solidAt && solidAt(x, z)) { out[o] = out[o + 1] = out[o + 2] = 20; continue; }
    const y = heightAt(x, z) + 0.04;
    let r = 0, g = 0, b = 0;
    for (const [dx, dy, dz] of dirs) {
      if (bvh.occluded(x, y, z, dx, dy, dz, sky.dist || 60)) { r += sky.bounce.r; g += sky.bounce.g; b += sky.bounce.b; }
      else { const t = Math.max(0, Math.min(1, dy)); r += sky.horizon.r + (sky.zenith.r - sky.horizon.r) * t; g += sky.horizon.g + (sky.zenith.g - sky.horizon.g) * t; b += sky.horizon.b + (sky.zenith.b - sky.horizon.b) * t; }
    }
    r /= dirs.length; g /= dirs.length; b /= dirs.length;
    let vis = 0; for (const d of sunDirs) if (!bvh.occluded(x, y, z, d.x, d.y, d.z, 400)) vis++;
    const k = Math.max(0, sd.y) * vis / sunDirs.length; r += sun.col.r * k; g += sun.col.g * k; b += sun.col.b * k;
    out[o] = Math.min(255, r / scale * 255 + 0.5); out[o + 1] = Math.min(255, g / scale * 255 + 0.5); out[o + 2] = Math.min(255, b / scale * 255 + 0.5);
  }
  return out;
}
export function applyGroundMap(meshes, rgb, { scale = 2, gain = 1 } = {}) {
  const [W, H] = lmSize(); if (rgb.length !== W * H * 3) { console.warn('ground map size', rgb.length, W * H * 3); return false; }
  const rgba = new Uint8Array(W * H * 4); for (let i = 0; i < W * H; i++) { rgba[i * 4] = rgb[i * 3]; rgba[i * 4 + 1] = rgb[i * 3 + 1]; rgba[i * 4 + 2] = rgb[i * 3 + 2]; rgba[i * 4 + 3] = 255; }
  const tex = new THREE.DataTexture(rgba, W, H, THREE.RGBAFormat); tex.magFilter = THREE.LinearFilter; tex.minFilter = THREE.LinearFilter; tex.channel = 1; tex.needsUpdate = true;
  const conv = new Map();
  for (const m of meshes) {
    const src = Array.isArray(m.material) ? m.material[0] : m.material;
    if (!conv.has(src)) conv.set(src, new THREE.MeshBasicMaterial({ map: src.map, color: src.color.clone().multiplyScalar(gain), lightMap: tex, lightMapIntensity: Math.PI * scale, transparent: src.transparent, alphaTest: src.alphaTest, side: src.side, fog: true }));
    m.material = conv.get(src); m.receiveShadow = false; m.castShadow = false;
  }
  return tex;
}

// ---------------- the bake ----------------
// lights: [{ p: Vector3, col: Color (already times intensity), r: range, lobe: 'down' | 'omni' | Vector3 (facing dir), size }]
// outdoors (city): sun = { dir: Vector3 towards the sun, col: Color, spread: radians, rays }, sky = { zenith, horizon, bounce: Color, dist }
// meshes with userData.bakeIndoor use the old room model (lamps + local AO with `ambient`, or their own colour)
export function bakeLighting({ receivers, occluders, lights, ambient = new THREE.Color(0.22, 0.22, 0.21), aoRays = 24, aoDist = 2.6, falloff = 1.6, onProgress = null, sun = null, sky = null }) {
  const bvh = new BVH(worldTriangles(occluders));
  // fixed, evenly spread cosine-weighted directions (same result every run)
  const dirs = [];
  for (let i = 0; i < aoRays; i++) { const u = (i + 0.5) / aoRays, v = ((i * 0.618034) % 1); const r = Math.sqrt(u), a = 2 * Math.PI * v; dirs.push([r * Math.cos(a), r * Math.sin(a), Math.sqrt(1 - u)]); }
  const cache = new Map();
  const P = new THREE.Vector3(), Nn = new THREE.Vector3(), nm = new THREE.Matrix3();
  const T1 = new THREE.Vector3(), T2 = new THREE.Vector3();
  const res = [];
  let done = 0; const total = receivers.reduce((s, m) => s + m.geometry.attributes.position.count, 0);
  const sunDirs = [];
  if (sun) { const k = sun.rays || 4, t1 = new THREE.Vector3(), t2 = new THREE.Vector3(); const d = sun.dir.clone().normalize(); t1.set(0, 1, 0).cross(d).normalize(); t2.crossVectors(d, t1);
    for (let i = 0; i < k; i++) { const a = i / k * Math.PI * 2, r = i ? sun.spread || 0.02 : 0; sunDirs.push(d.clone().addScaledVector(t1, Math.cos(a) * r).addScaledVector(t2, Math.sin(a) * r).normalize()); } }
  const skyCol = (y) => { const t = Math.max(0, Math.min(1, y)); return [sky.horizon.r + (sky.zenith.r - sky.horizon.r) * t, sky.horizon.g + (sky.zenith.g - sky.horizon.g) * t, sky.horizon.b + (sky.zenith.b - sky.horizon.b) * t]; };
  for (const m of receivers) {
    const g = m.geometry, p = g.attributes.position, n = g.attributes.normal, cnt = p.count;
    nm.getNormalMatrix(m.matrixWorld);
    const indoor = !sky || !!m.userData.bakeIndoor, amb0 = m.userData.bakeIndoor && m.userData.bakeIndoor.isColor ? m.userData.bakeIndoor : ambient;
    const out = new Float32Array(cnt * 3);
    for (let i = 0; i < cnt; i++) {
      P.fromBufferAttribute(p, i).applyMatrix4(m.matrixWorld); Nn.fromBufferAttribute(n, i).applyMatrix3(nm).normalize();
      const key = `${indoor ? 'i' : 'o'}${Math.round(P.x * 200)},${Math.round(P.y * 200)},${Math.round(P.z * 200)},${Math.round(Nn.x * 20)},${Math.round(Nn.y * 20)},${Math.round(Nn.z * 20)}`;
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
        if (indoor) {
          let ao = 0;
          for (const [a, b, c] of dirs) {
            const rx = T2.x * a + T1.x * b + Nn.x * c, ry = T2.y * a + T1.y * b + Nn.y * c, rz = T2.z * a + T1.z * b + Nn.z * c;
            ao += bvh.occluded(ox, oy, oz, rx, ry, rz, aoDist) ? 0 : 1;
          }
          ao /= dirs.length;
          const amb = 0.25 + 0.75 * ao;
          e[0] += amb0.r * amb; e[1] += amb0.g * amb; e[2] += amb0.b * amb;
        } else {
          // sky light: rays that escape see the sky (brighter overhead), blocked rays get bounced light
          let sr = 0, sg = 0, sb = 0;
          for (const [a, b, c] of dirs) {
            const rx = T2.x * a + T1.x * b + Nn.x * c, ry = T2.y * a + T1.y * b + Nn.y * c, rz = T2.z * a + T1.z * b + Nn.z * c;
            if (ry < -0.05 || bvh.occluded(ox, oy, oz, rx, ry, rz, sky.dist || 60)) { sr += sky.bounce.r; sg += sky.bounce.g; sb += sky.bounce.b; }
            else { const c3 = skyCol(ry); sr += c3[0]; sg += c3[1]; sb += c3[2]; }
          }
          e[0] += sr / dirs.length; e[1] += sg / dirs.length; e[2] += sb / dirs.length;
          // sun with a soft edge
          const ndl = Nn.x * sun.dir.x + Nn.y * sun.dir.y + Nn.z * sun.dir.z;
          if (ndl > 0) {
            let vis = 0; for (const d of sunDirs) if (!bvh.occluded(ox, oy, oz, d.x, d.y, d.z, 400)) vis++;
            const k = ndl * vis / sunDirs.length; e[0] += sun.col.r * k; e[1] += sun.col.g * k; e[2] += sun.col.b * k;
          }
        }
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
