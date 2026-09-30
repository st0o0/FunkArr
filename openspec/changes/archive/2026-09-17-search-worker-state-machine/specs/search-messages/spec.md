## MODIFIED Requirements

### Requirement: RuleSet response types use abstract record base

FunkArr.Messages.RuleSet SHALL define an `abstract record RuleSetResponse` as the base for all RuleSet resolution responses. `RuleSetResolved` and `RuleSetFailed` SHALL extend `RuleSetResponse`. The `IRuleSetResponse` marker interface SHALL be removed.

#### Scenario: RuleSetResponse base type

- **WHEN** a caller uses `Ask<RuleSetResponse>` to resolve a RuleSet
- **THEN** the result SHALL be pattern-matched on `RuleSetResolved` or `RuleSetFailed`

### Requirement: RuleSetFailed replaces RuleSetNotFound

FunkArr.Messages.RuleSet SHALL define a `RuleSetFailed` sealed record containing `Cause (Exception)`, replacing `RuleSetNotFound(string TopicOrAlias)`. A custom `RuleSetNotFoundException` SHALL carry the `TopicOrAlias` information. `RuleSetFailed` SHALL extend `RuleSetResponse`.

#### Scenario: RuleSet not found

- **WHEN** the RuleSetResolver cannot find a matching ruleset for topic "Unknown Show"
- **THEN** it SHALL return `RuleSetFailed(new RuleSetNotFoundException("Unknown Show"))`

#### Scenario: RuleSet resolution error

- **WHEN** the RuleSet resolution fails due to an unexpected exception
- **THEN** the PipeTo failure handler SHALL return `RuleSetFailed(ex)` with the original exception

#### Scenario: RuleSetNotFoundException carries topic

- **WHEN** a `RuleSetFailed` is received with `Cause` of type `RuleSetNotFoundException`
- **THEN** the exception SHALL expose `TopicOrAlias` property with the original search term

### Requirement: RuleSetResolverState.Resolve returns typed response

The `RuleSetResolverState.Resolve()` method SHALL return `RuleSetResponse` instead of `object`, enabling compile-time type safety.

#### Scenario: Typed resolve return

- **WHEN** `RuleSetResolverState.Resolve()` is called
- **THEN** it SHALL return either `RuleSetResolved` or `RuleSetFailed`, both subtypes of `RuleSetResponse`
