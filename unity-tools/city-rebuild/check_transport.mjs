// Exercise the actual packaged iOS streaming code against every compiled part.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import vm from 'node:vm';
import {Readable} from 'node:stream';

const root = path.resolve(process.env.PLAYER_ROOT || 'unity-preview');
const manifest = JSON.parse(fs.readFileSync(path.join(root, 'transport.json'), 'utf8'));
const errors = [];
const origin = 'https://city-preview.invalid/';
const window = {
  fetch: async (input, options = {}) => {
    const url = new URL(typeof input === 'string' ? input : input.url, origin);
    if (url.origin !== new URL(origin).origin) throw Error('Unexpected transport origin');
    const local = path.resolve(root, '.' + url.pathname);
    if (!local.startsWith(root + path.sep)) throw Error('Unexpected transport path');
    const stream = fs.createReadStream(local, {highWaterMark: 1024 * 1024, signal: options.signal});
    return new Response(Readable.toWeb(stream), {headers: {'Content-Length': String(fs.statSync(local).size)}});
  },
  dispatchEvent: event => {errors.push(event.detail);},
};
vm.runInNewContext(fs.readFileSync(path.join(root, 'city-transport.js'), 'utf8'), {
  window, document: {baseURI: origin}, URL, Request, Response, ReadableStream,
  DecompressionStream, AbortController, DOMException, CustomEvent, Error,
});
const response = await window.BraxCityFetch(new URL(manifest.dataPath, origin));
if (Number(response.headers.get('Content-Length')) !== manifest.uncompressedBytes) throw Error('Decoded content length differs');
const hash = crypto.createHash('sha256');
let count = 0;
for await (const chunk of response.body) {count += chunk.byteLength; hash.update(chunk);}
const digest = hash.digest('hex');
const stats = window.BraxCityLoadingStats;
if (count !== manifest.uncompressedBytes || digest !== manifest.uncompressedSha256) throw Error('Packaged native-gzip stream changed compiled data bytes');
if (!stats.complete || stats.maxInputChunk > 65536 || stats.receivedBytes !== manifest.bytes || errors.length) throw Error('Streaming invariant failed: ' + JSON.stringify({stats, errors}));
const report = {
  passed: true, sourceCommit: manifest.sourceCommit, decodedBytes: count, decodedSha256: digest,
  stats, runtime: 'Node native web streams; this is not a browser or physical iPhone performance test',
};
const output = path.resolve(process.env.QA_OUT || 'city-rebuild-review');
fs.mkdirSync(output, {recursive: true});
fs.writeFileSync(path.join(output, 'transport-check.json'), JSON.stringify(report, null, 2) + '\n');
console.log('PASS native iOS streaming code preserves compiled Unity bytes', count, digest);
