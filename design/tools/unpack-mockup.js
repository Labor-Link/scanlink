// Unpacks the UI team's standalone mockup (a self-extracting bundle) into readable files.
//
//   node design/tools/unpack-mockup.js
//
// The mockup ships every asset gzip+base64 encoded inside one HTML line, which is why coding
// agents could only see the page shell. This script writes:
//   design/mockup/source/components/**  one readable file per design-system component (committed)
//   design/mockup/screens/*.js          one file per screen (committed)
//   design/mockup/vendor/*              React, ReactDOM, Babel, Lucide + the DS bundle (gitignored;
//                                       needed by the reference gallery and the icon generator)
// Run it again whenever the UI team sends a new mockup, then review the diff.
'use strict';
const fs = require('fs');
const path = require('path');
const zlib = require('zlib');

const MOCKUP = path.join(__dirname, '..', 'mockup', 'ScanLink-Client-Standalone.html');
const VENDOR = path.join(__dirname, '..', 'mockup', 'vendor');

function scriptBlock(html, type) {
  const m = html.match(new RegExp('<script type="__bundler/' + type + '">([\\s\\S]*?)</script>'));
  return m ? m[1] : null;
}

function main() {
  const html = fs.readFileSync(MOCKUP, 'utf8');
  const manifest = JSON.parse(scriptBlock(html, 'manifest'));
  fs.mkdirSync(VENDOR, { recursive: true });

  // Identify vendor libraries by their licence banner rather than by uuid, so a re-export of
  // the mockup with new uuids still unpacks.
  const wanted = [
    { file: 'react.js', banner: 'react.development.js' },
    { file: 'react-dom.js', banner: 'react-dom.development.js' },
    { file: 'lucide.js', banner: '@license lucide' },
    { file: 'babel.js', banner: null } // the only multi-megabyte script without a banner match
  ];

  const scripts = [];
  for (const uuid of Object.keys(manifest)) {
    const entry = manifest[uuid];
    if (!/javascript/.test(entry.mime)) continue;
    let bytes = Buffer.from(entry.data, 'base64');
    if (entry.compressed) bytes = zlib.gunzipSync(bytes);
    scripts.push({ uuid, text: bytes.toString('utf8') });
  }

  for (const w of wanted) {
    let hit = w.banner
      ? scripts.find((s) => s.text.slice(0, 400).includes(w.banner))
      : scripts.filter((s) => s.text.length > 2000000).sort((a, b) => b.text.length - a.text.length)[0];
    if (!hit) throw new Error('could not find ' + w.file + ' in the mockup bundle');
    fs.writeFileSync(path.join(VENDOR, w.file), hit.text);
    console.log('vendor/' + w.file, hit.text.length, 'bytes');
  }

  // The design-system bundle itself (all components in one file) is what the reference
  // gallery loads.
  const ds = scripts.find((s) => s.text.startsWith('/* @ds-bundle'));
  if (!ds) throw new Error('could not find the design-system bundle');
  fs.writeFileSync(path.join(VENDOR, 'design-system.js'), ds.text);
  console.log('vendor/design-system.js', ds.text.length, 'bytes');

  // One file per component, split on the bundle's "// components/<path>.jsx" markers.
  const SOURCE = path.join(__dirname, '..', 'mockup', 'source');
  const lines = ds.text.split('\n');
  const marks = [];
  lines.forEach((l, i) => { const m = l.match(/^\/\/ ([a-zA-Z_./]+)\.(jsx|js)$/); if (m) marks.push({ i, file: m[1] + '.js' }); });
  marks.forEach((m, n) => {
    const end = n + 1 < marks.length ? marks[n + 1].i : lines.length;
    const out = path.join(SOURCE, m.file);
    fs.mkdirSync(path.dirname(out), { recursive: true });
    fs.writeFileSync(out, lines.slice(m.i + 1, end).join('\n').trim() + '\n');
  });
  console.log('source/: ' + marks.length + ' components');

  // Screens: the babel scripts that publish a component on window ("Object.assign(window, { X })").
  const SCREENS = path.join(__dirname, '..', 'mockup', 'screens');
  fs.mkdirSync(SCREENS, { recursive: true });
  let screens = 0;
  for (const s of scripts) {
    const m = s.text.match(/Object\.assign\(window,\s*\{\s*([A-Za-z]+)\s*\}\)/);
    if (!m || s.text.startsWith('/* @ds-bundle')) continue;
    fs.writeFileSync(path.join(SCREENS, m[1] + '.js'), s.text);
    screens++;
  }
  console.log('screens/: ' + screens + ' files');
}

main();
