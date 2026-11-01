// Dekodowanie .glb z wersji przeglądarkowej (gltfpack: EXT_meshopt_compression + tekstury KTX2/Basis) do zwykłego .glb,
// który glTFast w Unity czyta bez dodatkowych pakietów (meshopt / KTX nie są w manifeście — zmiana = pełny reimport).
// Geometria: dekoder meshoptimizer (MIT, vendor/meshopt_decoder.module.js). Tekstury: transkoder Basis z repo
// (public/assets/basis, Apache-2.0) → RGBA → PNG (zlib Node). Wynik: jeden bufor, obrazy PNG w buforze; potem
// odchudzanie_modeli.py wyjmuje je na pliki, a Unity kompresuje (ETC2 / limit 1K).
// Użycie: node dekoduj_glb.mjs <plik.glb | katalog> <katalog_wyjściowy>
import fs from 'fs';
import path from 'path';
import zlib from 'zlib';
import { createRequire } from 'module';
import { fileURLToPath } from 'url';
import { MeshoptDecoder } from './vendor/meshopt_decoder.module.js';

const here = path.dirname(fileURLToPath(import.meta.url));
const require = createRequire(import.meta.url);
const repo = path.resolve(here, '..', '..');
const BASIS = require(path.join(repo, 'public/assets/basis/basis_transcoder.js'));

function crc32(buf) {
  let c, crc = 0xffffffff;
  for (let n = 0; n < buf.length; n++) { c = (crc ^ buf[n]) & 0xff; for (let k = 0; k < 8; k++) c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1; crc = (crc >>> 8) ^ c; }
  return (crc ^ 0xffffffff) >>> 0;
}
function png(rgba, w, h, alpha) {
  const ch = alpha ? 4 : 3, stride = w * ch, raw = Buffer.alloc((stride + 1) * h);
  for (let y = 0; y < h; y++) {
    raw[y * (stride + 1)] = 0;
    for (let x = 0; x < w; x++) for (let c = 0; c < ch; c++) raw[y * (stride + 1) + 1 + x * ch + c] = rgba[(y * w + x) * 4 + c];
  }
  const chunk = (type, data) => { const len = Buffer.alloc(4); len.writeUInt32BE(data.length); const td = Buffer.concat([Buffer.from(type), data]); const crc = Buffer.alloc(4); crc.writeUInt32BE(crc32(td)); return Buffer.concat([len, td, crc]); };
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = alpha ? 6 : 2; ihdr[10] = 0; ihdr[11] = 0; ihdr[12] = 0;
  return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', ihdr), chunk('IDAT', zlib.deflateSync(raw, { level: 6 })), chunk('IEND', Buffer.alloc(0))]);
}

function readGlb(file) {
  const d = fs.readFileSync(file);
  if (d.readUInt32LE(0) !== 0x46546C67) throw new Error('to nie .glb');
  let off = 12, json = null, bin = null;
  while (off < d.length) {
    const len = d.readUInt32LE(off), type = d.readUInt32LE(off + 4), body = d.subarray(off + 8, off + 8 + len);
    if (type === 0x4E4F534A) json = JSON.parse(body.toString('utf8')); else if (type === 0x004E4942) bin = body;
    off += 8 + len;
  }
  return { json, bin };
}
function writeGlb(file, json, bin) {
  let js = Buffer.from(JSON.stringify(json), 'utf8'); js = Buffer.concat([js, Buffer.alloc((4 - js.length % 4) % 4, 0x20)]);
  const b = Buffer.concat([bin, Buffer.alloc((4 - bin.length % 4) % 4)]);
  const head = Buffer.alloc(12); head.writeUInt32LE(0x46546C67, 0); head.writeUInt32LE(2, 4); head.writeUInt32LE(12 + 8 + js.length + 8 + b.length, 8);
  const h1 = Buffer.alloc(8); h1.writeUInt32LE(js.length, 0); h1.writeUInt32LE(0x4E4F534A, 4);
  const h2 = Buffer.alloc(8); h2.writeUInt32LE(b.length, 0); h2.writeUInt32LE(0x004E4942, 4);
  fs.writeFileSync(file, Buffer.concat([head, h1, js, h2, b]));
}

