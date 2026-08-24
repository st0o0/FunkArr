## Purpose

Verify-based snapshot tests ensuring wire format stability for persistence journal types, SABnzbd JSON responses, and Newznab XML output. Prevents accidental breaking changes to serialization contracts that external consumers (Sonarr, Radarr, Prowlarr) and persisted data depend on.

## Requirements

### Requirement: Journal round-trip contract tests
The system SHALL have Verify-based snapshot tests that validate persistence journal wire format stability for all four actor domains: QueueActor, DownloadActor, DownloadRequestActor, MatchQualityActor.

#### Scenario: Round-trip consistency
- **WHEN** a domain event is converted via `ToJournal()`, serialized to JSON with Newtonsoft.Json, deserialized back, and converted via `ToDomain()`
- **THEN** the resulting domain event SHALL be equal to the original

#### Scenario: Wire format snapshot
- **WHEN** a journal type is serialized to JSON
- **THEN** the JSON output SHALL match the approved `.verified.txt` snapshot

#### Scenario: Unknown field tolerance
- **WHEN** a journal JSON string contains extra fields not defined in the current type
- **THEN** deserialization SHALL succeed without exception and the unknown fields SHALL be ignored

#### Scenario: All journal types covered
- **WHEN** listing all journal types across the four persistence files
- **THEN** each type SHALL have a round-trip test and a wire format snapshot test

### Requirement: SABnzbd contract tests
The system SHALL have Verify-based snapshot tests that validate SABnzbd JSON response wire format stability for Sonarr/Radarr compatibility.

#### Scenario: Version response snapshot
- **WHEN** a `SabnzbdVersionResponse` is serialized with System.Text.Json
- **THEN** the JSON SHALL match the approved `.verified.txt` snapshot and contain `version` as a top-level key

#### Scenario: Config response snapshot
- **WHEN** a `SabnzbdConfigResponse` is built with representative data and serialized
- **THEN** the JSON SHALL match the approved snapshot and contain `config.misc.complete_dir` and `config.categories` with correct snake_case keys

#### Scenario: Queue response snapshot
- **WHEN** a `SabnzbdQueueResponse` is built with representative slot data and serialized
- **THEN** the JSON SHALL match the approved snapshot with correct `nzo_id`, `status`, `percentage`, `mb`, `mbleft`, `timeleft` keys

#### Scenario: History response snapshot
- **WHEN** a `SabnzbdHistoryResponse` is built with representative slot data and serialized
- **THEN** the JSON SHALL match the approved snapshot with correct `nzo_id`, `status`, `fail_message`, `storage`, `completed` keys

#### Scenario: AddFile response snapshot
- **WHEN** a `SabnzbdAddFileResponse` is serialized
- **THEN** the JSON SHALL match the approved snapshot with correct `status` and `nzo_ids` keys

### Requirement: Newznab XML contract tests
The system SHALL have Verify-based snapshot tests that validate Newznab XML output stability for Prowlarr compatibility.

#### Scenario: Caps response snapshot
- **WHEN** `NewznabXmlBuilder` generates a caps response
- **THEN** the XML SHALL match the approved `.verified.txt` snapshot and contain `<categories>` with TV and Movie categories

#### Scenario: TV search response snapshot
- **WHEN** `NewznabXmlBuilder` generates a tvsearch response with representative `NewznabResult` data
- **THEN** the XML SHALL match the approved snapshot and contain `<newznab:attr>` elements for category, size, resolution, video, tvdbid, season, episode

#### Scenario: Movie search response snapshot
- **WHEN** `NewznabXmlBuilder` generates a movie search response with representative data
- **THEN** the XML SHALL match the approved snapshot and contain `<newznab:attr>` elements for category, size, resolution, video

#### Scenario: Empty search results snapshot
- **WHEN** `NewznabXmlBuilder` generates a search response with no results
- **THEN** the XML SHALL match the approved snapshot with an empty `<channel>` element
