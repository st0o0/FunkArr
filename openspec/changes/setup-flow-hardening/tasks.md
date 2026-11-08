## 1. Config and model changes

- [x] 1.1 Add `BaseUrl` property to `FunkArrOptions` (default empty string)
- [x] 1.2 Add `FunkArr__BaseUrl` entry to `appsettings.json` (empty default) and `docker-compose.dev.yml` (`http://funkarr:6969`)

## 2. ArrSetupClient URI fix

- [x] 2.1 Fix `PostResourceAsync` URI construction: replace `new Uri(baseUri, apiPath)` with string concatenation that preserves base path
- [x] 2.2 Add tests for sub-path URI preservation (with/without trailing slash, with/without sub-path)

## 3. Self-URL resolution and validation

- [x] 3.1 Extract `ResolveSelfUrl` static helper in `SetupApiEndpoints` — three-tier chain (request > config > context), validation, returns `(Uri?, ArrResourceResponse?)`
- [x] 3.2 Update all 5 setup endpoints to use `ResolveSelfUrl` instead of inline selfUrl logic
- [x] 3.3 Add tests: per-request override priority, config fallback, context fallback, malformed URL returns error, non-HTTP scheme returns error

## 4. Payload fixes

- [x] 4.1 Change `SabnzbdDownloadClientPayload.Create` to accept `Uri` instead of `string`, add `useSsl` field derived from scheme
- [x] 4.2 Change `ProwlarrIndexerPayload.Create` and `SonarrRadarrIndexerPayload.Create` to accept `Uri` instead of `string`
- [x] 4.3 Add tests: HTTPS URI produces `useSsl: true`, HTTP produces `useSsl: false`

## 5. Health check additions

- [x] 5.1 Add `tvdbApiKey` warn-level check (empty = warn with hint, configured = ok)
- [x] 5.2 Add `tmdbApiKey` warn-level check (empty = warn with hint, configured = ok)
- [x] 5.3 Add `baseUrl` to `connectionInfo` response from `FunkArrOptions.BaseUrl`
- [x] 5.4 Add tests for TVDB/TMDB key checks and baseUrl in connectionInfo

## 6. Frontend: self-URL pre-fill

- [x] 6.1 Update `setup.ts` API types to include `baseUrl` in connectionInfo
- [x] 6.2 Pre-fill FunkArr URL field from `connectionInfo.baseUrl` when non-empty, fall back to `window.location`
- [x] 6.3 Update manual config host/port to derive from `connectionInfo.baseUrl` when available

## 7. Frontend: Prowlarr topology

- [x] 7.1 Add computed property: `isProwlarrSelected` based on step 2 selections
- [x] 7.2 Conditionally hide "Create Indexer" button on Sonarr/Radarr steps when `isProwlarrSelected`
- [x] 7.3 Add info callout on Sonarr/Radarr steps when Prowlarr selected: "Prowlarr syncs the indexer automatically. Only the download client is needed here."
- [x] 7.4 Fix Prowlarr `downloadClient` handler — remove the incorrect `createProwlarrIndexer` assignment

## 8. Verify and format

- [x] 8.1 Run `dotnet build src/FunkArr.slnx` — zero warnings
- [x] 8.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 8.3 Run backend tests: `dotnet run --project src/FunkArr.Api.Tests/FunkArr.Api.Tests.csproj`
