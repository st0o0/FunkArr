# matchmagic-evaluation

## Purpose

Defines the evaluation logic for the MatchMagic domain: how rulesets evaluate media items through filter groups, matching strategies, title construction, quality variant building, and regex safety.

## Requirements

### Requirement: RuleSet evaluation entry point
RuleSet SHALL expose an `Evaluate(IReadOnlyList<MediaItem> items)` method that returns `IReadOnlyList<MatchResult>`. It SHALL iterate rules sorted by priority (0 = highest). For each item, the first rule whose Match succeeds wins — later rules are not evaluated for that item. Items that match no rule SHALL not appear in the result.

#### Scenario: Single item matches first rule
- **WHEN** a RuleSet has two rules (priority 0 and 10) and an item passes rule 0's filters and strategy
- **THEN** the result SHALL contain one MatchResult with MatchedRule = rule 0, and rule 10 SHALL NOT be evaluated for that item

#### Scenario: Item falls through to second rule
- **WHEN** an item fails rule 0's filters but passes rule 10's filters and strategy
- **THEN** the result SHALL contain one MatchResult with MatchedRule = rule 10

#### Scenario: Item matches no rule
- **WHEN** an item fails all rules' filters or strategies
- **THEN** the result SHALL NOT contain a MatchResult for that item

#### Scenario: Multiple items evaluated independently
- **WHEN** three items are evaluated and item 1 matches rule 0, item 2 matches rule 10, item 3 matches nothing
- **THEN** the result SHALL contain exactly two MatchResults

#### Scenario: Empty rules list
- **WHEN** a RuleSet has an empty rules list
- **THEN** Evaluate SHALL return an empty result list

### Requirement: Rule matching
Rule SHALL expose a `Match(MediaItem item, float defaultConfidence)` method that returns a `MatchResult?`. It SHALL first evaluate its FilterGroup against the item. If filters fail, return null. If filters pass, apply the matching strategy. If the strategy produces no identification, return null. If the strategy succeeds, build quality variants from the item's URL fields and return a MatchResult.

#### Scenario: Filters pass, strategy succeeds
- **WHEN** a Rule's FilterGroup passes for an item AND the strategy identifies an episode
- **THEN** Match SHALL return a MatchResult with the identification, confidence, and quality variants

#### Scenario: Filters fail
- **WHEN** a Rule's FilterGroup fails for an item
- **THEN** Match SHALL return null without evaluating the strategy

#### Scenario: Filters pass, strategy fails
- **WHEN** a Rule's FilterGroup passes but the strategy cannot identify an episode (e.g., regex doesn't match)
- **THEN** Match SHALL return null

### Requirement: FilterGroup evaluation
FilterGroup SHALL expose an `Evaluate(MediaItem item)` method returning bool. Evaluation SHALL be recursive: `All` requires every node to pass, `Any` requires at least one node to pass, `Not` requires every node to fail. When multiple groups are present (e.g., both All and Not), ALL groups must pass for the overall FilterGroup to pass.

#### Scenario: All group — all pass
- **WHEN** a FilterGroup has `All: [duration > 30, duration < 90]` and the item has duration 2700 seconds (45 min)
- **THEN** Evaluate SHALL return true

#### Scenario: All group — one fails
- **WHEN** a FilterGroup has `All: [duration > 30, duration < 90]` and the item has duration 6000 seconds (100 min)
- **THEN** Evaluate SHALL return false

#### Scenario: Any group — one passes
- **WHEN** a FilterGroup has `Any: [channel eq "ARD", channel eq "Das Erste"]` and the item channel is "Das Erste"
- **THEN** Evaluate SHALL return true

#### Scenario: Any group — none pass
- **WHEN** a FilterGroup has `Any: [channel eq "ARD", channel eq "Das Erste"]` and the item channel is "ZDF"
- **THEN** Evaluate SHALL return false

#### Scenario: Not group — blocks matching item
- **WHEN** a FilterGroup has `Not: [title contains "Audiodeskription"]` and the item title contains "Audiodeskription"
- **THEN** Evaluate SHALL return false

