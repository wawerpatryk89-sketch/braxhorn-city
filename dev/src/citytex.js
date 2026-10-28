// Procedural textures for the district (plansza 1): paving slabs, asphalt, quay stone, weathered planks, render,
// corrugated metal, chain-link fence, shop/club floors and painted signs. All matte (no glow); light comes from the bake.
import * as THREE from 'three';
import { fbm } from './textures.js';

let seed = 99173;
const rnd = () => { seed = (seed * 16807) % 2147483647; return seed / 2147483647; };
export const canvas = (w, h) => { const c = document.createElement('canvas'); c.width = w; c.height = h; return c; };
export function ctex(c, { srgb = true, repeat = true, aniso = 8 } = {}) {
  const t = new THREE.CanvasTexture(c); if (srgb) t.colorSpace = THREE.SRGBColorSpace;
  if (repeat) t.wrapS = t.wrapT = THREE.RepeatWrapping; t.anisotropy = aniso; return t;
}
const cache = new Map();
const memo = (k, f) => { if (!cache.has(k)) cache.set(k, f()); return cache.get(k); };

// grain + stains over a filled canvas (keeps it from looking flat): one shared noise tile, multiplied on top
let noiseTile = null;
function getNoise() {
  if (noiseTile) return noiseTile;
  const S = 256, c = canvas(S, S), x = c.getContext('2d'), img = x.createImageData(S, S), d = img.data;
  for (let y = 0; y < S; y++) for (let i = 0; i < S; i++) {
    const n = fbm(i / S * 6, y / S * 6, 6, 4, 7), f = fbm(i / S * 32, y / S * 32, 32, 2, 12);
    const g = 255 * Math.max(0, Math.min(1, 1 - (0.28 * n + 0.14 * f) + 0.12)); const o = (y * S + i) * 4;
    d[o] = d[o + 1] = d[o + 2] = g; d[o + 3] = 255;
  }
  x.putImageData(img, 0, 0); noiseTile = c; return c;
}
function grime(x, W, H, k = 1, s = 7) {
  const n = getNoise(), ox = (s * 37) % 256, oy = (s * 91) % 256;
  x.save(); x.globalCompositeOperation = 'multiply'; x.globalAlpha = Math.min(1, 0.55 * k);
  for (let py = -oy; py < H; py += 256) for (let px = -ox; px < W; px += 256) x.drawImage(n, px, py);
  x.restore();
}

// sidewalk / courtyard slabs (one texture tile = 2 m x 2 m, 4 x 4 slabs of 50 cm)
export const slabs = (tint = [168, 164, 156]) => memo('slabs' + tint, () => {
  const S = 512, c = canvas(S, S), x = c.getContext('2d'), n = 4, cs = S / n;
  x.fillStyle = '#5c5a56'; x.fillRect(0, 0, S, S);
  for (let i = 0; i < n; i++) for (let j = 0; j < n; j++) {
    const v = 0.86 + rnd() * 0.18; x.fillStyle = `rgb(${tint[0] * v | 0},${tint[1] * v | 0},${tint[2] * v | 0})`;
    x.fillRect(i * cs + 2, j * cs + 2, cs - 4, cs - 4);
    if (rnd() < 0.18) { x.strokeStyle = 'rgba(40,38,34,0.45)'; x.lineWidth = 1.2; x.beginPath(); let px = i * cs + rnd() * cs, py = j * cs + rnd() * cs; x.moveTo(px, py); for (let k = 0; k < 6; k++) { px += (rnd() - 0.5) * 40; py += (rnd() - 0.5) * 40; x.lineTo(px, py); } x.stroke(); }
  }
  grime(x, S, S, 1.0, 13);
  return ctex(c);
});

// asphalt (tile = 4 m)
export const asphalt = (tone = 62) => memo('asph' + tone, () => {
  const S = 512, c = canvas(S, S), x = c.getContext('2d');
  x.fillStyle = `rgb(${tone},${tone},${tone * 1.03 | 0})`; x.fillRect(0, 0, S, S);
  for (let i = 0; i < 9000; i++) { const v = rnd(); x.fillStyle = v > 0.5 ? `rgba(255,255,255,${0.05 + rnd() * 0.08})` : `rgba(0,0,0,${0.06 + rnd() * 0.1})`; x.fillRect(rnd() * S, rnd() * S, 1 + rnd() * 2, 1 + rnd() * 2); }
  grime(x, S, S, 1.2, 3);
  x.strokeStyle = 'rgba(20,20,20,0.5)'; x.lineWidth = 2;
  for (let k = 0; k < 5; k++) { x.beginPath(); let px = rnd() * S, py = rnd() * S; x.moveTo(px, py); for (let s2 = 0; s2 < 14; s2++) { px += (rnd() - 0.5) * 50; py += (rnd() - 0.5) * 50; x.lineTo(px, py); } x.stroke(); }
  return ctex(c);
});

