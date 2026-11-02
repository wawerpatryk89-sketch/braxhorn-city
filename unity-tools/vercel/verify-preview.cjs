'use strict';
const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');

// This command is also run by Vercel. Never allow accidental production builds.
if (process.env.VERCEL_ENV !== 'preview') {
  throw new Error('Only VERCEL_ENV=preview is authorized. Production publication is blocked.');
}
const root = path.resolve(__dirname, 'unity-webgl');
const proof = JSON.parse(fs.readFileSync(path.join(root, 'deployment-proof.json'), 'utf8'));
if (proof.engine !== 'Unity 6' || proof.target !== 'preview' ||
    !/^[a-f0-9]{40}$/.test(proof.sourceCommit) || !Object.keys(proof.files || {}).length) {
  throw new Error('Missing verified Unity 6 artifact provenance');
}
async function verify() {
  for (const [name, expected] of Object.entries(proof.files)) {
    const file = path.resolve(root, name);
    if (!file.startsWith(root + path.sep) || fs.lstatSync(file).isSymbolicLink()) {
      throw new Error('Unsafe artifact path');
    }
    const hash = crypto.createHash('sha256');
    for await (const block of fs.createReadStream(file)) hash.update(block);
    if (hash.digest('hex') !== expected) throw new Error('Artifact checksum mismatch: ' + name);
  }
  console.log('UNITY6_VERCEL_PREVIEW_VERIFIED', proof.sourceCommit, Object.keys(proof.files).length);
}
verify().catch(error => { console.error(error.message); process.exitCode = 1; });
