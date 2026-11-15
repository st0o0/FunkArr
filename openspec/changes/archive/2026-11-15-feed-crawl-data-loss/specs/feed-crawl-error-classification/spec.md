# Feed Crawl Error Classification

## ADDED Requirements

### Requirement: Probe failure produces HarvestFailed

When the probe stage receives a non-Completed response from MediathekViewWeb (error,
QueueFull, timeout), the harvest actor SHALL produce a HarvestFailed result instead of
HarvestUnchanged. HarvestUnchanged SHALL only be produced when the probe succeeds and
the listTimestamp is unchanged (<= cursor).

#### Scenario: MVW returns error during probe

- **WHEN** the probe Ask to MediathekViewWebManager returns QueryMediathekError
- **THEN** the harvest stream fails with an exception
- **THEN** PipeTo maps the exception to HarvestFailed
- **THEN** the Manager increments ConsecutiveFailures

#### Scenario: MVW queue full during probe

- **WHEN** the probe Ask returns QueryMediathekQueueFull
- **THEN** the harvest stream fails with an exception
- **THEN** PipeTo maps the exception to HarvestFailed
- **THEN** the Manager increments ConsecutiveFailures

#### Scenario: Probe succeeds with unchanged timestamp

- **WHEN** the probe Ask returns QueryMediathekCompleted with listTimestamp <= cursor
- **THEN** the stream emits nothing (Where filter)
- **THEN** Sink.FirstOrDefault produces null
- **THEN** the result is HarvestUnchanged (not HarvestFailed)

### Requirement: Page error fails the stream

When a page fetch inside UnfoldAsync receives a non-Completed response, the stream
SHALL fail instead of returning ([], reachedEnd:true). This ensures the cursor does
not advance past items that were never fetched.

#### Scenario: MVW 503 on page 2 of 5

- **WHEN** page 1 succeeds but page 2 returns QueryMediathekError
- **THEN** the stream fails with an exception
- **THEN** PipeTo maps the exception to HarvestFailed
- **THEN** the cursor does NOT advance (stays at previous value)
- **THEN** items from page 1 are NOT published (all-or-nothing per run)

#### Scenario: Queue full during page fetch

- **WHEN** a page fetch returns QueryMediathekQueueFull
- **THEN** the stream fails (same as MVW error)
- **THEN** PipeTo maps the exception to HarvestFailed

### Requirement: HarvestFailed carries listTimestamp

HarvestFailed SHALL carry the listTimestamp from the probe (or 0 if the probe itself
failed). The Manager SHALL use this value in the IngestSummary so that the gap-advance
branch can evaluate `summary.ListTimestamp > _state.Cursor`.

#### Scenario: Page failure after successful probe

- **WHEN** the probe succeeds with listTimestamp=1700000000
- **WHEN** a subsequent page fetch fails
- **THEN** HarvestFailed carries ListTimestamp=1700000000
- **THEN** after MaxConsecutiveFailures, the gap-advance branch can fire

#### Scenario: Probe itself fails

- **WHEN** the probe returns an error
- **THEN** HarvestFailed carries ListTimestamp=0
- **THEN** the gap-advance branch does NOT fire (0 is not > cursor)
- **THEN** the cursor stays put and the next attempt retries the same window

### Requirement: MaxPages cap sets Truncated flag

When the page loop reaches MaxPages, the HarvestAccumulator SHALL have its Truncated
flag set to true before the stream completes. The Manager SHALL record this as
CompletedWithGap.

#### Scenario: Harvest reaches MaxPages limit

- **WHEN** the page loop fetches MaxPages pages without reaching the end of the window
- **THEN** the accumulator's Truncated flag is true
- **THEN** the Manager persists FeedCursorAdvanced with Gap=true
- **THEN** the CrawlOutcome is CompletedWithGap

#### Scenario: Harvest completes before MaxPages

- **WHEN** the page loop reaches the end of the window before MaxPages
- **THEN** the accumulator's Truncated flag remains false (unless the window was
  clipped by maxCatchUp)
