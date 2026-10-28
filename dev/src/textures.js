// Procedural textures for the gym, generated on canvas at startup (no image files needed).
import * as THREE from 'three';

function hash(i, j, s) {
  let h = (Math.imul(i, 374761393) + Math.imul(j, 668265263) + Math.imul(s + 1, 1442695041)) | 0;
  h = Math.imul(h ^ (h >>> 13), 1274126177);
  return ((h ^ (h >>> 16)) >>> 0) / 4294967296;
}
function vnoise(x, y, P, s) {
  const xi = Math.floor(x), yi = Math.floor(y);
  const xf = x - xi, yf = y - yi;
  const u = xf * xf * (3 - 2 * xf), v = yf * yf * (3 - 2 * yf);
  const i0 = ((xi % P) + P) % P, i1 = (i0 + 1) % P, j0 = ((yi % P) + P) % P, j1 = (j0 + 1) % P;
  const a = hash(i0, j0, s), b = hash(i1, j0, s), c = hash(i0, j1, s), d = hash(i1, j1, s);
  return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
}
export function fbm(x, y, P, oct, s) {
  let a = 0.5, f = 1, sum = 0, norm = 0;
  for (let o = 0; o < oct; o++) { sum += a * vnoise(x * f, y * f, P * f, s + o * 17); norm += a; a *= 0.5; f *= 2; }
  return sum / norm;
}
const clamp01 = (v) => v < 0 ? 0 : v > 1 ? 1 : v;
const smooth = (a, b, x) => { const t = clamp01((x - a) / (b - a)); return t * t * (3 - 2 * t); };

function canvas(w, h) { const c = document.createElement('canvas'); c.width = w; c.height = h; return c; }
function texFromCanvas(c, srgb, repeat) {
  const t = new THREE.CanvasTexture(c);
  if (srgb) t.colorSpace = THREE.SRGBColorSpace;
  t.wrapS = t.wrapT = repeat ? THREE.RepeatWrapping : THREE.ClampToEdgeWrapping;
  t.anisotropy = 8;
  t.needsUpdate = true;
  return t;
}
// height (Float32Array, w*h, tiling) -> tangent-space normal map canvas
function normalFromHeight(hgt, w, h, strength) {
  const c = canvas(w, h), ctx = c.getContext('2d'), img = ctx.createImageData(w, h), d = img.data;
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const xl = (x - 1 + w) % w, xr = (x + 1) % w, yu = (y - 1 + h) % h, yd = (y + 1) % h;
    const dx = (hgt[y * w + xr] - hgt[y * w + xl]) * strength;
    const dy = (hgt[yd * w + x] - hgt[yu * w + x]) * strength;
    const l = Math.hypot(dx, dy, 1);
    const i = (y * w + x) * 4;
    d[i] = (-dx / l * 0.5 + 0.5) * 255; d[i + 1] = (dy / l * 0.5 + 0.5) * 255; d[i + 2] = (1 / l * 0.5 + 0.5) * 255; d[i + 3] = 255;
  }
  ctx.putImageData(img, 0, 0);
  return c;
}

// ---------- polished, worn concrete (tiles) ----------
export function concrete(size = 512) {
  const w = size, h = size;
  const col = canvas(w, h), cx = col.getContext('2d'), ci = cx.createImageData(w, h), cd = ci.data;
  const rough = canvas(w, h), rx = rough.getContext('2d'), ri = rx.createImageData(w, h), rd = ri.data;
  const hgt = new Float32Array(w * h);
  const P = 4;
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const u = x / w * P, v = y / h * P;
    const large = fbm(u * 0.5, v * 0.5, P * 0.5, 4, 3);
    const mid = fbm(u * 2, v * 2, P * 2, 4, 11);
    const fine = vnoise(u * 48, v * 48, P * 48, 29);
    const speck = hash(x, y, 7);
    let g = 0.085 + 0.07 * large + 0.035 * mid + (speck > 0.985 ? 0.06 : 0) - (speck < 0.01 ? 0.03 : 0);
    const stain = smooth(0.55, 0.75, fbm(u * 1.3 + 9, v * 1.3, P * 1.3, 3, 41));
    g *= 1 - 0.35 * stain;
    const i = (y * w + x) * 4;
    cd[i] = 255 * g * 0.98; cd[i + 1] = 255 * g * 1.0; cd[i + 2] = 255 * g * 1.07; cd[i + 3] = 255;
    const r = 0.32 + 0.28 * mid + 0.18 * stain + 0.1 * fine;
    rd[i] = rd[i + 1] = rd[i + 2] = 255 * clamp01(r); rd[i + 3] = 255;
    hgt[y * w + x] = 0.6 * mid + 0.25 * fine + (speck > 0.985 ? 0.4 : 0);
  }
  // hairline cracks
  cx.putImageData(ci, 0, 0);
  cx.strokeStyle = 'rgba(0,0,0,0.35)'; cx.lineWidth = 1;
  for (let k = 0; k < 7; k++) {
    let px = hash(k, 1, 91) * w, py = hash(k, 2, 91) * h, a = hash(k, 3, 91) * Math.PI * 2;
    cx.beginPath(); cx.moveTo(px, py);
    for (let s = 0; s < 40; s++) { a += (hash(k, s, 5) - 0.5) * 0.9; px += Math.cos(a) * 6; py += Math.sin(a) * 6; cx.lineTo(px, py); }
    cx.stroke();
  }
  rx.putImageData(ri, 0, 0);
  return {
    map: texFromCanvas(col, true, true),
    roughnessMap: texFromCanvas(rough, false, true),
    normalMap: texFromCanvas(normalFromHeight(hgt, w, h, 3.0), false, true),
  };
}

