# Feed Partial Harvest

Partial failure handling with the stream architecture.

## Behavior

With sequential page fetch via UnfoldAsync, a page failure fails the
stream entirely. There is no partial collection because pages are
processed one at a time — a failed page stops the loop.

### Consequence

- Partial harvest (CompletedWithGap from page failures) is no longer
  possible in the fetch phase
- CompletedWithGap is still used for truncated windows (cursor too far
  behind, maxCatchUp applied)
- The Manager's consecutive failure tracking (MaxConsecutiveFailures=3
  before cursor force-advance) remains unchanged and handles repeated
  failures

### Evaluation Failures

Individual evaluation group failures within a successfully fetched set
of pages can be handled gracefully:
- EvaluateAsync can use `.Recover()` on the Ask Task to return a
  ScoredGroup with empty results instead of failing the stream
- Or: let eval failures fail the stream (simpler, consistent with
  page failure behavior)
