## MODIFIED Requirements

### Requirement: Chunked HTTP video download
`DownloadService` SHALL download the video content of a `DownloadRequest` via HTTP GET using `HttpCompletionOption.ResponseHeadersRead`, then stream the response body to a temp file in 8192-byte chunks rather than buffering the whole response in memory. This method SHALL only handle direct HTTP downloads (.mp4 URLs). HLS downloads are handled by a separate flow.

#### Scenario: Successful video download
- **WHEN** `DownloadService.DownloadAsync` is called with a `DownloadRequest` whose `VideoUrl` returns a 200 response with a video body
- **THEN** the response is read in 8192-byte chunks and each chunk is written to the temp file as it is read, and the method returns the temp file path as `VideoPath`

#### Scenario: Video download fails with non-success status
- **WHEN** the HTTP GET for `VideoUrl` returns a non-success status code
- **THEN** `DownloadService.DownloadAsync` throws (via `HttpResponseMessage.EnsureSuccessStatusCode`), and no partial temp file is left open for writing

## REMOVED Requirements

### Requirement: Subtitle download with graceful fallback
**Reason**: Subtitle download is extracted into a dedicated stream stage (`subtitle-processing` capability). `DownloadService` no longer downloads subtitles.
**Migration**: The subtitle acquisition stage in the pipeline handles subtitle downloads after the video download stage completes.

### Requirement: Temp file management via IFileService
**Reason**: Video temp path resolution remains in `DownloadService`. Subtitle temp path resolution moves to the subtitle acquisition stage.
**Migration**: `DownloadService` still uses `IFileService.GetTempVideoPath`. Subtitle paths are managed by the subtitle stage.
