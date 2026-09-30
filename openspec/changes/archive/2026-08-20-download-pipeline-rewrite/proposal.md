## Why

The DownloadQueueActor's `MaterializeStream()` method does not match the architecture described in existing specs. Stage classes (`DownloadStages`, `SubtitleStages`, `MuxStages`) exist but are never wired into the stream — instead a single monolithic `SelectAsyncUnordered` lambda does everything inline, defeating Akka Streams' backpressure, per-stage parallelism, and fault isolation. Additionally, recovery does not re-push queued jobs into the stream, temp files are never cleaned up, and there is no cancellation propagation.

## What Changes

- **Remove the monolithic lambda** in `MaterializeStream()` and replace it with a composed `GraphDSL` pipeline using the existing stage classes
- **Wire Partition/Merge** for HLS vs. MP4 source type routing
- **Replace `Sink.ForEach` with `Sink.ActorRef`** so stream outcomes flow directly into the actor mailbox as typed messages
- **Fix recovery bug**: call `PushQueuedJobs()` after stream materialization in `OnRecoveryCompleted`
- **Add temp-file cleanup stage** after successful muxing (as specified in muxing-pipeline spec but not implemented)
- **Thread CancellationToken** through download/mux stages via KillSwitch-linked token source
- **Remove dead code**: the unused stage variable assignments in `MaterializeStream()` and the `RetryCount` property (no retry logic exists or is planned)

## Capabilities

### New Capabilities

_(none)_

### Modified Capabilities

- `download-pipeline`: replace inline lambda with composed GraphDSL graph, add Sink.ActorRef as stream termination, add temp-file cleanup as pipeline-level concern
- `stream-supervision`: ensure recovery path calls PushQueuedJobs, add CancellationToken propagation via KillSwitch-linked token source

## Impact

- **Code**: `DownloadQueueActor.cs`, `DownloadStages.cs`, `SubtitleStages.cs`, `MuxStages.cs`, `PipelineModels.cs`, `DownloadJob.cs`
- **Configuration**: `DownloadOptions.cs` unchanged (shared parallelism per spec)
- **Tests**: existing actor/service tests remain valid; pipeline composition tests may need updates
- **APIs**: no external API changes — SABnzbd and Newznab surfaces are unaffected
