#!/usr/bin/env node
'use strict';
const { execSync } = require('child_process');
const path = require('path');
const fs = require('fs');

const repoRoot = path.resolve(__dirname, '../..');
const siteServerBin = path.join(
  repoRoot,
  'Helix.Orchestration.SiteServer',
  'bin',
  'Release',
  'net10.0',
  'Helix.Orchestration.SiteServer.dll'
);
const outputPath = path.resolve(__dirname, '../static/openapi.json');

if (!fs.existsSync(siteServerBin)) {
  console.warn('WARNING: SiteServer binary not found at ' + siteServerBin);
  console.warn('Skipping OpenAPI generation. Build Helix.Orchestration.SiteServer in Release first.');
  process.exit(0);
}

try {
  console.log('Generating OpenAPI spec...');
  execSync(
    `dotnet swagger tofile --output "${outputPath}" "${siteServerBin}" v1`,
    {
      stdio: 'inherit',
      cwd: repoRoot
    }
  );
  console.log('OpenAPI spec written to docs-site/static/openapi.json');
} catch (err) {
  console.warn('WARNING: OpenAPI generation failed:', err.message);
  console.warn('Continuing with placeholder openapi.json');
  process.exit(0);
}
