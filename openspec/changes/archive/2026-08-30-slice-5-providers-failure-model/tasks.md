## 1. IQueryResult model in Core

- [x] 1.1 Create `FunkArr.Core/Results/IQueryResult.cs` with `IQueryResult` interface, `QuerySuccess<T>` record, `QueryFailure` record, and `FailureReason` enum
- [x] 1.2 Add helper extension method `IQueryResult.IsSuccess()` / `IQueryResult.AsSuccess<T>()` for convenient pattern matching

## 2. Package dependency

- [x] 2.1 Add `Microsoft.Extensions.Http.Resilience` to `Directory.Packages.props`
- [x] 2.2 Add `PackageReference` to `FunkArr.csproj` for `Microsoft.Extensions.Http.Resilience`

## 3. Provider renames and moves (structure only, no behavior changes)

- [x] 3.1 Move+rename `Search/Resolvers/TvdbV4Client.cs` → `Providers/Tvdb/TvdbProvider.cs`, update class name and all references
- [x] 3.2 Move `Search/Resolvers/TvdbGatewayActor.cs` → `Providers/Tvdb/TvdbGatewayActor.cs`, update namespace
- [x] 3.3 Move `Search/Resolvers/TvdbGatewayActorState.cs` → `Providers/Tvdb/TvdbGatewayActorState.cs`, update namespace
- [x] 3.4 Move+rename `Search/Resolvers/TmdbClient.cs` → `Providers/Tmdb/TmdbProvider.cs`, update class name and all references
- [x] 3.5 Move `Search/Resolvers/TmdbGatewayActor.cs` → `Providers/Tmdb/TmdbGatewayActor.cs`, update namespace
- [x] 3.6 Move `Search/Resolvers/TmdbGatewayActorState.cs` → `Providers/Tmdb/TmdbGatewayActorState.cs`, update namespace
- [x] 3.7 Move+rename `Search/MediathekClient.cs` → `Providers/Mediathek/MediathekProvider.cs`, update class name and all references
- [x] 3.8 Move `Search/MediathekGatewayActor.cs` → `Providers/Mediathek/MediathekGatewayActor.cs`, update namespace
- [x] 3.9 Move+rename `RuleSet/GitHubReleaseClient.cs` → `Providers/GitHub/GitHubProvider.cs`, update class name and all references
- [x] 3.10 Move+rename `DownloadClient/Ffmpeg/FfmpegService.cs` → `Providers/Ffmpeg/FfmpegProvider.cs`, rename class/interface to `FfmpegProvider`/`IFfmpegProvider`, update all references
- [x] 3.11 Move+rename `Shared/FileService.cs` → `Providers/FileSystem/FileSystemProvider.cs`, rename class/interface to `FileSystemProvider`/`IFileSystemProvider`, update all references
- [x] 3.12 Move any related types (TvdbAuthHandler, TvdbModels, TmdbModels, MediathekModels) into their respective `Providers/` folders

## 4. Service suffix removal

- [x] 4.1 Rename `SetupValidationService` → `SetupValidator`, update all references
- [x] 4.2 Rename `ApiKeyValidationService` → `ApiKeyValidator`, update all references
- [x] 4.3 Remove `RuleSetGenerationService` — inline gateway+generate calls at the call sites (controller endpoints for preview generation)

## 5. IQueryResult integration into providers

- [x] 5.1 Update `TvdbProvider` to return `Task<IQueryResult>` from public methods, add single catch site
- [x] 5.2 Update `TmdbProvider` to return `Task<IQueryResult>` from public methods, add single catch site
- [x] 5.3 Update `MediathekProvider` to return `Task<IQueryResult>` from public methods, add single catch site
- [x] 5.4 Update `GitHubProvider` to return `Task<IQueryResult>` from public methods, add single catch site

## 6. Gateway actors updated for QueryFailure

- [x] 6.1 Update `TvdbGatewayActor` to handle `QueryFailure` (serve stale cache or propagate)
- [x] 6.2 Update `TmdbGatewayActor` to handle `QueryFailure` (serve stale cache or propagate)
- [x] 6.3 Update `MediathekGatewayActor` to handle `QueryFailure` (propagate, log)
- [x] 6.4 Update `RefreshActor` to handle `QueryFailure` from `GitHubProvider` (retain files, log)

## 7. HTTP resilience handlers

- [x] 7.1 Add `.AddStandardResilienceHandler()` to `MediathekProvider` HTTP client registration in `FunkArrServiceSetup.cs`
- [x] 7.2 Add `.AddStandardResilienceHandler()` to `TvdbProvider` HTTP client registration
- [x] 7.3 Add `.AddStandardResilienceHandler()` to `TmdbProvider` HTTP client registration
- [x] 7.4 Add `.AddStandardResilienceHandler()` to `GitHubProvider` HTTP client registration

## 8. DI registration updates

- [x] 8.1 Update `FunkArrServiceSetup.cs` — all `AddHttpClient<OldName>()` calls updated to new provider names
- [x] 8.2 Update singleton registrations for `IFfmpegProvider`, `IFileSystemProvider`, `SetupValidator`, `ApiKeyValidator`

## 9. Test updates

- [x] 9.1 Update provider test files — rename test classes and update to new type names
- [x] 9.2 Update `FakeHttpMessageHandler` usages in tests for new provider types
- [x] 9.3 Update actor test setups that reference old type names
- [x] 9.4 Add tests for `IQueryResult` mapping in each provider (success + failure cases)

## 10. Verification

- [x] 10.1 Run `dotnet build FunkArr.slnx` — must compile clean
- [x] 10.2 Run all tests — all must pass
- [x] 10.3 Run `dotnet format` on all changed .cs files
- [x] 10.4 Verify no `*Service` suffix remains in non-infrastructure types (grep)
- [x] 10.5 Verify all providers are in `Providers/` folders (grep for `*Provider.cs` locations)