// puddles / wet streaks across the whole floor (not tiled). R = wetness
export function wetMask(w = 256, h = 192) {
  const c = canvas(w, h), x2 = c.getContext('2d'), img = x2.createImageData(w, h), d = img.data;
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const n = fbm(x / w * 6, y / h * 4.5, 64, 5, 77);
    const streak = fbm(x / w * 2, y / h * 14, 64, 3, 5);
    const wet = smooth(0.38, 0.62, n * 0.8 + streak * 0.3);
    const i = (y * w + x) * 4;
    d[i] = d[i + 1] = d[i + 2] = 255 * (0.35 + 0.65 * wet); d[i + 3] = 255;
  }
  x2.putImageData(img, 0, 0);
  return texFromCanvas(c, false, false);
}

// ---------- old red-brown brick ----------
export function brick(w = 1024, h = 1024, opts = {}) {
  const paint = opts.paint || null; // painted brick: one colour, only the relief and grime remain
  const col = canvas(w, h), cx = col.getContext('2d');
  const hgt = new Float32Array(w * h);
  const rows = opts.rows || 30, bh = h / rows, bw = bh * 3.1, mortar = opts.rows ? 4 : 3;
  cx.fillStyle = paint ? `rgb(${paint[0] * 0.82 | 0},${paint[1] * 0.82 | 0},${paint[2] * 0.82 | 0})` : '#4a4038'; cx.fillRect(0, 0, w, h);
  const cols = Math.round(w / bw);
  const bwx = w / cols;
  for (let r = 0; r < rows; r++) {
    const off = (r % 2) * bwx / 2;
    for (let k = -1; k <= cols; k++) {
      const x0 = k * bwx + off, y0 = r * bh;
      const seed = hash(r, (k + cols) % cols, 13);
      const burnt = seed > 0.86;
      const base = paint ? paint : burnt ? [52, 30, 24] : [118 + 40 * hash(r, k, 2), 58 + 22 * hash(r, k, 3), 36 + 14 * hash(r, k, 4)];
      const dark = paint ? 0.9 + 0.1 * hash(r, k, 9) : 0.7 + 0.3 * hash(r, k, 9);
      cx.fillStyle = `rgb(${base[0] * dark | 0},${base[1] * dark | 0},${base[2] * dark | 0})`;
      cx.fillRect(x0 + mortar, y0 + mortar, bwx - mortar * 2, bh - mortar * 2);
    }
  }
  // per-pixel grime + height
  const img = cx.getImageData(0, 0, w, h), d = img.data;
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const i = (y * w + x) * 4;
    const r = Math.floor(y / bh), off = (r % 2) * bwx / 2;
    const lx = ((x - off) % bwx + bwx) % bwx, ly = y % bh;
    const edge = Math.min(lx, bwx - lx, ly, bh - ly);
    const inBrick = edge > mortar;
    const n = fbm(x / w * 8, y / h * 8, 8, 4, 21);
    const g = paint ? 0.95 + 0.08 * n : 0.75 + 0.45 * n;
    const soot = smooth(0.55, 0.85, fbm(x / w * 2, y / h * 3, 2, 3, 61)) * (paint ? 0.05 : 0.45);
    for (let c = 0; c < 3; c++) d[i + c] = d[i + c] * g * (1 - soot);
    hgt[y * w + x] = inBrick ? 0.6 + 0.4 * Math.min(1, (edge - mortar) / 4) * (0.8 + 0.2 * n) : 0.1 * n;
  }
  cx.putImageData(img, 0, 0);
  return { map: texFromCanvas(col, true, true), normalMap: texFromCanvas(normalFromHeight(hgt, w, h, 2.2), false, true) };
}

// ---------- dark timber planks ----------
export function wood(w = 512, h = 512) {
  const c = canvas(w, h), x2 = c.getContext('2d'), img = x2.createImageData(w, h), d = img.data;
  const planks = 6;
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const p = Math.floor(x / (w / planks));
    const lx = (x % (w / planks)) / (w / planks);
    const grain = Math.sin((y / h * 40 + fbm(x / w * 3 + p, y / h * 2, 64, 4, 3 + p) * 18) ) * 0.5 + 0.5;
    const tone = 0.55 + 0.25 * hash(p, 0, 4) + 0.2 * grain;
    const seam = lx < 0.02 || lx > 0.98 ? 0.35 : 1;
    const i = (y * w + x) * 4;
    d[i] = 72 * tone * seam; d[i + 1] = 44 * tone * seam; d[i + 2] = 26 * tone * seam; d[i + 3] = 255;
  }
  x2.putImageData(img, 0, 0);
  return texFromCanvas(c, true, true);
}

