// BRAXHORN CITY -> Unity: export the original runtime gym, not a recreation.
// Usage (with a local static server on :8765 serving the repository root):
//   node dev/tools/export-gym-for-unity.mjs ./unity-city/Source
// Requires Playwright Chromium. Does not edit the source game or production.
import fs from 'node:fs/promises';
import path from 'node:path';
import { chromium } from 'playwright';

const outDir = path.resolve(process.argv[2] || 'unity-city/Source');
const baseURL = process.env.BX_EXPORT_URL || 'http://127.0.0.1:8765/dev/tools/unity-gym-export.html';
await fs.mkdir(outDir, { recursive: true });
const browser = await chromium.launch({
  headless: true,
  args: [
    '--use-gl=angle',
    '--use-angle=swiftshader',
    '--enable-unsafe-swiftshader',
    '--ignore-gpu-blocklist',
    '--disable-dev-shm-usage'
  ]
});
const logs = [];
try {
  const page = await browser.newPage({
    viewport: { width: 1000, height: 640 },
    acceptDownloads: true
  });
  page.on('pageerror', e => logs.push('PAGE ERROR: ' + e.message));
  page.on('console', m => {
    if (m.type() === 'error' || m.type() === 'warning') logs.push(m.type() + ': ' + m.text().slice(0, 500));
  });
  await page.goto(baseURL, { waitUntil: 'domcontentloaded', timeout: 120000 });
  await page.waitForFunction(() => window.gymReady || window.gymExportError, null, { timeout: 180000 });
  const error = await page.evaluate(() => window.gymExportError);
  if (error) throw new Error('Original gym did not load: ' + error);

  await page.screenshot({ path: path.join(outDir, 'gym-web-source.png'), timeout: 120000 });
  const downloadPromise = page.waitForEvent('download', { timeout: 240000 });
  const metadataPromise = page.evaluate(() => window.exportGym());
  const [download, metadata] = await Promise.all([downloadPromise, metadataPromise]);

  const glbPath = path.join(outDir, 'braxhorn-original-gym.glb');
  await download.saveAs(glbPath);
  const stats = await fs.stat(glbPath);
  if (stats.size < 100000 || Math.abs(stats.size - metadata.bytes) > 8)
    throw new Error('Exported GLB size does not match the in-browser result.');

  const manifest = {
    ...metadata,
    exportedUtc: new Date().toISOString(),
    sourceCommit: process.env.GITHUB_SHA || 'local',
    resultFile: path.basename(glbPath),
    verifiedBytes: stats.size
  };
  await fs.writeFile(path.join(outDir, 'gym-manifest.json'), JSON.stringify(manifest, null, 2) + '\n');
  await fs.writeFile(path.join(outDir, 'gym-collisions.json'), JSON.stringify(manifest.collisions, null, 2) + '\n');
  if (manifest.omitted.length > 0) {
    await fs.writeFile(path.join(outDir, 'gym-export-warnings.json'), JSON.stringify(manifest.omitted, null, 2) + '\n');
    console.warn('Excluded meshes:', manifest.omitted.length, '(see gym-export-warnings.json)');
  }
  console.log('BRAXHORN ORIGINAL GYM EXPORTED', JSON.stringify({
    glbPath, meshes: manifest.meshes, triangles: manifest.triangles,
    boxes: manifest.collisions.boxes.length, circles: manifest.collisions.circles.length,
    bytes: stats.size
  }));
} catch (err) {
  console.error('GYM EXPORT FAILED:', err.stack || err);
  throw err;
} finally {
  await fs.writeFile(path.join(outDir, 'export-browser.log'), logs.join('\n') + '\n');
  await browser.close();
}
