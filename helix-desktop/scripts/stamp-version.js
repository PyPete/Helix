import { readFileSync, writeFileSync } from "fs";
const version = process.argv[2];
if (!version) { console.error("Usage: node helix-desktop/scripts/stamp-version.js <version>"); process.exit(1); }
const confPath = "helix-desktop/src-tauri/tauri.conf.json";
const conf = JSON.parse(readFileSync(confPath, "utf8"));
conf.version = version;
writeFileSync(confPath, JSON.stringify(conf, null, 2) + "\n");
console.log("Stamped tauri.conf.json: " + version);
