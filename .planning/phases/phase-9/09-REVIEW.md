---
phase: phase-9
reviewed: 2026-06-02T00:00:00Z
depth: standard
files_reviewed: 4
files_reviewed_list:
  - .github/workflows/deploy-docs.yml
  - Helix.Orchestration.SiteServer/Helix.Orchestration.SiteServer.csproj
  - docs-site/docusaurus.config.ts
  - docs-site/sidebars.ts
findings:
  critical: 1
  warning: 3
  info: 3
  total: 7
status: issues_found
---

# Phase 9: Code Review Report

**Reviewed:** 2026-06-02T00:00:00Z
**Depth:** standard
**Files Reviewed:** 4
**Status:** issues_found

## Summary

Four files were reviewed: the GitHub Actions deploy workflow, the SiteServer C# project file, the Docusaurus site config, and the sidebar configuration. All sidebars resolve to actual files on disk. The Docusaurus config is structurally sound and `markdown.hooks.onBrokenMarkdownLinks` is the correct placement in Docusaurus 3.10.x with `future.v4`. The primary concern is a silent misdeployment path in the workflow when `workflow_dispatch` is triggered from a branch other than `main`, `stage`, or `dev`. Two other warnings relate to unpinned third-party CI actions (supply-chain risk) and a malformatted line in the csproj.

---

## Critical Issues

### CR-01: `workflow_dispatch` from any non-listed branch silently deploys to `gh-pages` root

**File:** `.github/workflows/deploy-docs.yml:36-50`

**Issue:** The `case` block maps only `main`, `stage`, and `dev` to `base_url`/`dest_dir` outputs. There is no `*` default case. When `workflow_dispatch` is triggered from any other branch (e.g., `feature/foo`), both outputs are empty strings. `peaceiris/actions-gh-pages` treats an empty `destination_dir` as the repository root — identical to `dest_dir: "."` (the `main` production path). The build also receives `BASE_URL=` (empty), which falls back to `/Helix/` via the `??` operator in `docusaurus.config.ts`, so the built artifact appears valid. The net result is a silent, unintended overwrite of the production `gh-pages` root from an arbitrary branch.

**Fix:** Add a default case that either fails fast or emits a safe no-op:
```yaml
          case "${{ github.ref_name }}" in
            main)
              echo "base_url=/Helix/"         >> "$GITHUB_OUTPUT"
              echo "dest_dir=."               >> "$GITHUB_OUTPUT"
              ;;
            stage)
              echo "base_url=/Helix/stage/"   >> "$GITHUB_OUTPUT"
              echo "dest_dir=stage"           >> "$GITHUB_OUTPUT"
              ;;
            dev)
              echo "base_url=/Helix/dev/"     >> "$GITHUB_OUTPUT"
              echo "dest_dir=dev"             >> "$GITHUB_OUTPUT"
              ;;
            *)
              echo "ERROR: workflow_dispatch not supported on branch '${{ github.ref_name }}'" >&2
              exit 1
              ;;
          esac
```
Alternatively, restrict `workflow_dispatch` to only the three supported branches by adding an `inputs` selector or a workflow-level branch filter.

---

## Warnings

### WR-01: Third-party CI action pinned to mutable tag, not commit SHA

**File:** `.github/workflows/deploy-docs.yml:63`

**Issue:** `peaceiris/actions-gh-pages@v4` is pinned to a mutable tag. If the tag is moved to a malicious commit (tag mutation, account compromise), the workflow will execute the attacker's code with `contents: write` permission against the repository. `actions/checkout@v4` and `actions/setup-node@v4` carry the same risk but are first-party Microsoft-owned actions with a lower (but non-zero) risk profile; `peaceiris/actions-gh-pages` is a third-party action with a higher risk profile.

**Fix:** Pin to the commit SHA corresponding to the intended release:
```yaml
        uses: peaceiris/actions-gh-pages@4f9cc6213b7feb61b029de6091f03afb45a6027b  # v4.0.0
```
Retrieve the correct SHA via `gh api repos/peaceiris/actions-gh-pages/git/ref/tags/v4`.

---

### WR-02: Inconsistent (missing) indentation on `MQTTnet` PackageReference

