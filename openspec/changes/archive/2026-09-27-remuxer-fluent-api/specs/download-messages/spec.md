## MODIFIED Requirements

### Requirement: InitDownload command without route parameters
The `InitDownload` command SHALL carry only `DownloadId` and `Media`. It SHALL NOT carry `RouteName` or `ProxyUrl`.

#### Scenario: InitDownload shape
- **WHEN** an InitDownload is constructed
- **THEN** it SHALL contain `Guid DownloadId` and `DownloadMedia Media`
- **AND** SHALL NOT contain RouteName or ProxyUrl

## ADDED Requirements

### Requirement: DownloadInitialized persistence event without route fields
The `DownloadInitialized` persistence event SHALL contain only `DownloadId` and `Media`. The `RouteName` and `ProxyUrl` fields SHALL be removed (v0.x breaking change).

#### Scenario: DownloadInitialized shape
- **WHEN** a DownloadInitialized event is persisted
- **THEN** it SHALL contain `Guid DownloadId` and `PersistedDownloadMedia Media`
- **AND** SHALL NOT contain RouteName or ProxyUrl
