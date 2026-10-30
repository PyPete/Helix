# CI/CD Pipeline and Branch Protection

This repository uses GitHub Actions workflow `.github/workflows/ci.yml`.

## Branch Model

- `dev` — active development integration branch
- `stage` — pre-production validation branch
- `main` — production-ready branch

## Workflow Triggers

The CI workflow runs on:

- `push` to `dev`, `stage`, `main`
- `pull_request` targeting `dev`, `stage`, `main`

## Jobs

### 1) `build-and-test`
- Restores, builds, and tests `Helix.Orchestration/Helix.Orchestration.slnx`
- Uses .NET `10.0.x`
- Builds with `/warnaserror`
- Produces TRX test results artifact (`test-results`, retention 7 days)

### 2) `code-quality`
- Runs after `build-and-test`
- Executes `dotnet format --verify-no-changes`

### 3) `publish`
- Runs after `build-and-test`
- Runs only for `push` on `main`
- Publishes `Helix.Orchestration.Core` output as artifact (`helix-orchestration-core-publish`, retention 30 days)

## Recommended Branch Protection

Configure branch protection rules in GitHub repository settings.

### `dev`
Require status checks:
- `Build and Test`
- `Code Quality`

### `stage`
Require status checks:
- `Build and Test`
- `Code Quality`

### `main`
Require status checks:
- `Build and Test`
- `Code Quality`

Recommended additional protections:
- Require pull request before merging
- Require at least 1 approval
- Dismiss stale approvals when new commits are pushed
- Require branches to be up to date before merging
- Restrict direct pushes

## Notes

- Keep workflow changes in `.github/workflows/ci.yml` reviewed via PR.
- If additional test projects are added, ensure they are included by the solution so CI picks them up automatically.
