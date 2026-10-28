// Sound engine: every effect and the music are synthesised once at start-up into sample buffers
// (layered transients, filtered noise, formants, inharmonic partials), then played through a small mixer
// with stereo panning, distance attenuation and a convolution reverb that matches the location.
const SR = 44100;
const TAU = Math.PI * 2;

// ---------------- DSP helpers (offline, on Float32Arrays) ----------------
let seed = 22222;
const rand = () => { seed = (seed * 16807) % 2147483647; return seed / 2147483647; };
const white = () => rand() * 2 - 1;
function biquad(type, f, q, sr = SR) {
  const w = TAU * Math.min(f, sr * 0.45) / sr, c = Math.cos(w), s = Math.sin(w), a = s / (2 * q);
  let b0, b1, b2, a0, a1, a2;
  if (type === 'lp') { b0 = (1 - c) / 2; b1 = 1 - c; b2 = (1 - c) / 2; }
  else if (type === 'hp') { b0 = (1 + c) / 2; b1 = -(1 + c); b2 = (1 + c) / 2; }
  else { b0 = a; b1 = 0; b2 = -a; } // band-pass (0 dB peak)
  a0 = 1 + a; a1 = -2 * c; a2 = 1 - a;
  return { b0: b0 / a0, b1: b1 / a0, b2: b2 / a0, a1: a1 / a0, a2: a2 / a0, x1: 0, x2: 0, y1: 0, y2: 0 };
}
function bq(st, x) { const y = st.b0 * x + st.b1 * st.x1 + st.b2 * st.x2 - st.a1 * st.y1 - st.a2 * st.y2; st.x2 = st.x1; st.x1 = x; st.y2 = st.y1; st.y1 = y; return y; }
// filter whole array with optional cutoff automation fn(t) -> Hz
function filt(buf, type, f, q = 0.707, fAt = null) {
  let st = biquad(type, f, q);
  for (let i = 0; i < buf.length; i++) {
    if (fAt && (i & 31) === 0) { const n = biquad(type, fAt(i / SR), q); n.x1 = st.x1; n.x2 = st.x2; n.y1 = st.y1; n.y2 = st.y2; st = n; }
    buf[i] = bq(st, buf[i]);
  }
  return buf;
}
const env = (t, a, d) => t < a ? t / a : Math.exp(-(t - a) / d);
function mk(sec) { return new Float32Array(Math.ceil(sec * SR)); }
function mixInto(dst, src, gain = 1, off = 0) { const o = Math.round(off * SR); for (let i = 0; i < src.length && i + o < dst.length; i++) dst[i + o] += src[i] * gain; return dst; }
function norm(buf, peak = 0.9) { let m = 0; for (const v of buf) m = Math.max(m, Math.abs(v)); if (m > 0) for (let i = 0; i < buf.length; i++) buf[i] *= peak / m; return buf; }
function sat(buf, k) { const n = Math.tanh(k); for (let i = 0; i < buf.length; i++) buf[i] = Math.tanh(buf[i] * k) / n; return buf; }
function noiseBurst(sec, a, d) { const b = mk(sec); for (let i = 0; i < b.length; i++) b[i] = white() * env(i / SR, a, d); return b; }
function tone(sec, f0, f1, a, d, shape = 'sine', glideT = 0.08) {
  const b = mk(sec); let ph = 0;
  for (let i = 0; i < b.length; i++) {
    const t = i / SR, f = f1 + (f0 - f1) * Math.exp(-t / glideT); ph += TAU * f / SR;
    const s = shape === 'sine' ? Math.sin(ph) : shape === 'saw' ? ((ph / TAU) % 1) * 2 - 1 : shape === 'tri' ? Math.asin(Math.sin(ph)) * 0.6366 : Math.sign(Math.sin(ph)) * 0.7;
    b[i] = s * env(t, a, d);
  }
  return b;
}
function partials(sec, f, ratios, decays, amps) {
  const b = mk(sec);
  ratios.forEach((r, k) => { const ff = f * r, d = decays[k], am = amps[k]; for (let i = 0; i < b.length; i++) { const t = i / SR; b[i] += Math.sin(TAU * ff * t + k) * Math.exp(-t / d) * am * Math.min(1, t * 400); } });
  return b;
}
// a short voiced sound through vowel formants (growls, grunts, roars)
function voiced(sec, f0, { jitter = 0.06, formants = [[700, 6], [1150, 8], [2600, 10]], breath = 0.25, a = 0.05, d = 0.6, vib = 5, drop = 0.85 } = {}) {
  const src = mk(sec); let ph = 0, jit = 0;
  for (let i = 0; i < src.length; i++) {
    const t = i / SR; if ((i & 255) === 0) jit = (rand() - 0.5) * jitter;
    const f = f0 * (1 + jit + 0.02 * Math.sin(TAU * vib * t)) * (1 - (1 - drop) * Math.min(1, t / sec));
    ph += TAU * f / SR;
    src[i] = (((ph / TAU) % 1) * 2 - 1) * 0.6 + white() * breath;
  }
  const out = mk(sec);
  for (const [ff, q] of formants) { const c = Float32Array.from(src); filt(c, 'bp', ff, q); mixInto(out, c, 1); }
  for (let i = 0; i < out.length; i++) out[i] *= env(i / SR, a, d) * (i / SR > sec - 0.08 ? (sec - i / SR) / 0.08 : 1);
  return out;
}

