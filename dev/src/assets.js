// Asset loading for the hosted build: glTF (+KTX2 textures, meshopt geometry), gzipped character rigs,
// WebP textures. Tracks download progress for the loading screens.
import * as THREE from 'three';
import { GLTFLoader } from '../three/examples/jsm/loaders/GLTFLoader.js';
import { KTX2Loader } from '../three/examples/jsm/loaders/KTX2Loader.js';
import { MeshoptDecoder } from '../three/examples/jsm/libs/meshopt_decoder.module.js';
import { gunzipSync } from '../three/examples/jsm/libs/fflate.module.js';

export const BASE = 'assets/';
let loader = null, ktx2 = null;
const cache = new Map();
export const net = { bytes: 0, onProgress: null };

export function initLoaders(renderer) {
  ktx2 = new KTX2Loader().setTranscoderPath(BASE + 'basis/').detectSupport(renderer);
  loader = new GLTFLoader().setKTX2Loader(ktx2).setMeshoptDecoder(MeshoptDecoder);
}

async function fetchBuf(path) {
  const r = await fetch(BASE + path);
  if (!r.ok) throw new Error('Brak pliku ' + path + ' (' + r.status + ')');
  if (!r.body || !r.body.getReader) { const b = await r.arrayBuffer(); net.bytes += b.byteLength; net.onProgress && net.onProgress(); return b; }
  const reader = r.body.getReader(); const parts = []; let n = 0;
  for (;;) { const { done, value } = await reader.read(); if (done) break; parts.push(value); n += value.length; net.bytes += value.length; net.onProgress && net.onProgress(); }
  const out = new Uint8Array(n); let o = 0; for (const p of parts) { out.set(p, o); o += p.length; }
  return out.buffer;
}

export function loadGLTF(path) {
  if (!cache.has(path)) cache.set(path, fetchBuf(path).then(buf => new Promise((res, rej) => loader.parse(buf, BASE, res, rej))));
  return cache.get(path);
}
export function loadRig(path) {
  if (!cache.has(path)) cache.set(path, fetchBuf(path).then(buf => {
    const raw = gunzipSync(new Uint8Array(buf));
    return new Promise((res, rej) => loader.parse(raw.buffer.slice(raw.byteOffset, raw.byteOffset + raw.byteLength), BASE, res, rej));
  }));
  return cache.get(path);
}
export async function loadJSON(path) { return JSON.parse(new TextDecoder().decode(await fetchBuf(path))); }
export function loadTex(path, srgb = true, opts = {}) {
  const key = 'tex:' + path + srgb;
  if (!cache.has(key)) cache.set(key, fetchBuf(path).then(async (buf) => {
    const type = path.endsWith('.png') ? 'image/png' : path.endsWith('.jpg') ? 'image/jpeg' : 'image/webp';
    const img = new Image(); img.src = URL.createObjectURL(new Blob([buf], { type }));
    await img.decode();
    const t = new THREE.Texture(img);
    if (srgb) t.colorSpace = THREE.SRGBColorSpace;
    t.anisotropy = opts.aniso ?? 4; if (opts.flipY === false) t.flipY = false;
    if (opts.repeat) { t.wrapS = t.wrapT = THREE.RepeatWrapping; }
    t.needsUpdate = true;
    return t;
  }));
  return cache.get(key);
}
export function forget(path) { cache.delete(path); }

// Drop triangles of a scanned mesh that lie on its outer bounding box (the black "shell" some photogrammetry
// exports have), and optionally everything outside a keep-box (local coordinates of the scan root).
export function trimScan(root, { shell = 0, keep = null, drop = null } = {}) {
  root.updateMatrixWorld(true);
  const inv = new THREE.Matrix4().copy(root.matrixWorld).invert();
  const box = new THREE.Box3();
  const v = new THREE.Vector3();
  root.traverse(o => { if (o.isMesh) { const p = o.geometry.attributes.position; const m = new THREE.Matrix4().multiplyMatrices(inv, o.matrixWorld); for (let i = 0; i < p.count; i += 7) box.expandByPoint(v.fromBufferAttribute(p, i).applyMatrix4(m)); } });
  let removed = 0, kept = 0;
  root.traverse(o => {
    if (!o.isMesh) return;
    const g = o.geometry, p = g.attributes.position, idx = g.index;
    if (!idx) return;
    const m = new THREE.Matrix4().multiplyMatrices(inv, o.matrixWorld);
    const P = new Float32Array(p.count * 3);
    for (let i = 0; i < p.count; i++) { v.fromBufferAttribute(p, i).applyMatrix4(m); P[i * 3] = v.x; P[i * 3 + 1] = v.y; P[i * 3 + 2] = v.z; }
    const src = idx.array, out = [];
    const near = (a, b, c, k, val) => Math.abs(P[a * 3 + k] - val) < shell && Math.abs(P[b * 3 + k] - val) < shell && Math.abs(P[c * 3 + k] - val) < shell;
    for (let t = 0; t < src.length; t += 3) {
      const a = src[t], b = src[t + 1], c = src[t + 2];
      let bad = false;
      if (shell > 0) {
        bad = near(a, b, c, 0, box.min.x) || near(a, b, c, 0, box.max.x) || near(a, b, c, 2, box.min.z) || near(a, b, c, 2, box.max.z) || near(a, b, c, 1, box.min.y) || near(a, b, c, 1, box.max.y);
      }
      if (!bad && (keep || drop)) {
        const cx = (P[a * 3] + P[b * 3] + P[c * 3]) / 3, cy = (P[a * 3 + 1] + P[b * 3 + 1] + P[c * 3 + 1]) / 3, cz = (P[a * 3 + 2] + P[b * 3 + 2] + P[c * 3 + 2]) / 3;
        if (keep && (cx < keep.min.x || cx > keep.max.x || cy < keep.min.y || cy > keep.max.y || cz < keep.min.z || cz > keep.max.z)) bad = true;
        if (drop) for (const d of drop) if (cx > d.min.x && cx < d.max.x && cy > d.min.y && cy < d.max.y && cz > d.min.z && cz < d.max.z) { bad = true; break; }
      }
      if (bad) removed++; else { out.push(a, b, c); kept++; }
    }
    g.setIndex(out);
    g.computeBoundingSphere(); g.computeBoundingBox();
  });
  return { removed, kept, box };
}
