import { readFileSync, writeFileSync } from "fs";
const version = process.argv[2];
if (!version) { console.error("Usage: node helix-desktop/scripts/stamp-cargo-version.js <version>"); process.exit(1); }
const path = "helix-desktop/src-tauri/Cargo.toml";
const content = readFileSync(path, "utf8");
const updated = content.replace(/^version = ".*"/m, `version = "${version}"`);
writeFileSync(path, updated);
console.log("Stamped Cargo.toml: " + version);
