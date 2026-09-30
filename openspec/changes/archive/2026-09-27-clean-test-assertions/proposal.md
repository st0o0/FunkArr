## Why

Test assertions across the codebase use null-forgiving `!.` operators and direct `[0]` indexing without verifying collection size. This silently suppresses null warnings and skips count validation, meaning tests can pass even when the data shape is wrong. Replacing these with `Assert.NotNull` + clean access and `Assert.Single` / count assertions makes failures explicit and test intent clear.

## What Changes

- Replace `!.` (null-forgiving operator) with `Assert.NotNull` followed by clean property access across all test projects (~50 instances)
- Replace `[0]` indexing with `Assert.Single` where tests expect exactly one element (~100 instances across ~29 files)
- Add `Assert.Equal(expectedCount, list.Count)` before multi-element `[i]` access where tests expect more than one element
- Leave XLinq `!.Value` patterns in ArrApi XML tests unchanged (standard XLinq ergonomics)

## Capabilities

### New Capabilities

None. This is a test-only refactoring with no new capabilities.

### Modified Capabilities

None. No spec-level behavior changes.

## Impact

- All test projects: Scoring.Tests, Search.Tests, Download.Tests, RuleSet.Tests, ArrApi.Tests, History.Tests, Enrichment.Tests, Api.Tests, Persistence.Tests
- No production code changes
- No API changes
- No dependency changes
