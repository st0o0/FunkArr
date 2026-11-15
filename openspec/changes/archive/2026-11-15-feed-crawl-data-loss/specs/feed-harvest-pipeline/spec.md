# Feed Harvest Pipeline (delta)

## MODIFIED Requirements

### Requirement: Probe stage error handling

The probe stage SHALL distinguish between "content unchanged" (listTimestamp <= cursor)
and "probe failed" (non-Completed response). A failed probe SHALL throw an exception
that PipeTo maps to HarvestFailed, not produce an empty stream that maps to
HarvestUnchanged.

#### Scenario: Probe returns QueryMediathekError

- **WHEN** the probe Ask returns QueryMediathekError
- **THEN** the Select stage throws a descriptive exception
- **THEN** PipeTo maps the exception to HarvestFailed(reason, ListTimestamp=0)

#### Scenario: Probe returns QueryMediathekQueueFull

- **WHEN** the probe Ask returns QueryMediathekQueueFull
- **THEN** the Select stage throws a descriptive exception
- **THEN** PipeTo maps the exception to HarvestFailed(reason, ListTimestamp=0)

### Requirement: Page fetch error handling

FilterPage SHALL throw on non-Completed responses instead of returning
([], reachedEnd:true). The exception fails the stream, which PipeTo maps to
HarvestFailed with the probe's listTimestamp.

#### Scenario: Page returns QueryMediathekError

- **WHEN** a page Ask returns QueryMediathekError
- **THEN** FilterPage throws an exception
- **THEN** the UnfoldAsync terminates
- **THEN** the stream fails
- **THEN** PipeTo maps to HarvestFailed(reason, ListTimestamp=probeTimestamp)

#### Scenario: Page returns QueryMediathekQueueFull

- **WHEN** a page Ask returns QueryMediathekQueueFull
- **THEN** FilterPage throws (same as error)

### Requirement: MaxPages truncation sets Truncated flag

When the page loop reaches MaxPages without reaching the end of the window, the
accumulator SHALL be marked as Truncated before the stream completes.

#### Scenario: MaxPages reached

- **WHEN** the page loop fetches MaxPages pages
- **WHEN** the last page did not reach the end of the window
- **THEN** the accumulator's Truncated flag is set to true
- **THEN** the materialized IngestSummary has Truncated=true
- **THEN** the Manager records CrawlOutcome.CompletedWithGap

### Requirement: PipeTo failure mapping carries listTimestamp

The PipeTo failure handler SHALL capture the probe's listTimestamp (available in the
ConcatMany closure scope) and include it in the HarvestFailed message.

#### Scenario: Stream failure after successful probe

- **WHEN** the probe succeeds with listTimestamp=T
- **WHEN** any subsequent stage fails
- **THEN** PipeTo failure handler produces HarvestFailed(ex.Message, ListTimestamp=T)

**Implementation note:** FilterPage now throws InvalidOperationException on non-Completed
responses instead of returning ([], reachedEnd:true). MaxPages cap sets hitMaxPages flag
which is propagated to the accumulator via MarkTruncated() in the post-publish stage.
