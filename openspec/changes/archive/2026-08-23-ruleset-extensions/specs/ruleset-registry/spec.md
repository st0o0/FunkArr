## MODIFIED Requirements

### Requirement: RuleSetRegistryActor renamed to RuleSetCoordinator
The actor previously named `RuleSetRegistryActor` SHALL be renamed to `RuleSetCoordinator` following the Coordinator/Worker/Tracker naming convention. All message types and behavior SHALL remain unchanged. The registration name stays `"ruleset-registry"` for backward compatibility.

#### Scenario: Resolution by new type name
- **WHEN** code resolves `RuleSetCoordinator` via `IActorRegistry`
- **THEN** it SHALL receive the same actor instance previously registered as `RuleSetRegistryActor`
