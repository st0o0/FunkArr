## 1. Project scaffolding

- [x] 1.1 Create `FunkArr.ArrApi` project (`Microsoft.NET.Sdk`), reference `FunkArr.Core`, add to solution
- [x] 1.2 Create `FunkArr.ArrApi.Tests` project, reference `FunkArr.ArrApi` and `FunkArr.Tests.Shared`, add to solution
- [x] 1.3 Update `FunkArr.csproj` (host) to reference `FunkArr.ArrApi` instead of `FunkArr.IndexerApi` and `FunkArr.DownloadApi`
- [x] 1.4 Remove `FunkArr.IndexerApi` and `FunkArr.DownloadApi` projects from solution
- [x] 1.5 Remove `FunkArr.IndexerApi.Tests` and `FunkArr.DownloadApi.Tests` projects from solution

## 2. Shared types (root namespace)

- [x] 2.1 Move `Nzb.cs` model to `FunkArr.ArrApi` root namespace (single copy, merge both models — keep the fuller IndexerApi version with default values)
- [x] 2.2 Move `XmlHelper.cs` to `FunkArr.ArrApi` root namespace, update namespace
- [x] 2.3 Move `NzbGenerator.cs` and `NzbParser.cs` to `FunkArr.ArrApi` root namespace, update both to use shared `Nzb` model
- [x] 2.4 Create unified `ApiKeyEndpointFilter` with `Func<IResult>` error factory parameter

## 3. Newznab adapter (Newznab/ folder)

- [x] 3.1 Move `IndexerApiEndpoints.cs` to `Newznab/`, update namespace to `FunkArr.ArrApi.Newznab`
- [x] 3.2 Move `IndexerRequest.cs` to `Newznab/`, update namespace
- [x] 3.3 Move Newznab models (`Caps.cs`, `CapsJsonProjection.cs`, `Rss.cs`, `RssJsonProjection.cs`, `NewznabError.cs`) to `Newznab/Models/`, update namespace to `FunkArr.ArrApi.Newznab.Models`
- [x] 3.4 Remove hardcoded categories from `Caps.cs` — replace with empty defaults that will be populated from RuleSet domain
- [x] 3.5 Update `IndexerApiEndpoints.MapIndexerApi` to use the unified `ApiKeyEndpointFilter` with Newznab XML error factory

## 4. SABnzbd adapter (Sabnzbd/ folder)

- [x] 4.1 Move `DownloadApiEndpoints.cs` to `Sabnzbd/`, update namespace to `FunkArr.ArrApi.Sabnzbd`, remove `DownloadState` dependency
- [x] 4.2 Move `DownloadGetRequest.cs` and `DownloadPostRequest.cs` to `Sabnzbd/`, update namespace
- [x] 4.3 Move SABnzbd models (`QueueResponse.cs`, `HistoryResponse.cs`, `FullStatusResponse.cs`) to `Sabnzbd/Models/`, update namespace to `FunkArr.ArrApi.Sabnzbd.Models`
- [x] 4.4 Remove hardcoded categories from `BuildConfig` — replace with empty defaults
- [x] 4.5 Update `DownloadApiEndpoints.MapDownloadApi` to use unified `ApiKeyEndpointFilter` with JSON error factory
- [x] 4.6 Replace `DownloadState` usage with stub responses (queue: 0 slots, history: 0 slots, addfile: acknowledge without processing, retry/delete: not found)

## 5. Host wiring

- [x] 5.1 Update `Program.cs`/startup to call `MapIndexerApi` and `MapDownloadApi` from new `FunkArr.ArrApi` namespaces, remove `DownloadState` registration

## 6. Tests

- [x] 6.1 Migrate existing IndexerApi tests to `FunkArr.ArrApi.Tests`, update namespaces
- [x] 6.2 Migrate existing DownloadApi tests to `FunkArr.ArrApi.Tests`, update namespaces and remove/update tests that depend on `DownloadState` behavior
- [x] 6.3 Add test for `ApiKeyEndpointFilter` format-aware error responses (XML and JSON)
- [x] 6.4 Add test for NZB round-trip (generate → parse → same title+url)

## 7. Cleanup and verification

- [x] 7.1 Delete `FunkArr.IndexerApi/` and `FunkArr.DownloadApi/` project directories
- [x] 7.2 Delete `FunkArr.IndexerApi.Tests/` and `FunkArr.DownloadApi.Tests/` project directories
- [x] 7.3 Update architecture tests for new project name (`ArrApi` instead of `IndexerApi`/`DownloadApi`) — N/A, no arch test files exist yet
- [x] 7.4 Update `CLAUDE.md` solution structure section
- [x] 7.5 Run `dotnet build FunkArr.slnx`, `dotnet format --verify-no-changes`, and all test projects — verify green
