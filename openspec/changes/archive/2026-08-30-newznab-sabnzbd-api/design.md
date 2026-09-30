## Context

FunkArr.IndexerApi and FunkArr.DownloadApi are empty project scaffolds that reference only FunkArr.Core. The host project already references both and has a `FunkArrApplicationSetup` that maps health and liveness endpoints. `FunkArrOptions` has `ApiKey` and `PersistencePath`.

The Newznab and SABnzbd protocols are well-defined external contracts. Prowlarr, Sonarr, and Radarr are the clients — they expect exact wire format compliance. MediathekArr's implementation (TController, DownloadController) is a proven reference for what these clients actually send and expect.

## Goals / Non-Goals

**Goals:**
- Complete, correct Newznab XML wire format that Prowlarr validates during indexer setup
- Complete, correct SABnzbd JSON wire format that Sonarr/Radarr validate during download client setup
- ApiKey authentication on all endpoints
- Adapter projects remain thin translators — no business logic
- Stubbed responses where domains aren't built yet, but in the correct format
- Tests that validate XML/JSON structure against what *arr clients expect

**Non-Goals:**
- Actual search execution (Search domain not built yet)
- Actual download execution (Download domain not built yet)
- Actor integration or persistence
- Rate limiting, caching, or performance optimization
- Web UI endpoints (that's FunkArr.Api)

## Decisions

### Decision 1: ASP.NET Minimal API endpoints, not controllers

**Choice:** Use `app.MapGet`/`app.MapPost` with endpoint groups, not MVC controllers. Each adapter project exposes a static `MapEndpoints(WebApplication)` extension method called from `FunkArrApplicationSetup`.

**Why:** The project uses ASP.NET Minimal API (no MVC package reference). Minimal API endpoints are simpler, faster, and align with the existing health/liveness pattern in `FunkArrApplicationSetup`. The adapter projects don't need controllers, DI constructors, or action filters — they're thin route handlers.

### Decision 2: XML serialization via XmlSerializer with Newznab namespace

**Choice:** Define C# record/class models decorated with `[XmlRoot]`, `[XmlElement]`, `[XmlAttribute]` and serialize via `XmlSerializer`. The Newznab namespace `http://www.newznab.com/DTD/2010/feeds/attributes/` is registered as prefix `newznab`.

**Why:** Prowlarr and Sonarr parse Newznab XML strictly — attribute names, namespace prefixes, and element nesting must be exact. `XmlSerializer` with explicit attributes gives full control over the output format. `System.Text.Json` can't produce XML.

### Decision 3: Fake NZB encodes download URL in XML comments

**Choice:** The NZB download endpoint returns a minimal NZB XML file with the actual download URL and title encoded in XML comments (`<!-- url -->`, `<!-- title -->`). The SABnzbd addfile endpoint parses these comments to extract the URL.

**Why:** This is exactly how MediathekArr does it, and it works. The NZB format is a usenet standard — we generate a syntactically valid NZB so clients handle it correctly, but the real download URL lives in the comments. The addfile endpoint is the only consumer, so this is a clean internal contract.

**Encoding:** Base64 for the URL and title in the fake_nzb_download query parameters (same as MediathekArr), so special characters in URLs don't break query strings.

### Decision 4: ApiKey validation as an endpoint filter

**Choice:** Create a shared `ApiKeyFilter` that validates `?apikey=` query parameter against `FunkArrOptions.ApiKey`. Apply it as an endpoint filter to both adapter endpoint groups. Return 403 with an error XML/JSON body on failure.

**Why:** Both APIs need the same auth check. An endpoint filter keeps it out of individual handlers. The filter reads `FunkArrOptions` from DI. Prowlarr shows a clear error when it gets a 403 during indexer validation.

**Location:** `FunkArr.Core` since both adapter projects reference it.

### Decision 5: Stubbed search responses return empty valid XML

**Choice:** When tvsearch/search/movie endpoints are called, return a valid Newznab RSS XML with zero items and `total="0"`. When addfile is called, accept the NZB but return a queued status with a generated ID — don't actually download anything yet.

**Why:** Prowlarr validates the XML format during indexer setup even when no results exist. Sonarr validates the addfile response format when testing the download client. Returning correct-format empty/stubbed responses lets users configure FunkArr in their *arr stack now, before Search and Download are built.

### Decision 6: Download queue and history as in-memory state in DownloadApi

**Choice:** `DownloadApi` maintains a `ConcurrentDictionary` for queue items and history items, scoped as a singleton service. No persistence, no actors.

**Why:** This is temporary scaffolding — the real queue will live in the Download domain's actors with event-sourced persistence. For now, in-memory state is sufficient to make Sonarr's download client validation pass. It reports correct queue/history format, and items survive within a single process lifetime.

## Risks / Trade-offs

**[Risk] Newznab XML format doesn't match what Prowlarr expects** → Mitigation: Test against MediathekArr's known-working output format. The spec tests will validate XML structure element-by-element.

**[Risk] In-memory download state lost on restart** → Acceptable for now. This is scaffolding that will be replaced by actor persistence when the Download domain is built. Sonarr handles disappeared downloads gracefully (marks as failed).

**[Risk] Stubbed search returns confuse users** → The caps XML declares supported search types. Empty results are normal for a new indexer with no content indexed yet. Users will see results once the Search domain is wired in.
