## Context

Njord (reference project) uses Verify for JSON shape stability on all persistence DTOs. FunkArr has the NuGet package but zero usage. The History domain is severely under-tested with only 4 persistence roundtrip tests, while other domains have 45-131 tests.

This change depends on `cleanup-dead-code-and-renames` (dead types removed, names fixed) and `layer-separation` (Persistence owns its types). The Verify tests will test the Persisted* types from the layer-separation change.

## Goals / Non-Goals

**Goals:**
- Verify JSON shape stability for all Persistence events and snapshots
- Comprehensive History domain test coverage (state, actors, persistence, stats)
- Follow Njord's established Verify pattern exactly
- Catch any future field rename/removal/reorder in persistence types

**Non-Goals:**
- Verify tests for Messages or Api types (future work)
- Testing other under-tested domains like Api (future work)
- Performance testing
- Integration testing with real SQLite

## Decisions

### 1. Serializer: Newtonsoft.Json for Persistence Verify tests

**Decision:** Use `Newtonsoft.Json` with `Formatting.Indented` for Verify shape tests, matching Akka's default persistence serializer.

**Rationale:** The point is to test what Akka actually stores. Akka.Persistence.Sql uses Newtonsoft.Json by default. Testing with System.Text.Json would miss Newtonsoft-specific behaviors (property naming, null handling, enum serialization).

### 2. Test project: New FunkArr.Persistence.Tests

**Decision:** Create a dedicated `FunkArr.Persistence.Tests` project rather than adding Verify tests to existing domain test projects.

**Rationale:** Persistence type shape stability is a cross-cutting concern — it guards the journal contract, not domain logic. A dedicated project makes it clear these tests exist for serialization safety, not business logic.

### 3. Verify pattern: Njord-identical

**Decision:** Copy Njord's exact pattern:
- `ModuleInitializer.cs` with `DiffRunner.Disabled = true`
- `using static VerifyXunit.Verifier;`
- Two tests per type: `Task` shape test (`return Verify(json)`) + `void` roundtrip test (Assert-based)
- `.verified.txt` files next to test classes
- `*.received.*` in `.gitignore`
- `*.verified.txt text eol=lf` in `.gitattributes`

### 4. History tests: Pure state first, then actor TestKit

**Decision:** Prioritize pure state tests (no Akka infrastructure) for `HistoryState` and `StatsCollectorState`. Add actor TestKit tests for `HistoryWorker` and `StatsCollector` behavior that can't be tested through state alone (persistence, recovery, inter-actor communication).

**Rationale:** Pure state tests are fast, deterministic, and test the core logic. Actor tests add value for persistence lifecycle, message routing, and cross-actor interactions.

### 5. Test data: Shared builders for complex types

**Decision:** Create test data builders in `FunkArr.Tests.Shared` for the `ItemTrace` tree and other complex nested types reused across Persistence Verify tests and History tests.

**Rationale:** The existing `ScoringRecordedDtoSnapshotTests.CreateEvent()` builder already demonstrates this pattern. Centralizing avoids duplication and ensures consistent test data.

## Risks / Trade-offs

- **[Risk] Verified files drift if not reviewed in PRs** → Add PR review checklist item for `.verified.txt` changes. Any change to a verified file means the persistence contract changed.
- **[Risk] History actor tests may be flaky with TestKit** → Use `ExpectMsg<T>` with reasonable timeouts. Avoid timing-dependent assertions. Test state logic via pure state tests where possible.
- **[Trade-off] Two tests per type is verbose** → Worth it. The shape test catches shape changes; the roundtrip test catches deserialization issues. They serve different purposes.
