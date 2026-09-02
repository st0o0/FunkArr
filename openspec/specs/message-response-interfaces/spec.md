# message-response-interfaces

## Purpose

Domain-level response marker interfaces in FunkArr.Messages that enable typed Ask calls instead of Ask<object>, grouping all response types per domain under a single interface.

## Requirements

### Requirement: Domain-level response marker interfaces

FunkArr.Messages SHALL define one marker interface per domain to group all response types for that domain. Each interface SHALL be an empty interface (no members) defined in the domain's namespace within FunkArr.Messages.

#### Scenario: ISearchResponse interface

- **WHEN** `ISearchResponse` is defined
- **THEN** it SHALL be in namespace `FunkArr.Messages.Search` and `SearchCompleted` and `SearchFailed` SHALL implement it

#### Scenario: IRuleSetResponse interface

- **WHEN** `IRuleSetResponse` is defined
- **THEN** it SHALL be in namespace `FunkArr.Messages.RuleSet` and `RuleSetResolved`, `RuleSetNotFound`, `RuleSetDetailResult`, and `RegisteredRuleSetsResult` SHALL implement it

#### Scenario: IMediathekResponse interface

- **WHEN** `IMediathekResponse` is defined
- **THEN** it SHALL be in namespace `FunkArr.Messages.Mediathek` and `MediathekQueryCompleted` and `MediathekQueryFailed` SHALL implement it

#### Scenario: IScoringResponse interface

- **WHEN** `IScoringResponse` is defined
- **THEN** it SHALL be in namespace `FunkArr.Messages.Scoring` and `ScoreCompleted` SHALL implement it

### Requirement: Scoring history response types implement IScoringResponse

`ScoringHistoryResult`, `ScoringDetailResult`, and `ScoringDetailNotFound` in namespace `FunkArr.Messages.Scoring.History` SHALL implement `IScoringResponse` from `FunkArr.Messages.Scoring`.

#### Scenario: History responses are IScoringResponse

- **WHEN** an actor responds with `ScoringHistoryResult`, `ScoringDetailResult`, or `ScoringDetailNotFound`
- **THEN** each SHALL be assignable to `IScoringResponse`

### Requirement: Ask calls use domain response interfaces

All `Ask<object>` calls SHALL be replaced with `Ask<IDomainResponse>` using the appropriate domain interface. Calls that already use a concrete response type MAY keep that type if it is the only possible response.

#### Scenario: SearchHandler uses typed Ask

- **WHEN** the SearchHandler sends a SearchCommand to the SearchManager
- **THEN** it SHALL use `Ask<ISearchResponse>` instead of `Ask<object>`

#### Scenario: RuleSetApiEndpoints uses typed Ask for detail

- **WHEN** the RuleSetApi sends QueryRuleSetDetail to the RuleSetManager
- **THEN** it SHALL use `Ask<IRuleSetResponse>` instead of `Ask<object>`

#### Scenario: RuleSetApiEndpoints uses typed Ask for scoring detail

- **WHEN** the RuleSetApi sends QueryScoringDetail to the MatchHistoryRegion
- **THEN** it SHALL use `Ask<IScoringResponse>` instead of `Ask<object>`

#### Scenario: Workers use typed Ask for ResolveRuleSet

- **WHEN** TvSearchWorker or MovieSearchWorker sends ResolveRuleSet
- **THEN** they SHALL use `Ask<IRuleSetResponse>` with PipeTo

#### Scenario: Workers use typed Ask for QueryMediathek

- **WHEN** TvSearchWorker or MovieSearchWorker sends QueryMediathek
- **THEN** they SHALL use `Ask<IMediathekResponse>` with PipeTo

#### Scenario: Workers use typed Ask for ScoreItems

- **WHEN** TvSearchWorker or MovieSearchWorker sends ScoreItems
- **THEN** they SHALL use `Ask<ScoreCompleted>` with PipeTo (single response type)
