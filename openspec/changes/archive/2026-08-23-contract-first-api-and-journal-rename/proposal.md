## Why

The API boundary leaks domain types into wire contracts (MatchIntelligenceController exposes raw `MatchRecord`, `TopicStats`, and actor-internal `MatchQualityWorker.UnmatchedGroup`; RulesetController returns `RuleSetFile` directly). The persistence layer uses the generic "DTO" suffix when "Journal" would be precise. Static mapping helper classes are verbose at call-sites (`DownloadCoordinatorEventDtoMapping.ToDto(evt)`) while the rest of the codebase already uses extension methods (Diagnostics). There are no contract tests to catch wire format regressions for persistence events, SABnzbd JSON, or Newznab XML.

## What Changes

- **Persistence rename**: `*Dto` types become plain names in `FunkArr.Persistence` namespace (namespace is the context). `*DtoMapping` static classes become `*JournalExtensions` with extension methods (`evt.ToJournal()` / `journal.ToDomain()`). Files rename from `*EventDtos.cs` to `*Journal.cs`.
- **OpenAPI Spec-First for internal REST API**: Modular OpenAPI specs under `/openapi/` (`funkArr-v1.yaml` root + `queue.yaml`, `setup.yaml`, `rulesets.yaml`, `match-intelligence.yaml`). NSwag generates C# contract types as MSBuild step into `Api/Generated/`. Controllers rewired to use generated types with extension-method-based mapping (`Domain.ToContract()`). `Api/Models/` deleted for internal API types.
- **SABnzbd responses stay as-is**: They implement an external protocol with correct `[JsonPropertyName]` attributes. No OpenAPI spec — tested via snapshots instead.
- **Contract tests**: Verify-based snapshot tests for journal event round-trips (all 4 actor domains), SABnzbd JSON wire format, and Newznab XML output.
- **Extension methods over static helpers**: All new mapping code uses extensions, consistent with existing Diagnostics pattern.

## Capabilities

### New Capabilities
- `api-contracts`: OpenAPI spec-first contract generation for internal REST API with NSwag, modular YAML specs, generated C# types, and domain-to-contract mapping extensions.
- `contract-tests`: Verify-based snapshot tests for persistence journal round-trips, SABnzbd JSON wire format, and Newznab XML output stability.

### Modified Capabilities
- `persistence-dtos`: Rename from DTO terminology to Journal terminology, static mapping classes become extension methods. File and type naming changes, no behavioral changes to persistence itself.
- `match-intelligence-api`: Controller stops exposing domain types directly, uses generated contract types instead.
- `ruleset-api`: Controller stops exposing `RuleSetFile` domain type directly for GET/PUT, uses generated contract types instead.
- `queue-api`: Controller already clean, but moves to generated contract types for consistency.

## Impact

- **Persistence**: 4 files renamed, all call-sites in actors updated (DownloadCoordinator, QueueCoordinator, DownloadRequestTracker, MatchQualityWorker). Wire format unchanged — no migration needed.
- **API**: Controllers rewired to generated types. Frontend unaffected if JSON shape stays identical (contract tests enforce this).
- **Build**: NSwag added as MSBuild code-gen dependency. OpenAPI YAML files become source-of-truth for internal API shapes.
- **Tests**: New test files added. Verify package already in use. No existing tests broken.
- **Dependencies**: `NSwag.ApiDescription.Client` added to `Directory.Packages.props`.
