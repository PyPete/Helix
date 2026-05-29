---
phase: phase-5
plan: "03"
subsystem: ci
tags: [ci, github-actions, tauri, linux-build]
dependency_graph:
  requires:
    - phase-5/02-PLAN.md
  provides:
    - CI: tauri-build job in .github/workflows/ci.yml
  affects:
    - .github/workflows/ci.yml
tech_stack:
  added:
    - dtolnay/rust-toolchain@stable (GitHub Actions Rust toolchain)
    - actions/setup-node@v4 with npm cache
    - Tauri CLI build via npm run tauri build -- --bundles appimage,deb
  patterns:
    - Ubuntu apt-get system dep install for Tauri WebKit2GTK
    - Cargo artifact caching keyed by Cargo.lock hash
key_files:
  modified:
    - .github/workflows/ci.yml
decisions:
  - Added tauri-build in parallel with code-quality (both depend on build-and-test)
  - Inserted tauri-build between code-quality and publish to preserve existing job order visually
  - Used dtolnay/rust-toolchain@stable as specified (not @v1 or actions/setup-rust)
  - Cache path includes both ~/.cargo/** and helix-desktop/src-tauri/target/ for maximum reuse
metrics:
  duration: "~5 minutes"
  completed: "2026-05-28"
  tasks_completed: 1
  tasks_total: 1
  files_modified: 1
---

# Phase 5 Plan 03: CI Build Step Summary

**One-liner:** Tauri Linux CI job building AppImage + deb on ubuntu-latest with WebKit2GTK system deps and Rust artifact caching.

## What Was Built

Added a `tauri-build` job to `.github/workflows/ci.yml`. The job:

- Runs on `ubuntu-latest` in parallel with `code-quality` (both need `build-and-test`)
- Installs system dependencies required by Tauri on Linux: `libwebkit2gtk-4.1-dev`, `libappindicator3-dev`, `librsvg2-dev`, `patchelf`
- Installs Rust stable via `dtolnay/rust-toolchain@stable`
- Caches Cargo registry and `helix-desktop/src-tauri/target/` keyed by `Cargo.lock` hash
- Installs npm dependencies (`npm ci`) in `helix-desktop/`
- Builds Linux bundles: `npm run tauri build -- --bundles appimage,deb`
- Uploads artifact `helix-desktop-linux` from `helix-desktop/src-tauri/target/release/bundle/` with `retention-days: 7`

## Tasks Completed

| Task | Description | Commit | Files |
|------|-------------|--------|-------|
| T01 | Add tauri-build CI job | 41e5729 | .github/workflows/ci.yml |

## Deviations from Plan

None — plan executed exactly as written. The YAML block from the plan was inserted verbatim after the `code-quality` job.

## Verification Results

- YAML valid: `python3 yaml.safe_load` passed without error
- `tauri-build` job exists with `needs: build-and-test`
- All action versions confirmed `@v4` (no `@v3` or unversioned)
- `dtolnay/rust-toolchain@stable` present
- All four system deps present: `libwebkit2gtk-4.1-dev`, `libappindicator3-dev`, `librsvg2-dev`, `patchelf`
- `retention-days: 7` confirmed on artifact upload step
- Existing jobs `build-and-test`, `code-quality`, `publish` are unchanged (56 lines added, 0 modified in existing content)

## Known Stubs

None — this plan only modifies CI configuration. No frontend/backend code stubs introduced.

## Threat Flags

None — CI YAML addition introduces no new network endpoints, auth paths, or schema changes.

## Self-Check: PASSED

- [x] `.github/workflows/ci.yml` has `tauri-build` job (line 74)
- [x] `tauri-build` needs `build-and-test` (parallel with code-quality)
- [x] All action versions exactly `@v4`
- [x] `dtolnay/rust-toolchain@stable` used
- [x] Artifact upload uses `retention-days: 7`
- [x] YAML is valid (python3 yaml.safe_load passed)
- [x] Existing jobs not modified (git diff confirms 56 insertions, 0 deletions in existing lines)
- [x] Commit 41e5729 exists in git log