async function decode(file, outFile, basis) {
  const { json, bin } = readGlb(file);
  const parts = []; let size = 0;
  const add = (buf) => { const pad = (4 - size % 4) % 4; if (pad) { parts.push(Buffer.alloc(pad)); size += pad; } const at = size; parts.push(Buffer.from(buf)); size += buf.length; return at; };
  // 1) bufory: dekompresja meshopt, reszta skopiowana; jeden nowy bufor
  for (const bv of json.bufferViews || []) {
    const ext = bv.extensions && bv.extensions.EXT_meshopt_compression;
    let data;
    if (ext) {
      const src = bin.subarray(ext.byteOffset || 0, (ext.byteOffset || 0) + ext.byteLength);
      data = new Uint8Array(ext.count * ext.byteStride);
      MeshoptDecoder.decodeGltfBuffer(data, ext.count, ext.byteStride, new Uint8Array(src), ext.mode, ext.filter || 'NONE');
      delete bv.extensions.EXT_meshopt_compression; if (!Object.keys(bv.extensions).length) delete bv.extensions;
    } else {
      if ((bv.buffer || 0) !== 0) throw new Error('bufor zewnętrzny ' + bv.buffer);
      data = bin.subarray(bv.byteOffset || 0, (bv.byteOffset || 0) + bv.byteLength);
    }
    bv.byteOffset = add(data); bv.byteLength = data.length; bv.buffer = 0;
  }
  // 2) tekstury KTX2 → PNG w tym samym indeksie obrazu
  let n = 0;
  for (const tex of json.textures || []) {
    const ext = tex.extensions && tex.extensions.KHR_texture_basisu;
    if (!ext) continue;
    const img = json.images[ext.source];
    if (img.mimeType === 'image/ktx2' || img.mimeType === undefined) {
      const bv = json.bufferViews[img.bufferView];
      const bytes = Buffer.concat(parts).subarray(bv.byteOffset, bv.byteOffset + bv.byteLength);
      const k = new basis.KTX2File(new Uint8Array(bytes));
      if (!k.isValid()) throw new Error('zły KTX2');
      const w = k.getWidth(), h = k.getHeight(), alpha = !!k.getHasAlpha();
      if (!k.startTranscoding()) throw new Error('KTX2 start');
      const fmt = 13;   // cTFRGBA32
      const out = new Uint8Array(k.getImageTranscodedSizeInBytes(0, 0, 0, fmt));
      if (!k.transcodeImage(out, 0, 0, 0, fmt, 0, -1, -1)) throw new Error('KTX2 transkodowanie');
      k.close(); k.delete();
      const p = png(out, w, h, alpha);
      json.bufferViews.push({ buffer: 0, byteOffset: add(p), byteLength: p.length });
      img.bufferView = json.bufferViews.length - 1; img.mimeType = 'image/png'; n++;
    }
    tex.source = ext.source; delete tex.extensions.KHR_texture_basisu; if (!Object.keys(tex.extensions).length) delete tex.extensions;
  }
  json.buffers = [{ byteLength: size }];
  for (const key of ['extensionsUsed', 'extensionsRequired'])
    if (json[key]) { json[key] = json[key].filter(e => e !== 'EXT_meshopt_compression' && e !== 'KHR_texture_basisu'); if (!json[key].length) delete json[key]; }
  writeGlb(outFile, json, Buffer.concat(parts));
  return n;
}

const [inp, outDir] = process.argv.slice(2);
if (!inp || !outDir) { console.log('użycie: node dekoduj_glb.mjs <plik|katalog> <katalog_wyjściowy>'); process.exit(1); }
await MeshoptDecoder.ready;
const basis = await BASIS({ wasmBinary: fs.readFileSync(path.join(repo, 'public/assets/basis/basis_transcoder.wasm')) });
basis.initializeBasis();
fs.mkdirSync(outDir, { recursive: true });
const files = fs.statSync(inp).isDirectory() ? fs.readdirSync(inp).filter(f => f.endsWith('.glb')).map(f => path.join(inp, f)) : [inp];
let ok = 0;
for (const f of files) {
  try { const n = await decode(f, path.join(outDir, path.basename(f)), basis); ok++; console.log('zdekodowany', path.basename(f), '(tekstur PNG: ' + n + ')'); }
  catch (e) { console.log('DO POPRAWKI: ' + path.basename(f) + ': ' + e.message); }
}
console.log('dekodowanie glb: ' + ok + '/' + files.length);