#### Scenario: Not group — passes non-matching item
- **WHEN** a FilterGroup has `Not: [title contains "Audiodeskription"]` and the item title is "Tatort: Die goldene Zeit"
- **THEN** Evaluate SHALL return true

#### Scenario: Combined All and Not
- **WHEN** a FilterGroup has `All: [duration > 30]` and `Not: [title contains "Trailer"]` and the item has duration 45 min and title "Tatort: Trailer"
- **THEN** Evaluate SHALL return false (Not group fails)

#### Scenario: Nested FilterGroup inside All
- **WHEN** a FilterGroup has `All: [duration > 30, Any: [channel eq "ARD", channel eq "ZDF"]]` and the item is from ARD with 45 min
- **THEN** Evaluate SHALL return true

#### Scenario: Empty FilterGroup passes everything
- **WHEN** a FilterGroup has no All, Any, or Not entries
- **THEN** Evaluate SHALL return true for any item

### Requirement: Filter evaluation
Filter SHALL expose an `Evaluate(MediaItem item)` method returning bool. It SHALL resolve the Field string to the corresponding MediaItem property and apply the Op.

#### Scenario: Duration greaterThan
- **WHEN** a Filter has field "duration", op GreaterThan, value "60" and the item has duration 5400 seconds (90 min)
- **THEN** Evaluate SHALL return true (90 > 60)

#### Scenario: Duration greaterThan — value is in minutes
- **WHEN** a Filter has field "duration", op GreaterThan, value "60"
- **THEN** the system SHALL compare the item's duration in minutes (item.Duration / 60) against the filter value

#### Scenario: Title contains
- **WHEN** a Filter has field "title", op Contains, value "Tatort" and the item title is "Tatort: Die goldene Zeit"
- **THEN** Evaluate SHALL return true

#### Scenario: Title contains — case insensitive
- **WHEN** a Filter has field "title", op Contains, value "tatort" and the item title is "Tatort: Die goldene Zeit"
- **THEN** Evaluate SHALL return true

#### Scenario: Channel eq
- **WHEN** a Filter has field "channel", op Eq, value "ARD" and the item channel is "ARD"
- **THEN** Evaluate SHALL return true

#### Scenario: Channel eq — case insensitive
- **WHEN** a Filter has field "channel", op Eq, value "ard" and the item channel is "ARD"
- **THEN** Evaluate SHALL return true

#### Scenario: Regex filter on title
- **WHEN** a Filter has field "title", op Regex, value "^Tatort" and the item title is "Tatort: Schwarzer Freitag"
- **THEN** Evaluate SHALL return true

#### Scenario: Regex filter — no match
- **WHEN** a Filter has field "title", op Regex, value "^Tatort" and the item title is "heute-show vom 5. Juni"
- **THEN** Evaluate SHALL return false

#### Scenario: NotContains filter
- **WHEN** a Filter has field "title", op NotContains, value "Trailer" and the item title is "Tatort: Die goldene Zeit"
- **THEN** Evaluate SHALL return true

#### Scenario: Timestamp greaterThan
- **WHEN** a Filter has field "timestamp", op GreaterThan, value "1719244800" and the item timestamp is 1719331200
- **THEN** Evaluate SHALL return true

#### Scenario: Unknown field
- **WHEN** a Filter has field "unknown_field"
- **THEN** Evaluate SHALL return false

### Requirement: SeasonAndEpisodeNumber strategy
When a Rule uses strategy SeasonAndEpisodeNumber, it SHALL apply SeasonRegex and EpisodeRegex against the MediaItem's Title. It SHALL extract the capture group specified by CaptureGroup (default: last group). If both regexes match, it SHALL produce an EpisodeIdentification with the captured season and episode strings.

#### Scenario: Standard S##/E## pattern
- **WHEN** SeasonRegex is `(?<=S)(\d{2,4})(?=/E)` and EpisodeRegex is `(?<=E)(\d{2,4})(?=\))` and the title is "Tatort (S01/E05)"
- **THEN** the identification SHALL have Season "01", Episode "05"

#### Scenario: Season regex doesn't match
- **WHEN** SeasonRegex is `(?<=S)(\d{2,4})` and the title has no S## pattern
- **THEN** the strategy SHALL return null (no identification)

