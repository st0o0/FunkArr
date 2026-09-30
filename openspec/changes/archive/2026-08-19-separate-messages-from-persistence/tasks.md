## 1. Create persistence DTOs

- [x] 1.1 Create `Persistence/DownloadEventDtos.cs` with all 7 DTO classes (`DownloadEnqueuedDto`, `DownloadStartedDto`, `DownloadCompletedDto`, `DownloadFailedDto`, `MuxingStartedDto`, `MuxingCompletedDto`, `MuxingFailedDto`) — each `sealed class` with `[JsonProperty]` short keys and `int Version = 1`
- [x] 1.2 Add static mapping class `DownloadEventDtoMapping` in the same file with `ToDto()` and `ToDomain()` overloads per event type

## 2. Switch DownloadQueueActor to DTOs

- [x] 2.1 Change all `Persist(evt, ...)` calls to `Persist(DownloadEventDtoMapping.ToDto(evt), ...)`
- [x] 2.2 Change all `Recover<DownloadEvents.*>(ApplyEvent)` to `Recover<*Dto>(dto => ApplyEvent(DownloadEventDtoMapping.ToDomain(dto)))`

## 3. Verification

- [x] 3.1 Unit tests for `DownloadEventDtoMapping` roundtrip (ToDto → ToDomain) per event type
- [x] 3.2 Verify build — `dotnet build` must succeed
