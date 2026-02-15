# Release Guide

## Versioning

Aster follows [Semantic Versioning](https://semver.org/):

- **MAJOR.MINOR.PATCH** (e.g., 0.2.0)
- **MAJOR**: Incompatible API changes
- **MINOR**: New functionality (backward compatible)
- **PATCH**: Bug fixes (backward compatible)

## Current Version

The current version is stored in the `VERSION` file at the repository root.

## Creating a Release

### 1. Update Version

```bash
# Update VERSION file
echo "0.3.0" > VERSION

# Update CHANGELOG.md
# - Move items from [Unreleased] to [0.3.0]
# - Add release date
# - Create new [Unreleased] section
```

### 2. Update Changelog

Edit `CHANGELOG.md`:

```markdown
## [Unreleased]

## [0.3.0] - 2026-03-01

### Added
- Feature XYZ
- ...

### Changed
- ...

### Fixed
- ...
```

### 3. Commit and Tag

```bash
# Commit version changes
git add VERSION CHANGELOG.md
git commit -m "Bump version to 0.3.0"

# Create annotated tag
git tag -a v0.3.0 -m "Release v0.3.0"

# Push to GitHub
git push origin main
git push origin v0.3.0
```

### 4. GitHub Actions

The release workflow will automatically:
1. Create a GitHub Release
2. Extract changelog for the version
3. Build binaries for:
   - Linux (x64)
   - Windows (x64)
   - macOS (x64)
4. Upload binaries as release assets

### 5. Verify Release

1. Go to https://github.com/justinamiller/Aster-1/releases
2. Verify the release was created
3. Check that binaries are attached
4. Test download and installation

## Manual Release (Workflow Dispatch)

You can also create a release manually via GitHub Actions:

1. Go to Actions → Release workflow
2. Click "Run workflow"
3. Enter version number (e.g., 0.3.0)
4. Click "Run workflow"

## Release Checklist

Before releasing:

- [ ] All tests pass (`dotnet test`)
- [ ] Smoke test passes (`./scripts/smoke_test.sh`)
- [ ] VERSION file updated
- [ ] CHANGELOG.md updated
- [ ] Documentation updated if needed
- [ ] Breaking changes documented
- [ ] Migration guide written (for major versions)

## Binary Artifacts

Each release includes:

**Linux** (`aster-linux-x64.tar.gz`):
```bash
tar -xzf aster-linux-x64.tar.gz
cd aster-linux-x64
./aster --version
```

**Windows** (`aster-windows-x64.zip`):
```powershell
Expand-Archive aster-windows-x64.zip
cd aster-windows-x64
.\aster.exe --version
```

**macOS** (`aster-macos-x64.tar.gz`):
```bash
tar -xzf aster-macos-x64.tar.gz
cd aster-macos-x64
./aster --version
```

## Hotfix Releases

For urgent bug fixes:

1. Create a hotfix branch from the release tag:
   ```bash
   git checkout -b hotfix/0.2.1 v0.2.0
   ```

2. Fix the bug and test

3. Update VERSION to 0.2.1

4. Update CHANGELOG.md

5. Commit, tag, and push:
   ```bash
   git commit -m "Fix critical bug XYZ"
   git tag -a v0.2.1 -m "Hotfix v0.2.1"
   git push origin hotfix/0.2.1
   git push origin v0.2.1
   ```

6. Merge hotfix back to main:
   ```bash
   git checkout main
   git merge hotfix/0.2.1
   git push origin main
   ```

## Pre-releases

For alpha/beta releases:

```bash
# Version format: MAJOR.MINOR.PATCH-alpha.N
echo "0.3.0-alpha.1" > VERSION

# Tag format
git tag -a v0.3.0-alpha.1 -m "Alpha release 0.3.0-alpha.1"
```

Mark as pre-release in GitHub Releases.

## Version History

| Version | Date | Highlights |
|---------|------|------------|
| 0.2.0 | 2026-02-15 | Stage1 infrastructure, --stage1 flag |
| 0.1.0 | 2026-01-01 | Initial release, C# compiler |

## Automation

The release process is automated via `.github/workflows/release.yml`:

- **Trigger**: Push of tag matching `v*.*.*`
- **Actions**:
  1. Create GitHub Release
  2. Extract changelog
  3. Build binaries (Linux, Windows, macOS)
  4. Upload artifacts to release

## Dependencies

Release binaries are:
- **Self-contained**: Include .NET runtime
- **Single-file**: Packaged as single executable
- **Platform-specific**: Optimized for target OS

## Testing Releases

Before tagging:

```bash
# Smoke test
./scripts/smoke_test.sh

# Full test suite
dotnet test

# Bootstrap test
./scripts/bootstrap_stage1.sh
```

## Rollback

If a release has critical issues:

1. Create a hotfix release (e.g., 0.2.1)
2. Do NOT delete the broken release
3. Add a note to the release description warning users
4. Update documentation to point to the fixed version

## Communication

After release:

1. Update README.md with latest version
2. Announce on project channels
3. Update getting started guides
4. Tweet/blog about new features (for major releases)

---

**Last Updated**: 2026-02-15  
**Maintainer**: Aster Project Team