// ---------------- the sound bank ----------------
const BANK = {
  punch: () => [0, 1, 2].map(v => {
    const b = mk(0.4);
    mixInto(b, filt(noiseBurst(0.06, 0.0005, 0.012), 'bp', 2600 + v * 500, 1.2), 0.9);
    mixInto(b, tone(0.25, 170 + v * 15, 62, 0.001, 0.06), 1.1);
    mixInto(b, filt(noiseBurst(0.12, 0.002, 0.03), 'bp', 900 + v * 120, 0.9), 0.7);
    return norm(sat(b, 1.8), 0.95);
  }),
  heavy: () => [0, 1].map(v => {
    const b = mk(0.7);
    mixInto(b, filt(noiseBurst(0.05, 0.0004, 0.01), 'hp', 1800), 0.8);
    mixInto(b, tone(0.5, 120 + v * 10, 40, 0.001, 0.12), 1.4);
    mixInto(b, filt(noiseBurst(0.3, 0.003, 0.07), 'lp', 700), 0.9);
    mixInto(b, filt(noiseBurst(0.08, 0.001, 0.02), 'bp', 1400, 1.5), 0.6, 0.01);
    return norm(sat(b, 2.6), 0.97);
  }),
  block: () => [0, 1].map(v => {
    const b = mk(0.3);
    mixInto(b, filt(noiseBurst(0.15, 0.001, 0.03), 'lp', 900 + v * 200), 1);
    mixInto(b, tone(0.2, 220, 140, 0.001, 0.04), 0.5);
    mixInto(b, filt(noiseBurst(0.02, 0.0002, 0.004), 'hp', 3500), 0.4);
    return norm(b, 0.8);
  }),
  whoosh: () => [0, 1].map(v => {
    const len = v ? 0.42 : 0.26, b = mk(len);
    for (let i = 0; i < b.length; i++) { const t = i / b.length; b[i] = white() * Math.sin(Math.PI * Math.pow(t, 0.8)) ** 2; }
    filt(b, 'bp', 500, 2.2, (t) => (v ? 300 : 500) + 2600 * Math.pow(t / len, 1.3));
    return norm(b, 0.7);
  }),
  fall: () => [0, 1].map(v => {
    const b = mk(0.9);
    mixInto(b, tone(0.6, 85, 38, 0.002, 0.16), 1.3);
    mixInto(b, filt(noiseBurst(0.45, 0.004, 0.09), 'lp', 380), 1.2);
    mixInto(b, filt(noiseBurst(0.25, 0.002, 0.05), 'bp', 2200, 0.8), 0.25);
    mixInto(b, tone(0.3, 70, 40, 0.002, 0.07), 0.45, 0.17 + v * 0.05);
    return norm(sat(b, 1.6), 0.95);
  }),
  bag: () => [0, 1].map(v => {
    const b = mk(0.9);
    mixInto(b, tone(0.4, 110, 55, 0.001, 0.1), 1.2);
    mixInto(b, filt(noiseBurst(0.2, 0.001, 0.05), 'lp', 1200), 0.9);
    mixInto(b, partials(0.8, 2100 + v * 160, [1, 1.37, 2.11, 2.93], [0.12, 0.09, 0.07, 0.05], [0.12, 0.09, 0.07, 0.05]), 1, 0.02);
    return norm(sat(b, 1.5), 0.9);
  }),
  bell: () => [(() => { const b = partials(2.6, 620, [1, 2.76, 5.4, 8.93, 13.3], [1.4, 0.9, 0.5, 0.3, 0.18], [1, 0.5, 0.3, 0.2, 0.1]); mixInto(b, filt(noiseBurst(0.03, 0.0002, 0.006), 'hp', 3000), 0.4); return norm(b, 0.75); })()],
  horn: () => [0, 1].map(v => {
    const len = v ? 0.9 : 0.45, b = mk(len);
    for (const f of [392 + v * 20, 494 + v * 25]) { let ph = 0; for (let i = 0; i < b.length; i++) { ph += TAU * f / SR; const t = i / SR; b[i] += (Math.sign(Math.sin(ph)) * 0.5 + Math.sin(ph * 2) * 0.2) * Math.min(1, t * 60) * Math.min(1, (len - t) * 30); } }
    filt(b, 'lp', 2200); filt(b, 'hp', 250);
    return norm(b, 0.6);
  }),
  crash: () => [(() => {
    const b = mk(1.4);
    mixInto(b, tone(0.6, 90, 35, 0.001, 0.15), 1.4);
    mixInto(b, filt(noiseBurst(0.8, 0.001, 0.18), 'lp', 2500), 1);
    mixInto(b, partials(1.2, 340, [1, 1.53, 2.27, 3.71, 5.2], [0.4, 0.3, 0.22, 0.15, 0.1], [0.4, 0.3, 0.3, 0.2, 0.15]), 0.6);
    for (let k = 0; k < 14; k++) mixInto(b, partials(0.3, 3000 + rand() * 4000, [1, 1.6], [0.04, 0.03], [0.2, 0.1]), 0.5, 0.05 + rand() * 0.5);
    return norm(sat(b, 2), 0.95);
  })()],
  zombie: () => [0, 1, 2].map(v => norm(sat(voiced(1.1 + v * 0.3, 95 + v * 18, { formants: [[600 + v * 80, 5], [1000, 7], [2400, 9]], breath: 0.45, jitter: 0.12, a: 0.12, d: 0.7, vib: 3 + v, drop: 0.75 }), 1.5), 0.8)),
  roar: () => [0, 1].map(v => {
    const b = voiced(2.0 + v * 0.4, 58 + v * 8, { formants: [[380, 4], [820, 5], [1900, 7]], breath: 0.6, jitter: 0.18, a: 0.15, d: 1.4, vib: 6, drop: 0.7 });
    mixInto(b, filt(noiseBurst(2, 0.2, 0.9), 'lp', 300), 0.6);
    return norm(sat(b, 3), 0.95);
  }),
  grunt: () => [0, 1, 2, 3].map(v => norm(voiced(0.28 + v * 0.04, 120 + v * 25, { formants: [[650, 6], [1100, 8], [2500, 10]], breath: 0.35, a: 0.01, d: 0.12, drop: 0.8 }), 0.55)),
  cash: () => [(() => {
    const b = mk(0.7);
    for (let k = 0; k < 6; k++) mixInto(b, filt(noiseBurst(0.06, 0.002, 0.015), 'hp', 2500), 0.35, k * 0.035);
    mixInto(b, partials(0.6, 1760, [1, 2.4, 3.9], [0.25, 0.15, 0.08], [0.3, 0.15, 0.08]), 1, 0.1);
    mixInto(b, partials(0.5, 2349, [1, 2.4], [0.2, 0.1], [0.25, 0.1]), 1, 0.17);
    return norm(b, 0.6);
  })()],
  door: () => [(() => {
    const b = mk(1.3);
    const sq = mk(0.9); for (let i = 0; i < sq.length; i++) sq[i] = white() * Math.sin(Math.PI * i / sq.length);
    filt(sq, 'bp', 900, 12, (t) => 700 + 500 * Math.sin(t * 9) + 300 * t);
    mixInto(b, sq, 0.6, 0.05);
    mixInto(b, tone(0.3, 140, 70, 0.001, 0.06), 0.7, 0.95);
    mixInto(b, filt(noiseBurst(0.05, 0.0005, 0.01), 'hp', 2000), 0.5, 0.95);
    return norm(b, 0.6);
  })()],
  step: () => [0, 1, 2].map(v => norm(filt(noiseBurst(0.09, 0.001, 0.018 + v * 0.004), 'bp', 700 + v * 200, 1.1), 0.35)),
  siren: () => [(() => {
    const len = 4, b = mk(len); let ph = 0;
    for (let i = 0; i < b.length; i++) { const t = i / SR; const f = 900 + 450 * Math.sin(TAU * t / len * 2 - 1.5); ph += TAU * f / SR; b[i] = Math.sin(ph) * 0.6 + Math.sin(ph * 2) * 0.25 + Math.sin(ph * 3) * 0.1; }
    filt(b, 'lp', 3000); return norm(sat(b, 1.4), 0.5);
  })()],
  engine: () => [(() => {
    const len = 2, b = mk(len); let ph = 0, br = 0;
    for (let i = 0; i < b.length; i++) { const t = i / SR; const f = 46 * (1 + 0.04 * Math.sin(TAU * t * 1.5)); ph += TAU * f / SR; br = br * 0.995 + white() * 0.05; b[i] = (((ph / TAU) % 1) * 2 - 1) * 0.5 + br * 2; }
    filt(b, 'lp', 420); // loop seam: crossfade ends
    const n = 2000; for (let i = 0; i < n; i++) { const a = i / n; b[i] = b[i] * a + b[b.length - n + i] * (1 - a); }
    return norm(b.subarray(0, b.length - n).slice(), 0.6);
  })()],
  city: () => [(() => {
    const len = 9, b = mk(len); let br = 0, br2 = 0;
    for (let i = 0; i < b.length; i++) { br = br * 0.998 + white() * 0.04; br2 = br2 * 0.9 + white() * 0.1; b[i] = br * 1.5 + br2 * 0.15 * (0.5 + 0.5 * Math.sin(TAU * i / SR / 4.5)); }
    filt(b, 'lp', 520);
    for (let k = 0; k < 3; k++) { const pass = filt(noiseBurst(2.5, 1.0, 0.6), 'bp', 400 + k * 150, 0.9); mixInto(b, pass, 0.5, 0.5 + k * 2.8); }
    const n = 4000; for (let i = 0; i < n; i++) { const a = i / n; b[i] = b[i] * a + b[b.length - n + i] * (1 - a); }
    return norm(b.subarray(0, b.length - n).slice(), 0.5);
  })()],
  train: () => [(() => {
    const len = 4, b = mk(len); let br = 0, br2 = 0;
    for (let i = 0; i < b.length; i++) { br = br * 0.997 + white() * 0.05; br2 = br2 * 0.95 + white() * 0.08; b[i] = br * 2.2 + br2 * 0.35; }
    filt(b, 'lp', 300);
    for (const t0 of [0.1, 0.32, 1.1, 1.32, 2.1, 2.32, 3.1, 3.32]) { mixInto(b, filt(noiseBurst(0.12, 0.001, 0.025), 'bp', 380, 1.3), 0.9, t0); mixInto(b, tone(0.15, 95, 60, 0.001, 0.04), 0.7, t0); }
    mixInto(b, partials(4, 1720, [1, 1.013], [3, 3], [0.03, 0.02]), 1);
    const n = 3000; for (let i = 0; i < n; i++) { const a = i / n; b[i] = b[i] * a + b[b.length - n + i] * (1 - a); }
    return norm(b.subarray(0, b.length - n).slice(), 0.7);
  })()],
  crowd: () => [(() => {
    const len = 6, b = mk(len);
    for (let v = 0; v < 26; v++) { const s = voiced(0.5 + rand() * 1.2, 110 + rand() * 160, { formants: [[500 + rand() * 500, 4], [1200 + rand() * 600, 6]], breath: 0.6, a: 0.1, d: 0.4 + rand() * 0.4, drop: 0.95 }); mixInto(b, s, 0.15, rand() * (len - 1.8)); }
    let br = 0; for (let i = 0; i < b.length; i++) { br = br * 0.99 + white() * 0.05; b[i] += br * 0.8; }
    filt(b, 'lp', 2600);
    for (let k = 0; k < 60; k++) mixInto(b, filt(noiseBurst(0.04, 0.0005, 0.008), 'bp', 1400 + rand() * 900, 1.2), 0.08, rand() * (len - 0.1));
    const n = 6000; for (let i = 0; i < n; i++) { const a = i / n; b[i] = b[i] * a + b[b.length - n + i] * (1 - a); }
    return norm(b.subarray(0, b.length - n).slice(), 0.55);
  })()],
};

