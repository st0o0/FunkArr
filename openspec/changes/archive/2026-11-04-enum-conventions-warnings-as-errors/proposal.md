## Why

Persistence enums are stored in the Akka journal, but 8 of 11 have no explicit values, so reordering or inserting a member silently changes stored data. Enum-to-enum mapping partly relies on `(TTarget)(int)source` casts (History, Api), which couples ordinals across type worlds without compiler protection. The build also carries ~529 suppressed/unfixed `xUnit1051` warnings and one `CS8604`, so warnings are not enforced.

## What Changes

- All enums in `FunkArr.Persistence` get explicit `int` values, frozen to their current ordinals (starting at 0). `PersistedSearchSource` stays as is (`Feed = 4`).
- Enum-to-enum mapping (Messages <-> Persistence, Api <-> Messages) uses exhaustive `switch` expressions with a throwing default arm. All `(TEnum)(int)x` conversions between enum types are replaced:
  - `FunkArr.History/PersistenceMapping.cs` (12 casts)
  - `FunkArr.Api` (~35 casts across mapping extensions and endpoints, 11 enum pairs)
  - `FunkArr.ArrApi/Sabnzbd/SabnzbdDownloadService.cs` priority int parse (1 cast)
- Fix all `xUnit1051` warnings in every test project by passing `TestContext.Current.CancellationToken`; remove `xUnit1051` from `NoWarn` in `src/Directory.Build.props`.
- Fix `CS8604` in `FunkArr.ArrApi/Sabnzbd/SabnzbdResponseMapper.cs` (`HistorySlot.FailMessage` may be null).
- Enable `TreatWarningsAsErrors` in `src/Directory.Build.props` (last step, after the build is warning-free). This also makes CS8509 (non-exhaustive switch) a build error.
- Update `AGENTS.md` with the persistence-enum rule, the switch-only mapping rule, warnings-as-errors, and the test cancellation-token rule.

## Capabilities

### New Capabilities
- `persistence-enum-values`: Persistence enums declare explicit, frozen `int` values for every member.
- `enum-mapping-switch`: Enum-to-enum conversions use exhaustive switch expressions with a throwing default, never numeric casts.
- `warnings-as-errors`: The solution builds warning-free with `TreatWarningsAsErrors` enabled and no blanket `NoWarn` for xUnit analyzers.

### Modified Capabilities
- `cancellation-token-flow`: Add requirement that test code passes `TestContext.Current.CancellationToken` to token-accepting calls.

## Impact

- Code: `FunkArr.Persistence` (8 enum files), `FunkArr.History`, `FunkArr.Api`, `FunkArr.ArrApi` mapping code; all `*.Tests` projects and `FunkArr.IntegrationTests` (largest share of xUnit1051).
- Build: `src/Directory.Build.props` (`NoWarn` removed, `TreatWarningsAsErrors` added). Future warnings fail the build and CI.
- Docs: `AGENTS.md`; optionally the `funkarr-message` skill.
- No stored-data change: values are frozen to current ordinals, so existing journals stay readable.
- Conflict risk: uncommitted working-tree changes and open changes `rewrite-internal-api`, `download-queue-push`, `fix-internal-api-bugs` touch overlapping test and Api files.
