## 1. TvSearchWorker

- [x] 1.1 Move ToUnscoredResult from TvSearchWorkerStateExtensions to private method in TvSearchWorker
- [x] 1.2 Move ToScoredResult (both overloads) from TvSearchWorkerStateExtensions to private methods in TvSearchWorker
- [x] 1.3 Move ToResultItems helper to TvSearchWorker as private static method
- [x] 1.4 Remove moved methods from TvSearchWorkerState.cs

## 2. MovieSearchWorker

- [x] 2.1 Move ToUnscoredResult from MovieSearchWorkerStateExtensions to private method in MovieSearchWorker
- [x] 2.2 Move ToScoredResult (both overloads) from MovieSearchWorkerStateExtensions to private methods in MovieSearchWorker
- [x] 2.3 Move ToResultItems helper to MovieSearchWorker as private static method
- [x] 2.4 Remove moved methods from MovieSearchWorkerState.cs

## 3. Verify and format

- [x] 3.1 Verify no other callers of the moved methods exist
- [x] 3.2 Run dotnet format and verify build
- [x] 3.3 Run tests to confirm no regressions