**File:** `Helix.Orchestration.SiteServer/Helix.Orchestration.SiteServer.csproj:11`

**Issue:** The `MQTTnet` `PackageReference` element has no leading whitespace, while every other element in both `<ItemGroup>` blocks uses a tab. The file also mixes CRLF line endings throughout. While MSBuild is whitespace-tolerant and the project will compile correctly, the malformed indentation indicates the line was pasted without formatting and suggests it may have been added carelessly — raising the question of whether the version constraint was intentionally reviewed.

**Fix:**
```xml
	<PackageReference Include="MQTTnet" Version="4.*" />
```
(Leading tab, matching the surrounding elements.)

---

### WR-03: Wildcard version constraints across all NuGet packages allow silent breaking upgrades

**File:** `Helix.Orchestration.SiteServer/Helix.Orchestration.SiteServer.csproj:10-14`

**Issue:** Every package uses a floating major-pinned wildcard (`7.*`, `4.*`, `6.*`, `2.*`, `3.*`). NuGet resolves these to the latest patch within the major at restore time. For stable packages this is low-risk, but `MQTTnet 4.*` is notable: MQTTnet v4 introduced breaking API changes relative to v3, and a future `4.x` minor/patch could introduce behavioural regressions that are difficult to trace because the lock file is not checked in. All other `.csproj` files in the solution use the same pattern (confirmed in `Helix.Orchestration.Infrastructure.csproj`), so this is a project-wide convention — but it still means CI builds on different days may resolve to different binaries.

**Fix:** Establish a `Directory.Packages.props` (Central Package Management) file with exact versions, or at minimum check in a `packages.lock.json` per project (`<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>`). If floating wildcards are a deliberate project convention, document the decision in `CLAUDE.md`.

---

## Info

### IN-01: Default Docusaurus social-card placeholder image not replaced

**File:** `docs-site/docusaurus.config.ts:64`

**Issue:** `image: 'img/docusaurus-social-card.jpg'` references the stock Docusaurus social-card image (`static/img/docusaurus-social-card.jpg`). This image is served as the Open Graph / Twitter card preview for the entire site. It currently shows the generic Docusaurus brand rather than anything Helix-specific.

**Fix:** Replace `static/img/docusaurus-social-card.jpg` with a Helix-branded image and ensure `docusaurus.config.ts` references it.

---

### IN-02: `editUrl` in docs preset points to `main` branch regardless of active branch

**File:** `docs-site/docusaurus.config.ts:53`

**Issue:** `editUrl: 'https://github.com/PyPete/Helix/tree/main/docs-site/'` hardcodes `main`. When the `dev` or `stage` build is deployed, "Edit this page" links on every doc page point to the `main` branch, not the branch whose content is actually being viewed. This is a UX confusion rather than a correctness bug.

**Fix:** Use a dynamic value or accept the limitation. To make it branch-aware, pass the branch name as an environment variable during build (similar to `BASE_URL`) and use it here:
```ts
editUrl: `https://github.com/PyPete/Helix/tree/${process.env.GIT_BRANCH ?? 'main'}/docs-site/`,
```

---

### IN-03: `onBrokenLinks: 'throw'` / `onBrokenMarkdownLinks: 'warn'` severity asymmetry undocumented

**File:** `docs-site/docusaurus.config.ts:31, 41`

**Issue:** Hard HTML/route links fail the build (`throw`) while broken markdown relative links only warn (`warn`). This asymmetry is valid and intentional — broken markdown links are common during active doc authoring — but it is not explained by a comment, making the intent unclear to future contributors who may interpret `'warn'` as an oversight and change it to `'throw'`, which would break the build on every incomplete PR.

**Fix:** Add a brief inline comment:
```ts
onBrokenLinks: 'throw',  // hard links must resolve; fail the build

// markdown.hooks.onBrokenMarkdownLinks intentionally set to 'warn' (not 'throw')
// so that in-progress doc branches can have forward-references without blocking CI
```

---

_Reviewed: 2026-06-02T00:00:00Z_
_Reviewer: Claude (gsd-code-reviewer)_
_Depth: standard_
