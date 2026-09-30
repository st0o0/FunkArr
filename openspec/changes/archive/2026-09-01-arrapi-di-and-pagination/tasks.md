## 1. Search Messages — Add Pagination Parameters

- [x] 1.1 Add `Limit` (int?) and `Offset` (int?) to `TvSearchCommand` in `FunkArr.Messages/Search/TvSearchCommand.cs`
- [x] 1.2 Add `Limit` (int?) and `Offset` (int?) to `MovieSearchCommand` in `FunkArr.Messages/Search/MovieSearchCommand.cs`
- [x] 1.3 Add `Limit` (int?) and `Offset` (int?) to `GeneralSearchCommand` in `FunkArr.Messages/Search/GeneralSearchCommand.cs`

## 2. Search Workers — Use Pagination in MediathekQuery

- [x] 2.1 Update `TvSearchWorker` to use `cmd.Limit ?? 50` for Size and `cmd.Offset ?? 0` for Offset in the MediathekQuery
- [x] 2.2 Update `MovieSearchWorker` to use `cmd.Limit ?? 50` for Size and `cmd.Offset ?? 0` for Offset in the MediathekQuery

## 3. SearchManager — Forward Pagination

- [x] 3.1 Update `SearchManager.HandleTvSearch` to forward Limit and Offset from incoming command to the new TvSearchCommand
- [x] 3.2 Update `SearchManager.HandleMovieSearch` to forward Limit and Offset from incoming command to the new MovieSearchCommand
- [x] 3.3 Update `SearchManager.RouteToTv`, `RouteToMovie`, `RouteToAll` to forward Limit and Offset from GeneralSearchCommand

## 4. ArrApi — DI and Pagination in Endpoints

- [x] 4.1 Refactor `ApiKeyEndpointFilter` to resolve API key from `IOptions<FunkArrOptions>` via `HttpContext.RequestServices` instead of constructor parameter
- [x] 4.2 Refactor `IndexerApiEndpoints.MapIndexerApi` to be parameterless, resolve `IActorRegistry` and `IOptions<FunkArrOptions>` via DI in handler
- [x] 4.3 Update `HandleTvSearch`, `HandleMovieSearch`, `HandleGeneralSearch` to pass `req.Limit` (capped to 500) and `req.Offset` to search commands
- [x] 4.4 Update `ToRss` to slice items with offset/limit and set correct Total
- [x] 4.5 Update Caps `Limits` to `Max=500, Default=100`
- [x] 4.6 Refactor `DownloadApiEndpoints.MapDownloadApi` to be parameterless, resolve `IOptions<FunkArrOptions>` via DI in handler

## 5. Host — Simplify ApplicationSetupContainer

- [x] 5.1 Update `ApplicationSetupContainer.SetupApplication` to call parameterless `app.MapIndexerApi()` and `app.MapDownloadApi()`, remove manual IActorRegistry/IOptions resolution

## 6. Tests — Update for New Signatures

- [x] 6.1 Update `SearchManagerTests` for new command constructors with Limit/Offset parameters
- [x] 6.2 Update `TvSearchWorkerTests` for new TvSearchCommand constructor and verify pagination is forwarded to MediathekQuery
- [x] 6.3 Update `MovieSearchWorkerTests` for new MovieSearchCommand constructor and verify pagination is forwarded to MediathekQuery
- [x] 6.4 Update `IndexerApiEndpoints` tests (if any) for DI-based resolution and pagination in ToRss
- [x] 6.5 Run `dotnet build FunkArr.slnx` and `dotnet format` to verify everything compiles and passes style checks
