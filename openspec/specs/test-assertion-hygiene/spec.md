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