// quay: large granite blocks (tile = 3 m)
export const granite = () => memo('granite', () => {
  const S = 512, c = canvas(S, S), x = c.getContext('2d');
  x.fillStyle = '#4b4945'; x.fillRect(0, 0, S, S);
  const rows = 4, rh = S / rows;
  for (let r = 0; r < rows; r++) {
    let px = -(r % 2) * 60;
    while (px < S) { const w = 110 + rnd() * 90, v = 0.8 + rnd() * 0.25; x.fillStyle = `rgb(${132 * v | 0},${128 * v | 0},${121 * v | 0})`; x.fillRect(px + 3, r * rh + 3, w - 6, rh - 6); px += w; }
  }
  grime(x, S, S, 1.3, 23);
  return ctex(c);
});

// weathered grey planks (pier deck), tile = 2 m, planks along v
export const planks = () => memo('planks', () => {
  const W = 512, H = 512, c = canvas(W, H), x = c.getContext('2d'), P = 8, pw = W / P;
  for (let p = 0; p < P; p++) {
    const t = 0.62 + 0.18 * ((p * 7919) % 13) / 13; x.fillStyle = `rgb(${128 * t | 0},${118 * t | 0},${104 * t | 0})`; x.fillRect(p * pw, 0, pw, H);
    for (let k = 0; k < 40; k++) { x.strokeStyle = `rgba(${rnd() > 0.5 ? '255,255,255' : '0,0,0'},${0.05 + rnd() * 0.07})`; x.lineWidth = 1 + rnd() * 2; const gx = p * pw + rnd() * pw; x.beginPath(); x.moveTo(gx, 0); x.bezierCurveTo(gx + (rnd() - 0.5) * 10, H * 0.3, gx + (rnd() - 0.5) * 10, H * 0.6, gx + (rnd() - 0.5) * 8, H); x.stroke(); }
    x.fillStyle = 'rgba(20,16,12,0.65)'; x.fillRect(p * pw, 0, 2, H); x.fillRect(p * pw + pw - 2, 0, 2, H);
  }
  grime(x, W, H, 0.8, 17);
  for (let k = 0; k < 60; k++) { x.fillStyle = 'rgba(30,26,22,0.8)'; x.fillRect(rnd() * W, (Math.floor(rnd() * 4) + 0.5) * H / 4, 4, 4); } // nail heads
  return ctex(c);
});

// render / plaster, coloured (tile = 3 m)
export const plaster = (rgb) => memo('plaster' + rgb, () => {
  const S = 256, c = canvas(S, S), x = c.getContext('2d');
  x.fillStyle = `rgb(${rgb.join(',')})`; x.fillRect(0, 0, S, S);
  grime(x, S, S, 1.1, 31 + rgb[0]);
  for (let k = 0; k < 14; k++) { const gx = rnd() * S; const g = x.createLinearGradient(0, 0, 0, S); g.addColorStop(0, 'rgba(0,0,0,0)'); g.addColorStop(1, 'rgba(0,0,0,0.12)'); x.fillStyle = g; x.fillRect(gx, rnd() * S * 0.4, 3 + rnd() * 10, S); }
  return ctex(c);
});

// corrugated metal (garage doors, containers, roller shutters): tile = 1 m, ribs along u
export const corrugated = (rgb) => memo('corr' + rgb, () => {
  const W = 256, H = 256, c = canvas(W, H), x = c.getContext('2d');
  for (let i = 0; i < W; i++) { const s = 0.78 + 0.22 * Math.sin(i / W * Math.PI * 2 * 8); x.fillStyle = `rgb(${rgb[0] * s | 0},${rgb[1] * s | 0},${rgb[2] * s | 0})`; x.fillRect(i, 0, 1, H); }
  grime(x, W, H, 1.4, 41 + rgb[1]);
  for (let k = 0; k < 10; k++) { x.fillStyle = `rgba(${90 + rnd() * 40 | 0},${50 + rnd() * 20 | 0},20,${0.15 + rnd() * 0.2})`; x.beginPath(); x.arc(rnd() * W, rnd() * H, 4 + rnd() * 18, 0, 7); x.fill(); } // rust
  return ctex(c);
});

