// Copies the third-party browser files we need from node_modules into wwwroot/lib.
import { cpSync, mkdirSync } from "node:fs";

const files = [
  ["node_modules/preline/dist/preline.js", "wwwroot/lib/preline/preline.js"],
  ["node_modules/preline/LICENSE", "wwwroot/lib/preline/LICENSE"],
  ["node_modules/pdfjs-dist/build/pdf.min.mjs", "wwwroot/lib/pdfjs/pdf.min.mjs"],
  ["node_modules/pdfjs-dist/build/pdf.worker.min.mjs", "wwwroot/lib/pdfjs/pdf.worker.min.mjs"],
  ["node_modules/pdfjs-dist/LICENSE", "wwwroot/lib/pdfjs/LICENSE"],
  // Urbanist (variable font, latin + latin-ext), served locally: no request to a font CDN
  ["node_modules/@fontsource-variable/urbanist/files/urbanist-latin-wght-normal.woff2", "wwwroot/lib/urbanist/urbanist-latin-wght-normal.woff2"],
  ["node_modules/@fontsource-variable/urbanist/files/urbanist-latin-ext-wght-normal.woff2", "wwwroot/lib/urbanist/urbanist-latin-ext-wght-normal.woff2"],
  ["node_modules/@fontsource-variable/urbanist/LICENSE", "wwwroot/lib/urbanist/LICENSE"],
];

for (const [from, to] of files) {
  mkdirSync(to.substring(0, to.lastIndexOf("/")), { recursive: true });
  cpSync(from, to);
  console.log(`copied ${from} -> ${to}`);
}
