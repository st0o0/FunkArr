## Context

FunkArr has 11 Persistence enums, all starting with a domain value at position 0.
The Messages layer already follows `Unknown = 0` for newer enums (13 of 22), but
9 older enums and 1 Api enum still lack the sentinel. The existing spec
`domain-enums` explicitly exempts `DownloadPriority` (semantic negative range)
and FailureReason enums. This change removes those exemptions and aligns
everything.

Current state of `DownloadPriority`: `Paused = -2, Low = -1, Normal = 0, High = 1`.
Current exemption for FailureReason enums: 4 single/dual-member enums in
`LocalRuleSetCommands.cs`.

## Goals / Non-Goals

**Goals:**

- Every Persistence enum starts with `Unknown = 0`.
- Every Messages and Api enum starts with `Unknown = 0` (no exemptions).
- `DownloadPriority` and `QueuePriority` renumbered sequentially from 0.
- All enum-to-enum mappings handle the new numbering via exhaustive switches.
- All tests pass with the new values.

**Non-Goals:**

- Data migration for existing journals/snapshots. This is a v0.x break —
  existing data is invalidated.
- Changes to ArrApi enums (already have `Unknown = 0`).
- Changes to enums that already comply (13 Messages enums, 18 Api enums).

## Decisions

### D1: Renumber all Persistence enums, don't preserve old values

**Choice**: Insert `Unknown = 0`, shift all existing members by +1. Gaps are
preserved relatively (e.g. `PersistedSearchSource` gap at 3 → gap at 4).

**Why not preserve old ordinals**: The spec says "frozen values", but we're
explicitly breaking persistence at v0.x. Preserving old values while inserting
`Unknown = 0` would mean `Unknown = -1` or similar — worse than a clean renumber.

**Why not keep existing 0-values as-is and use Unknown = -1**: Negative values
are surprising and break the convention that `default(T) == Unknown`.

### D2: DownloadPriority — sequential renumber (Option B)

**Choice**: `Unknown = 0, Paused = 1, Low = 2, Normal = 3, High = 4`.

**Alternatives considered**:
- Option A (`Unknown = -99`): Keeps semantic sort order but introduces magic number.
- Option C (no Unknown): Inconsistent with the "every enum gets Unknown" rule.

**Trade-off**: Semantic ordering by int value is lost. Sorting by priority must
use explicit ordering logic, not `(int)priority`. This is already the case in
the Api layer (`QueuePriority`) and should be the pattern everywhere — sorting
by enum int values is fragile.

### D3: FailureReason enums get Unknown too

**Choice**: All 4 FailureReason enums in `LocalRuleSetCommands.cs` gain
`Unknown = 0`. The previous exemption ("always specific, 1-2 members") is
removed.

**Why**: Consistency. `default(T)` should always be `Unknown` for every enum.
A FailureReason that is `default` = `AlreadyExists` is misleading.

### D4: Mapping extensions — Unknown handled by default arm

Per existing `enum-mapping-switch` spec: `Unknown` has no explicit arm, it
falls through to `_ => throw new ArgumentOutOfRangeException(...)`. This
remains unchanged. The new `Unknown` members in Persistence enums follow the
same pattern.

### D5: Change order — Persistence first, then Messages, then Api, then mappings

Bottom-up through the reference direction. This way intermediate builds
succeed (mappings reference both sides).

Actual order:
1. Persistence enums (FunkArr.Persistence)
2. Messages enums (FunkArr.Messages)
3. Api enums (FunkArr.Api)
4. Mapping extensions (domain projects + FunkArr.Api/Extensions)
5. Tests

## Risks / Trade-offs

- **[Stored data invalidated]** → Accepted at v0.x. Users must wipe
  journal/snapshot data after upgrade. Document in release notes.
- **[Priority sort broken]** → Any code sorting by `(int)DownloadPriority`
  will produce wrong results. Verify no such sorting exists (DownloadManager
  uses explicit ordering).
- **[Wide file touch]** → ~40-50 files across 6+ projects. Risk of merge
  conflicts if other changes are in-flight. Mitigated by doing this as a
  single focused change.