#### Scenario: Explicit capture group
- **WHEN** CaptureGroup is 1 and the regex has multiple groups
- **THEN** the strategy SHALL use group 1 instead of the last group

### Requirement: ItemTitleExact strategy
When a Rule uses strategy ItemTitleExact, it SHALL apply the TitleRules to construct a title from the MediaItem. If title construction succeeds, it SHALL produce an EpisodeIdentification with the constructed title.

#### Scenario: Regex title rule extracts episode name
- **WHEN** a TitleRule has type "regex", field "title", pattern `^Tatort[^:]*:\s*(.+)` and the item title is "Tatort: Die goldene Zeit"
- **THEN** the constructed title SHALL be "Die goldene Zeit"

#### Scenario: Title rule chain with static separator
- **WHEN** TitleRules are [regex extracting "Alice", static " & ", regex extracting "Bob"]
- **THEN** the constructed title SHALL be "Alice & Bob"

#### Scenario: Regex title rule fails to match
- **WHEN** a regex TitleRule's pattern does not match the item
- **THEN** the entire title construction SHALL fail and the strategy SHALL return null

### Requirement: ItemTitleIncludes strategy
When a Rule uses strategy ItemTitleIncludes, it SHALL apply the TitleRules to construct a title. It SHALL then check if the MediaItem's Title contains the constructed title (case-insensitive, umlaut-normalized). If it does, produce an EpisodeIdentification with the constructed title.

#### Scenario: Title contains constructed title
- **WHEN** the constructed title is "Die goldene Zeit" and the item title is "Tatort: Die goldene Zeit (Audiodeskription)"
- **THEN** the strategy SHALL return an identification with Title "Die goldene Zeit"

#### Scenario: Title does not contain constructed title
- **WHEN** the constructed title is "Schwarzer Freitag" and the item title is "Tatort: Die goldene Zeit"
- **THEN** the strategy SHALL return null

### Requirement: ItemTitleEqualsAirdate strategy
When a Rule uses strategy ItemTitleEqualsAirdate, it SHALL extract a date from the MediaItem's Title using common German date formats (dd.MM.yyyy, dd.MM.yy, "dd. MMMM yyyy" with German month names). It SHALL produce an EpisodeIdentification with the extracted date as the Title.

#### Scenario: Numeric date in title
- **WHEN** the item title is "heute-show vom 24.10.2024"
- **THEN** the identification SHALL have Title "2024-10-24"

#### Scenario: German month name in title
- **WHEN** the item title is "heute-show vom 16. Juli 2024"
- **THEN** the identification SHALL have Title "2024-07-16"

#### Scenario: Two-digit year
- **WHEN** the item title contains "24.10.24"
- **THEN** the identification SHALL interpret it as 2024-10-24

#### Scenario: No date in title
- **WHEN** the item title contains no recognizable date
- **THEN** the strategy SHALL return null

### Requirement: ByAbsoluteEpisodeNumber strategy
When a Rule uses strategy ByAbsoluteEpisodeNumber, it SHALL apply EpisodeRegex against the MediaItem's Title to extract an absolute episode number. It SHALL produce an EpisodeIdentification with Episode set to the captured number and Season null.

#### Scenario: Absolute episode number extracted
- **WHEN** EpisodeRegex is `Folge\s*(\d+)` and the title is "Lowenzahn - Folge 312"
- **THEN** the identification SHALL have Season null, Episode "312"

#### Scenario: Regex doesn't match
- **WHEN** EpisodeRegex is `Folge\s*(\d+)` and the title has no "Folge" pattern
- **THEN** the strategy SHALL return null

### Requirement: Title construction from TitleRules
When evaluating TitleRules, the system SHALL process them in order, concatenating each result. Regex rules extract a capture group from the specified field. Static rules append their literal value. If ANY regex rule fails to match, the ENTIRE title construction fails (returns null).

#### Scenario: Single regex rule
- **WHEN** one TitleRule with type "regex", field "title", pattern `^heute-show vom (\d{1,2}\. \w+ \d{4})` is applied to "heute-show vom 5. Juni 2026"
- **THEN** the constructed title SHALL be "5. Juni 2026"

