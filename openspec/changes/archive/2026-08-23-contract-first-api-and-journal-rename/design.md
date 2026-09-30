## Context

FunkArr has three wire boundaries: persistence journal (Akka events), SABnzbd JSON API (external protocol), and Newznab XML API (external protocol), plus an internal REST API for the frontend. The persistence layer is well-isolated but uses "DTO" naming and static mapping classes. The internal REST API has domain-type leaks — MatchIntelligenceController exposes `MatchRecord`, `TopicStats`, and `MatchQualityWorker.UnmatchedGroup` directly; RulesetController exposes `RuleSetFile` directly. No contract tests exist for any boundary.

## Goals / Non-Goals

**Goals:**
- Replace "DTO" terminology with "Journal" for persistence types — precise naming for Akka journal entries
- Replace static mapping classes with extension methods — consistent with existing Diagnostics pattern, cleaner call-sites
- Introduce OpenAPI spec-first workflow for internal REST API with NSwag code generation
- Fix domain-type leaks in MatchIntelligenceController and RulesetController
- Add Verify-based contract tests for all three wire boundaries
- Modular OpenAPI specs (one file per feature area, composed via `$ref`)

**Non-Goals:**
- Core library extraction — single-project approach stays
- OpenAPI specs for SABnzbd or Newznab — they implement external protocols
- Changes to domain types, actor messages, or events
- Changes to SABnzbd response types — they're already clean
- Frontend changes — JSON shapes must remain identical

## Decisions

### D1: "Journal" naming for persistence types

Rename `*Dto` → plain names in `FunkArr.Persistence` namespace. The namespace itself communicates that these are persistence-layer types.

| Current | New |
|---------|-----|
| `QueueJobEnqueuedDto` | `QueueJobEnqueued` (in `FunkArr.Persistence`) |
| `DcJobAcceptedDto` | `DcJobAccepted` (in `FunkArr.Persistence`) |
| `RequestCreatedDto` | `RequestCreated` (in `FunkArr.Persistence`) |
| `MatchRecordedDto` | `MatchRecorded` (in `FunkArr.Persistence`) |
| `*EventDtos.cs` | `*Journal.cs` |

**Name collision risk**: Domain events like `QueueCoordinatorEvents.JobEnqueued` and persistence type `QueueJobEnqueued` don't collide — the domain events are nested in their container classes, persistence types are in `FunkArr.Persistence` namespace. Actors already import `FunkArr.Persistence` so the using statement doesn't change.

**Alternative considered**: Keeping a suffix like `Wire`, `Stored`, or `Envelope`. Rejected because the namespace already provides the context, and njord uses the same pattern (types named by what they represent, namespace says where they live).

### D2: Extension methods for all mapping

Replace static mapping classes with extension methods in the same file as the journal types.

```
// Current call-site (verbose):
Persist(DownloadCoordinatorEventDtoMapping.ToDto(evt), _ => ...);
Recover<DcJobAcceptedDto>(dto => ApplyAccepted(DownloadCoordinatorEventDtoMapping.ToDomain(dto)));

// New call-site (fluent):
Persist(evt.ToJournal(), _ => ...);
Recover<DcJobAccepted>(j => ApplyAccepted(j.ToDomain()));
```

