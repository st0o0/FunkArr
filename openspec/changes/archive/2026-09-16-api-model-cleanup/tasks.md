## 1. Request records

- [x] 1.1 Create `MediathekSearchRequest` record in `FunkArr.Api.Models/` with `[AsParameters]` and `[FromQuery]` properties
- [x] 1.2 Update `MediathekApiEndpoints` search endpoint to use `MediathekSearchRequest` instead of inline parameters
- [x] 1.3 Create `DownloadHistoryRequest` record in `FunkArr.Api.Models/` with `[AsParameters]` and `[FromQuery]` properties
- [x] 1.4 Update `DownloadsApiEndpoints` history endpoint to use `DownloadHistoryRequest` instead of inline parameters

## 2. RuleSetApiEndpoints cleanup

- [x] 2.1 Extract list endpoint lambda into `private static HandleList` method
- [x] 2.2 Consolidate duplicate `SerializeForDisk` methods into a single helper

## 3. Timeout constants

- [x] 3.1 Review and name all timeout constants in `MediathekApiEndpoints`, `DownloadsApiEndpoints`, `RuleSetApiEndpoints`, `SystemApiEndpoints` with descriptive field names
- [x] 3.2 Remove any inline `TimeSpan.FromSeconds(...)` calls from endpoint methods, replacing with named fields

## 4. Verify

- [x] 4.1 Run `dotnet build src/FunkArr.slnx` and fix any compilation errors
- [x] 4.2 Run `dotnet format src/FunkArr.slnx` to apply code style
- [x] 4.3 Run API test projects to verify no regressions
