## 1. Core model changes

- [x] 1.1 Add required `string Id` property to `Rule` record in `src/FunkArr.Core/RuleSet/RuleSetModels.cs`
- [x] 1.2 Redesign `OverrideConfig` record: replace `Mode`/`Base`/`Add`/`Remove` with `RuleSetLayer Base`, `IReadOnlyList<Rule> Add`, `IReadOnlyList<Rule> Replace`, `IReadOnlyList<string> Remove`
- [x] 1.3 Add `RuleSetLayer` enum (`Community`, `Generated`, `Local`) to `RuleSetModels.cs`
- [x] 1.4 Delete `OverrideMode` enum from `RuleSetModels.cs`

## 2. RuleSetMerger

- [x] 2.1 Create `src/FunkArr.Core/RuleSet/EffectiveRuleSet.cs` with `EffectiveRuleSet(IReadOnlyList<EffectiveRule>, string? Topic, IReadOnlyList<string>? Channels, string? Source)` and `EffectiveRule(Rule Rule, RuleSetLayer Layer)` records
- [x] 2.2 Create `src/FunkArr.Core/RuleSet/RuleSetMerger.cs` with static `Resolve(RuleSetFile? community, RuleSetFile? generated, RuleSetFile? local) → EffectiveRuleSet` implementing merge chain walk
- [x] 2.3 Add static `Validate(OverrideConfig overrides, IReadOnlyList<Rule> baseRules) → IReadOnlyList<ValidationError>` to `RuleSetMerger` with all six validation rules
- [x] 2.4 Create `ValidationError(string Message, string? RuleId)` record in `RuleSetMerger.cs`

## 3. Community ruleset IDs

- [x] 3.1 Add `"id"` field to all rules in all 61 community ruleset JSON files under `data/community/rulesets/`. Use descriptive slugs based on strategy/purpose (e.g., `"se-main"`, `"airdate"`, `"title-exact"`, `"abs-ep-primary"`, `"abs-ep-fallback"`, `"movie-title"`, `"movie-original"`)
- [x] 3.2 Update `data/community/ruleset.schema.json` to make `id` required on Rule

## 4. Generator updates

- [x] 4.1 Update `RuleSetGenerator.Generate()` in `src/FunkArr.Core/Generation/RuleSetGenerator.cs` to assign `Id = $"gen-{index}"` to each generated rule
- [x] 4.2 Update `RuleSetGenerator.GenerateForMovie()` to assign IDs to generated movie rules

## 5. Actor state integration

- [x] 5.1 Update `MediaRuleSetActorState.RecomputeEffectiveRules()` to call `RuleSetMerger.Resolve(community, generated, local)` and store the `EffectiveRuleSet`
- [x] 5.2 Replace `ActiveRuleSource` (string) with provenance derived from `EffectiveRuleSet` in `MediaRuleSetActorState`
- [x] 5.3 Update `MediaRuleSetActor` to pass provenance info in `RuleSetResponse` and validate overrides on `ApplyLocalOverride`

## 6. Tests

- [x] 6.1 Create `FunkArr.Tests/RuleSet/RuleSetMergerTests.cs` — test Resolve: standalone layers, merge with Add/Replace/Remove, three-layer chain, backward compat (no overrides), all-null, priority sorting
- [x] 6.2 Create `FunkArr.Tests/RuleSet/RuleSetMergerValidationTests.cs` — test all six validation rules: Replace-not-found, Add-collision, Remove-not-found, multi-list conflict, duplicate IDs, cycle detection, valid passes
- [x] 6.3 Update `FunkArr.Tests/RuleSet/RuleSetModelTests.cs` — add Id to test rules, update round-trip tests
- [x] 6.4 Update `FunkArr.Tests/RuleSet/RuleSetGeneratorTests.cs` — verify generated rules have IDs
- [x] 6.5 Update `FunkArr.Tests/RuleSet/CommunityRulesetFilesTests.cs` — verify all community rules have non-empty unique IDs
- [x] 6.6 Update `FunkArr.Tests/RuleSet/MediaRuleSetActorStateTests.cs` — test RecomputeEffectiveRules via RuleSetMerger

## 7. Verification

- [x] 7.1 Run `dotnet build FunkArr.slnx` — must compile clean
- [x] 7.2 Run all tests — must pass
- [x] 7.3 Run `dotnet format` on all changed .cs files