// chain-link fence (alpha), tile = 1 m
export const chainLink = () => memo('chain', () => {
  const S = 128, c = canvas(S, S), x = c.getContext('2d');
  x.clearRect(0, 0, S, S); x.strokeStyle = 'rgba(150,154,156,1)'; x.lineWidth = 3;
  const n = 6, st = S / n;
  for (let k = -n; k <= 2 * n; k++) { x.beginPath(); x.moveTo(k * st, 0); x.lineTo(k * st + S, S); x.stroke(); x.beginPath(); x.moveTo(k * st, S); x.lineTo(k * st + S, 0); x.stroke(); }
  return ctex(c);
});

// floor tiles (shop): 30 cm tiles, tile = 1.2 m
export const floorTiles = (a = '#cfcac0', b = '#bdb7ab') => memo('ft' + a + b, () => {
  const S = 256, c = canvas(S, S), x = c.getContext('2d'), n = 4, cs = S / n;
  x.fillStyle = '#8c877d'; x.fillRect(0, 0, S, S);
  for (let i = 0; i < n; i++) for (let j = 0; j < n; j++) { x.fillStyle = (i + j) % 2 ? a : b; x.fillRect(i * cs + 1.5, j * cs + 1.5, cs - 3, cs - 3); }
  grime(x, S, S, 0.6, 51);
  return ctex(c);
});

// dance floor: dark squares with thin metal joints (not lit, not glowing), tile = 2 m
export const danceFloor = () => memo('dance', () => {
  const S = 512, c = canvas(S, S), x = c.getContext('2d'), n = 4, cs = S / n;
  x.fillStyle = '#5a5d62'; x.fillRect(0, 0, S, S);
  for (let i = 0; i < n; i++) for (let j = 0; j < n; j++) { const v = (i + j) % 2 ? 34 : 52; x.fillStyle = `rgb(${v},${v + 2},${v + 6})`; x.fillRect(i * cs + 3, j * cs + 3, cs - 6, cs - 6); }
  grime(x, S, S, 0.5, 61);
  return ctex(c);
});

// rubber/epoxy floor of the club back rooms and the cellar
export const epoxy = (rgb = [70, 72, 74]) => memo('epoxy' + rgb, () => {
  const S = 256, c = canvas(S, S), x = c.getContext('2d');
  x.fillStyle = `rgb(${rgb.join(',')})`; x.fillRect(0, 0, S, S); grime(x, S, S, 1.6, 71); return ctex(c);
});

// sports court surface: asphalt with painted volleyball lines (texture covers the whole court area w x d metres)
export function courtTex(w, d, court) {
  const PX = 40, W = Math.round(w * PX), H = Math.round(d * PX), c = canvas(W, H), x = c.getContext('2d');
  const a = asphalt(70).image; for (let i = 0; i < W; i += 512) for (let j = 0; j < H; j += 512) x.drawImage(a, i, j);
  // worn red-brown playing area
  const cx = W / 2, cy = H / 2, cw = court.len * PX, ch = court.wid * PX;
  x.fillStyle = 'rgba(128,58,44,0.55)'; x.fillRect(cx - cw / 2 - 2 * PX, cy - ch / 2 - 1.5 * PX, cw + 4 * PX, ch + 3 * PX);
  grime(x, W, H, 0.9, 81);
  x.strokeStyle = 'rgba(232,230,222,0.9)'; x.lineWidth = 0.06 * PX;
  x.strokeRect(cx - cw / 2, cy - ch / 2, cw, ch);
  x.beginPath(); x.moveTo(cx, cy - ch / 2); x.lineTo(cx, cy + ch / 2); x.stroke(); // centre (net) line
  for (const s of [-1, 1]) { x.beginPath(); x.moveTo(cx + s * 3 * PX, cy - ch / 2); x.lineTo(cx + s * 3 * PX, cy + ch / 2); x.stroke(); } // attack lines
  // paint wear
  for (let k = 0; k < 400; k++) { x.fillStyle = 'rgba(60,60,60,0.35)'; x.fillRect(rnd() * W, rnd() * H, 2 + rnd() * 10, 1 + rnd() * 3); }
  return ctex(c, { repeat: false });
}

