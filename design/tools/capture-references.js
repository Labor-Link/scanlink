// Screenshots every scene of design/reference/gallery.html into design/reference/png/<id>.png.
//
//   node design/tools/capture-references.js [--out <dir>]
//
// Browser: $CHROME_PATH if set, otherwise the installed Google Chrome (channel "chrome").
// Run it on Windows for pixel-comparable references: there Chrome renders Segoe UI, the same
// font the WinForms client uses. On macOS the text falls back to San Francisco, so macOS
// references are good for eyeballing layout but not for pixel scores.
'use strict';
const path = require('path');
const fs = require('fs');
const { chromium } = require('playwright-core');

const ROOT = path.join(__dirname, '..');
const GALLERY = path.join(ROOT, 'reference', 'gallery.html');
const outArg = process.argv.indexOf('--out');
const OUT = outArg > 0 ? path.resolve(process.argv[outArg + 1]) : path.join(ROOT, 'reference', 'png');

async function launch() {
  if (process.env.CHROME_PATH) return chromium.launch({ executablePath: process.env.CHROME_PATH });
  return chromium.launch({ channel: 'chrome' });
}

(async () => {
  if (!fs.existsSync(path.join(ROOT, 'mockup', 'vendor', 'react.js'))) {
    console.error('design/mockup/vendor is missing — run: node design/tools/unpack-mockup.js');
    process.exit(1);
  }
  fs.mkdirSync(OUT, { recursive: true });
  const browser = await launch();
  const page = await browser.newPage({ viewport: { width: 1400, height: 1000 }, deviceScaleFactor: 1 });
  const url = 'file:///' + GALLERY.replace(/\\/g, '/');

  await page.goto(url);
  await page.waitForFunction(() => window.__READY__ === true, null, { timeout: 30000 });
  const scenes = await page.evaluate(() => window.__SCENES__);

  for (const scene of scenes) {
    await page.goto(url + '?scene=' + encodeURIComponent(scene.id));
    await page.waitForFunction(() => window.__READY__ === true, null, { timeout: 30000 });
    await page.mouse.move(0, 0); // no hover states in references
    await page.evaluate(() => document.activeElement && document.activeElement.blur());
    const selector = scene.dialog
      ? `[data-scene="${scene.id}"] [role="dialog"]`
      : `[data-scene="${scene.id}"]`;
    const el = await page.$(selector);
    if (!el) { console.warn('missing scene element', scene.id); continue; }
    // CSS line-heights produce fractional heights (e.g. 249.4px); an element screenshot then
    // includes a half-covered row of the grey page behind. Round the scene up to whole pixels.
    await el.evaluate((node) => {
      const h = node.getBoundingClientRect().height;
      if (h !== Math.round(h)) node.style.height = Math.ceil(h) + 'px';
    });
    // Clip on whole pixels: a centred dialog can sit on a half pixel, and an element
    // screenshot would then include a row of whatever is behind it.
    const box = await el.boundingBox();
    await page.screenshot({
      path: path.join(OUT, scene.id + '.png'),
      clip: { x: Math.round(box.x), y: Math.round(box.y), width: Math.round(box.width), height: Math.round(box.height) }
    });
    console.log('captured', scene.id);
  }
  await browser.close();
})().catch((e) => { console.error(e); process.exit(1); });