// ---------------- music: two loops (street groove, boss fight) ----------------
function drumKick(b, t, g = 1) { const k = tone(0.45, 140, 45, 0.001, 0.13); mixInto(b, sat(k, 1.4), g, t); }
function drumSnare(b, t, g = 1) { const s = mk(0.3); mixInto(s, filt(noiseBurst(0.25, 0.001, 0.06), 'bp', 2100, 0.7), 1); mixInto(s, tone(0.15, 220, 180, 0.001, 0.04), 0.5); mixInto(b, s, 0.55 * g, t); }
function drumHat(b, t, g = 1, open = false) { mixInto(b, filt(noiseBurst(open ? 0.25 : 0.06, 0.0005, open ? 0.06 : 0.012), 'hp', 7000), 0.22 * g, t); }
function bassNote(b, t, f, dur, g = 1) { const s = mk(dur + 0.05); let ph = 0; for (let i = 0; i < s.length; i++) { ph += TAU * f / SR; const tt = i / SR; s[i] = (Math.sin(ph) * 0.8 + (((ph / TAU) % 1) * 2 - 1) * 0.35) * Math.min(1, tt * 200) * Math.min(1, (dur + 0.05 - tt) * 40); } filt(s, 'lp', 260); mixInto(b, s, 0.5 * g, t); }
function pad(b, t, freqs, dur, g = 1) { const s = mk(dur); for (const f of freqs) for (const det of [-0.006, 0.006]) { let ph = rand() * TAU; for (let i = 0; i < s.length; i++) { ph += TAU * f * (1 + det) / SR; const tt = i / SR; s[i] += (((ph / TAU) % 1) * 2 - 1) * Math.min(1, tt / 0.6) * Math.min(1, (dur - tt) / 0.8); } } filt(s, 'lp', 1100); mixInto(b, s, 0.05 * g, t); }
function musicLoop(kind) {
  const bpm = kind === 'boss' ? 142 : 88, beat = 60 / bpm, bars = 8, len = bars * 4 * beat;
  const b = mk(len + 1.2);
  const root = kind === 'boss' ? 41.2 : 43.65; // E1 / F1
  const prog = kind === 'boss' ? [0, 0, 3, 1] : [0, 3, 5, 1];
  const scale = [1, 1.122, 1.189, 1.335, 1.498, 1.587, 1.782];
  for (let bar = 0; bar < bars; bar++) {
    const t0 = bar * 4 * beat, deg = prog[bar % 4], f = root * scale[deg] * (deg >= 5 ? 0.5 : 1);
    if (kind === 'boss') {
      for (let s = 0; s < 16; s++) { const t = t0 + s * beat / 4; if (s % 4 === 0 || s === 10) drumKick(b, t, 1); if (s === 4 || s === 12) drumSnare(b, t, 1.1); drumHat(b, t + (s % 2 ? 0.012 : 0), s % 2 ? 0.6 : 1, s === 14); }
      for (let s = 0; s < 8; s++) bassNote(b, t0 + s * beat / 2, f * (s % 4 === 3 ? 1.498 : 1), beat / 2 * 0.9, 1);
      pad(b, t0, [f * 4, f * 4 * 1.189, f * 4 * 1.498], 4 * beat, 0.8);
    } else {
      for (let s = 0; s < 16; s++) { const t = t0 + s * beat / 4 + (s % 2 ? beat * 0.06 : 0); if (s === 0 || s === 7 || s === 10) drumKick(b, t, 1); if (s === 4 || s === 12) drumSnare(b, t, 0.9); if (s % 2 === 0 || rand() > 0.6) drumHat(b, t, s % 4 === 2 ? 1 : 0.55); }
      bassNote(b, t0, f, beat * 1.5, 1.1); bassNote(b, t0 + beat * 1.75, f, beat * 0.5, 0.8); bassNote(b, t0 + beat * 2.5, f * (bar % 2 ? 1.335 : 1.189), beat * 1.2, 0.9);
      pad(b, t0, [f * 4, f * 4 * 1.189, f * 4 * 1.498, f * 8 * 1.122], 4 * beat, 1);
    }
  }
  // fold the tail into the start so the loop is seamless
  const L = Math.round(len * SR); for (let i = L; i < b.length; i++) b[i - L] += b[i];
  return norm(b.subarray(0, L).slice(), 0.75);
}

