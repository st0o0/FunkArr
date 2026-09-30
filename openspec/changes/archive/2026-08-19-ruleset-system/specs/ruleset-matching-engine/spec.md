## ADDED Requirements

### Requirement: Filter evaluation
The matching engine SHALL evaluate all filters in a rule against a Mediathek result item. All filters MUST pass for the rule to be applied.

#### Scenario: Duration greater-than filter passes
- **WHEN** a filter requires duration greaterThan 35 and the item has duration 2700 seconds (45 minutes)
- **THEN** the filter SHALL pass (45 > 35)

#### Scenario: Duration greater-than filter fails
- **WHEN** a filter requires duration greaterThan 35 and the item has duration 300 seconds (5 minutes)
- **THEN** the filter SHALL fail (5 < 35)

#### Scenario: Multiple filters — all must pass
- **WHEN** a rule has filters [duration greaterThan 15, duration lessThan 30] and the item has duration 1500 seconds (25 minutes)
- **THEN** all filters SHALL pass (25 > 15 AND 25 < 30)

#### Scenario: Contains filter on title
- **WHEN** a filter requires title contains "Spezial" and the item title is "Terra X Spezial: Vulkane"
- **THEN** the filter SHALL pass

#### Scenario: Regex filter
- **WHEN** a filter requires title regex "^(?!.*Staffel).*" and the item title is "Der Schwarm Folge 3"
- **THEN** the filter SHALL pass (title does not contain "Staffel")

### Requirement: Duration filter unit conversion
Duration filter values SHALL be interpreted as minutes. The Mediathek API returns duration in seconds. The engine SHALL convert seconds to minutes before comparison.

#### Scenario: Conversion applied
- **WHEN** a filter has field="duration", op="greaterThan", value="35" and item duration is 2700 seconds
- **THEN** the engine SHALL compare 45 minutes > 35 minutes (pass)

### Requirement: Title construction from title rules
The matching engine SHALL construct a title string by applying title rules in order — regex rules extract capture groups from specified fields, static rules append literal text.

#### Scenario: Single regex title rule
- **WHEN** a title rule extracts from title using pattern "^Tatort:\\s*(.+)" and the item title is "Tatort: Die goldene Zeit"
- **THEN** the constructed title SHALL be "Die goldene Zeit"

#### Scenario: Multiple title rules with static
- **WHEN** title rules are [regex extracting "Alice" from title, static " & ", regex extracting "Bob" from title]
- **THEN** the constructed title SHALL be "Alice & Bob"

#### Scenario: Title construction fails on regex miss
- **WHEN** a regex title rule does not match the item's field value
- **THEN** the title construction SHALL return null and the rule SHALL not match

### Requirement: Season and episode number matching
The seasonAndEpisodeNumber strategy SHALL extract season and episode numbers from the item title using the rule's regex patterns and validate them against TVDB episode data.

#### Scenario: Successful S/E extraction and TVDB match
- **WHEN** seasonRegex extracts "11" and episodeRegex extracts "08" from title "Folge 8: ... (S11/E08)"
- **AND** TVDB has season 11 episode 8 for this show
- **THEN** the strategy SHALL return a successful match with the TVDB episode info

#### Scenario: S/E extracted but no TVDB match
- **WHEN** seasonRegex extracts "99" and episodeRegex extracts "01" but TVDB has no season 99
- **THEN** the strategy SHALL return no match

#### Scenario: Regex extraction fails
- **WHEN** the seasonRegex does not match the item title
- **THEN** the strategy SHALL return no match

### Requirement: Exact title matching
The itemTitleExact strategy SHALL construct a title from title rules and compare it case-insensitively against TVDB episode names.

#### Scenario: Exact match found
- **WHEN** the constructed title is "Die goldene Zeit" and TVDB has an episode named "Die goldene Zeit"
- **THEN** the strategy SHALL return a match with that episode

#### Scenario: Multiple episodes match — disambiguate by air date
- **WHEN** the constructed title matches two TVDB episodes and the item timestamp matches one episode's air date
- **THEN** the strategy SHALL return the episode whose air date matches

#### Scenario: Multiple episodes match — no air date match
- **WHEN** the constructed title matches two TVDB episodes and neither air date matches
- **THEN** the strategy SHALL return the most recently aired episode

### Requirement: Includes title matching
The itemTitleIncludes strategy SHALL construct a title from title rules and check if any TVDB episode name contains the constructed title (case-insensitive).

#### Scenario: Partial match found
- **WHEN** the constructed title is "Vulkan" and TVDB has an episode named "Der Vulkan-Check"
- **THEN** the strategy SHALL return a match with that episode

### Requirement: Airdate matching
The itemTitleEqualsAirdate strategy SHALL extract a date from the item using title rules and match it against TVDB episode air dates.

#### Scenario: German long date format
- **WHEN** the title rule extracts "5. Juni 2026" from the item title
- **THEN** the system SHALL parse this as 2026-06-05 and match against TVDB air dates

#### Scenario: German short date format
- **WHEN** the title rule extracts "16.08.2026" from the item title
- **THEN** the system SHALL parse this as 2026-08-16 and match against TVDB air dates

#### Scenario: Air date match found
- **WHEN** the parsed date is 2026-06-05 and TVDB has an episode that aired on 2026-06-05
- **THEN** the strategy SHALL return a match with that episode

### Requirement: Absolute episode number matching
The byAbsoluteEpisodeNumber strategy SHALL extract an absolute episode number from the item title using the rule's episodeRegex and match it against TVDB absolute episode numbering.

#### Scenario: Absolute number extraction
- **WHEN** episodeRegex "\\((\\d{3,4})\\)" matches "(1606)" in the item title
- **THEN** the system SHALL extract absolute episode number 1606

#### Scenario: TVDB absolute match
- **WHEN** the extracted absolute number is 1606 and TVDB maps this to season 1 episode 1606
- **THEN** the strategy SHALL return a match with that episode

### Requirement: Skip keyword filtering
The matching engine SHALL skip items whose title or topic contains accessibility variant keywords before applying rules.

#### Scenario: Audiodeskription filtered
- **WHEN** an item title is "Tatort: Die goldene Zeit (Audiodeskription)"
- **THEN** the engine SHALL skip this item

#### Scenario: Gebärdensprache filtered
- **WHEN** an item title ends with "(Gebärdensprache)" or "(Gebardensprache)"
- **THEN** the engine SHALL skip this item

#### Scenario: Normal title passes
- **WHEN** an item title is "Tatort: Die goldene Zeit"
- **THEN** the engine SHALL NOT skip this item

### Requirement: First-match-wins rule evaluation
The matching engine SHALL evaluate rules in priority order and stop at the first rule that produces a successful match.

#### Scenario: First rule matches
- **WHEN** a ruleset has rules at priority 0 and 10, and the priority-0 rule matches
- **THEN** the engine SHALL return the priority-0 match and NOT evaluate the priority-10 rule

#### Scenario: First rule fails, second matches
- **WHEN** a ruleset has rules at priority 0 and 10, and the priority-0 rule does not match but priority-10 does
- **THEN** the engine SHALL return the priority-10 match
