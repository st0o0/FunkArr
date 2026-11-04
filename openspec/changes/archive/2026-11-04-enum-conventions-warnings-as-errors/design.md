## Context

`FunkArr.Persistence` holds 11 enums that end up in the Akka journal and snapshots. Three declare explicit values (`PersistedDownloadStatus`, `PersistedDownloadPriority`, `PersistedSearchSource`); eight rely on implicit ordinals. Messages enums mirror the Persisted enums, and mapping between them is mixed:

- `FunkArr.Download` and `FunkArr.Feed` use `switch` expressions / ternaries.
- `FunkArr.History/PersistenceMapping.cs` uses `(PersistedX)(int)domainValue` casts, silently coupling Messages ordinals to Persistence ordinals.
- `FunkArr.Api` (`RuleSetMappingExtensions`, `DownloadsApiEndpoints`) and `SabnzbdDownloadService` cast ints/enums to Messages enums.

The build currently has ~529 `xUnit1051` warnings (all test projects; `NoWarn` only hides them in `Directory.Build.props`) and one `CS8604` in `SabnzbdResponseMapper`. `TreatWarningsAsErrors` is not set.

## Goals / Non-Goals

**Goals:**
- Persisted enum values are an explicit, stable contract independent of Messages enum ordering.
- Enum mapping is compiler-checked for exhaustiveness.
- The solution builds warning-free and stays that way.

**Non-Goals:**
- Changing any stored value (existing ordinals are frozen; no journal migration).
- Adding explicit values to Messages or Api enums.
- Adding an analyzer or ArchUnit test to forbid numeric enum casts; the rule lives in `AGENTS.md`.
- Introducing a `.editorconfig` severity override for CS8509.

## Decisions

**1. Explicit values equal current ordinals, starting at 0.**
Writing `Show = 0, Movie = 1` changes nothing at runtime. Starting at 1 (so `default` is invalid) was rejected: it would alter stored values. `PersistedSearchSource` keeps `Feed = 4` and the gap at 3.

**2. Plain `int` underlying type, no `: int` and no `: byte`.**
`int` is the default; the convention is about explicit member values, not the underlying type.

**3. Exhaustive `switch` expressions with `_ => throw new ArgumentOutOfRangeException(nameof(x), x, null)`.**
A throwing default fails loudly if an undefined numeric value reaches the mapper, instead of persisting wrong data. Together with Decision 5, a newly added enum member without an arm fails the build (CS8509). The default arm is still required for out-of-range values, which the compiler cannot rule out.

Alternative considered: keep casts and guard with a test asserting name-for-name equality. Rejected: runtime/test-time protection only, and still couples the two ordinals.

Nullable enums map via `x is { } v ? v.ToPersistence() : null`.

**4. Mapping helpers are extension methods `ToPersistence()` / `ToDomain()` per enum.**
This matches `FunkArr.Download/PersistenceMapping.cs`. History gets the same shape; Api gets `ToMessage()`-style helpers in the existing `*MappingExtensions` files.

**5. `TreatWarningsAsErrors=true` in `src/Directory.Build.props`, not an `.editorconfig` severity.**
CS8509 is a compiler warning; `.editorconfig` `dotnet_analyzer_diagnostic.severity` only affects analyzers (CAxxxx/IDExxxx), not CSxxxx compiler diagnostics. The MSBuild property covers both and also promotes the two existing naming-rule warnings. No special CS8509 handling is needed.

**6. xUnit1051 is fixed, not suppressed.**
Calls to token-accepting methods in tests pass `TestContext.Current.CancellationToken`. `NoWarn xUnit1051` is removed from `Directory.Build.props`. Mechanical, so it can be split per test project.

**7. Sabnzbd priority parse becomes a switch.**
`-1 => Low`, `0 => Normal`, `1 => High`. Out-of-range input keeps the current clamping behavior by clamping before the switch, so the observable result is unchanged.

**8. Ordering.**
Enum values and mappings first, then CS8604, then xUnit1051 per project, and only then `TreatWarningsAsErrors` and removal of `NoWarn`. Flipping the switch earlier breaks every intermediate build.

## Risks / Trade-offs

- [Merge conflicts with uncommitted work and open changes `rewrite-internal-api`, `download-queue-push`, `fix-internal-api-bugs`] -> Land or commit those first, or apply the test-project fixes last and re-run the warning count before each batch.
- [New warnings appearing on other machines/SDK versions break the build] -> Accepted; that is the intent. A new analyzer warning is fixed or explicitly suppressed with a justified, scoped `#pragma`/`NoWarn`.
- [Throwing default arm turns a latent mapping bug into a runtime exception inside an actor] -> Preferable to silent corruption; supervision restarts the actor and the failure is visible in logs.
- [CS8604 fix semantics] -> `HistorySlot.FailMessage` is a non-nullable `string`; the fix supplies `string.Empty` at the mapping site rather than loosening the model, unless the Sabnzbd contract needs a null.
- [`dotnet format --verify-no-changes` interaction] -> Run after each batch; `TreatWarningsAsErrors` does not change formatting output.
- [Large mechanical diff in tests] -> Review per project; no behavior change expected, only an extra argument.
