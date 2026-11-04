## ADDED Requirements

### Requirement: Domain implementations live in their domain project
Implementation classes that serve a single domain SHALL live in that domain's project, not in Core. Core SHALL only contain cross-domain contracts (interfaces, shared types).

#### Scenario: RouteResolver in FunkArr.Download
- **WHEN** `RouteResolver` (the `IRouteResolver` implementation) is needed
- **THEN** it SHALL be defined in `FunkArr.Download`, not `FunkArr.Core`
- **AND** `IRouteResolver` interface and `ResolvedRoute` record SHALL remain in `FunkArr.Core`

#### Scenario: RouteResolver DI registration
- **WHEN** `RouteResolver` is registered in DI
- **THEN** registration SHALL be in `DownloadSetupContainer`, not `CoreSetupContainer`

### Requirement: Interfaces without cross-domain consumers live in their domain
Interfaces defined in Core that have both implementation and all consumers in a single domain SHALL be moved to that domain project.

#### Scenario: IRuleSetValidator in FunkArr.RuleSet
- **WHEN** `IRuleSetValidator` is referenced
- **THEN** it SHALL be defined in `FunkArr.RuleSet` alongside its implementation and consumer
- **AND** it SHALL NOT exist in `FunkArr.Core`