function impulse(sec, decay, bright = 1) {
  const n = Math.round(sec * SR), L = new Float32Array(n), R = new Float32Array(n);
  for (let i = 0; i < n; i++) { const e = Math.exp(-i / SR / decay); L[i] = white() * e; R[i] = white() * e; }
  filt(L, 'lp', 2500 * bright); filt(R, 'lp', 2500 * bright);
  return [L, R];
}

export class Sound {
  constructor(on = true) {
    this.on = on; this.ctx = null; this.raw = {}; this.buffers = {}; this.ready = false; this.music = null; this.musicVol = 0.32; this.loops = {};
    this.listener = { x: 0, z: 0, yaw: 0 };
  }
  // generate all buffers (call during loading; yields to keep the page responsive)
  async prepare(onStep) {
    for (const [k, fn] of Object.entries(BANK)) { this.raw[k] = fn(); onStep && onStep(k); await new Promise(r => setTimeout(r, 0)); }
    this.raw.musicStreet = [musicLoop('street')]; await new Promise(r => setTimeout(r, 0));
    this.raw.musicBoss = [musicLoop('boss')];
    this.irs = { gym: impulse(1.4, 0.45, 1), street: impulse(1.0, 0.22, 1.2), crypt: impulse(3.2, 1.1, 0.8) };
    this.prepared = true;
    if (this.ctx) this.makeBuffers();
  }
  // iPhone: Web Audio is muted by the silent switch unless the page also plays a media element / uses the
  // 'playback' audio session. Start a silent looping <audio> inside the first tap.
  iosUnmute() {
    if (this.unmuted) return; this.unmuted = true;
    try { if (navigator.audioSession) navigator.audioSession.type = 'playback'; } catch { }
    try {
      const n = 4410, buf = new ArrayBuffer(44 + n * 2), v = new DataView(buf);
      const w = (o, s) => { for (let i = 0; i < s.length; i++) v.setUint8(o + i, s.charCodeAt(i)); };
      w(0, 'RIFF'); v.setUint32(4, 36 + n * 2, true); w(8, 'WAVE'); w(12, 'fmt '); v.setUint32(16, 16, true); v.setUint16(20, 1, true); v.setUint16(22, 1, true);
      v.setUint32(24, 44100, true); v.setUint32(28, 88200, true); v.setUint16(32, 2, true); v.setUint16(34, 16, true); w(36, 'data'); v.setUint32(40, n * 2, true);
      const el = document.createElement('audio'); el.setAttribute('playsinline', ''); el.loop = true; el.src = URL.createObjectURL(new Blob([buf], { type: 'audio/wav' }));
      el.volume = 0.01; const p = el.play(); if (p && p.catch) p.catch(() => { this.unmuted = false; }); this.silentEl = el;
    } catch { }
  }
  unlock() {
    if (!this.on) return;
    this.iosUnmute();
    if (this.ctx) { if (this.ctx.state !== 'running') this.ctx.resume(); return; }
    try {
      const AC = window.AudioContext || window.webkitAudioContext;
      this.ctx = new AC();
      const c = this.ctx;
      this.master = c.createGain(); this.master.gain.value = 0.9;
      const comp = c.createDynamicsCompressor(); comp.threshold.value = -14; comp.ratio.value = 4; comp.attack.value = 0.003; comp.release.value = 0.2;
      this.master.connect(comp); comp.connect(c.destination);
      this.fx = c.createGain(); this.fx.connect(this.master);
      this.verb = c.createConvolver(); this.verbGain = c.createGain(); this.verbGain.gain.value = 0.35;
      this.verb.connect(this.verbGain); this.verbGain.connect(this.master);
      this.musicBus = c.createGain(); this.musicBus.gain.value = this.musicVol; this.musicBus.connect(this.master);
      this.ambBus = c.createGain(); this.ambBus.gain.value = 0.5; this.ambBus.connect(this.master);
      if (this.prepared) this.makeBuffers();
      // iOS: play a silent buffer inside the gesture
      const s = c.createBufferSource(); s.buffer = c.createBuffer(1, 1, 22050); s.connect(c.destination); s.start(0);
    } catch { this.ctx = null; }
  }
  makeBuffers() {
    const c = this.ctx;
    for (const [k, arr] of Object.entries(this.raw)) this.buffers[k] = arr.map(d => { const b = c.createBuffer(1, d.length, SR); b.copyToChannel ? b.copyToChannel(d, 0) : b.getChannelData(0).set(d); return b; });
    this.irBuffers = {};
    for (const [k, [L, R]] of Object.entries(this.irs)) { const b = c.createBuffer(2, L.length, SR); b.getChannelData(0).set(L); b.getChannelData(1).set(R); this.irBuffers[k] = b; }
    this.setRoom(this.room || 'gym');
    this.ready = true;
    if (this.wantMusic) this.playMusic(this.wantMusic);
    for (const [k, v] of Object.entries(this.wantLoops || {})) this.loop(k, v);
  }
  toggle() { this.on = !this.on; if (this.on) this.unlock(); if (this.master) this.master.gain.value = this.on ? 0.9 : 0; }
  ok() { return this.on && this.ready && this.ctx && this.ctx.state === 'running'; }
  setRoom(r) { this.room = r; if (this.verb && this.irBuffers) { this.verb.buffer = this.irBuffers[r]; this.verbGain.gain.value = r === 'crypt' ? 0.5 : r === 'street' ? 0.22 : 0.32; } }
  setListener(x, z, yaw) { this.listener.x = x; this.listener.z = z; this.listener.yaw = yaw; }
  // play a one-shot; pos (optional) = {x,z} world position for distance + pan
  play(name, { gain = 1, rate = 1, pos = null, wet = 0.6, variant = -1 } = {}) {
    if (!this.ok()) return;
    const arr = this.buffers[name]; if (!arr || !arr.length) return;
    const c = this.ctx, src = c.createBufferSource();
    src.buffer = arr[variant >= 0 ? variant % arr.length : Math.floor(Math.random() * arr.length)];
    src.playbackRate.value = rate * (0.94 + Math.random() * 0.12);
    const g = c.createGain(); let gg = gain, pan = 0;
    if (pos) {
      const dx = pos.x - this.listener.x, dz = pos.z - this.listener.z, d = Math.hypot(dx, dz);
      gg *= 1 / (1 + Math.max(0, d - 3) * 0.12);
      const cy = Math.cos(this.listener.yaw), sy = Math.sin(this.listener.yaw);
      pan = Math.max(-1, Math.min(1, (dx * cy - dz * sy) / (d + 2)));
    }
    g.gain.value = gg;
    src.connect(g);
    if (c.createStereoPanner && pan) { const p = c.createStereoPanner(); p.pan.value = pan * 0.8; g.connect(p); p.connect(this.fx); p.connect(this.verbSend()); }
    else { g.connect(this.fx); g.connect(this.verbSend(wet)); }
    src.start();
    return src;
  }
  verbSend(w = 0.6) { const g = this.ctx.createGain(); g.gain.value = w; g.connect(this.verb); return g; }
  // continuous loops (ambience, engines, sirens): set volume each frame, 0 stops
  loop(name, vol, rate = 1) {
    if (!this.ready) { (this.wantLoops ||= {})[name] = vol; return; }
    let L = this.loops[name];
    if (!L && vol > 0.001) {
      const c = this.ctx, src = c.createBufferSource(); src.buffer = this.buffers[name][0]; src.loop = true;
      const g = c.createGain(); g.gain.value = 0; src.connect(g); g.connect(name === 'city' || name === 'crowd' ? this.ambBus : this.fx); src.start();
      L = this.loops[name] = { src, g };
    }
    if (!L) return;
    L.g.gain.setTargetAtTime(Math.max(0, vol), this.ctx.currentTime, 0.15);
    L.src.playbackRate.setTargetAtTime(rate, this.ctx.currentTime, 0.1);
  }
  playMusic(kind) {
    this.wantMusic = kind;
    if (!this.ready) return;
    const key = kind === 'boss' ? 'musicBoss' : 'musicStreet';
    if (this.music && this.music.key === key) return;
    const c = this.ctx, now = c.currentTime;
    if (this.music) { const old = this.music; old.g.gain.setTargetAtTime(0, now, 0.6); setTimeout(() => { try { old.src.stop(); } catch { } }, 3000); }
    if (!kind) { this.music = null; return; }
    const src = c.createBufferSource(); src.buffer = this.buffers[key][0]; src.loop = true;
    const g = c.createGain(); g.gain.value = 0; g.gain.setTargetAtTime(1, now, 0.8); src.connect(g); g.connect(this.musicBus); src.start();
    this.music = { src, g, key };
  }
  // convenience wrappers used by the game
  punch(k = 1, pos) { this.play(k > 0.8 ? 'heavy' : 'punch', { gain: 0.7 + 0.5 * Math.min(1, k), pos }); }
  bag(k = 1, pos) { this.play('bag', { gain: 0.6 + 0.4 * k, pos }); }
  block(pos) { this.play('block', { gain: 0.8, pos }); }
  thump(k = 1, pos) { this.play('fall', { gain: 0.5 + 0.6 * k, pos }); }
  whoosh(heavy, pos) { this.play('whoosh', { gain: heavy ? 0.55 : 0.4, variant: heavy ? 1 : 0, pos, wet: 0.2 }); }
  bell(n = 3) { if (!this.ok()) return; for (let i = 0; i < n; i++) setTimeout(() => this.play('bell', { gain: 0.6, wet: 0.8 }), i * 330); }
}
