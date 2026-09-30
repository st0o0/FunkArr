## Context

Two domain projects have names that don't match the vocabulary used in their code:
- `FunkArr.MatchMagic` — every internal type says "Scoring", the project name is a brand name nobody uses in conversation
- `FunkArr.MetadataResolver` — "Resolver" implies match resolution (scoring domain), but the actual domain verb is "enrich" (add TMDB/TVDB metadata to Mediathek items)

Additionally `FunkArr.MetadataMatching.Tests` exists as an empty shell with leftover assembly infos from three previous naming attempts.

## Goals / Non-Goals

**Goals:**
- Align project names, namespaces, actor names, and actor keys with actual domain vocabulary
- Make domain boundaries self-documenting: `Scoring` scores, `Enrichment` enriches
- Remove the dead `MetadataMatching.Tests` project
- Update all references: solution file, project references, setup containers, actor keys, config sections, architecture tests, CLAUDE.md, openspec specs

**Non-Goals:**
- Renaming message types (they already use the right vocabulary)
- Changing `Messages.Scoring` / `Messages.Scoring.History` namespace (already correct)
- Renaming persistence event types (`ScoringRecorded` — already correct)
- Refactoring actor behavior or state logic
- Addressing the `Messages.Mediathek` orphan namespace (separate concern)

## Decisions

### 1. Project + namespace rename via directory move, not in-place

Move directories on disk (`FunkArr.MatchMagic/` → `FunkArr.Scoring/`) rather than renaming csproj files in place. This keeps git history cleaner with `git mv` and avoids stale directory names.

### 2. Persistence IDs stay unchanged

The `PersistenceId` in `MatchHistoryWorker` is `match-history-{ruleSetId}`. The shard region name is `"match-history"`. Both are baked into the SQLite journal. Since this is 0.x with no production data to preserve, we rename both:
- Shard region: `"match-history"` → `"scoring-history"`
- PersistenceId: `match-history-{id}` → `scoring-history-{id}`

Any existing dev databases become stale — acceptable for 0.x.

### 3. Config section rename

`FunkArr:MatchHistory` → `FunkArr:ScoringHistory`. The `SectionName` constant in `MatchHistoryOptions` (renamed to `ScoringHistoryOptions`) drives this. Docker compose / appsettings need updating.

### 4. Persistence event namespace moves, type names stay

`FunkArr.Persistence.Events.MatchHistory` → `FunkArr.Persistence.Events.ScoringHistory` (folder + namespace). The type `ScoringRecorded` keeps its name — it's already correct. Akka.Persistence.Sql uses type manifests that include the full namespace, so existing journal entries won't deserialize — acceptable for 0.x.

### 5. Architecture tests update

ArchUnitNET tests may assert on namespace patterns or naming conventions. These need updating to reflect the new project/namespace names.

## Risks / Trade-offs

- **[Journal data loss]** Renaming persistence IDs and event namespaces orphans existing journal/snapshot data → Acceptable for 0.x, no migration needed
- **[Spec count]** Many specs reference old names in their text → Bulk delta spec updates, mechanical but tedious
- **[Git history]** Directory renames make `git log --follow` less reliable → Use `git mv` for best tracking; acceptable trade-off for clean naming

## Migration Plan

Execution order:
1. Rename directories with `git mv`
2. Update csproj filenames and internal `<RootNamespace>` / `<AssemblyName>`
3. Update solution file (`FunkArr.slnx`)
4. Find-and-replace namespaces across all .cs files
5. Rename types (actor names, state names, actor keys, options)
6. Update Akka registration (setup container)
7. Update config section names
8. Delete `FunkArr.MetadataMatching.Tests`
9. Update architecture tests
10. Update CLAUDE.md
11. `dotnet build` + `dotnet format` + run all tests
12. Update openspec specs with delta files
