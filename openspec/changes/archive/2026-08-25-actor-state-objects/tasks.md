## 1. Simple persistent actors (no snapshots, no partials)

- [x] 1.1 Extract DownloadRequestActorState from DownloadRequestActor — create state class, move Apply methods, add IsInitialized guard, update actor to use _state field
- [x] 1.2 Extract DownloadActorState from DownloadActor — state machine with stage tracking, move Apply methods, add stage guard properties
- [x] 1.3 Extract QueueActorState from QueueActor — move queue/active/allJobs collections and Apply methods into state class

## 2. Persistent actors with snapshots (no partials currently)

- [x] 2.1 Extract RecentMatchActorState from RecentMatchActor — move state fields, Apply methods, add ToSnapshot()/FromSnapshot() factory

## 3. Persistent actors with snapshots and partials (collapse partials)

- [x] 3.1 Extract ShowActorState from ShowActor — move state fields, Apply methods, ruleset merging logic, ToSnapshot()/FromSnapshot(); merge Messages.cs inline into actor; keep Events.cs as standalone static class
- [x] 3.2 Extract MovieActorState from MovieActor — same pattern as ShowActor; merge Messages.cs inline; keep Events.cs standalone
- [x] 3.3 Extract RuleSetRegistryActorState from RuleSetRegistryActor — move state fields, Apply methods, ToSnapshot()/FromSnapshot(); merge Messages.cs inline; keep Events.cs standalone

## 4. Verification

- [x] 4.1 Build passes (dotnet build FunkArr.slnx)
- [x] 4.2 All existing tests pass (dotnet run --project FunkArr.Tests/FunkArr.Tests.csproj)
- [x] 4.3 Run dotnet format on all modified files
