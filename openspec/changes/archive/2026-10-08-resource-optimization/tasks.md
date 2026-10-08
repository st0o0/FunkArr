## 1. FFmpeg Thread Limit

- [x] 1.1 Add `-threads 1` to input options in `FfmpegProcess.BuildArguments`
- [x] 1.2 Add test assertion in `FfmpegProcess` tests verifying `-threads 1` appears in built arguments

## 2. Journal Cleanup

- [x] 2.1 Update `DownloadManager.SaveSnapshotSuccess` handler to call `DeleteMessages` and `DeleteSnapshots`
- [x] 2.2 Update `DownloadHistoryManager.SaveSnapshotSuccess` handler to call `DeleteMessages` and `DeleteSnapshots`
- [x] 2.3 Update `HistoryWorker.SaveSnapshotSuccess` handler to call `DeleteMessages` and `DeleteSnapshots`
- [x] 2.4 Add `DeleteMessagesSuccess` and `DeleteSnapshotSuccess` handlers (no-op, suppress unhandled message warnings)

## 3. Verify

- [x] 3.1 Run existing tests to verify no regressions
- [x] 3.2 Run `dotnet format` to ensure style compliance
