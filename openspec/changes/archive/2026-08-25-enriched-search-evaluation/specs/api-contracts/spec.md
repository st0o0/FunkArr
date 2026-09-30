## MODIFIED Requirements

### Requirement: Extension-method-based contract mapping
The system SHALL provide extension methods in `Api/Mapping/ContractMappingExtensions.cs` for converting between domain types and generated contract types. Mapping methods SHALL follow the pattern `domain.ToContract()`. Mapping SHALL cover the new types: `SearchEvaluation`, `ItemEvaluation`, `RuleEvaluation`, `FilterCheck`, `StrategyDetail`.

#### Scenario: SearchEvaluation to contract mapping
- **WHEN** a controller needs to return a SearchEvaluation
- **THEN** it SHALL call `searchEvaluation.ToContract()` which maps the Items list and all nested types

#### Scenario: ItemEvaluation to contract mapping
- **WHEN** an ItemEvaluation with Outcome=Matched is mapped
- **THEN** the contract SHALL contain the int-backed outcome (0), raw item fields, match result fields, and the full ruleEvaluations array

#### Scenario: Enum serialization as integer
- **WHEN** contract types containing EvaluationOutcome, RuleOutcome, MatchingStrategy, or FilterOp are serialized to JSON
- **THEN** all enum values SHALL be serialized as integers, not strings

## REMOVED Requirements

### Requirement: RuleStrategy enum
**Reason**: Replaced by int-backed MatchingStrategy enum serialized directly from the domain model. The separate contract-level string enum `MatchedTraceContractStrategy` is no longer needed.
**Migration**: Use MatchingStrategy enum (int-backed) from the domain model, serialized as integer in API contracts.
