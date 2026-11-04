## MODIFIED Requirements

### Requirement: Namespace separation
Newznab-specific types SHALL reside in `FunkArr.ArrApi.Newznab` or its sub-namespaces (`.Models`, `.Requests`, `.Services`) matching their folder. SABnzbd-specific types SHALL reside in `FunkArr.ArrApi.Sabnzbd` or its sub-namespaces (`.Models`, `.Requests`, `.Services`) matching their folder. Shared types (ApiKeyActionFilter, ArrApiOptions, ServiceCollectionExtensions) SHALL reside in `FunkArr.ArrApi` root namespace. No protocol-specific type SHALL sit in the project root folder.

#### Scenario: Newznab types in correct namespace
- **WHEN** examining NewznabController, NewznabSearchService, NzbService, Nzb, NzbPayload, Caps, Rss, NewznabError, SearchResultCache
- **THEN** all SHALL be in `FunkArr.ArrApi.Newznab` or one of its sub-namespaces, and `Nzb` SHALL be in `Newznab/Models/`

#### Scenario: SABnzbd types in correct namespace
- **WHEN** examining SabnzbdController, SabnzbdQueueService, SabnzbdDownloadService, SabnzbdResponseMapper, SabnzbdConstants, QueueResponse, HistoryResponse, FullStatusResponse
- **THEN** all SHALL be in `FunkArr.ArrApi.Sabnzbd` or one of its sub-namespaces, and `SabnzbdConstants` SHALL be in `Sabnzbd/`

#### Scenario: Shared types in root namespace
- **WHEN** examining ApiKeyActionFilter, ArrApiOptions, ServiceCollectionExtensions
- **THEN** all SHALL be in `FunkArr.ArrApi`

#### Scenario: Request records in Requests folders
- **WHEN** examining IndexerRequest, DownloadGetRequest, DownloadPostRequest
- **THEN** each SHALL reside in the `Requests/` folder and sub-namespace of its protocol

#### Scenario: Project root holds only cross-cutting files
- **WHEN** listing `.cs` files directly in the `FunkArr.ArrApi` project folder
- **THEN** none SHALL be a Newznab- or SABnzbd-specific type

### Requirement: Co-located NZB generation and parsing
NZB generation and parsing SHALL be encapsulated in `NzbService` within the `FunkArr.ArrApi.Newznab` namespace. The `Nzb` model class SHALL reside in `FunkArr.ArrApi.Newznab.Models`.

#### Scenario: Round-trip integrity
- **WHEN** an NZB is generated with title "Test Show" and url "https://example.com/video.mp4"
- **AND** the generated NZB XML is parsed back
- **THEN** the parsed title SHALL be "Test Show" and the parsed url SHALL be "https://example.com/video.mp4"

#### Scenario: Generator accessible from controller
- **WHEN** NewznabController handles a `t=get` request
- **THEN** it SHALL delegate to `NzbService` for NZB generation

#### Scenario: Parser accessible from download service
- **WHEN** SabnzbdDownloadService handles an `addfile` POST
- **THEN** it SHALL use `NzbService` for NZB parsing
