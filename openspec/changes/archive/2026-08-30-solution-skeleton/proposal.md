## Why

The previous single-csproj architecture suffered from domain coupling, eroded code
quality, and no compiler-enforced boundaries. The entire solution was deleted for a
clean restart. We need the structural foundation — a multi-project solution skeleton
with all projects, references, build infrastructure, and style enforcement — before
any business logic can be implemented.

## What Changes

- Create 18-project .NET solution with domain isolation enforced by project references
- Establish build infrastructure: `global.json`, `Directory.Build.props`, `Directory.Packages.props`
- Add `.editorconfig` with default .NET style rules
- Create empty but compiling project shells with correct inter-project references
- Update Dockerfile and CI pipelines for the new solution structure
- Add Vue.js UI project shell (FunkArr.UI)

## Capabilities

### New Capabilities

- `solution-infrastructure`: Build tooling — global.json, Directory.Build.props, Directory.Packages.props, .editorconfig, slnx
- `project-structure`: All 18 .csproj files with correct SDK types, project references, and NuGet package references
- `ci-pipeline`: Updated GitHub Actions workflows and Dockerfile for multi-project solution
- `ui-shell`: Vue.js project scaffold (FunkArr.UI) with Vite + Tailwind

### Modified Capabilities

(none — clean restart, no existing specs apply)

## Impact

- `src/` directory: entirely new, 18 projects created
- `Dockerfile`, `Dockerfile.dev`: updated for new solution layout
- `.github/workflows/`: CI pipelines adapted to new project structure
- All existing OpenSpec main specs are stale (from old architecture) — will be cleaned up separately
