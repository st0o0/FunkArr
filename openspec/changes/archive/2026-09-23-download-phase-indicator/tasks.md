## 1. Backend — Phase derivation helper

- [x] 1.1 Create `DownloadPhase` enum (`Downloading`, `Remuxing`) in `FunkArr.Messages/Download/`
- [x] 1.2 Create static helper method `DerivePhase(long bytesDownloaded, long totalBytes, long currentTimeUs)` that returns `DownloadPhase` — place in a shared location accessible to both Api and ArrApi mappers

## 2. Internal API — Phase-aware response

- [x] 2.1 Add `Phase` string property to `DownloadQueueItem` record in `FunkArr.Api/Models/DownloadQueue.cs`
- [x] 2.2 Update `DownloadMappingExtensions.ToApi()` to derive phase and calculate percentage per phase (bytes-based for downloading, time-based for remuxing)

## 3. SABnzbd API — Phase-aware percentage

- [x] 3.1 Update `SabnzbdResponseMapper.BuildQueueSlot()` percentage calculation to use phase-aware logic (bytes-based during download, time-based during remux)
- [x] 3.2 Keep `Status` field as `"Downloading"` for both phases (Sonarr/Radarr compatibility)

## 4. Frontend — Phase label in UI

- [x] 4.1 Add `phase` field to `QueueItem` interface in `FunkArr.UI/src/api/downloads.ts`
- [x] 4.2 Add i18n keys for phase labels: `activity.phase.downloading` / `activity.phase.remuxing` in all 4 locales (de, en, de-AT, de-CH)
- [x] 4.3 Update `ActiveDownloadCard.vue` to show phase label next to percentage text

## 5. Verification

- [x] 5.1 Build solution: `dotnet build src/FunkArr.slnx`
- [x] 5.2 Run format check: `dotnet format src/FunkArr.slnx --verify-no-changes`
- [x] 5.3 Run all test projects
