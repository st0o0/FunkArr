## Context

FunkArr's foundation re-architecture (§11) identified dead code that survived prior migrations. This slice removes it before any structural changes, reducing noise for subsequent slices. All deletions are verified unreachable — no production caller exists for any target.

## Goals / Non-Goals

**Goals:**
- Remove all dead code identified in the design doc §11 delete list
- Remove DI registrations and test references for deleted types
- Build compiles and all remaining tests pass after every deletion

**Non-Goals:**
- Deleting `*Service` types (`RuleSetGenerationService`, `SetupValidationService`, `ApiKeyValidationService`) — these need replacement implementations first (later slices)
- Moving code to new locations (later slices handle `Shared/` contents migration)
- Any new code, abstractions, or refactoring

## Decisions

1. **Delete in dependency order, not alphabetical.** Start with leaf deletions (types nothing depends on), then work inward. This keeps the build green after each step.

2. **`Shared/` deletion is partial in Slice 0.** Only delete files within `Shared/` that are genuinely dead. Files that are still referenced (e.g., `Models/`, `ContentFilter`, `FileService`) stay until later slices move them to their proper locations. If `Shared/` becomes empty after dead code removal, delete the folder; otherwise leave it.

3. **Remove DI registrations inline with type deletion.** When deleting a type, also remove its `AddSingleton`/`AddScoped`/`AddTransient` call in setup. This prevents build errors from dangling registrations.

4. **Delete test files that test deleted code.** `QualityProbeService` tests, the unreachable matching engine tests, and the fake controller tests all go. Snapshot verified files for deleted tests are also removed.

## Risks / Trade-offs

- **Risk: A "dead" type is actually called via reflection or Akka message routing.** → Mitigation: Verify with grep before each deletion. The build + test run after each step catches anything missed.
- **Risk: `Shared/` contains files used by code outside the obvious dependency graph.** → Mitigation: Only delete files confirmed dead by grep. Leave anything with references for later slices.
