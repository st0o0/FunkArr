# Cross-Domain Consistency & Queue Performance Tests

## Why

The QA plan identifies gaps in cross-domain state consistency verification
(Part E.3) and download queue behavior under load (Part H.1). No tests exist
that verify API responses are internally consistent across endpoints, and the
queue's behavior with 50+ items or ForceStart-beyond-capacity is untested.

## What Changes

- E.3 integration tests verify counts from different API endpoints match
- H.1 actor/state tests verify priority ordering at scale, ForceStart bypass,
  and rapid enqueue/dequeue stability
