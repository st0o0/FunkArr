# test-assertion-hygiene Specification

## Purpose
TBD - created by archiving change clean-test-assertions. Update Purpose after archive.
## Requirements
### Requirement: Nullable properties use Assert.NotNull before access
Tests SHALL use `Assert.NotNull(value)` before accessing properties on nullable values. The null-forgiving operator `!.` SHALL NOT be used to bypass nullable warnings in test assertions.

#### Scenario: Nullable property is non-null
- **WHEN** a test needs to assert on properties of a nullable value
- **THEN** the test calls `Assert.NotNull(value)` first, then accesses properties without `!.`

#### Scenario: Nullable property is unexpectedly null
- **WHEN** a test accesses a nullable property that is null at runtime
- **THEN** the test fails with an `Assert.NotNull` failure (not a NullReferenceException)

### Requirement: Single-element collections use Assert.Single
Tests expecting exactly one element in a collection SHALL use `Assert.Single(collection)` to both assert the count and bind the element to a variable. Direct `[0]` indexing without a count assertion SHALL NOT be used.

#### Scenario: Collection has exactly one element
- **WHEN** a test expects a single result from an operation
- **THEN** the test uses `var item = Assert.Single(collection)` and asserts on the bound variable

#### Scenario: Collection unexpectedly has zero or multiple elements
- **WHEN** a collection has zero or more than one element
- **THEN** the test fails with an `Assert.Single` failure (not an IndexOutOfRangeException or silent pass)

### Requirement: Multi-element collections assert count before indexing
Tests accessing multiple collection elements by index SHALL assert the expected count first with `Assert.Equal(expectedCount, collection.Count)`.

#### Scenario: Multi-element collection access
- **WHEN** a test accesses `collection[0]` and `collection[1]`
- **THEN** the test first asserts `Assert.Equal(2, collection.Count)` (or the expected count)

### Requirement: XLinq attribute access is exempt
XLinq `Attribute("name")!.Value` and `Element("name")!.Value` patterns in XML test files SHALL remain unchanged. This is standard XLinq ergonomics.

#### Scenario: XML attribute access in ArrApi tests
- **WHEN** a test accesses XML attributes via XLinq
- **THEN** the `!.Value` pattern is acceptable and SHALL NOT be replaced with `Assert.NotNull`

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

