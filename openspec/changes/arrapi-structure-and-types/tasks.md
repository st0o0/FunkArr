## 1. Characterization tests

- [x] 1.1 Add JSON snapshot tests for every SABnzbd response produced by the services and controller mapping: version, get_config (with and without categories), fullstatus, queue (empty and with slots in Downloading and Queued), history (Completed and Failed), pause/resume/delete/retry/priority/swap status, addfile with `nzo_ids`, errors (invalid mode, invalid queue command, API key)
- [x] 1.2 Add tests for the Newznab `t=get` NZB output (head/meta contents for title, url, subtitle, channel, duration, size, category) and an `addfile` round trip from that output
- [x] 1.3 Run all new tests against the unchanged code and confirm green

## 2. Move files and namespaces

- [x] 2.1 Move `Nzb.cs` to `Newznab/Models/Nzb.cs`; move `SabnzbdConstants.cs` to `Sabnzbd/`
- [x] 2.2 Create `Requests/` and `Services/` folders per protocol and move request records and services with matching namespaces (content unchanged)
- [x] 2.3 Fix usings and test namespaces; build at 0 warnings, `FunkArr.Architecture.Tests` and all group 1 tests green

## 3. Typed SABnzbd responses

- [x] 3.1 Add `SabnzbdStatusResponse`, `SabnzbdAddResponse` and typed config records (misc, categories, sorters) with `[JsonPropertyName]` matching the current JSON
- [x] 3.2 Make the result type generic (`SabnzbdResult<T>` with `Ok`, `Error`, implicit conversions from `T` and `SabnzbdError`) and update services and `MapResult<T>` in the controller
- [x] 3.3 Add ArrApi enums for slot status and priority (`Unknown = 0`, `[JsonStringEnumMemberName]`, `[JsonConverter(typeof(JsonStringEnumConverter<T>))]`); map from Messages enums with exhaustive `switch` and `_ => throw`; update `QueueSlot`/`HistorySlot` and the mapper
- [x] 3.4 Replace the anonymous object in `SabnzbdApiKeyFilter` with `SabnzbdErrorResponse`
- [x] 3.5 Group 1 snapshot tests pass unchanged (JSON byte-identical); add mapper tests for enum mapping including the throwing arm

## 4. NzbPayload

- [x] 4.1 Add `NzbPayload` with `Encode()`/`Decode()` (JSON in Base64, local serializer options) and tests: round trip incl. tab in title and long title, optional subtitle, invalid Base64, invalid JSON, missing url
- [x] 4.2 Use `Encode()` in `NewznabSearchService.ToItem` and `Decode()` in `NzbService.GetNzb`; remove tab splitting, `Strip`, and positional parsing
- [x] 4.3 Build NZB `head/meta` from the payload; read uploaded NZB metas into a payload in the `addfile` flow; remove `NzbParseResult` and the `Meta("url")` fallback
- [x] 4.4 Update `NzbServiceTests`, `NzbGetTests`, `NzbRoundTripTests`, `NzbGeneratorTests`, `NzbParserTests`, `SearchResultMappingTests`, `SabnzbdDownloadServiceTests`

## 5. Verification

- [x] 5.1 `dotnet build src/FunkArr.slnx` at 0 warnings; `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 5.2 Run `FunkArr.ArrApi.Tests`, `FunkArr.Architecture.Tests` and `FunkArr.IntegrationTests`
- [ ] 5.3 Check against the dev stack (`docker compose -f docker-compose.dev.yml up -d --build`): Prowlarr indexer test, Sonarr/Radarr search and grab, SABnzbd client test; user takes part

## Working rules

- One commit per task group, single-line message, no Co-Authored-By.
- Stop and report after groups 2, 3 and 4.
