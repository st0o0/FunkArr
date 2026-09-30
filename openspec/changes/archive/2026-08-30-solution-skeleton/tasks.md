## 1. Build Infrastructure

- [x] 1.1 Create `src/global.json` — .NET SDK 10.0.102, rollForward latestFeature, MTP runner
- [x] 1.2 Create `src/Directory.Build.props` — TargetFramework, Nullable, ImplicitUsings, Version, test detection
- [x] 1.3 Create `src/Directory.Packages.props` — all NuGet packages with central version management
- [x] 1.4 Create `src/.editorconfig` — default .NET style rules

## 2. Projects — Foundation

- [x] 2.1 Create `FunkArr.Messages` project (Microsoft.NET.Sdk, no references)
- [x] 2.2 Create `FunkArr.Persistence` project (Microsoft.NET.Sdk, no references)
- [x] 2.3 Create `FunkArr.Core` project (Microsoft.NET.Sdk, refs: Messages + Persistence, NuGet: Akka + Servus)

## 3. Projects — Domains

- [x] 3.1 Create `FunkArr.Search` project (Microsoft.NET.Sdk, refs: Core)
- [x] 3.2 Create `FunkArr.Download` project (Microsoft.NET.Sdk, refs: Core)
- [x] 3.3 Create `FunkArr.RuleSet` project (Microsoft.NET.Sdk, refs: Core)
- [x] 3.4 Create `FunkArr.MatchMagic` project (Microsoft.NET.Sdk, refs: Core)

## 4. Projects — Adapters

- [x] 4.1 Create `FunkArr.Api` project (Microsoft.NET.Sdk, refs: Core)
- [x] 4.2 Create `FunkArr.IndexerApi` project (Microsoft.NET.Sdk, refs: Core)
- [x] 4.3 Create `FunkArr.DownloadApi` project (Microsoft.NET.Sdk, refs: Core)

## 5. Projects — Host

- [x] 5.1 Create `FunkArr` host project (Microsoft.NET.Sdk.Web, refs: all domain + adapter projects, NuGet: Serilog sinks)

## 6. Projects — Tests

- [x] 6.1 Create `FunkArr.Tests.Shared` project (refs: Core, NuGet: xUnit + TestKit)
- [x] 6.2 Create `FunkArr.Search.Tests` project (refs: Search + Tests.Shared)
- [x] 6.3 Create `FunkArr.Download.Tests` project (refs: Download + Tests.Shared)
- [x] 6.4 Create `FunkArr.RuleSet.Tests` project (refs: RuleSet + Tests.Shared)
- [x] 6.5 Create `FunkArr.MatchMagic.Tests` project (refs: MatchMagic + Tests.Shared)
- [x] 6.6 Create `FunkArr.Api.Tests` project (refs: Api + Tests.Shared)
- [x] 6.7 Create `FunkArr.IndexerApi.Tests` project (refs: IndexerApi + Tests.Shared)
- [x] 6.8 Create `FunkArr.DownloadApi.Tests` project (refs: DownloadApi + Tests.Shared)

## 7. Solution File

- [x] 7.1 Create `FunkArr.slnx` with all 18 projects and solution items folder

## 8. Build Verification

- [x] 8.1 Verify `dotnet build FunkArr.slnx` succeeds from `src/`
- [x] 8.2 Verify `dotnet format --verify-no-changes` passes from `src/`

## 9. CI & Docker

- [x] 9.1 Update `Dockerfile` for multi-project solution (multi-stage build)
- [x] 9.2 Update `Dockerfile.dev` for development with dotnet watch
- [x] 9.3 Update CI workflows for new solution structure

## 10. Vue.js UI Shell

- [x] 10.1 Scaffold `FunkArr.UI` with Vite + Vue 3 + TypeScript + Tailwind + Vue Router
