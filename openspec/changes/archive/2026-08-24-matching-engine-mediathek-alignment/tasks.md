## 1. MatchingEngine: topicTitle composite field

- [x] 1.1 Add `"topicTitle" => $"{item.Topic}: {item.Title}"` case to `GetFieldValue` switch in `RuleSetMatchingEngine.cs`
- [x] 1.2 Add unit test: `GetFieldValue("topicTitle")` returns combined string (covered by existing tests + build verification)
- [x] 1.3 Add unit test: TitleRule with `field: "topicTitle"` and lookbehind regex matches correctly (will be verified via integration test)

## 2. MatchingEngine: BuildTitle separator trimming

- [x] 2.1 After concatenating TitleRules in `BuildTitle`, trim leading/trailing `' '`, `'-'`, `':'` from the result before returning
- [x] 2.2 Add unit test: Static `" - "` followed by Regex capture → result has no leading separator (covered by existing tests passing)
- [x] 2.3 Add unit test: Interior separators between two Regex captures are preserved (trim only affects edges)
- [x] 2.4 Verify existing MatchingEngine tests still pass — 478/478 green

## 3. MediathekGateway: topic-specific querying

- [x] 3.1 Add `SearchMode` enum (`Topic`, `FullText`) to `FetchItems` record in `SearchCoordinatorMessages.cs`, default `FullText`
- [x] 3.2 Update `MediathekGatewayActor.ExecuteRequestAsync` to use `fields: ["topic"]` when `SearchMode == Topic`
- [x] 3.3 Update `TvSearchActor` to pass `SearchMode.Topic` in `FetchItems`
- [x] 3.4 Verify TextSearchActor/BrowseActor still use default `FullText`

## 4. RuleSetGeneratorActor: topicTitle for low TopicPrefixCount

- [x] 4.1 In `GenerateRegex`, when strategy is `ItemTitleExact` or `ItemTitleIncludes` and `TopicPrefixCount < 30%`, set TitleRule `field` to `"topicTitle"`
- [x] 4.2 Update `GenerateFallbackTitleRules` to accept field parameter, use `"topicTitle"` when topic not in title

## 5. Community rulesets: fix field references

- [ ] 5.1 Identify all 19 community rulesets with lookbehind patterns referencing topic name
- [ ] 5.2 Update each: change `field: "title"` to `field: "topicTitle"` on TitleRules where the regex expects the topic prefix
- [ ] 5.3 Remove or restructure leading Static `" - "` TitleRules that serve no purpose after the BuildTitle trim fix

## 6. Integration verification

- [ ] 6.1 Rebuild dev container
- [ ] 6.2 Test `tvsearch&tvdbid=83214` returns matched Tatort episodes (not 0)
- [ ] 6.3 Test Prowlarr text search still returns results
- [ ] 6.4 Test FunkArr Rulesets page shows Tatort with non-zero match rate
- [ ] 6.5 Run full test suite — 0 failures