// ---------- ring canvas with logo ----------
export function ringCanvas(font, badge = null, base = '#7f8c97') {
  const s = 1024, c = canvas(s, s), x2 = c.getContext('2d');
  x2.fillStyle = base; x2.fillRect(0, 0, s, s);
  const img = x2.getImageData(0, 0, s, s), d = img.data;
  for (let y = 0; y < s; y++) for (let x = 0; x < s; x++) {
    const i = (y * s + x) * 4;
    const n = 0.86 + 0.14 * fbm(x / s * 6, y / s * 6, 64, 4, 8) + ((x + y) % 4 === 0 ? -0.025 : 0);
    const stain = smooth(0.6, 0.8, fbm(x / s * 3, y / s * 3, 64, 3, 93)) * 0.18;
    for (let k = 0; k < 3; k++) d[i + k] *= n * (1 - stain);
  }
  x2.putImageData(img, 0, 0);
  x2.save(); x2.translate(s / 2, s / 2);
  if (badge) {
    x2.globalAlpha = 0.92; x2.save(); x2.beginPath(); x2.arc(0, 0, 300, 0, Math.PI * 2); x2.clip(); x2.drawImage(badge, -300, -300, 600, 600); x2.restore(); x2.globalAlpha = 1;
    x2.strokeStyle = 'rgba(139,255,58,0.85)'; x2.lineWidth = 8; x2.beginPath(); x2.arc(0, 0, 306, 0, Math.PI * 2); x2.stroke();
  } else {
    x2.strokeStyle = 'rgba(30,20,20,0.55)'; x2.lineWidth = 10;
    x2.beginPath(); x2.arc(0, 0, 190, 0, Math.PI * 2); x2.stroke();
    x2.fillStyle = 'rgba(150,34,30,0.78)';
    x2.font = `800 108px ${font}`; x2.textAlign = 'center'; x2.textBaseline = 'middle';
    x2.fillText('BRAXHORN', 0, -6);
  }
  x2.restore();
  return texFromCanvas(c, true, false);
}

export function apronSkirt(font) {
  const w = 2048, h = 256, c = canvas(w, h), x2 = c.getContext('2d');
  x2.fillStyle = '#121319'; x2.fillRect(0, 0, w, h);
  x2.fillStyle = '#5fd422'; x2.fillRect(0, 18, w, 10); x2.fillRect(0, h - 28, w, 10);
  x2.fillStyle = '#e4e6e1'; x2.font = `800 104px ${font}`; x2.textBaseline = 'middle';
  let x = 40;
  while (x < w) { x2.fillText('BRAXHORN GYM', x, h / 2 + 4); x += x2.measureText('BRAXHORN GYM').width + 120; }
  return texFromCanvas(c, true, true);
}

export function neonSign(font, text, color) {
  const w = 1024, h = 256, c = canvas(w, h), x2 = c.getContext('2d');
  x2.font = `800 150px ${font}`; x2.textAlign = 'center'; x2.textBaseline = 'middle';
  const tw = x2.measureText(text).width; if (tw > w * 0.88) x2.font = `800 ${150 * w * 0.88 / tw | 0}px ${font}`; // never clip with a wider fallback font
  x2.shadowColor = color; x2.shadowBlur = 40; x2.fillStyle = color;
  x2.fillText(text, w / 2, h / 2 + 6);
  x2.shadowBlur = 12; x2.fillStyle = '#fff6ea';
  x2.fillText(text, w / 2, h / 2 + 6);
  return texFromCanvas(c, true, false);
}

// soft round shadow / glow sprite
export function radial(inner = 'rgba(0,0,0,0.85)', outer = 'rgba(0,0,0,0)') {
  const s = 128, c = canvas(s, s), x2 = c.getContext('2d');
  const g = x2.createRadialGradient(s / 2, s / 2, 0, s / 2, s / 2, s / 2);
  g.addColorStop(0, inner); g.addColorStop(1, outer);
  x2.fillStyle = g; x2.fillRect(0, 0, s, s);
  return texFromCanvas(c, false, false);
}

// leather for heavy bags
export function leather(baseHex) {
  const w = 256, h = 256, c = canvas(w, h), x2 = c.getContext('2d'), img = x2.createImageData(w, h), d = img.data;
  const base = new THREE.Color(baseHex);
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    const n = 0.8 + 0.3 * fbm(x / w * 16, y / h * 16, 16, 3, 4);
    const crease = Math.abs(fbm(x / w * 3, y / h * 6, 3, 3, 12) - 0.5) < 0.02 ? 0.6 : 1;
    const i = (y * w + x) * 4;
    d[i] = base.r * 255 * n * crease; d[i + 1] = base.g * 255 * n * crease; d[i + 2] = base.b * 255 * n * crease; d[i + 3] = 255;
  }
  x2.putImageData(img, 0, 0);
  return texFromCanvas(c, true, true);
}
