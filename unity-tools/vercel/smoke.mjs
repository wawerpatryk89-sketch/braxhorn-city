import { chromium } from 'playwright';
import fs from 'node:fs/promises';

const base = process.env.PREVIEW_URL || 'http://127.0.0.1:8765';
const report = { url: base, runtimePassed: false, errors: [], failedRequests: [] };
const browser = await chromium.launch({ args: ['--no-sandbox', '--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
try {
  const page = await browser.newPage({ viewport: { width: 1280, height: 720 } });
  page.on('pageerror', error => report.errors.push(error.message));
  page.on('response', response => { if (response.status() >= 400) report.failedRequests.push({url:response.url(),status:response.status()}); });
  const response = await page.goto(base + '/unity-gym/', { waitUntil: 'domcontentloaded' });
  if (!response.ok()) throw new Error('Unity entry returned ' + response.status());
  await page.locator('#start').click();
  await page.waitForFunction(() => Boolean(window.unityInstance), undefined, { timeout: 300000 });
  await page.waitForTimeout(5000);
  report.unity = await page.evaluate(() => ({
    instance: Boolean(window.unityInstance),
    sendMessage: typeof window.unityInstance?.SendMessage,
    canvas: {width:document.querySelector('#game')?.width,height:document.querySelector('#game')?.height},
    resources: performance.getEntriesByType('resource').map(r => r.name).filter(n => n.includes('/Build/'))
  }));
  await page.screenshot({ path: 'unity-webgl-smoke.png' });
  if (report.errors.length || report.failedRequests.length || !report.unity.canvas.width || report.unity.sendMessage !== 'function') {
    throw new Error('Unity runtime or asset requests failed');
  }
  report.runtimePassed = true;
  console.log('UNITY6_BROWSER_SMOKE_PASS', report.unity.resources.length);
} catch (error) {
  report.failure = error.message;
  console.error('UNITY6_BROWSER_SMOKE_FAILED', error.message);
  process.exitCode = 1;
} finally {
  await fs.writeFile('unity-webgl-smoke.json', JSON.stringify(report, null, 2));
  await browser.close();
}
