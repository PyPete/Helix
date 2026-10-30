import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const src  = path.join(__dirname, "..", "node_modules", "scichart", "_wasm");
const dest = path.join(__dirname, "..", "public");
fs.mkdirSync(dest, { recursive: true });
["scichart2d.wasm", "scichart2d-nosimd.wasm", "scichart2d.js"].forEach((f) => {
  fs.copyFileSync(path.join(src, f), path.join(dest, f));
  console.log("Copied", f);
});