#### Scenario: Explicit capture group on title rule
- **WHEN** a TitleRule has pattern `(\w+):\s*(.+)` with captureGroup 2
- **THEN** it SHALL use capture group 2

#### Scenario: Default capture group is last
- **WHEN** a TitleRule has pattern `(\w+):\s*(.+)` with no captureGroup specified
- **THEN** it SHALL use the last capture group (group 2 in this case)

#### Scenario: One rule in chain fails
- **WHEN** a chain of 3 TitleRules has the second rule fail to match
- **THEN** the entire construction SHALL return null

### Requirement: Quality variant construction
When producing a MatchResult, the system SHALL build QualityVariant entries from the MediaItem's URL fields. UrlVideoHd maps to HD1080, UrlVideo maps to HD720, UrlVideoLow maps to SD. Null or empty URLs SHALL be skipped. EstimatedSizeBytes SHALL be calculated as `duration x bitrateConstant / 8` where bitrateConstant is 5000 kbps for HD1080, 2500 kbps for HD720, 800 kbps for SD.

#### Scenario: All three URLs present
- **WHEN** a MediaItem has non-empty UrlVideoHd, UrlVideo, and UrlVideoLow with duration 3600 seconds
- **THEN** the result SHALL have 3 QualityVariants: HD1080 (estimated ~2.25 GB), HD720 (estimated ~1.12 GB), SD (estimated ~360 MB)

#### Scenario: Only standard URL
- **WHEN** a MediaItem has only UrlVideo (UrlVideoHd and UrlVideoLow are null)
- **THEN** the result SHALL have 1 QualityVariant with Quality HD720

#### Scenario: No URLs
- **WHEN** a MediaItem has all three URL fields null or empty
- **THEN** the result SHALL have an empty Qualities list

### Requirement: MatchMagicActor builds FilterGroupTrace during filter evaluation

The MatchMagicActor SHALL build a FilterGroupTrace while evaluating filters. Each condition evaluation SHALL record the field, operator, expected value, actual resolved value, and pass/fail. Short-circuited conditions SHALL be recorded with Skipped=true.

#### Scenario: All conditions evaluated

- **WHEN** a FilterGroup with operator All has 3 conditions and all pass
- **THEN** the FilterGroupTrace SHALL contain 3 FilterNodeTraces with Passed=true and Skipped=false, each with ActualValue populated

#### Scenario: Short-circuit in All group

- **WHEN** a FilterGroup with operator All has 3 conditions and condition 2 fails
- **THEN** the FilterGroupTrace SHALL contain condition 1 with Passed=true, condition 2 with Passed=false (with ActualValue), and condition 3 with Skipped=true and ActualValue=null

#### Scenario: Nested group traced recursively

- **WHEN** a FilterGroup contains a nested FilterGroup
- **THEN** the outer FilterGroupTrace SHALL contain a FilterNodeTrace of type Group with a nested FilterGroupTrace

### Requirement: MatchMagicActor builds IdentificationTrace during identification

The MatchMagicActor SHALL build an IdentificationTrace after each identification attempt.

#### Scenario: Successful identification trace

- **WHEN** identification succeeds
- **THEN** the IdentificationTrace SHALL have Attempted=true, the strategy name, and Detail=null

#### Scenario: Failed identification trace

- **WHEN** identification fails (regex no match, title construction fails, no date found)
- **THEN** the IdentificationTrace SHALL have Attempted=true, the strategy name, and a Detail string describing the failure reason

#### Scenario: Skipped identification trace

- **WHEN** filters failed before identification
- **THEN** the IdentificationTrace SHALL have Attempted=false

### Requirement: Regex timeout safety
All regex evaluations (filters, season/episode extraction, title rules) SHALL use a timeout of 100 milliseconds. If a regex exceeds the timeout, the evaluation SHALL treat it as a non-match (return false for filters, null for extractions).

#### Scenario: Pathological regex times out
- **WHEN** a filter regex pattern causes catastrophic backtracking on the input
- **THEN** the filter SHALL return false instead of hanging

#### Scenario: Normal regex completes
- **WHEN** a filter regex pattern matches within the timeout
- **THEN** the filter SHALL return the correct match result