Extensions live in the same file as the journal types they extend (no separate `*Extensions.cs` file needed — keeps journal type and its mapping together, same as today's pattern where DTOs and mapping share a file).

For the API contract layer, same pattern: `ContractMappingExtensions.cs` with `domain.ToContract()` methods.

### D3: Modular OpenAPI specs with NSwag

**Spec structure** — one root spec that composes feature-specific files:

```
openapi/
├── funkArr-v1.yaml           # Root: info, servers, security, $ref to features
├── queue.yaml                # /api/v1/queue, /api/v1/history
├── setup.yaml                # /api/v1/setup/*
├── rulesets.yaml              # /api/v1/rulesets/*
└── match-intelligence.yaml    # /api/v1/matches/*
```

Each feature file defines its own paths and schemas. The root file uses `$ref` to compose them. Schemas in each file define the contract types for that feature.

**NSwag configuration** — MSBuild-integrated code generation via `NSwag.ApiDescription.Client` NuGet package. Configuration in `nswag.json` at `src/FunkArr/`. Generates C# records into `Api/Generated/Contracts.g.cs` with namespace `FunkArr.Api.Contracts`. The generated file is checked into git (not gitignored) so builds don't require NSwag tooling at CI time.

**Alternative considered**: Kiota (Microsoft) — better for client generation, less suited for server-side contract types. OpenAPI Generator — Java dependency, heavier build chain. NSwag is .NET-native and generates clean C# types.

### D4: SABnzbd responses remain hand-written

SABnzbd response types in `Api/Models/SabnzbdResponses.cs` stay untouched. They implement an external protocol where the wire format is defined by SABnzbd, not by us. These types use `[JsonPropertyName]` with SABnzbd-compatible snake_case keys. Moving them to generated types would add complexity without benefit — the spec is SABnzbd's, not ours.

They move from `Api/Models/` to `Api/Contracts/Sabnzbd/` for namespace consistency but remain hand-written.

### D5: Contract tests with Verify snapshots

Three test families, all using Verify for snapshot-based approval testing:

**Journal round-trip tests** (`Contracts/JournalRoundTripSpec.cs`):
- For each persistence actor domain: create domain event → `ToJournal()` → serialize to JSON → deserialize → `ToDomain()` → assert equality
- Verify snapshot of the serialized JSON (catches accidental wire format changes)
- Unknown-field tolerance: deserialize JSON with extra fields → no exception

**SABnzbd contract tests** (`Contracts/SabnzbdContractSpec.cs`):
- Build each response type with representative data → serialize → Verify snapshot
- Covers: version, config, queue, history, addfile, error responses

**Newznab contract tests** (`Contracts/NewznabContractSpec.cs`):
- Call `NewznabXmlBuilder` methods with representative data → Verify snapshot of XML output
- Covers: caps, tvsearch, movie, text search responses

### D6: Api/Models/ restructuring

Current `Api/Models/` is deleted. Types redistribute as follows:

| Current location | New location | Reason |
|-----------------|-------------|--------|
| `SabnzbdResponses.cs` | `Api/Contracts/Sabnzbd/SabnzbdResponses.cs` | Hand-written, external protocol |
| `ErrorResponse.cs` | `Api/Contracts/ErrorResponse.cs` | Shared across controllers |
| `QueueResponses.cs` | Replaced by generated types | NSwag from `queue.yaml` |
| `RulesetResponses.cs` | Replaced by generated types | NSwag from `rulesets.yaml` |
| `RulesetActionResponses.cs` | Replaced by generated types | NSwag from `rulesets.yaml` |
| `SetupResponses.cs` | Replaced by generated types | NSwag from `setup.yaml` |
| `TestRulesRequest.cs` | Replaced by generated types | NSwag from `rulesets.yaml` |
| `TestRulesResponse.cs` | Replaced by generated types | NSwag from `rulesets.yaml` |

## Risks / Trade-offs

**[Risk] Persistence type name collision** → Domain events use nested records (`QueueCoordinatorEvents.JobEnqueued`), persistence uses flat names (`QueueJobEnqueued` in `Persistence` namespace). No actual collision because domain events are always qualified via their container class. Verified by checking all current usages.

**[Risk] NSwag generated code quality** → Generated records may not match project style (e.g., nullable annotations, record vs class). Mitigated by NSwag configuration options for nullable reference types, record generation, and namespace control. The generated file is `.g.cs` (excluded from format checks by convention).

**[Risk] OpenAPI spec drift from implementation** → The spec becomes source-of-truth but controllers could diverge. Mitigated by: (1) contract tests catch response shape changes, (2) generated types force compile errors if spec changes, (3) Scalar UI continues to show the live spec for visual verification.

**[Risk] Build dependency on NSwag** → NSwag adds a build-time dependency. Mitigated by checking generated code into git — CI and Docker builds don't need NSwag installed, only developers changing the spec re-run generation.

**[Trade-off] Two API contract mechanisms** → SABnzbd stays hand-written while internal API uses generated types. This is intentional — SABnzbd implements an external spec, generating from our own OpenAPI would be circular. The inconsistency is the right trade-off.

**[Trade-off] Journal types share names with domain concepts** → `FunkArr.Persistence.RequestCreated` vs `DownloadRequestTrackerEvents.RequestCreated`. The namespace disambiguation works but requires `using FunkArr.Persistence;` to be present in actor files (which it already is today).
