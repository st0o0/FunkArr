## Purpose

Descriptive named constants for actor Ask timeouts in API endpoint classes, replacing inline `TimeSpan.FromSeconds(...)` calls.

## Requirements

### Requirement: API endpoint timeout constants use descriptive names

Each API endpoint class SHALL define its actor Ask timeouts as `private static readonly TimeSpan` fields with descriptive names. Timeout values SHALL NOT appear as inline `TimeSpan.FromSeconds(...)` calls in endpoint lambdas or methods.

#### Scenario: MediathekApiEndpoints timeout

- **WHEN** MediathekApiEndpoints performs an actor Ask
- **THEN** it SHALL use a named constant field (e.g., `_queryTimeout`) instead of an inline `TimeSpan.FromSeconds(15)`

#### Scenario: RuleSetApiEndpoints timeout

- **WHEN** RuleSetApiEndpoints performs an actor Ask
- **THEN** it SHALL use named constant fields for its different timeout values (query vs stats)

#### Scenario: No inline TimeSpan construction in endpoint methods

- **WHEN** any endpoint handler method or lambda performs an actor Ask
- **THEN** the timeout SHALL reference a named static field, not an inline `TimeSpan.FromSeconds(...)` call
