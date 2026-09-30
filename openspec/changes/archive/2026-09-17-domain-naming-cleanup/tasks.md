## 1. Scoring Domain — Project Rename

- [x] 1.1 `git mv src/FunkArr.MatchMagic src/FunkArr.Scoring` — move directory
- [x] 1.2 Rename csproj: `FunkArr.MatchMagic.csproj` → `FunkArr.Scoring.csproj`, update `<RootNamespace>` and `<AssemblyName>`
- [x] 1.3 `git mv src/FunkArr.MatchMagic.Tests src/FunkArr.Scoring.Tests` — move test directory
- [x] 1.4 Rename test csproj: `FunkArr.MatchMagic.Tests.csproj` → `FunkArr.Scoring.Tests.csproj`, update `<RootNamespace>`, `<AssemblyName>`, and `<ProjectReference>`
- [x] 1.5 Update `FunkArr.slnx` — replace MatchMagic project paths with Scoring

## 2. Scoring Domain — Type Renames

- [x] 2.1 Rename `MatchMagicManager` → `ScoringManager` (class + file)
- [x] 2.2 Rename `MatchMagicManagerState` → `ScoringManagerState` (record + file)
- [x] 2.3 Rename `MatchMagicManagerStateExtensions` → `ScoringManagerStateExtensions` (class + file)
- [x] 2.4 Rename `MatchMagicActor` → `ScoringActor` (class + file)
- [x] 2.5 Rename `MatchHistoryWorker` → `ScoringHistoryWorker` (class + file)
- [x] 2.6 Rename `MatchHistoryState` → `ScoringHistoryState` (record + file)
- [x] 2.7 Rename `MatchHistoryStateExtensions` → `ScoringHistoryStateExtensions` (class + file)
- [x] 2.8 Replace all `namespace FunkArr.MatchMagic` → `namespace FunkArr.Scoring` across .cs files
- [x] 2.9 Replace all `using FunkArr.MatchMagic` → `using FunkArr.Scoring` across .cs files

## 3. Scoring Domain — Actor Keys and Wiring

- [x] 3.1 Rename `IMatchMagicManager` → `IScoringManager` in `ActorKeys.cs`
- [x] 3.2 Rename `IMatchHistoryRegion` → `IScoringHistoryRegion` in `ActorKeys.cs`
- [x] 3.3 Rename `MatchHistoryOptions` → `ScoringHistoryOptions` (class + file), update `SectionName` to `"FunkArr:ScoringHistory"`
- [x] 3.4 Update `AkkaSetupContainer.cs` — actor registration names: `"match-magic-manager"` → `"scoring-manager"`, `"match-history"` → `"scoring-history"`, type references
- [x] 3.5 Update `RuleSetSetupContainer.cs` — `MatchHistoryOptions` → `ScoringHistoryOptions` reference
- [x] 3.6 Update PersistenceId in ScoringHistoryWorker: `"match-history-{id}"` → `"scoring-history-{id}"`

## 4. Scoring Domain — Persistence Events

- [x] 4.1 `git mv src/FunkArr.Persistence/Events/MatchHistory src/FunkArr.Persistence/Events/ScoringHistory` — move directory
- [x] 4.2 Replace `namespace FunkArr.Persistence.Events.MatchHistory` → `namespace FunkArr.Persistence.Events.ScoringHistory`
- [x] 4.3 Update all `using FunkArr.Persistence.Events.MatchHistory` references

## 5. Scoring Domain — Tests

- [x] 5.1 Replace `namespace FunkArr.MatchMagic.Tests` → `namespace FunkArr.Scoring.Tests` in all test files
- [x] 5.2 Update test type references (MatchMagicManager → ScoringManager, etc.)
- [x] 5.3 Update `<ProjectReference>` in any project referencing `FunkArr.MatchMagic`

## 6. Enrichment Domain — Project Rename

- [x] 6.1 `git mv src/FunkArr.MetadataResolver src/FunkArr.Enrichment` — move directory
- [x] 6.2 Rename csproj: `FunkArr.MetadataResolver.csproj` → `FunkArr.Enrichment.csproj`, update `<RootNamespace>` and `<AssemblyName>`
- [x] 6.3 `git mv src/FunkArr.MetadataResolver.Tests src/FunkArr.Enrichment.Tests` — move test directory
- [x] 6.4 Rename test csproj: `FunkArr.MetadataResolver.Tests.csproj` → `FunkArr.Enrichment.Tests.csproj`, update `<RootNamespace>`, `<AssemblyName>`, and `<ProjectReference>`
- [x] 6.5 Delete `src/FunkArr.MetadataMatching.Tests` project (empty shell)
- [x] 6.6 Update `FunkArr.slnx` — replace MetadataResolver project paths with Enrichment, remove MetadataMatching.Tests

## 7. Enrichment Domain — Type Renames

- [x] 7.1 Rename `MetadataResolverManager` → `EnrichmentManager` (class + file)
- [x] 7.2 Rename `TmdbResolverActor` → `TmdbEnrichmentActor` (class + file)
- [x] 7.3 Rename `TvdbResolverActor` → `TvdbEnrichmentActor` (class + file)
- [x] 7.4 Rename `EpisodeResolver` → `EpisodeEnricher` (class + file)
- [x] 7.5 Rename `MovieResolver` → `MovieEnricher` (class + file)
- [x] 7.6 Replace all `namespace FunkArr.MetadataResolver` → `namespace FunkArr.Enrichment` across .cs files
- [x] 7.7 Replace all `using FunkArr.MetadataResolver` → `using FunkArr.Enrichment` across .cs files

## 8. Enrichment Domain — Messages Namespace

- [x] 8.1 `git mv src/FunkArr.Messages/MetadataResolver src/FunkArr.Messages/Enrichment` — move directory
- [x] 8.2 Replace `namespace FunkArr.Messages.MetadataResolver` → `namespace FunkArr.Messages.Enrichment`
- [x] 8.3 Replace all `using FunkArr.Messages.MetadataResolver` → `using FunkArr.Messages.Enrichment` across .cs files

## 9. Enrichment Domain — Actor Keys and Wiring

- [x] 9.1 Rename `IMetadataResolver` → `IEnrichmentManager` in `ActorKeys.cs`
- [x] 9.2 Update `AkkaSetupContainer.cs` — registration name `"metadata-resolver"` → `"enrichment-manager"`, type references
- [x] 9.3 Update `MetadataSetupContainer.cs` — using/type references

## 10. Enrichment Domain — Tests

- [x] 10.1 Replace `namespace FunkArr.MetadataResolver.Tests` → `namespace FunkArr.Enrichment.Tests` in all test files
- [x] 10.2 Update test type references (MetadataResolverManager → EnrichmentManager, etc.)
- [x] 10.3 Update `<ProjectReference>` in any project referencing `FunkArr.MetadataResolver`

## 11. Cross-Cutting Updates

- [x] 11.1 Update architecture tests (`ArchitectureSpec.cs`) for new namespace/naming patterns
- [x] 11.2 Update `CLAUDE.md` solution structure section
- [x] 11.3 Update Docker compose / appsettings config section `FunkArr:MatchHistory` → `FunkArr:ScoringHistory`

## 12. Verify

- [x] 12.1 `dotnet build src/FunkArr.slnx` — clean build
- [x] 12.2 `dotnet format src/FunkArr.slnx --verify-no-changes` — formatting check
- [x] 12.3 Run all test projects
