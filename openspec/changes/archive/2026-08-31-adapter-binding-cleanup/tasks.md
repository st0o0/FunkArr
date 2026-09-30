## 1. NZB Object Model (IndexerApi)

- [x] 1.1 Create NZB XML model classes (`Nzb`, `NzbHead`, `NzbMeta`, `NzbFile`, `NzbGroups`, `NzbSegments`) with `[XmlRoot]`/`[XmlElement]`/`[XmlAttribute]` decorations in `FunkArr.IndexerApi/Models/`
- [x] 1.2 Rewrite `NzbGenerator.Generate` to build the `Nzb` object and serialize via `XmlHelper.Serialize<Nzb>()`
- [x] 1.3 Remove `NzbGenerator.ParseNzb` (unused after format change) if not referenced elsewhere
- [x] 1.4 Update IndexerApi tests for new NZB generation format

## 2. NZB Parser (DownloadApi)

- [x] 2.1 Create NZB XML model classes (duplicate of IndexerApi's model) in `FunkArr.DownloadApi/Models/`
- [x] 2.2 Rewrite `NzbParser.Parse` to deserialize NZB XML via `XmlSerializer` and extract title/url from `<head><meta>` elements
- [x] 2.3 Update DownloadApi tests for new NZB parsing format

## 3. Parameter Binding (IndexerApi)

- [x] 3.1 Create `IndexerRequest` record with `[FromQuery]` attributes in `FunkArr.IndexerApi/`
- [x] 3.2 Refactor `IndexerApiEndpoints` to accept `[AsParameters] IndexerRequest` instead of `HttpContext` for query params
- [x] 3.3 Update IndexerApi tests (no changes needed — existing tests cover serialization, not HTTP binding) for parameter binding changes

## 4. Parameter Binding (DownloadApi)

- [x] 4.1 Create `DownloadGetRequest` and `DownloadPostRequest` records with `[FromQuery]` attributes in `FunkArr.DownloadApi/`
- [x] 4.2 Refactor `DownloadApiEndpoints` GET handler to accept `[AsParameters] DownloadGetRequest`
- [x] 4.3 Refactor `DownloadApiEndpoints` POST handler to accept `[AsParameters] DownloadPostRequest` (keep `HttpContext` only for form file access)
- [x] 4.4 Update DownloadApi tests (no changes needed — existing tests cover model serialization and state, not HTTP binding) for parameter binding changes

## 5. Verification

- [x] 5.1 Run `dotnet build` and `dotnet format` across solution
- [x] 5.2 Run all adapter test projects
