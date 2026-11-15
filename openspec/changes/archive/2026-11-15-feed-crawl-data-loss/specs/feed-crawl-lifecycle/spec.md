# Feed Crawl Lifecycle (delta)

## MODIFIED Requirements

### Requirement: Cursor force-advance after MaxConsecutiveFailures

The Manager SHALL force-advance the cursor after MaxConsecutiveFailures (3) consecutive
failures, provided the failure carries a valid ListTimestamp (> 0 and > current cursor).
HarvestFailed SHALL carry the probe's listTimestamp when the probe succeeded, or 0 when
the probe itself failed. The IngestSummary for CrawlOutcome.Failed SHALL use the
HarvestFailed's ListTimestamp instead of hardcoded 0.

#### Scenario: Three consecutive page failures with valid listTimestamp

- **WHEN** the probe succeeds with listTimestamp=T (T > cursor)
- **WHEN** a page fetch fails, producing HarvestFailed(ListTimestamp=T)
- **WHEN** this is the 3rd consecutive failure
- **THEN** the Manager persists FeedCursorAdvanced(Cursor=T, Gap=true)
- **THEN** ConsecutiveFailures resets to 0
- **THEN** the next crawl starts from cursor=T

#### Scenario: Three consecutive probe failures (no listTimestamp)

- **WHEN** the probe itself fails (ListTimestamp=0)
- **WHEN** this is the 3rd consecutive failure
- **THEN** the gap-advance branch does NOT fire (0 is not > cursor)
- **THEN** the Manager persists FeedRunFailed with failures=3
- **THEN** the cursor stays at its current value

### Requirement: HarvestUnchanged is persisted

The Manager SHALL persist a FeedUnchangedRun event when it receives HarvestUnchanged.
The state SHALL update LastRunAt and LastOutcome from this event so that the values
survive container restarts.

#### Scenario: Unchanged run persisted and recovered

- **WHEN** the Manager receives HarvestUnchanged
- **THEN** it persists FeedUnchangedRun(Timestamp=now)
- **THEN** _state.LastRunAt reflects the unchanged run's timestamp
- **THEN** after a container restart, LastRunAt is restored from the persisted event

#### Scenario: Unchanged run participates in snapshot

- **WHEN** a FeedUnchangedRun is persisted
- **THEN** it increments LastSequenceNr
- **THEN** it participates in the 20-event snapshot interval

## MODIFIED Requirements

### Requirement: HarvestFailed message carries ListTimestamp

HarvestFailed SHALL be extended to `HarvestFailed(string Reason, long ListTimestamp)`.
The harvester actor passes the probe's listTimestamp (or 0 if probe failed). The Manager
constructs IngestSummary with this ListTimestamp.

#### Scenario: HarvestFailed from page error

- **WHEN** probe succeeded with listTimestamp=T
- **WHEN** page fetch fails
- **THEN** HarvestFailed carries Reason=<error message> and ListTimestamp=T

#### Scenario: HarvestFailed from probe error

- **WHEN** probe itself fails
- **THEN** HarvestFailed carries Reason=<error message> and ListTimestamp=0

#### Scenario: HarvestUnchanged is persisted (implemented)

- **WHEN** the Manager receives HarvestUnchanged
- **THEN** it persists FeedUnchangedRun(Timestamp=now)
- **THEN** LastRunAt and LastOutcome survive container restart via event replay and snapshots
