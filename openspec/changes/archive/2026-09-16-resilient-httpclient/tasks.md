## 1. Package setup

- [x] 1.1 Add `Microsoft.Extensions.Http.Resilience` to `Directory.Packages.props`
- [x] 1.2 Add `PackageReference` to `FunkArr.csproj` (host project)
- [x] 1.3 Add `PackageReference` to `FunkArr.Download.csproj`

## 2. Configure resilience on host-registered clients

- [x] 2.1 Add `AddStandardResilienceHandler()` to "MediathekViewWeb" client in `MediathekSetupContainer.cs` with 45s total / 15s attempt timeout overrides
- [x] 2.2 Add `AddStandardResilienceHandler()` to "GitHub" client in `RuleSetSetupContainer.cs` with standard defaults
- [x] 2.3 Add `AddStandardResilienceHandler()` to `TvdbClient` in `MetadataSetupContainer.cs` with standard defaults
- [x] 2.4 Add `AddStandardResilienceHandler()` to `TmdbClient` in `MetadataSetupContainer.cs` with standard defaults

## 3. Configure resilience on download-registered client

- [x] 3.1 Add `AddStandardResilienceHandler()` to `SubtitlePreparer` client in `DownloadServiceExtensions.cs` with 15s total / 5s attempt timeout overrides

## 4. Verify and format

- [x] 4.1 Run `dotnet build src/FunkArr.slnx` and fix any compilation errors
- [x] 4.2 Run `dotnet format src/FunkArr.slnx` to apply code style
- [x] 4.3 Run test projects to verify no regressions
