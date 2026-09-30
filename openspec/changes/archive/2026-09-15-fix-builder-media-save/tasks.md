## 1. Backend: Fix MergeMedia

- [x] 1.1 Fix `MergeMedia()` in `RuleSetMerger.cs` to include `Name = local.Name ?? community.Name` and `Type = local.Type ?? community.Type`
- [x] 1.2 Add tests for Name/Type preservation in merger tests

## 2. Frontend: Add media type and media name fields

- [x] 2.1 Add `mediaType` (string, default "show") and `mediaName` (string) and `mediaNameEdited` (boolean) to form state
- [x] 2.2 Load `mediaType` and `mediaName` from raw JSON on mount; set `mediaNameEdited = true` if `media.name !== topic`
- [x] 2.3 Add a `watch` on `form.topic` that syncs `mediaName` when `mediaNameEdited` is false
- [x] 2.4 Add Media Type radio/select (show/movie) to the Identity section in the template, between Aliases and the ID fields
- [x] 2.5 Add Media Name text field to the Identity section, with helper text "Name used for metadata lookup (TVDB/TMDB)"
- [x] 2.6 Set `mediaNameEdited = true` on direct input to the mediaName field

## 3. Frontend: Fix serializeForm

- [x] 3.1 Build media object with `name` and `type` always included; omit `tvdbId`/`imdbId`/`tmdbId` when null/empty instead of sending null
- [x] 3.2 Set `standalone: true` in serialized output when editing a community ruleset (detect via source info from raw JSON or absence of local path)
- [x] 3.3 Also strip null values from rule fields (`seasonRegex`, `episodeRegex`, `captureGroup`, `confidence`) — omit instead of sending null

## 4. Verify

- [x] 4.1 Run `dotnet build src/FunkArr.slnx`
- [x] 4.2 Run `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 4.3 Run merger tests: `dotnet run --project src/FunkArr.RuleSet.Tests/FunkArr.RuleSet.Tests.csproj`
- [x] 4.4 Rebuild Docker and E2E test: edit a community ruleset in the Builder → Save succeeds → export works
