/*
 * Renders the copied Lucide SVGs to PNG for the WinForms client.
 * WinForms has no SVG renderer and Lucide strokes with `currentColor`, so each icon is
 * baked once per tint it is used in.
 *   npm i sharp && node tools/render-icons.js
 */
const fs = require("fs");
const path = require("path");
const sharp = require("sharp");

const SRC = process.env.ICON_SRC || path.join("ScanLink", "Assets", "icons");
const OUT = path.join(SRC, "png");

const SIZES = [16, 20, 24];              // buttons / sidebar / headings
const TINTS = {
  dark:  "#1D2939",  // Theme.TextBody          - on white
  light: "#C4C9D1",  // Theme.TextOnShell       - sidebar, inactive
  white: "#FFFFFF"   // Theme.TextOnShellStrong - sidebar, active
};
const STROKE = "1.75";                   // design system fixes this; Lucide ships 2

async function main() {
  if (!fs.existsSync(SRC)) { console.error(`Missing ${SRC}`); process.exit(1); }
  fs.mkdirSync(OUT, { recursive: true });

  const files = fs.readdirSync(SRC).filter(f => f.endsWith(".svg"));
  if (!files.length) { console.error(`No .svg files in ${SRC}`); process.exit(1); }

  let count = 0;
  for (const file of files) {
    const name = path.basename(file, ".svg");
    const raw = fs.readFileSync(path.join(SRC, file), "utf8");
    for (const [tint, hex] of Object.entries(TINTS)) {
      for (const size of SIZES) {
        const svg = raw
          .replace(/currentColor/g, hex)
          .replace(/stroke-width="[^"]*"/g, `stroke-width="${STROKE}"`)
          .replace(/width="[^"]*"/, `width="${size}"`)
          .replace(/height="[^"]*"/, `height="${size}"`);
        await sharp(Buffer.from(svg)).png().toFile(path.join(OUT, `${name}_${size}_${tint}.png`));
        count++;
      }
    }
  }
  console.log(`${count} PNGs written to ${OUT}`);
}
main().catch(e => { console.error(e); process.exit(1); });
