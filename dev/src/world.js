// Simple 2.5D collision world: characters are vertical capsules (circle in XZ + height),
// obstacles are axis-aligned boxes or vertical cylinders with a bottom and a top.
export const STEP_UP = 0.36;

export class World {
  constructor() { this.boxes = []; this.circles = []; this.dynamic = []; this.regions = []; }
  // mesh-derived collision for a rectangular area (villa, scanned arenas)
  addRegion(minX, maxX, minZ, maxZ, col) { const r = { minX, maxX, minZ, maxZ, col }; this.regions.push(r); return r; }
  regionAt(x, z) { for (const r of this.regions) if (!r.off && x >= r.minX && x <= r.maxX && z >= r.minZ && z <= r.maxZ) return r; return null; }
  box(minX, maxX, minZ, maxZ, top, bottom = 0, tag = '') { const b = { minX, maxX, minZ, maxZ, top, bottom, tag }; this.boxes.push(b); return b; }
  boxC(cx, cz, w, d, top, bottom = 0, tag = '') { return this.box(cx - w / 2, cx + w / 2, cz - d / 2, cz + d / 2, top, bottom, tag); }
  circle(x, z, r, top, bottom = 0, tag = '') { const c = { x, z, r, top, bottom, tag }; this.circles.push(c); return c; }

  // highest walkable surface under (x,z) that is reachable from height y
  groundAt(x, z, y, r = 0) {
    const reg = this.regionAt(x, z);
    let g = reg ? Math.max(-100, reg.col.groundAt(x, z, y)) : 0;
    for (const b of this.boxes) {
      if (b.top > y + STEP_UP || b.bottom > y + 0.05) continue;
      if (x >= b.minX - r * 0.3 && x <= b.maxX + r * 0.3 && z >= b.minZ - r * 0.3 && z <= b.maxZ + r * 0.3) g = Math.max(g, b.top);
    }
    for (const c of this.circles) {
      if (c.top > y + STEP_UP || c.bottom > y + 0.05 || c.noStand) continue;
      if ((x - c.x) ** 2 + (z - c.z) ** 2 <= (c.r * 0.8) ** 2) g = Math.max(g, c.top);
    }
    return g;
  }

  // push a circle (pos.x,pos.z, radius r, vertical span [y, y+h]) out of obstacles. Returns true if hit anything.
  resolve(pos, r, y, h, ignore, ignore2) {
    let hit = false;
    const reg = this.regionAt(pos.x, pos.z);
    if (reg && reg.col.resolve(pos, r, y, h)) hit = true;
    const blocks = (o) => o.top > y + STEP_UP && o.bottom < y + h;
    for (const b of this.boxes) {
      if (!blocks(b)) continue;
      const cx = Math.max(b.minX, Math.min(pos.x, b.maxX)), cz = Math.max(b.minZ, Math.min(pos.z, b.maxZ));
      let dx = pos.x - cx, dz = pos.z - cz;
      const d2 = dx * dx + dz * dz;
      if (d2 >= r * r) continue;
      hit = true;
      if (d2 > 1e-9) { const d = Math.sqrt(d2); pos.x = cx + dx / d * r; pos.z = cz + dz / d * r; }
      else {
        const pen = [pos.x - b.minX + r, b.maxX - pos.x + r, pos.z - b.minZ + r, b.maxZ - pos.z + r];
        const m = Math.min(...pen), k = pen.indexOf(m);
        if (k === 0) pos.x -= m; else if (k === 1) pos.x += m; else if (k === 2) pos.z -= m; else pos.z += m;
      }
    }
    for (const c of [...this.circles, ...this.dynamic]) {
      if (c === ignore || (ignore2 && c.owner === ignore2) || !blocks(c)) continue;
      const dx = pos.x - c.x, dz = pos.z - c.z, rr = r + c.r, d2 = dx * dx + dz * dz;
      if (d2 >= rr * rr) continue;
      hit = true;
      const d = Math.sqrt(d2) || 1e-4;
      const push = rr - d;
      if (c.mobile) { // share the push with movable things (other fighters, bags)
        pos.x += dx / d * push * 0.6; pos.z += dz / d * push * 0.6;
        c.onPush && c.onPush(-dx / d * push * 0.4, -dz / d * push * 0.4);
      } else { pos.x += dx / d * push; pos.z += dz / d * push; }
    }
    return hit;
  }

  // circle vs static obstacles for small rolling things (balls). Returns normal of the contact or null.
  bounce(pos, r, y) {
    let n = null;
    for (const b of this.boxes) {
      if (b.top < y + 0.05 || b.bottom > y + r) continue;
      const cx = Math.max(b.minX, Math.min(pos.x, b.maxX)), cz = Math.max(b.minZ, Math.min(pos.z, b.maxZ));
      const dx = pos.x - cx, dz = pos.z - cz, d2 = dx * dx + dz * dz;
      if (d2 >= r * r) continue;
      const d = Math.sqrt(d2) || 1e-4;
      pos.x = cx + dx / d * r; pos.z = cz + dz / d * r; n = { x: dx / d, z: dz / d };
    }
    for (const c of this.circles) {
      if (c.top < y + 0.05) continue;
      const dx = pos.x - c.x, dz = pos.z - c.z, rr = r + c.r, d2 = dx * dx + dz * dz;
      if (d2 >= rr * rr) continue;
      const d = Math.sqrt(d2) || 1e-4;
      pos.x = c.x + dx / d * rr; pos.z = c.z + dz / d * rr; n = { x: dx / d, z: dz / d };
    }
    return n;
  }
}
