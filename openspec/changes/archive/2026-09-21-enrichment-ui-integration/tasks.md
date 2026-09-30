## 1. Backend Messages & Models

- [x] 1.1 Add EnrichmentTrace record to FunkArr.Messages/Scoring/History/ (method, confidence, enriched, resolvedSeason, resolvedEpisode, resolvedTitle, resolvedYear, daysDiff, detail)
- [x] 1.2 Add optional EnrichmentTrace field to ItemTrace record
- [x] 1.3 Add enrichment config to RuleSetDetailResult message (EnrichmentConfig field via RuleSetManagerState detail building)
- [x] 1.4 TestScoreItems unchanged — enrichment orchestrated in endpoint handler per design (not in ScoringManager)

## 2. API Models

- [x] 2.1 Create EnrichmentConfigInput/Output API models in FunkArr.Api/Models/ (mirroring JSON schema structure)
- [x] 2.2 Add EnrichmentConfigOutput to RuleSetDetail API model
- [x] 2.3 Add optional EnrichmentConfigInput to CreateRuleSetRequest and UpdateRuleSetRequest
- [x] 2.4 Extend TestScoreRequest with optional enrichment config and identity fields (tvdbId, tmdbId, imdbId, mediaType)
- [x] 2.5 Add EnrichmentTraceOutput to ItemTrace API model in TestScoreResponse (method, confidence, enriched, resolvedSeason, resolvedEpisode, resolvedTitle, resolvedYear, daysDiff, detail)

## 3. API Endpoint Wiring

- [x] 3.1 Map enrichment config in RuleSetMappingExtensions.ToApi() for detail response
- [x] 3.2 Include enrichment in SerializeForDisk methods for create/update
- [x] 3.3 Map enrichment trace in TestScoreMappingExtensions for ItemTrace
- [x] 3.4 Build EpisodeCandidate[]/MovieCandidate[] from scored ItemTraces (ConstructedTitle from Identification.Title, AiredAt from Timestamp, ExistingSeason/Episode from Identification, Duration from candidate)
- [x] 3.5 Wire test endpoint to run enrichment after scoring: ask EnrichmentManager with built candidates, merge enrichment results into ItemTraces with EnrichmentTrace, handle timeout gracefully (return scoring-only results on enrichment failure)
- [x] 3.6 Build EnrichmentTrace from EnrichedEpisode/EnrichedMovie results, including failure traces with detail strings for unresolved items

## 4. RuleSet Manager State

- [x] 4.1 Include enrichment config in RuleSetManagerState detail result building (already available via ExtractIdentity)

## 5. Frontend API Types

- [x] 5.1 Add enrichment config TypeScript interfaces to api/rulesets.ts (EnrichmentConfig, TitleMatchConfig, AirdateMatchConfig, RuntimeMatchConfig, YearMatchConfig)
- [x] 5.2 Add EnrichmentTrace interface to api/rulesets.ts (method, confidence, enriched, resolvedSeason, resolvedEpisode, resolvedTitle, resolvedYear, daysDiff, detail)
- [x] 5.3 Extend RuleSetDetail interface with enrichment field
- [x] 5.4 Extend RuleSetWriteRequest with optional enrichment field
- [x] 5.5 Extend TestScoringRequest with optional enrichment config and identity fields
- [x] 5.6 Extend ItemTrace interface with optional enrichmentTrace field

## 6. RuleSet Detail View - Enrichment Display

- [x] 6.1 Add read-only enrichment config section to RuleSetDetail.vue (enabled/disabled badge, active methods as tags, threshold values)
- [x] 6.2 Add i18n keys for enrichment detail labels

## 7. Builder Form - Enrichment Section

- [x] 7.1 Add enrichment form state to RuleSetBuilder reactive form (enabled, methods array, title threshold, airdate tolerance, runtime tolerance, runtime mode, year tolerance)
- [x] 7.2 Add enrichment section template between Default Confidence and Matching Rules (collapsible, expanded by default)
- [x] 7.3 Add enabled toggle control
- [x] 7.4 Add method checkboxes (Title, Airdate, Runtime, Year) with per-method threshold/tolerance controls that only show when method is checked
- [x] 7.5 Add missing external IDs hint (show needs tvdbId, movie needs tmdbId/imdbId)
- [x] 7.6 Load enrichment config from detail API in edit mode onMounted
- [x] 7.7 Include enrichment in serializeForm() output
- [x] 7.8 Add i18n keys for enrichment builder labels

## 8. Debugger - Enrichment Display

- [x] 8.1 Extend LiveMatchPreview props with enrichment config and identity fields from builder state
- [x] 8.2 Include enrichment config and identity in runFullTest request body
- [x] 8.3 Add two-phase loading indicator: "Scoring..." → "Enriching..." when enrichment is enabled
- [x] 8.4 Enrichment trace display integrated directly into LiveMatchPreview (inline, not separate component — simpler for the amount of template)
- [x] 8.5 Integrate enrichment trace into full test results below rule pipeline trace for matched items

## 9. Verify & Format

- [x] 9.1 Run dotnet build and fix compilation errors
- [x] 9.2 Run dotnet format
- [x] 9.3 Run existing tests to verify no regressions (366 tests, 0 failures)
