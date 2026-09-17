# shared-constants

## Purpose

Centralized string constants for HTTP headers, HttpClient names, media types, and resolution strategies used across multiple FunkArr projects, eliminating magic strings.

## Requirements

### Requirement: FunkArr custom HTTP headers are centralized constants

FunkArr.Core SHALL define a static class `FunkArrHeaders` containing string constants for all custom `X-FunkArr-*` HTTP headers used in NZB metadata exchange between Newznab and SABnzbd endpoints.

#### Scenario: Header constants defined

- **WHEN** `FunkArrHeaders` is inspected
- **THEN** it SHALL contain constants for `X-FunkArr-Url`, `X-FunkArr-Channel`, `X-FunkArr-Duration`, `X-FunkArr-Size`, `X-FunkArr-Title`, and `X-FunkArr-SubtitleUrl`
- **AND** all usages of these header strings across FunkArr.ArrApi SHALL reference these constants

### Requirement: Named HttpClient keys are centralized constants

FunkArr.Core SHALL define a static class `HttpClientNames` containing string constants for all named HttpClient registrations.

#### Scenario: HttpClient name constants defined

- **WHEN** `HttpClientNames` is inspected
- **THEN** it SHALL contain constants for `"MediathekViewWeb"` and `"GitHub"`
- **AND** both the registration site (setup containers) and the usage site (IHttpClientFactory.CreateClient) SHALL reference these constants

### Requirement: Common media types are centralized constants

FunkArr.Core SHALL define a static class `MediaTypes` containing string constants for content types used across multiple projects.

#### Scenario: Media type constants defined

- **WHEN** `MediaTypes` is inspected
- **THEN** it SHALL contain constants for at minimum `"application/xml"`, `"application/x-nzb"`, `"text/plain"`, `"text/event-stream"`, and `"application/json"` where these are used as literal strings in endpoint or client code
- **AND** all usages of these content type strings SHALL reference these constants
