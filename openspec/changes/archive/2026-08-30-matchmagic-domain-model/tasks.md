## 1. Data Model — Enums and Leaf Records

- [x] 1.1 Add Quality enum (HD1080, HD720, SD), MatchStrategy enum (5 values), FilterOp enum (6 values) with System.Text.Json serialization attributes
- [x] 1.2 Add MediaRef, Filter, TitleRule, QualityVariant, EpisodeIdentification records
- [x] 1.3 Add MediaItem record (raw MediathekViewWeb input)
- [x] 1.4 Add FilterGroup record with FilterNode discriminated union (Filter | nested FilterGroup) and JSON deserialization
- [x] 1.5 Add Rule record with all fields, default CaptureGroup and TitleRules handling
- [x] 1.6 Add RuleSet record with JSON deserialization entry point (RuleSet.FromJson)
- [x] 1.7 Add MatchResult record
- [x] 1.8 Add Tatort sample ruleset JSON as embedded test resource, verify full round-trip deserialization

## 2. Filter Evaluation

- [x] 2.1 Implement Filter.Evaluate — field resolution via switch expression, all 6 operators, case-insensitive string ops, duration in minutes, regex with 100ms timeout
- [x] 2.2 Implement FilterGroup.Evaluate — recursive all/any/not tree, combined groups, empty group passes all
- [x] 2.3 Tests: all filter operators, nested groups, combined all+not, empty group, unknown field, regex timeout

## 3. Matching Strategies

- [x] 3.1 Implement title construction from TitleRule chains — regex capture with configurable group, static concatenation, fail-fast on any regex miss
- [x] 3.2 Implement SeasonAndEpisodeNumber strategy — apply season/episode regexes, configurable capture group, produce EpisodeIdentification
- [x] 3.3 Implement ItemTitleExact strategy — build title from TitleRules, produce identification with constructed title
- [x] 3.4 Implement ItemTitleIncludes strategy — build title, check if item title contains it (case-insensitive, umlaut-normalized)
- [x] 3.5 Implement ItemTitleEqualsAirdate strategy — extract German date formats (dd.MM.yyyy, dd.MM.yy, "dd. MMMM yyyy"), produce identification with ISO date
- [x] 3.6 Implement ByAbsoluteEpisodeNumber strategy — apply episode regex, produce identification with absolute number
- [x] 3.7 Tests: each strategy with matching and non-matching inputs, capture group edge cases, German date formats

## 4. Rule and RuleSet Evaluation

- [x] 4.1 Implement Rule.Match — combine filter evaluation + strategy dispatch + quality variant construction + confidence resolution
- [x] 4.2 Implement quality variant construction — map URL fields to QualityVariant with size estimation from duration × bitrate constants
- [x] 4.3 Implement RuleSet.Evaluate — iterate rules by priority, first-match-wins per item, collect results
- [x] 4.4 Tests: full integration — Tatort ruleset against synthetic MediaItems, verify correct episode identification, priority ordering, unmatched items excluded, confidence from rule vs file default

## 5. Verification

- [x] 5.1 dotnet build, dotnet format --verify-no-changes, run FunkArr.MatchMagic.Tests
