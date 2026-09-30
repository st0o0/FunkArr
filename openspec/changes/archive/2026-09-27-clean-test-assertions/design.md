## Context

Tests across FunkArr use two anti-patterns that weaken assertion quality:

1. **Null-forgiving `!.`** - Silences nullable warnings without verifying the value. If the property is null, the test either throws a NullReferenceException (not an assertion failure) or silently asserts on default values.
2. **Direct `[0]` indexing** - Accesses collection elements without asserting collection size. The test doesn't verify "there is exactly one result" - it just grabs the first one, passing even if extra unexpected elements exist.

The gold standard pattern (already used in some Scoring tests) is:
```csharp
var traceItem = Assert.Single(result.ItemTraces);
Assert.NotNull(ruleItem.FilterTrace);
var filterNodeItem = Assert.Single(ruleItem.FilterTrace.Nodes);
```

## Goals / Non-Goals

**Goals:**
- Every nullable property access in tests goes through `Assert.NotNull` first
- Every single-element collection access uses `Assert.Single` with variable binding
- Every multi-element collection access has an `Assert.Equal(count, list.Count)` guard
- All test projects pass after changes

**Non-Goals:**
- Changing test logic or adding new test cases
- Refactoring production code
- Touching XLinq `Attribute("x")!.Value` patterns in ArrApi XML tests (standard XLinq idiom)
- Changing assertion libraries or test frameworks

## Decisions

### 1. Use `Assert.Single` with variable binding over `Assert.Equal(1, list.Count)` + `[0]`

`Assert.Single` returns the typed element, eliminating the indexer and making the assertion intent explicit. This is the xUnit-idiomatic way.

```csharp
// Before
Assert.True(result.Results[0].Matched);

// After
var item = Assert.Single(result.Results);
Assert.True(item.Matched);
```

### 2. Use `Assert.NotNull` before property access, not `Assert.True(x != null)`

`Assert.NotNull` provides clear failure messages. After the assertion, the compiler knows the value is non-null (xUnit's `[NotNull]` attribute on the parameter), so no `!.` is needed downstream.

```csharp
// Before
Assert.Equal("02", traces[0].Identification!.Season);

// After
var trace = Assert.Single(traces);
Assert.NotNull(trace.Identification);
Assert.Equal("02", trace.Identification.Season);
```

### 3. Multi-element access gets count guard

Where a test legitimately expects multiple elements and accesses by index, add a count assertion at the top of the assert block:

```csharp
// Before
Assert.True(filterNodes[0].Passed);
Assert.True(filterNodes[1].Skipped);

// After
Assert.Equal(2, filterNodes.Count);
Assert.True(filterNodes[0].Passed);
Assert.True(filterNodes[1].Skipped);
```

### 4. Domain-by-domain execution, one commit per test project

Each test project is independent. Working domain-by-domain keeps changes reviewable and lets each commit be tested in isolation.

### 5. Leave XLinq `!.Value` alone

`element.Attribute("name")!.Value` is standard XLinq ergonomics. Adding `Assert.NotNull` before every XML attribute access would make those tests unreadable without meaningful safety improvement.

## Risks / Trade-offs

- **Large diff, no behavior change** - This is purely mechanical but touches ~150 assertion sites across ~29 files. Risk of introducing typos or accidentally changing test logic. Mitigation: run each test project after each commit, no logic changes.
- **Variable naming in Assert.Single** - Need consistent local variable names when extracting from `Assert.Single`. Use descriptive names matching the collection's element type (e.g., `item` from `Results`, `trace` from `ItemTraces`, `ruleTrace` from `RuleTraces`).
