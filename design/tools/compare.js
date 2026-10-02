// Scores the WinForms gallery against the mockup references.
//
//   node design/tools/compare.js --ref design/reference/png --actual out/actual --out out/report [--min 90]
//
// For every reference scene it writes a diff image and a side-by-side report.html, plus
// summary.md (also appended to $GITHUB_STEP_SUMMARY in CI). The score is the share of pixels
// that match within pixelmatch's perceptual threshold, after padding both images to the same
// size — so a scene that is the wrong height loses points for the missing rows.
//
// Scores are a guide, not a contract: anti-aliasing and ClearType differ between Chrome and
// GDI, so ~94–98% is what a faithful port looks like. Look at the diff images; red shows
// where pixels differ. A size mismatch is always worth fixing first.
'use strict';
const fs = require('fs');
const path = require('path');
const { PNG } = require('pngjs');
const pixelmatch = require('pixelmatch');

function arg(name, fallback) {
  const i = process.argv.indexOf('--' + name);
  return i > 0 ? process.argv[i + 1] : fallback;
}

const ROOT = path.join(__dirname, '..', '..');
const REF = path.resolve(arg('ref', path.join(ROOT, 'design', 'reference', 'png')));
const ACTUAL = path.resolve(arg('actual', path.join(ROOT, 'out', 'actual')));
const OUT = path.resolve(arg('out', path.join(ROOT, 'out', 'report')));
const MIN = Number(arg('min', '0'));

function read(file) { return PNG.sync.read(fs.readFileSync(file)); }

/** Copies img onto a w x h canvas filled with magenta, so missing area always counts as wrong. */
function pad(img, w, h) {
  const out = new PNG({ width: w, height: h });
  for (let i = 0; i < out.data.length; i += 4) { out.data[i] = 255; out.data[i + 1] = 0; out.data[i + 2] = 255; out.data[i + 3] = 255; }
  PNG.bitblt(img, out, 0, 0, img.width, img.height, 0, 0);
  return out;
}

function main() {
  fs.mkdirSync(path.join(OUT, 'img'), { recursive: true });
  const refs = fs.readdirSync(REF).filter((f) => f.endsWith('.png')).sort();
  const rows = [];

  for (const file of refs) {
    const id = file.replace(/\.png$/, '');
    const refPath = path.join(REF, file);
    const actPath = path.join(ACTUAL, file);
    fs.copyFileSync(refPath, path.join(OUT, 'img', id + '.ref.png'));

    if (!fs.existsSync(actPath)) {
      const err = path.join(ACTUAL, id + '.error.txt');
      rows.push({ id, score: 0, note: fs.existsSync(err) ? 'render failed: ' + fs.readFileSync(err, 'utf8').split('\n')[0] : 'no WinForms render' });
      continue;
    }
    fs.copyFileSync(actPath, path.join(OUT, 'img', id + '.actual.png'));

    const ref = read(refPath), act = read(actPath);
    const w = Math.max(ref.width, act.width), h = Math.max(ref.height, act.height);
    const a = pad(ref, w, h), b = pad(act, w, h);
    const diff = new PNG({ width: w, height: h });
    const bad = pixelmatch(a.data, b.data, diff.data, w, h, { threshold: 0.12, includeAA: false, alpha: 0.25 });
    fs.writeFileSync(path.join(OUT, 'img', id + '.diff.png'), PNG.sync.write(diff));

    const score = 100 * (1 - bad / (w * h));
    const sizeNote = (ref.width !== act.width || ref.height !== act.height)
      ? `size ${act.width}x${act.height}, expected ${ref.width}x${ref.height}` : '';
    rows.push({ id, score, note: sizeNote });
  }

  // C#-only scenes (no reference): listed so they still get looked at.
  const extras = fs.existsSync(ACTUAL)
    ? fs.readdirSync(ACTUAL).filter((f) => f.endsWith('.png') && !refs.includes(f)).sort() : [];
  for (const f of extras) fs.copyFileSync(path.join(ACTUAL, f), path.join(OUT, 'img', f.replace(/\.png$/, '.actual.png')));

  const scored = rows.filter((r) => r.score > 0);
  const mean = scored.length ? scored.reduce((s, r) => s + r.score, 0) / scored.length : 0;

  const md = ['## Design fidelity: WinForms vs mockup', '',
    `Mean match **${mean.toFixed(1)}%** across ${rows.length} scenes${MIN ? ` (minimum ${MIN}%)` : ''}.`, '',
    '| Scene | Match | Notes |', '|---|---:|---|',
    ...rows.map((r) => `| ${r.id} | ${r.score ? r.score.toFixed(1) + '%' : '—'} | ${r.note || ''} |`),
    ...(extras.length ? ['', '**App screens** (no mockup reference, review by eye in report.html): ' + extras.map((f) => f.replace(/\.png$/, '').replace(/^app-/, '')).join(', ')] : [])
  ].join('\n');
  fs.writeFileSync(path.join(OUT, 'summary.md'), md + '\n');
  if (process.env.GITHUB_STEP_SUMMARY) fs.appendFileSync(process.env.GITHUB_STEP_SUMMARY, md + '\n');

  const cell = (src) => `<td><img src="img/${src}" onerror="this.replaceWith('—')"></td>`;
  const html = `<!doctype html><meta charset="utf-8"><title>ScanLink fidelity report</title>
<style>body{font:13px/1.5 "Segoe UI",system-ui,sans-serif;margin:24px;color:#1D2939;background:#F7F8FA}
table{border-collapse:collapse;background:#fff}td,th{border:1px solid #E4E7EC;padding:8px;vertical-align:top;text-align:left}
th{background:#F7F8FA;font-weight:600}img{display:block;max-width:560px;image-rendering:pixelated}h2{margin-top:32px}
.score{font:600 15px "Segoe UI",sans-serif}.low{color:#B42318}.ok{color:#0C6944}</style>
<h1>WinForms vs mockup</h1>
<p>Mean match <b>${mean.toFixed(1)}%</b> across ${rows.length} component scenes.</p>
${extras.length ? `<h2>App screens</h2><p>Real ScanLink screens rendered by the WinForms app. The mockup has no
pixel reference for these, so review them by eye.</p>
<table><tr><th>Screen</th><th>WinForms</th></tr>
${extras.map((f) => { const id = f.replace(/\.png$/, ''); return `<tr><td><b>${id.replace(/^app-/, '')}</b></td>${cell(id + '.actual.png').replace('max-width:560px', '')}</tr>`; }).join('\n')}
</table>` : ''}
<h2>Component scenes</h2><p>Left: mockup reference. Middle: WinForms. Right: differing pixels in red.</p>
<table><tr><th>Scene</th><th>Mockup</th><th>WinForms</th><th>Diff</th></tr>
${rows.map((r) => `<tr><td><b>${r.id}</b><div class="score ${r.score >= 90 ? 'ok' : 'low'}">${r.score ? r.score.toFixed(1) + '%' : '—'}</div><div>${r.note || ''}</div></td>
${cell(r.id + '.ref.png')}${cell(r.id + '.actual.png')}${cell(r.id + '.diff.png')}</tr>`).join('\n')}
</table>`;
  fs.writeFileSync(path.join(OUT, 'report.html'), html);

  console.log(md);
  if (MIN && mean < MIN) { console.error(`mean ${mean.toFixed(1)}% is below --min ${MIN}%`); process.exit(1); }
}

main();
