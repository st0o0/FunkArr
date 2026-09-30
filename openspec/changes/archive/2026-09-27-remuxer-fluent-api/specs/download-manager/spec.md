## MODIFIED Requirements

### Requirement: DownloadManager does not resolve routes
The DownloadManager SHALL NOT depend on `IRouteResolver`. Route resolution SHALL be handled by the Remuxer at download time.

#### Scenario: Enqueueing a download
- **WHEN** the DownloadManager receives an AddDownload command
- **THEN** it SHALL create an InitDownload with the media (containing Channel) but without RouteName or ProxyUrl
