## 1. Newznab Caps Enhancements

- [x] 1.1 Add `Server` model class to `Caps.cs` with `[XmlElement("server")]` and `title` attribute, add `BookSearch` search type to `Searching` with `available="no"`
- [x] 1.2 Update `Caps.Searching.MovieSearch` supportedParams from `"q,imdbid"` to `"q,imdbid,tmdbid"`
- [x] 1.3 Update `CapsJsonProjection` to include `server` and `book-search` fields in JSON output
- [x] 1.4 Add `TmdbId` parameter (`[FromQuery(Name = "tmdbid")] string? TmdbId`) to `IndexerRequest`

## 2. SABnzbd Config Completeness

- [x] 2.1 Add `enable_date_sorting = false`, `tv_categories = []`, `movie_categories = []`, `date_categories = []` to `BuildConfig` misc section in `DownloadApiEndpoints.cs`
- [x] 2.2 Add `Output` parameter (`[FromQuery(Name = "output")] string? Output`) to `DownloadGetRequest`

## 3. Tests

- [x] 3.1 Update `NewznabXmlTests.Caps_declares_search_types` to assert `<server>`, `<book-search>`, and `tmdbid` in movie-search supportedParams
- [x] 3.2 Add test `Caps_declares_server_element` verifying `<server title="FunkArr"/>` is present
- [x] 3.3 Update `JsonOutputTests` (if caps JSON tests exist) to verify `server` and `book-search` in JSON caps
- [x] 3.4 Add test `Config_response_contains_all_sorting_fields` verifying `enable_date_sorting`, `tv_categories`, `movie_categories`, `date_categories` in get_config JSON
- [x] 3.5 Run `dotnet format` and verify all tests pass
