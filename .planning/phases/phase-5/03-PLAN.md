---
phase: phase-5
plan: 03
type: execute
wave: 3
depends_on:
  - phase-5/02-PLAN.md
files_modified:
  - .github/workflows/ci.yml
autonomous: true
requirements:
  - C1
  - C7
  - C8

must_haves:
  truths:
    - "tauri-build CI job exists in ci.yml with needs: build-and-test"
    - "CI job installs libwebkit2gtk-4.1-dev libappindicator3-dev librsvg2-dev patchelf"
    - "CI job runs npm ci then npm run tauri build -- --bundles appimage,deb"
    - "CI artifact is uploaded as helix-desktop-linux with retention-days: 7"
    - "All existing CI jobs are preserved unchanged"
  artifacts:
    - ".github/workflows/ci.yml"

key_links:
  - "tauri-build job -> needs: build-and-test (parallel with code-quality)"
  - "CI actions -> checkout@v4, setup-node@v4, cache@v4, upload-artifact@v4, dtolnay/rust-toolchain@stable"
---

# Phase 5 — Plan 03: CI Build Step

**Goal:** Add a `tauri-build` job to `.github/workflows/ci.yml` that builds the Tauri Linux bundle (AppImage + deb) on ubuntu-latest. The job runs in parallel with the existing `code-quality` job, both depending on `build-and-test`.

---

## Threat Model

| Threat | Likelihood | Mitigation |
|--------|-----------|------------|
| `libwebkit2gtk-4.1-dev` package name differs on ubuntu-latest runner | Low | Package names confirmed from Tauri CI docs in RESEARCH.md |
| `npm run tauri build` fails because `src-tauri/` has no Rust compilation errors on CI but local build has been validated | Low | `cargo check` in Plan 01 catches Rust errors pre-commit |
| Existing CI job structure edited incorrectly (YAML indentation) | Medium | Append new job block after code-quality; do not touch existing job blocks |
| action version drift | Low | Strict pin required: `@v4` only, match existing jobs exactly |

---

## Deviation Protocol

If `npm run tauri build -- --bundles appimage,deb` fails because `libappindicator3-dev` is unavailable on the runner, fall back to `--bundles appimage` only and document in SUMMARY.md.

If `dtolnay/rust-toolchain@stable` is unavailable, use `actions/cache@v4` with the Rust toolchain manual install script.

---

## Tasks

### T01 — Add `tauri-build` CI Job

**Type:** auto  
**Files:**
- `.github/workflows/ci.yml` (MODIFY — append new job)

**Actions:**

Read current `.github/workflows/ci.yml`. Locate the `code-quality` job. Insert the following `tauri-build` job block after the `code-quality` job (preserve all existing whitespace and structure):

```yaml
  tauri-build:
    name: Tauri Linux Build
    runs-on: ubuntu-latest
    needs: build-and-test

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: npm
          cache-dependency-path: helix-desktop/package-lock.json

      - name: Install system dependencies (Tauri Linux)
        run: |
          sudo apt-get update
          sudo apt-get install -y \
            libwebkit2gtk-4.1-dev \
            libappindicator3-dev \
            librsvg2-dev \
            patchelf

      - name: Install Rust stable
        uses: dtolnay/rust-toolchain@stable

      - name: Cache Rust build artifacts
        uses: actions/cache@v4
        with:
          path: |
            ~/.cargo/bin/
            ~/.cargo/registry/index/
            ~/.cargo/registry/cache/
            ~/.cargo/git/db/
            helix-desktop/src-tauri/target/
          key: ${{ runner.os }}-cargo-${{ hashFiles('helix-desktop/src-tauri/Cargo.lock') }}
          restore-keys: |
            ${{ runner.os }}-cargo-

      - name: Install frontend dependencies
        working-directory: helix-desktop
        run: npm ci

      - name: Build Tauri (Linux)
        working-directory: helix-desktop
        run: npm run tauri build -- --bundles appimage,deb

      - name: Upload Tauri bundle
        uses: actions/upload-artifact@v4
        with:
          name: helix-desktop-linux
          path: helix-desktop/src-tauri/target/release/bundle/
          retention-days: 7
```

**Verify:**
```bash
# Validate YAML is well-formed
python3 -c "import yaml, sys; yaml.safe_load(open('.github/workflows/ci.yml'))" && echo "YAML valid"

# Verify tauri-build job exists
grep -A2 "tauri-build:" .github/workflows/ci.yml

# Verify existing jobs untouched
grep "build-and-test:" .github/workflows/ci.yml
grep "code-quality:" .github/workflows/ci.yml

# Verify action versions
grep "actions/checkout" .github/workflows/ci.yml | grep -v "@v4" && exit 1 || echo "checkout@v4 — OK"
grep "setup-node" .github/workflows/ci.yml | grep -v "@v4" && exit 1 || echo "setup-node@v4 — OK"
```

**Acceptance criteria:**
- `tauri-build` job exists with `needs: build-and-test`
- All action versions are `@v4` (no `@v3` or unversioned)
- `dtolnay/rust-toolchain@stable` is present
- `libwebkit2gtk-4.1-dev`, `libappindicator3-dev`, `librsvg2-dev`, `patchelf` in apt-get install
- `retention-days: 7` matches existing artifacts
- YAML parses without error
- Existing `build-and-test` and `code-quality` jobs are unchanged

---

## Self-Check

- [ ] `.github/workflows/ci.yml` has `tauri-build` job
- [ ] `tauri-build` needs `build-and-test` (parallel with code-quality)
- [ ] All action versions exactly `@v4`
- [ ] `dtolnay/rust-toolchain@stable` used (not `@v1` or other)
- [ ] Artifact upload uses `retention-days: 7`
- [ ] YAML is valid (python3 yaml.safe_load or yq passes)
- [ ] Existing jobs not modified
