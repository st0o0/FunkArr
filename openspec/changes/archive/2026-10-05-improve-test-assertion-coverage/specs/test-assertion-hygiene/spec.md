## ADDED Requirements

### Requirement: No vacuous assertion after an already-proven value
Tests SHALL NOT assert `Assert.NotNull(value)` (or equivalent) as the only check on a value whose non-null, correctly-typed state was already proven by a preceding operation, such as `ExpectMsg<T>()`. Once that proof exists, the following assertion SHALL inspect the value's actual content (property values, message text, counts, etc.).

#### Scenario: ExpectMsg already proved type and non-nullness
- **WHEN** a test calls `ExpectMsg<TResponse>()` and then needs to assert on the result
- **THEN** the test asserts on `TResponse`'s properties or content, not merely `Assert.NotNull(result)`

#### Scenario: Null guard followed by real checks remains valid
- **WHEN** a test uses `Assert.NotNull(value)` to guard access to a nullable property, per the existing nullable-property requirement
- **THEN** that `Assert.NotNull` call followed by further assertions on `value`'s properties remains compliant and SHALL NOT be flagged, since it is not the only check

### Requirement: Endpoint-registration tests assert on request/response behavior
Tests for API endpoint mapping extensions (e.g. `Map<X>Api`) SHALL exercise at least one mapped route via an actual request and assert on the response (status code and/or body), not solely on route registration metadata such as `IEndpointRouteBuilder.DataSources` being non-empty.

#### Scenario: Endpoint mapping test exists for a route group
- **WHEN** a test verifies that `Map<X>Api` wires up its endpoints correctly
- **THEN** the test sends a request to at least one of the mapped routes and asserts on the resulting response, rather than asserting only that `DataSources` is non-empty

#### Scenario: Registration-only assertion is insufficient
- **WHEN** a test asserts only `Assert.NotEmpty(endpoints.DataSources)` after mapping an API
- **THEN** that assertion alone does not satisfy this requirement, because it does not prove any specific route, method, or response shape was registered correctly
