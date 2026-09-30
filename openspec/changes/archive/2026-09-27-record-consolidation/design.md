## Context

FunkArr's message and persistence records evolved organically, resulting in flat records with 10-17 positional parameters. The same field groups are duplicated across records: external IDs (TvdbId/ImdbId/TmdbId) appear in 10 records, download media identity (Title/VideoUrl/SubtitleUrl/Channel/Duration/Size/Category) in 4 records, and candidate data is flattened with Candidate* prefixes in ItemTrace/PersistedItemTrace instead of embedding the existing ScoreCandidate record.

This is v0.x -- persistence breaking changes are acceptable. After this change, persistence records become extend-only permanently.

## Goals / Non-Goals

**Goals:**
- Extract five shared record types that capture natural data groupings
- Reduce positional parameter counts (largest: DownloadWorkerState 17 -> ~8, SearchResultItem 17 -> ~10)
- Eliminate manual field-by-field copying at construction sites (e.g., ScoringEngine.cs:71 unpacking ScoreCandidate into ItemTrace)
- Establish a clean persistence baseline before freezing the schema
- Maintain three-tier type separation: Messages records, Persistence records, API models

**Non-Goals:**
- Changing external API contracts (Newznab XML, SABnzbd JSON responses stay identical)
- Introducing shared base types or inheritance hierarchies between message types
- Changing actor behavior or message flow
- Refactoring the Pathfinder state pattern itself

## Decisions

### 1. Shared records live in their respective projects (Messages / Persistence)

Shared message records go in `FunkArr.Messages/Shared/`. Persistence mirrors go in `FunkArr.Persistence/Events/Shared/`. This follows the existing project structure where Messages and Persistence are the two shared contract projects.

**Alternative considered:** A single `FunkArr.Shared` project. Rejected because it would break the existing reference direction and add a new dependency for all domain projects.

### 2. Persistence records get their own copies, not shared with Messages

Each shared group gets both a Messages version and a Persistence version (e.g., `ExternalIds` in Messages, `PersistedExternalIds` in Persistence). This follows the existing three-tier pattern (domain state / message snapshot / persisted record) and keeps persistence independently evolvable after the freeze.

**Alternative considered:** Sharing a single record type between Messages and Persistence. Rejected because it couples persistence schema evolution to message changes, which directly contradicts the extend-only freeze.

### 3. Records are embedded, not inherited

Consuming records embed the shared type as a property (composition). No record inheritance or flattening.

```
// Before: 13 params
record ItemTrace(string CandidateTitle, string CandidateTopic, ..., bool Matched, double Score, ...)

// After: 7 params
record ItemTrace(ScoreCandidate Candidate, bool Matched, double Score, ...)
```

**Alternative considered:** Using record inheritance (ItemTrace extends ScoreCandidate). Rejected because the relationship is "has-a" not "is-a", and inheritance creates serialization complications.

### 4. ExternalIds uses all-nullable fields

Even though `SearchSeries` only has TvdbId+ImdbId (no TmdbId) and `SearchMovie` only has ImdbId+TmdbId (no TvdbId), the shared `ExternalIds` record uses all three as nullable. This avoids splitting into sub-variants and keeps a single type across all domains.

### 5. DownloadCompletion does not embed DownloadId

The `DownloadCompletion` record contains only the completion payload (Title, Category, Size, Status, RelativePath, FailMessage, DownloadTimeSeconds, CompletedAt). The `DownloadId` stays on the consuming records (`RecordDownload`, `HistoryItem`, `DownloadHistoryRecorded`) because it serves as identity/routing, not completion data. This keeps `DownloadCompletion` a pure value object.

### 6. SearchSeries and SearchMovie keep inline ID fields

`SearchSeries` (TvdbId + ImdbId) and `SearchMovie` (ImdbId + TmdbId) do not use ExternalIds. They have partial ID sets specific to each search type and inherit from SearchRequest. Forcing ExternalIds with unused nullable fields would add noise. ExternalIds is used where all three IDs can be present: RuleSet, enrichment outputs, SearchResultItem.

### 7. Implementation order: small to large, cross-cutting first

1. **ExternalIds** -- smallest record (3 fields), most consumers (~10), cross-cutting (Search, RuleSet, Enrichment). Proves the pattern.
2. **DownloadMedia** -- 7 fields, confined to Download domain
3. **DownloadProgress** -- 3 fields, confined to Download domain
4. **DownloadCompletion** -- 8 fields, confined to Download/History domain
5. **ScoreCandidate embedding** -- restructures ItemTrace/PersistedItemTrace in Scoring/History
6. **MatchMetadata** -- extracts optional fields from SearchResultItem

Each step is independently compilable and testable.

## Risks / Trade-offs

**[Nested serialization in Akka persistence]** Embedding records changes the JSON shape in the journal. Since we're wiping the journal anyway (final break), this is acceptable. After this change, the nested shape becomes the permanent schema.
-> Mitigation: Document the journal wipe requirement in release notes.

**[Increased object allocations]** Nested records mean more small objects on the heap. For message records that are short-lived and small, this is negligible.
-> Mitigation: None needed. These are tiny records with no hot-path allocation pressure.

**[Test churn]** Every test that constructs these records needs updating. The Download and ArrApi test projects have the most construction sites.
-> Mitigation: Do each shared record type as a separate task with its own test pass. Builder/factory methods are not worth adding for records with 2-3 params after consolidation.

**[API model mapping]** Internal API models (FunkArr.Api) may need updated mapping from the restructured message records.
-> Mitigation: Update mappings in the same task that restructures the source record.
