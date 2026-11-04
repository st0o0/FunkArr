## ADDED Requirements

### Requirement: Entity actor PersistenceId uses field keyword pattern
Entity actors (sharded or local region) that receive their entity ID via constructor SHALL define PersistenceId using the C# 14 `field` keyword pattern: `public override string PersistenceId => $"prefix-{field}";` where the constructor assigns the raw entity ID to the backing field.

#### Scenario: DownloadWorker uses field pattern
- **WHEN** `DownloadWorker` defines its `PersistenceId`
- **THEN** it SHALL use `=> $"download-{field}"` with the constructor assigning to the backing field
- **AND** it SHALL NOT use the `{ get; }` auto-property pattern

#### Scenario: Convention documented in AGENTS.md
- **WHEN** a developer looks up the PersistenceId convention in AGENTS.md
- **THEN** the Akka conventions section SHALL document the `field` keyword pattern for entity actors and the literal pattern for singletons
