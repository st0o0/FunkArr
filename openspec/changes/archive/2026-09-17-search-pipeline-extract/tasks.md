## 1. Extract SearchPipeline

- [x] 1.1 Create `SearchPipeline` static class in FunkArr.Search with `BuildUnscoredResult`, `BuildScoredResult`, and `BuildResultItems` methods extracted from TvSearchWorker
- [x] 1.2 Replace TvSearchWorker's private Build* methods with calls to SearchPipeline
- [x] 1.3 Replace MovieSearchWorker's private Build* methods with calls to SearchPipeline
- [x] 1.4 Verify build compiles and all Search tests pass
- [x] 1.5 Run `dotnet format`
