## 1. Generate version in Core

- [x] 1.1 Add `GenerateVersionConst` target to `src/FunkArr.Core/FunkArr.Core.csproj` (`InformationalVersion` with fallback to `Version`, `public` class)
- [x] 1.2 Remove `GenerateVersionInfo` target from `src/Directory.Build.props`
- [x] 1.3 Build and inspect the generated file in `FunkArr.Core/obj`; `+sha` was present (`0.2.0-rc1+<sha>`), now stripped in the target

## 2. Switch consumers

- [x] 2.1 `SystemApiEndpoints.cs`: use the new const instead of `VersionInfo.Version`
- [x] 2.2 `LoggingSetupContainer.cs`: `ApplicationVersion` from the new const
- [x] 2.3 `TelemetrySetupContainer.cs`: `serviceVersion` from the new const
- [x] 2.4 `RuleSetSetupContainer.cs`: User-Agent from the new const

## 3. Verify

- [x] 3.1 Solution builds with 0 errors; `dotnet format --verify-no-changes` only reports an unrelated IDE1006 in `MovieSearchWorker.cs`
- [x] 3.2 Api.Tests (106) and Architecture.Tests (13) pass; IntegrationTests 64/66 pass incl. SystemTests, the 2 failures are unrelated `DownloadReorderTests` priority-validation cases
- [x] 3.3 Confirm no remaining `Assembly.GetName().Version` or `VersionInfo` references via search
