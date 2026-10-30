# CI/CD Pipeline Documentation

## Overview
This repository uses GitHub Actions for continuous integration and deployment. The pipeline is triggered on push and pull requests to the following branches:
- `dev` (development)
- `stage` (staging)
- `main` (production)

## Pipeline Stages

### 1. Build and Test
**Runs on**: All branches on push and pull requests

**Steps**:
- **Checkout**: Clones the repository
- **Setup .NET**: Installs .NET 10.0
- **Cache NuGet packages**: Caches NuGet packages for faster builds
- **Restore**: Restores NuGet dependencies in Release configuration
- **Build**: Compiles the solution in Release mode
- **Test**: Runs all unit tests and generates TRX reports
- **Upload test results**: Archives test results as artifacts for 7 days

**Success criteria**:
- Build completes without errors
- All tests pass (skipped Docker integration tests are allowed)

### 2. Code Quality
**Runs on**: All branches after build-and-test succeeds

**Steps**:
- **Checkout**: Clones the repository
- **Setup .NET**: Installs .NET 10.0
- **Restore**: Restores NuGet dependencies
- **Check formatting**: Runs `dotnet format --verify-no-changes` (informational only, does not fail the pipeline)

**Note**: Formatting checks are informational to guide developers, but don't block the pipeline to allow for flexibility with legacy code patterns.

### 3. Publish Artifact
**Runs on**: `main` branch on push only

**Steps**:
- **Checkout**: Clones the repository
- **Setup .NET**: Installs .NET 10.0
- **Publish core project**: Publishes `Helix.Orchestration.Core` in Release mode
- **Upload publish artifact**: Archives the published output for 30 days

**Success criteria**:
- Must wait for build-and-test to succeed
- Only triggers on `main` branch

## Test Coverage
The pipeline runs tests across the following test projects:

| Project | Tests | Status |
|---------|-------|--------|
| Helix.Orchestration.Agent.Tests | 8 | ✅ All passing |
| Helix.Orchestration.Core.Tests | 107 | ✅ All passing |
| Helix.Orchestration.Devices.Tests | 57 | ✅ All passing |
| Helix.Orchestration.SiteServer.Tests | 44 | ✅ All passing |
| Helix.Orchestration.Infrastructure.Tests | 2 | ⏭️ Skipped (Docker required) |

**Total**: 216+ tests (+ 2 skipped Docker integration tests)

## Environment Variables
The pipeline uses the following environment configurations:
- **Configuration**: Release
- **.NET Version**: 10.0.x (preview)
- **Platform**: Ubuntu-latest

## Artifacts

### Test Results
- **Name**: test-results
- **Retention**: 7 days
- **Format**: TRX (Test Results XML)
- **Location**: `test-results/`

### Published Artifacts (main branch only)
- **Name**: helix-orchestration-core-publish
- **Retention**: 30 days
- **Contents**: Compiled binaries and dependencies for Helix.Orchestration.Core
- **Location**: `publish/`

## Branch Strategy

### dev
- Runs full CI pipeline on every push
- Test failures block merges
- Used for active development

### stage
- Runs full CI pipeline on every push
- Test failures block merges
- Staging/pre-release candidate

### main
- Runs full CI pipeline on every push
- Generates published artifacts
- Production branch

## Troubleshooting

### Build Fails
1. Check that all dependencies are specified in project files
2. Verify .NET 10.0 compatibility
3. Run `dotnet restore` locally to check for package resolution issues

### Tests Fail
1. Run tests locally: `dotnet test Helix.Orchestration/Helix.Orchestration.slnx -c Release`
2. Check test output in uploaded artifacts
3. For Docker integration tests, ensure Docker is installed locally

### Format Warnings
1. Auto-fix formatting: `dotnet format Helix.Orchestration/Helix.Orchestration.slnx`
2. Review changes and commit formatting updates
3. Note: Formatting checks in CI are informational only

## Local Development

To run the same checks locally:

```bash
# Restore dependencies
dotnet restore Helix.Orchestration/Helix.Orchestration.slnx -p:Configuration=Release

# Build in Release mode
dotnet build Helix.Orchestration/Helix.Orchestration.slnx -c Release

# Run tests
dotnet test Helix.Orchestration/Helix.Orchestration.slnx -c Release

# Check formatting
dotnet format Helix.Orchestration/Helix.Orchestration.slnx --verify-no-changes

# Auto-fix formatting
dotnet format Helix.Orchestration/Helix.Orchestration.slnx

# Publish (Core project)
dotnet publish Helix.Orchestration/Helix.Orchestration.Core/Helix.Orchestration.Core.csproj -c Release -o publish/
```

## Contact
For CI/CD pipeline issues, refer to the repository maintainers.