// painted sign board: dark board, light letters (matte; lit like the wall around it)
export function signBoard(font, text, { fg = '#e9e6dc', bg = '#16181a', w = 1024, h = 256, size = 150, border = null, sub = null } = {}) {
  const c = canvas(w, h), x = c.getContext('2d');
  x.fillStyle = bg; x.fillRect(0, 0, w, h);
  if (border) { x.strokeStyle = border; x.lineWidth = 10; x.strokeRect(12, 12, w - 24, h - 24); }
  x.font = `800 ${size}px ${font}`; x.textAlign = 'center'; x.textBaseline = 'middle';
  const tw = x.measureText(text).width; if (tw > w * 0.86) x.font = `800 ${size * w * 0.86 / tw | 0}px ${font}`;
  x.fillStyle = fg; x.fillText(text, w / 2, sub ? h * 0.42 : h / 2 + 4);
  if (sub) { x.font = `700 ${size * 0.34 | 0}px ${font}`; x.fillStyle = fg; x.globalAlpha = 0.8; x.fillText(sub, w / 2, h * 0.8); x.globalAlpha = 1; }
  return ctex(c, { repeat: false });
}

// product label strip for shelves (tubs, boxes): a row of packages, tile = shelf length
export function shelfGoods(font, kind = 'tubs') {
  return memo('goods' + kind, () => {
    const W = 1024, H = 128, c = canvas(W, H), x = c.getContext('2d');
    x.fillStyle = '#1a1b1c'; x.fillRect(0, 0, W, H);
    let px = 4;
    while (px < W) {
      const w = kind === 'tubs' ? 46 + rnd() * 10 : kind === 'bottles' ? 18 + rnd() * 6 : 60 + rnd() * 30;
      const h = kind === 'tubs' ? 70 + rnd() * 30 : kind === 'bottles' ? 88 + rnd() * 26 : 60 + rnd() * 50;
      const pal = kind === 'bottles' ? ['#d8d2c4', '#3c6fa8', '#c9452e', '#e0b22d', '#2f8a4c', '#e8e8e8'] : ['#0d0e0f', '#0d0e0f', '#e9e9e4', '#8bff3a', '#1c1d1f', '#b8bcc0'];
      const col = pal[Math.floor(rnd() * pal.length)];
      x.fillStyle = col; x.fillRect(px, H - h, w, h);
      if (kind !== 'bottles') { x.fillStyle = col === '#8bff3a' ? '#0d0e0f' : '#8bff3a'; x.fillRect(px, H - h * 0.62, w, h * 0.12); x.fillStyle = 'rgba(255,255,255,0.75)'; x.fillRect(px + w * 0.15, H - h * 0.4, w * 0.7, 4); }
      else { x.fillStyle = 'rgba(255,255,255,0.5)'; x.fillRect(px + 2, H - h * 0.55, w - 4, h * 0.2); x.fillStyle = '#222'; x.fillRect(px + w * 0.3, H - h - 8, w * 0.4, 10); }
      x.fillStyle = 'rgba(0,0,0,0.35)'; x.fillRect(px + w - 3, H - h, 3, h);
      px += w + 3 + rnd() * 6;
    }
    return ctex(c, { repeat: true });
  });
}

// poster on a wall (gig flyer style), random colours from a seed
export function poster(font, title, sub, hue) {
  const W = 256, H = 360, c = canvas(W, H), x = c.getContext('2d');
  const g = x.createLinearGradient(0, 0, 0, H); g.addColorStop(0, `hsl(${hue},55%,32%)`); g.addColorStop(1, `hsl(${(hue + 40) % 360},50%,14%)`);
  x.fillStyle = g; x.fillRect(0, 0, W, H);
  x.fillStyle = 'rgba(255,255,255,0.08)'; for (let i = 0; i < 12; i++) { x.beginPath(); x.arc(W / 2, H * 0.42, 20 + i * 12, 0, 7); x.lineWidth = 3; x.strokeStyle = 'rgba(255,255,255,0.06)'; x.stroke(); }
  x.textAlign = 'center'; x.fillStyle = '#efe9dc'; x.font = `800 54px ${font}`; x.fillText(title, W / 2, H * 0.78);
  x.font = `700 22px ${font}`; x.fillStyle = 'rgba(239,233,220,0.8)'; x.fillText(sub, W / 2, H * 0.88);
  return ctex(c, { repeat: false });
}
