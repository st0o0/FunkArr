# Capability: Arr API Structure

## Purpose

Defines the unified ArrApi adapter project that consolidates Newznab indexer and SABnzbd download client APIs into a single project with namespace separation.

## Requirements

### Requirement: Unified ArrApi project
`FunkArr.ArrApi` SHALL be a single adapter project containing both the Newznab indexer API and SABnzbd download client API. It SHALL replace the separate `FunkArr.IndexerApi` and `FunkArr.DownloadApi` projects.

#### Scenario: Project exists and compiles
- **WHEN** `dotnet build FunkArr.slnx` is run
- **THEN** `FunkArr.ArrApi` SHALL compile successfully

#### Scenario: Both endpoint groups registered
- **WHEN** the application starts
- **THEN** `/index/api` (Newznab) and `/download/api` (SABnzbd) endpoint groups SHALL both be available

### Requirement: Namespace separation
Newznab-specific types SHALL reside in `FunkArr.ArrApi.Newznab` namespace. SABnzbd-specific types SHALL reside in `FunkArr.ArrApi.Sabnzbd` namespace. Shared types (NZB model, ApiKeyEndpointFilter, XmlHelper) SHALL reside in `FunkArr.ArrApi` root namespace.

#### Scenario: Newznab types in correct namespace
- **WHEN** examining IndexerApiEndpoints, IndexerRequest, Caps, Rss, RssJsonProjection, CapsJsonProjection, NewznabError
- **THEN** all SHALL be in `FunkArr.ArrApi.Newznab` or `FunkArr.ArrApi.Newznab.Models`

#### Scenario: SABnzbd types in correct namespace
- **WHEN** examining DownloadApiEndpoints, DownloadGetRequest, DownloadPostRequest, QueueResponse, HistoryResponse, FullStatusResponse
- **THEN** all SHALL be in `FunkArr.ArrApi.Sabnzbd` or `FunkArr.ArrApi.Sabnzbd.Models`

#### Scenario: Shared types in root namespace
- **WHEN** examining Nzb, NzbHead, NzbMeta, NzbFile, ApiKeyEndpointFilter, XmlHelper
- **THEN** all SHALL be in `FunkArr.ArrApi`

### Requirement: Unified ApiKeyEndpointFilter
A single `ApiKeyEndpointFilter` SHALL validate the `apikey` query parameter for both API surfaces. It SHALL accept an error result factory (`Func<IResult>`) to produce format-appropriate error responses.

#### Scenario: Newznab error format
- **WHEN** authentication fails on a `/index/api` endpoint
- **THEN** the error response SHALL be Newznab XML: `<error code="100" description="Invalid API Key"/>` with HTTP 403

#### Scenario: SABnzbd error format
- **WHEN** authentication fails on a `/download/api` endpoint
- **THEN** the error response SHALL be JSON: `{"status":false,"error":"API Key Incorrect"}` with HTTP 403

#### Scenario: Valid API key passes through
- **WHEN** a request includes a valid `apikey` parameter
- **THEN** the request SHALL proceed to the endpoint handler regardless of API surface

### Requirement: Co-located NZB generation and parsing
NZB generation (title+url -> NZB XML) and NZB parsing (NZB XML -> title+url) SHALL use the same `Nzb` model class. Both operations SHALL reside in the `FunkArr.ArrApi` root namespace.

#### Scenario: Round-trip integrity
- **WHEN** an NZB is generated with title "Test Show" and url "https://example.com/video.mp4"
- **AND** the generated NZB XML is parsed back
- **THEN** the parsed title SHALL be "Test Show" and the parsed url SHALL be "https://example.com/video.mp4"

#### Scenario: Generator accessible from Newznab endpoints
- **WHEN** IndexerApiEndpoints handles a `t=get` request
- **THEN** it SHALL use the shared NZB generation to produce the NZB file

#### Scenario: Parser accessible from SABnzbd endpoints
- **WHEN** DownloadApiEndpoints handles an `addfile` POST
- **THEN** it SHALL use the shared NZB parsing to extract title and url

### Requirement: No business logic in adapter
`FunkArr.ArrApi` SHALL NOT contain queue management, history tracking, retry logic, or any other domain state. All stateful operations SHALL be delegated to domain projects via Messages.

#### Scenario: No DownloadState class
- **WHEN** examining the ArrApi project
- **THEN** no class managing download queue or history state SHALL exist

#### Scenario: Download endpoints return stubs without domain actors
- **WHEN** download API endpoints are called and no domain actors are wired
- **THEN** queue SHALL return zero slots, history SHALL return zero slots, and addfile SHALL acknowledge receipt without processing

### Requirement: No hardcoded categories
Category definitions (IDs, names, subcategories) SHALL NOT be hardcoded in the adapter. The adapter SHALL receive category information from the RuleSet domain via Messages.

#### Scenario: Caps categories not hardcoded
- **WHEN** examining the Caps response construction
- **THEN** category data SHALL NOT be defined as literals in the adapter code

#### Scenario: Config categories not hardcoded
- **WHEN** examining the SABnzbd get_config response construction
- **THEN** category entries SHALL NOT be defined as literals in the adapter code
