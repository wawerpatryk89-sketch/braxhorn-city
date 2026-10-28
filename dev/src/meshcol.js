// Collision derived from a static mesh: a grid of walkable floor heights (several per cell, so multi-storey
// buildings and stairs work) and wall spans (vertical geometry blocks movement between two heights).
import * as THREE from 'three';
import { STEP_UP } from './world.js';

export function buildMeshCollider(root, { cell = 0.25, exclude = () => false, wallExclude = () => false, maxFloors = 6 } = {}) {
  root.updateMatrixWorld(true);
  const box = new THREE.Box3().setFromObject(root);
  const x0 = box.min.x - 1, z0 = box.min.z - 1;
  const nx = Math.ceil((box.max.x - x0 + 1) / cell), nz = Math.ceil((box.max.z - z0 + 1) / cell);
  const floors = new Array(nx * nz), walls = new Array(nx * nz);
  const a = new THREE.Vector3(), b = new THREE.Vector3(), c = new THREE.Vector3(), n = new THREE.Vector3(), e1 = new THREE.Vector3(), e2 = new THREE.Vector3();
  const addFloor = (i, h) => { const f = floors[i] || (floors[i] = []); for (const v of f) if (Math.abs(v - h) < 0.12) return; if (f.length < maxFloors) f.push(h); };
  const addWall = (i, lo, hi) => { const w = walls[i] || (walls[i] = []); for (const s of w) if (lo <= s[1] + 0.05 && hi >= s[0] - 0.05) { s[0] = Math.min(s[0], lo); s[1] = Math.max(s[1], hi); return; } w.push([lo, hi]); };
  let ntri = 0;
  root.traverse(o => {
    if (!o.isMesh || exclude(o)) return;
    const g = o.geometry, p = g.attributes.position, idx = g.index, m = o.matrixWorld;
    const noWall = wallExclude(o);
    const cnt = idx ? idx.count : p.count;
    for (let t = 0; t < cnt; t += 3) {
      const ia = idx ? idx.getX(t) : t, ib = idx ? idx.getX(t + 1) : t + 1, ic = idx ? idx.getX(t + 2) : t + 2;
      a.fromBufferAttribute(p, ia).applyMatrix4(m); b.fromBufferAttribute(p, ib).applyMatrix4(m); c.fromBufferAttribute(p, ic).applyMatrix4(m);
      n.crossVectors(e1.subVectors(b, a), e2.subVectors(c, a)); const len = n.length(); if (len < 1e-9) continue; n.divideScalar(len);
      ntri++;
      if (Math.abs(n.y) > 0.62) {
        // walkable (or ceiling): rasterise cell centres inside the XZ triangle
        const minx = Math.max(0, Math.floor((Math.min(a.x, b.x, c.x) - x0) / cell)), maxx = Math.min(nx - 1, Math.floor((Math.max(a.x, b.x, c.x) - x0) / cell));
        const minz = Math.max(0, Math.floor((Math.min(a.z, b.z, c.z) - z0) / cell)), maxz = Math.min(nz - 1, Math.floor((Math.max(a.z, b.z, c.z) - z0) / cell));
        const d = (b.z - c.z) * (a.x - c.x) + (c.x - b.x) * (a.z - c.z); if (Math.abs(d) < 1e-9) continue;
        for (let iz = minz; iz <= maxz; iz++) for (let ix = minx; ix <= maxx; ix++) {
          const px = x0 + (ix + 0.5) * cell, pz = z0 + (iz + 0.5) * cell;
          const w1 = ((b.z - c.z) * (px - c.x) + (c.x - b.x) * (pz - c.z)) / d, w2 = ((c.z - a.z) * (px - c.x) + (a.x - c.x) * (pz - c.z)) / d, w3 = 1 - w1 - w2;
          if (w1 < -0.02 || w2 < -0.02 || w3 < -0.02) continue;
          addFloor(iz * nx + ix, a.y * w1 + b.y * w2 + c.y * w3);
        }
      } else if (Math.abs(n.y) < 0.45 && !noWall) {
        // steep: mark the cells along its footprint with its vertical span
        const lo = Math.min(a.y, b.y, c.y), hi = Math.max(a.y, b.y, c.y);
        for (const [p1, p2] of [[a, b], [b, c], [c, a]]) {
          const L = Math.hypot(p2.x - p1.x, p2.z - p1.z), steps = Math.max(1, Math.ceil(L / (cell * 0.5)));
          for (let s = 0; s <= steps; s++) {
            const k = s / steps, px = p1.x + (p2.x - p1.x) * k, pz = p1.z + (p2.z - p1.z) * k;
            const ix = Math.floor((px - x0) / cell), iz = Math.floor((pz - z0) / cell);
            if (ix >= 0 && iz >= 0 && ix < nx && iz < nz) addWall(iz * nx + ix, lo, hi);
          }
        }
      }
    }
  });
  for (const f of floors) if (f) f.sort((p, q) => p - q);
  const col = {
    x0, z0, nx, nz, cell, floors, walls, ntri,
    idx(x, z) { const ix = Math.floor((x - x0) / cell), iz = Math.floor((z - z0) / cell); return ix < 0 || iz < 0 || ix >= nx || iz >= nz ? -1 : iz * nx + ix; },
    // highest floor at or a little above the feet (step up); -Infinity if none
    groundAt(x, z, y) {
      let best = -Infinity;
      for (const [dx, dz] of [[0, 0], [cell * 0.5, 0], [-cell * 0.5, 0], [0, cell * 0.5], [0, -cell * 0.5]]) {
        const i = col.idx(x + dx, z + dz); if (i < 0 || !floors[i]) continue;
        for (const h of floors[i]) if (h <= y + STEP_UP && h > best) best = h;
      }
      return best;
    },
    blocked(i, y, h) { const w = walls[i]; if (!w) return false; for (const s of w) if (s[1] > y + STEP_UP && s[0] < y + h) return true; return false; },
    resolve(pos, r, y, h) {
      let hit = false;
      const ix0 = Math.floor((pos.x - r - x0) / cell), ix1 = Math.floor((pos.x + r - x0) / cell);
      const iz0 = Math.floor((pos.z - r - z0) / cell), iz1 = Math.floor((pos.z + r - z0) / cell);
      for (let iz = iz0; iz <= iz1; iz++) for (let ix = ix0; ix <= ix1; ix++) {
        if (ix < 0 || iz < 0 || ix >= nx || iz >= nz) continue;
        const i = iz * nx + ix; if (!col.blocked(i, y, h)) continue;
        const bx0 = x0 + ix * cell, bz0 = z0 + iz * cell;
        const cx = Math.max(bx0, Math.min(pos.x, bx0 + cell)), cz = Math.max(bz0, Math.min(pos.z, bz0 + cell));
        const dx = pos.x - cx, dz = pos.z - cz, d2 = dx * dx + dz * dz;
        if (d2 >= r * r) continue;
        hit = true;
        if (d2 > 1e-9) { const d = Math.sqrt(d2); pos.x = cx + dx / d * r; pos.z = cz + dz / d * r; }
        else { const ccx = bx0 + cell / 2, ccz = bz0 + cell / 2; const ddx = pos.x - ccx, ddz = pos.z - ccz; if (Math.abs(ddx) > Math.abs(ddz)) pos.x = ccx + Math.sign(ddx || 1) * (cell / 2 + r); else pos.z = ccz + Math.sign(ddz || 1) * (cell / 2 + r); }
      }
      return hit;
    },
  };
  return col;
}
