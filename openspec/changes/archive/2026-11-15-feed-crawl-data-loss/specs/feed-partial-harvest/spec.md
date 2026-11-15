# Feed Partial Harvest (delta)

## MODIFIED Requirements

### Requirement: Page failures fail the stream

A page-level failure (MVW error, QueueFull) SHALL fail the entire stream. No partial
collection of earlier pages is attempted. This is consistent with the sequential
UnfoldAsync architecture: pages are fetched one at a time, and a mid-stream failure
means the window is incomplete.

#### Scenario: Page 3 of 5 fails

- **WHEN** pages 1-2 succeed but page 3 returns an error
- **THEN** the stream fails
- **THEN** items from pages 1-2 are NOT published (all-or-nothing)
- **THEN** the cursor does NOT advance
- **THEN** the Manager receives HarvestFailed with the probe's listTimestamp

#### Scenario: CompletedWithGap only from window truncation

- **WHEN** a harvest completes with Truncated=true (MaxPages or maxCatchUp)
- **THEN** the Manager records CompletedWithGap
- **WHEN** a harvest fails due to page error
- **THEN** the Manager records CrawlOutcome.Failed (not CompletedWithGap)

**Implementation note:** Page failures now throw inside FilterPage (instead of returning
empty results), which fails the entire Akka.Streams pipeline. PipeTo maps the exception
to HarvestFailed with the probe's captured listTimestamp. This is confirmed by tests:
`Page_fetch_error_response_produces_HarvestFailed` and `Page_failure_does_not_advance_cursor`.
