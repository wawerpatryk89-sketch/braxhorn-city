'use strict';
// Transport parts preserve the exact original model bytes. Run before deployment.
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const root = path.resolve(__dirname, '../..');
const manifest = JSON.parse(fs.readFileSync(path.join(root, 'dev/deploy/asset-parts.json'), 'utf8'));
function inside(relative) {
  const resolved = path.resolve(root, relative);
  if (!resolved.startsWith(root + path.sep)) throw new Error('Invalid asset path: ' + relative);
  return resolved;
}
function gitHash(data) {
  return crypto.createHash('sha1').update('blob ' + data.length + '\0').update(data).digest('hex');
}
for (const asset of manifest.assets) {
  const dest = inside(asset.path);
  if (fs.existsSync(dest)) {
    const existing = fs.readFileSync(dest);
    if (existing.length === asset.size && gitHash(existing) === asset.sha) continue;
  }
  const chunks = asset.parts.map(part => {
    const data = fs.readFileSync(inside(part.path));
    if (data.length !== part.size || gitHash(data) !== part.sha) throw new Error('Corrupt asset part: ' + part.path);
    return data;
  });
  const data = Buffer.concat(chunks);
  if (data.length !== asset.size || gitHash(data) !== asset.sha) throw new Error('Corrupt assembled asset: ' + asset.path);
  fs.mkdirSync(path.dirname(dest), { recursive: true });
  fs.writeFileSync(dest + '.assembling', data);
  fs.renameSync(dest + '.assembling', dest);
  console.log('Assembled ' + asset.path + ' (' + data.length + ' bytes)');
}
console.log('Verified ' + manifest.assets.length + ' split assets.');
