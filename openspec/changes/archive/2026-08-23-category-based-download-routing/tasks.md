## 1. Configuration

- [x] 1.1 Rename `DownloadOptions.DownloadPath` to `Path` and add `Dictionary<string, string> Category` property with empty dictionary default
- [x] 1.2 Update `appsettings.json` and `appsettings.Development.json` — rename `DownloadPath` to `Path`
- [x] 1.3 Update all references to `DownloadOptions.DownloadPath` across the codebase to use `Path`
- [x] 1.4 Update `DownloadOptionsValidator` if it validates `DownloadPath` to validate `Path` instead

## 2. Category Resolution (in FileService)

- [x] 2.1 Create static `CategoryResolver.Resolve(string basePath, string? category, Dictionary<string, string> categoryConfig)` method with three-tier resolution logic (absolute override, relative override, subfolder fallback, no-category default)
- [x] 2.2 Use `StringComparer.OrdinalIgnoreCase` for category dictionary lookup
- [x] 2.3 Add filesystem character sanitization for fallback subfolder names
- [x] 2.4 Update `FileService` — store `_categoryConfig` from options, update `GetOutputPath(title, category?)` and `EnsureOutputDirectory(title, category?)` to resolve via `CategoryResolver` internally
- [x] 2.5 Update `IFileService` interface — add `category?` parameter to `GetOutputPath` and `EnsureOutputDirectory`
- [x] 2.6 Write unit tests for `CategoryResolver` — all four resolution tiers, case insensitivity, invalid characters
- [x] 2.7 Update `FileServiceTests` for new category-aware output path scenarios

## 3. Pipeline Messages

- [x] 3.1 Add `string? Category` to `QueueCoordinator.Enqueue` message
- [x] 3.2 Add `string? Category` to `QueueCoordinator.JobEnqueued` event and internal queue entry state
- [x] 3.3 Add `string? Category` to `DownloadCoordinator.StartDownload` message (no OutputDir — category only)
- [x] 3.3b Add `string? Category` to `RemuxVideo` internal message (for FfmpegService → FileService resolution)
- [x] 3.4 Add `string? Category` to `DownloadRequestTracker.CreateRequest` message
- [x] 3.5 Add `string? Category` to `DownloadRequestTracker.StatusResponse` and `HistoryEntryResponse`
- [x] 3.6 Add `string? Category` to `QueueCoordinator` queue order response

## 4. Persistence DTOs

- [x] 4.1 Add `[JsonProperty("cat")] public string? Category` to `JobEnqueuedDto` in QueueCoordinator journal
- [x] 4.2 Add `[JsonProperty("cat")] public string? Category` to `RequestCreatedDto` in DownloadRequestTracker journal
- [x] 4.3 Add `[JsonProperty("cat")] public string? Category` to `JobAcceptedDto` in DownloadCoordinator journal
- [x] 4.4 Update `ToDto()`/`ToDomain()` mappings in all three journal files

## 5. Actor Logic

- [x] 5.1 Update `QueueCoordinator` — store category in queue entries, pass category (not resolved path) in `StartDownload`, pass category to `DownloadRequestTracker.CreateRequest`
- [x] 5.2 Update `DownloadCoordinator` — store `_category` from `StartDownload`, persist in `JobAccepted` event, pass category to `RemuxVideo`, use `_fileService.GetOutputPath(title, category)` for completion path
- [x] 5.3 Update `DownloadRequestTracker` — accept category in `CreateRequest`, persist in `RequestCreated`, include in `StatusResponse` and `HistoryEntryResponse`
- [x] 5.4 Update `QueueCoordinator` recovery to reconstruct category from replayed events
- [x] 5.5 Update `FfmpegService` / `RemuxWorker` — accept category from `RemuxVideo`, pass to `FileService.GetOutputPath(title, category)` and `EnsureOutputDirectory(title, category)`

## 6. API Controllers

- [x] 6.1 Update `SabnzbdController.HandleAddFile` — accept `cat` query parameter, pass to `QueueCoordinator.Enqueue`
- [x] 6.2 Update `SabnzbdController.HandleGetConfig` — populate `categories` from `DownloadOptions.Category` dynamically with resolved paths instead of hard-coded entries
- [x] 6.3 Update `SabnzbdController` queue response — include `cat` field from tracker `StatusResponse`
- [x] 6.4 Update `SabnzbdController` history response — include `cat` field from tracker `HistoryEntryResponse`
- [x] 6.5 Update `QueueController` queue response — include `category` field
- [x] 6.6 Update `QueueController` history response — include `category` field
- [x] 6.7 Update SABnzbd response models (`SabnzbdQueueSlot`, `SabnzbdHistorySlot`) — add `cat` property with `[JsonPropertyName("cat")]`
- [x] 6.8 Update clean API response models (`QueueItemResponse`, `HistoryItemResponse`) — add `Category` property

## 7. Tests

- [x] 7.1 Update existing `QueueCoordinator` tests for category threading
- [x] 7.2 Update existing `DownloadRequestTracker` tests for category field
- [x] 7.3 Update existing `SabnzbdController` tests for `cat` parameter acceptance and response inclusion
- [x] 7.4 Verify persistence backwards compatibility — old events without `cat` deserialize with null category
