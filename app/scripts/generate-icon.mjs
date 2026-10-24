// Generates the monochrome lightning-bolt icons:
//  - source.png      1024×1024 app icon (bolt on dark rounded square)
//  - tray-white.png  64×64 white bolt, transparent bg (dark taskbars/menubars)
//  - tray-black.png  64×64 black bolt, transparent bg (light theme / macOS template)
// Run: node scripts/generate-icon.mjs
import { writeFileSync, mkdirSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { Resvg } from "@resvg/resvg-js";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const outDir = join(root, "src-tauri", "icons");
mkdirSync(outDir, { recursive: true });

const BOLT_PATH_1024 =
  "M576.4 79.6 L259 576.4 H424.6 L415.4 944.4 L765 429.2 H553.4 Z";
const BOLT_PATH_64 = "M36.2 3.8 L15.5 36.2 H26.3 L25.7 60.2 L48.5 26.6 H34.7 Z";

const appIconSvg = `<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="1024" viewBox="0 0 1024 1024">
  <defs>
    <linearGradient id="bg" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#1b1e26"/>
      <stop offset="1" stop-color="#0d0e12"/>
    </linearGradient>
  </defs>
  <rect width="1024" height="1024" rx="190" fill="url(#bg)"/>
  <rect x="14" y="14" width="996" height="996" rx="178" fill="none" stroke="rgba(255,255,255,0.10)" stroke-width="8"/>
  <path d="${BOLT_PATH_1024}" fill="#f2f4f8"/>
</svg>`;

const traySvg = (color) => `<svg xmlns="http://www.w3.org/2000/svg" width="64" height="64" viewBox="0 0 64 64">
  <path d="${BOLT_PATH_64}" fill="${color}"/>
</svg>`;

function render(svg, out, width) {
  const resvg = new Resvg(svg, { fitTo: { mode: "width", value: width } });
  writeFileSync(join(outDir, out), resvg.render().asPng());
  console.log("wrote", out);
}

render(appIconSvg, "source.png", 1024);
render(traySvg("#ffffff"), "tray-white.png", 64);
render(traySvg("#000000"), "tray-black.png", 64);
