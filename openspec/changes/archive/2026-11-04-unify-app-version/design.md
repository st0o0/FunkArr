## Context

`src/Directory.Build.props` defines a `GenerateVersionInfo` target that writes `VersionInfo.g.cs` into every non-test project (`internal static partial class VersionInfo` in namespace `FunkArr.Core`, value `$(Version)`). Only `FunkArr.Api` consumes it (`/api/system/version`). The host setup containers instead read `Assembly.GetName().Version`, which is the 4-part `AssemblyVersion` without prerelease suffix. Version comes from release-please (`<Version>` in the props file) and from `/p:Version=${VERSION}` in the Dockerfile.

## Goals / Non-Goals

**Goals:**
- One generated const, one copy, in `FunkArr.Core`.
- All consumers print the same string for the same build.
- Prerelease suffixes survive (`0.2.0-rc1`).

**Non-Goals:**
- Splitting version and commit into separate values.
- A DI service or interface for the version.
- UI changes (shared composable, deduplicated fetch).
- Touching ruleset versioning or the SABnzbd fake version.

## Decisions

**Generate in `FunkArr.Core.csproj`, not `Directory.Build.props`.** Core is referenced by every domain, Api and Host, so a `public` class there is reachable everywhere without breaking the reference direction. The target moves out of the props file, which removes the per-assembly copies. The target has no condition on `Tests` projects since it lives only in Core.

**Source value is `$(InformationalVersion)` with fallback to `$(Version)`.** The informational version keeps prerelease suffixes. The fallback covers the case where the SDK has not resolved it yet when the target runs.

**Plain `public const string`, no DI service.** The value is build-time constant, nothing needs to mock it, and the existing consumers are static setup code and a minimal API lambda. A wrapper type can be added later if a commit split is wanted.

**Alternatives considered:**
- Read `AssemblyInformationalVersionAttribute` at runtime: no MSBuild target, but reflection in three places and a helper to strip `+sha`.
- Keep the target in the props file and make the class public: still generates a conflicting `public` type into every assembly.

## Risks / Trade-offs

- [`InformationalVersion` may not be resolved before `CoreCompile`] → inspect the generated `obj/.../BuildInfo.g.cs` after a build; the `$(Version)` fallback keeps the const valid either way.
- [`+<sha>` suffix from SourceLink appears in the const] → strip it in the target if it shows up, so logs, User-Agent and the endpoint stay clean. Decide after inspecting the generated file.
- [Log, telemetry and User-Agent strings change from `0.1.0.0`/`0.1.0` to one shared format] → intended; no consumer parses them.
- [Existing `VersionInfo` references in tests] → none found in `src/`; confirmed by search before removal.

## Migration Plan

Single commit. Rollback is a revert. Version 0.x, no compatibility shim needed.

## Open Questions

- Does the generated const contain `+sha` in CI/Docker builds? Check with `/p:ContinuousIntegrationBuild=true` as in the Dockerfile.
