## 1. Propagate media.name through resolver

- [x] 1.1 Add `Name` to `RawMedia` in `RuleSetMerger.cs`
- [x] 1.2 Add `MediaName` to `ExtractIdentity` return tuple
- [x] 1.3 Add `MediaName` parameter to `RegisterRuleSet` message
- [x] 1.4 Add `MediaName` to `RuleSetResolved` message
- [x] 1.5 Add `MediaNameByRuleSetId` to `RuleSetResolverState` and wire through Apply/Resolve

## 2. Use media name in release titles

- [x] 2.1 Pass `MediaName` through `RuleSetWorker` -> `RegisterRuleSet`
- [x] 2.2 Add `MediaName` to `TvSearchWorkerState` and `MovieSearchWorkerState`
- [x] 2.3 Pass `resolved.MediaName` through `ApplyRuleSet` in both search workers
- [x] 2.4 Use `state.MediaName ?? raw.Topic` in `ToResultItems` for both TV and Movie

## 3. Verify

- [x] 3.1 Build passes (0 errors)
- [x] 3.2 Search tests: 59/59 pass
- [x] 3.3 RuleSet tests: 54/54 pass
- [x] 3.4 Format check clean
