# scoring-trace-persistence

## Purpose

Formerly defined persistence DTOs for scoring traces, versioning rules, JSON property stability, mapping between Message records and persistence DTOs, and golden-file snapshot tests. These concerns have been superseded by the actor-state-management capability (persistence records in `FunkArr.Persistence/Events/`, Akka default serializer, and serializer roundtrip tests).

## Requirements

No active requirements. All former requirements have been removed:

- **Persistence DTOs for scoring trace are separate from Messages** -- replaced by persistence records in `FunkArr.Persistence/Events/`. See actor-state-management.
- **Persistence DTOs use stable JSON property names** -- JSON property stability is handled by Akka's default serializer. No custom serializer at 0.x.
- **Persistence DTOs have version tracking** -- not needed at 0.x, breaking changes acceptable. Will be addressed post-1.0 if needed.
- **JSON snapshot tests verify serialization stability** -- replaced by serializer roundtrip tests. See actor-state-management.
- **Mapping between Messages and Persistence DTOs** -- eliminated. `ProcessCommand` on state produces persistence records directly. See actor-state-management.
