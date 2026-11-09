## MODIFIED Requirements

### Requirement: Persistence enums declare explicit values
Every member of every enum declared in `FunkArr.Persistence` SHALL have an explicit integer value. Implicit ordinals SHALL NOT be used. Every Persistence enum SHALL start with `Unknown = 0` as its first member.

#### Scenario: Enum without explicit values is rejected in review
- **WHEN** an enum in `FunkArr.Persistence` has a member without an assigned value
- **THEN** it violates this requirement and SHALL be corrected before merge

#### Scenario: All existing persistence enums comply
- **WHEN** the enums in `FunkArr.Persistence` are inspected
- **THEN** `PersistedMediaType`, `PersistedFailureKind`, `PersistedMatchMethod`, `PersistedMediathekVariant`, `PersistedRuleOutcome`, `PersistedFilterGroupOp`, `PersistedIdentificationStrategy`, `PersistedIdentificationFailureReason`, `PersistedDownloadStatus`, `PersistedDownloadPriority`, and `PersistedSearchSource` each assign every member an explicit value
- **AND** each starts with `Unknown = 0`

#### Scenario: Unknown is the default value
- **WHEN** `default(PersistedMediaType)` is evaluated
- **THEN** the result SHALL be `PersistedMediaType.Unknown`

### Requirement: Persisted enum values are frozen
Once a persistence enum member has an assigned value, that value SHALL NOT be changed, reused for a different member, or reordered. New members SHALL receive a new, previously unused value. **Exception**: The one-time v0.x renumber adding `Unknown = 0` is permitted; after this change, values are frozen at their new assignments.

#### Scenario: Existing ordinals after renumber
- **WHEN** `PersistedMediaType` is inspected after this change
- **THEN** `Unknown` is `0`, `Show` is `1`, `Movie` is `2`

#### Scenario: Gaps are preserved relatively
- **WHEN** `PersistedSearchSource` is inspected after this change
- **THEN** `Unknown` is `0`, `Sonarr` is `1`, `Radarr` is `2`, `Prowlarr` is `3`, `Feed` is `5`

#### Scenario: Member added later
- **WHEN** a new member is added to a persistence enum after this change
- **THEN** it uses a value greater than every existing value and does not alter existing members

#### Scenario: PersistedDownloadPriority renumbered sequentially
- **WHEN** `PersistedDownloadPriority` is inspected after this change
- **THEN** members SHALL be `Unknown = 0, Paused = 1, Low = 2, Normal = 3, High = 4`

#### Scenario: PersistedDownloadStatus renumbered
- **WHEN** `PersistedDownloadStatus` is inspected after this change
- **THEN** members SHALL be `Unknown = 0, Queued = 1, Processing = 2, Completed = 3, Failed = 4`

#### Scenario: PersistedFailureKind renumbered
- **WHEN** `PersistedFailureKind` is inspected after this change
- **THEN** members SHALL be `Unknown = 0, Transient = 1, Permanent = 2`

#### Scenario: PersistedMatchMethod renumbered
- **WHEN** `PersistedMatchMethod` is inspected after this change
- **THEN** members SHALL be `Unknown = 0, RegexExtracted = 1, TitleMatch = 2, AirdateMatch = 3, YearMatch = 4`

#### Scenario: PersistedMediathekVariant renumbered
- **WHEN** `PersistedMediathekVariant` is inspected after this change
- **THEN** members SHALL be `Unknown = 0, Standard = 1, AudioDescription = 2, SignLanguage = 3, PlainLanguage = 4, OriginalVersion = 5`

#### Scenario: PersistedRuleOutcome renumbered
- **WHEN** `PersistedRuleOutcome` is inspected after this change
- **THEN** members SHALL be `Unknown = 0, Matched = 1, FilterFailed = 2, IdentificationFailed = 3`

#### Scenario: PersistedFilterGroupOp renumbered
- **WHEN** `PersistedFilterGroupOp` is inspected after this change
- **THEN** members SHALL be `Unknown = 0, All = 1, Any = 2, Not = 3`

#### Scenario: PersistedIdentificationStrategy renumbered
- **WHEN** `PersistedIdentificationStrategy` is inspected after this change
- **THEN** members SHALL be `Unknown = 0, SeasonAndEpisodeNumber = 1, AbsoluteEpisodeNumber = 2, TitleExact = 3, TitleIncludes = 4, AirdateExtraction = 5`

#### Scenario: PersistedIdentificationFailureReason renumbered
- **WHEN** `PersistedIdentificationFailureReason` is inspected after this change
- **THEN** members SHALL be `Unknown = 0, UnknownStrategy = 1, SeasonPatternNotMatched = 2, NoEpisodePatternConfigured = 3, EpisodePatternNotMatched = 4, NoTitlePartsConfigured = 5, TitlePartRegexNotMatched = 6, TitleDoesNotMatch = 7, NoDateFoundInTitle = 8`
