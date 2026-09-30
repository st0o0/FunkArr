## 1. Backend: Structured Detail Models

- [x] 1.1 Replace `RuleSetDetailRule` in `FunkArr.Messages/RuleSet/QueryRuleSetDetail.cs` with structured record: `Strategy` as `IdentificationStrategy` enum, `Filters` as structured filter output (nullable), `TitleRules` as structured title rule output array (nullable), plus `SeasonRegex`/`EpisodeRegex`/`CaptureGroup` flat fields. Remove `MatchMode`, `FilterSummary`, and `TitleParts` string fields.
- [x] 1.2 Create filter/title-rule output records in `FunkArr.Messages`: `FilterGroupOutput` (`All`/`Any`/`Not` arrays of `FilterConditionOutput`), `FilterConditionOutput` (`Field` as `FilterField`, `Op` as `FilterOp`, `Value` as string), `TitleRuleOutput` (`Type` as `TitlePartType`, `Field` as `FilterField?`, `Pattern`/`Value` as string?, `CaptureGroup` as int?).
- [x] 1.3 Replace `RuleSetDetailRule` in `FunkArr.Api/Models/RuleSetDetail.cs` with matching structured API record using the same shape.

## 2. Backend: Mapping and State

- [x] 2.1 Rewrite `ToDetailRules()` in `RuleSetManagerState.cs`: decompose `IdentificationSpec` back to flat fields (`SeasonRegex`, `EpisodeRegex`, `CaptureGroup`), map `FilterSpec` to `FilterGroupOutput`, map `TitlePart[]` to `TitleRuleOutput[]`. Remove `MatchModeFromStrategy()`, `SummarizeFilters()`, and `FormatTitlePart()`.
- [x] 2.2 Update `RuleSetMappingExtensions.cs` to map from the new structured Messages record to the new structured Api record.

## 3. Backend: Remove /raw Endpoint

- [x] 3.1 Remove `HandleGetRaw` method and its route registration from `RuleSetApiEndpoints.cs`.
- [x] 3.2 Update the endpoint file structure comment/list in `RuleSetApiEndpoints` to no longer reference `/raw`.

## 4. Backend: Build and Test

- [x] 4.1 Fix any compilation errors from the model changes across the solution (check `TestScoreMappingExtensions`, `RuleSetMergerTests`, etc.).
- [x] 4.2 Run `dotnet build src/FunkArr.slnx` and `dotnet format src/FunkArr.slnx --verify-no-changes`.
- [x] 4.3 Run `dotnet run --project src/FunkArr.RuleSet.Tests/FunkArr.RuleSet.Tests.csproj` and `dotnet run --project src/FunkArr.Api.Tests/FunkArr.Api.Tests.csproj` — fix any assertion failures from the model change.

## 5. Frontend: Shared Strategy Utility

- [x] 5.1 Create `src/FunkArr.UI/src/utils/strategy.ts` exporting `strategyLabel(strategy: string, t: (key: string) => string): string` mapping wire enum names to i18n keys. Use existing builder i18n keys.
- [x] 5.2 Update `RuleSetBuilder.vue` to import and use the shared `strategyLabel` instead of the local function.

## 6. Frontend: Types and API Client

- [x] 6.1 Update `RuleSetDetailRule` in `rulesets.ts`: replace `matchMode`, `filterSummary`, `titleParts: string[]` with `filters: FilterGroupOutput | null`, `titleRules: TitleRuleOutput[] | null`, `seasonRegex: string | null`, `episodeRegex: string | null`, `captureGroup: number | null`. Add `FilterGroupOutput`, `FilterConditionOutput`, `TitleRuleOutput` types.
- [x] 6.2 Remove `getRuleSetRaw()` function and `RuleSetWriteRequest`/`RuleSetWriteRule` types that were only used for raw fetching (keep them if still used for write serialization).

## 7. Frontend: Detail-View Update

- [x] 7.1 Update `RuleSetDetail.vue`: replace `rule.strategy` raw display with `strategyLabel()` call, remove `matchMode` template block, replace `filterSummary` display with a structured filter rendering section (inline or component), replace `titleParts.join()` with structured title rule rendering.
- [x] 7.2 Create a `FilterConditionDisplay.vue` component (or inline in Detail-View) that renders `FilterGroupOutput` as grouped field/op/value rows under all/any/not labels.

## 8. Frontend: Builder Hydration

- [x] 8.1 Update `RuleSetBuilder.vue` `onMounted`: fetch from `getRuleSetDetail(editId)` instead of `getRuleSetRaw(editId)`. Map the structured detail response fields to the form state (strategy is already wire name, filters/titleRules are structured).

## 9. Frontend: Debugger and Scoring Views

- [x] 9.1 Update `DebuggerPanel.vue` and `LiveMatchPreview.vue` to use shared `strategyLabel()` for strategy display where applicable.
- [x] 9.2 Update `ScoringDetail.vue` to use shared `strategyLabel()` for identification trace strategy display.

## 10. Frontend: i18n Cleanup

- [x] 10.1 Remove `matchMode` key from `en.json` and `de.json` locale files.

## 11. Verify

- [x] 11.1 Build frontend (`pnpm build` or dev server) and verify no TypeScript errors.
- [x] 11.2 Start dev environment and verify Detail-View renders strategy labels, filters, and title rules correctly with i18n.
- [x] 11.3 Verify Builder edit mode loads from Detail endpoint and populates form correctly.
