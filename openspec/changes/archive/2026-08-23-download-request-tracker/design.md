## Context

Currently `DownloadQueueActor` handles both download execution and API query responses (`GetQueue`, `GetHistory`). Sonarr polls `mode=queue` every second, hitting the same mailbox that processes download events. Phase 2b extracts the API-facing status into per-download `DownloadRequestTracker` shard entities using single-node Cluster Sharding.

## Goals / Non-Goals

**Goals:**
- Set up Akka.Cluster.Sharding infrastructure (single-node, as specified in architecture-redesign.md §6.1)
- Per-nzoId DownloadRequestTracker shard entities for API-facing status
- Event-sourced (Tier 1) persistence per tracker entity
- SabnzbdController queries trackers instead of DownloadQueueActor for status/history
- DownloadQueueActor forwards status updates to tracker entities

**Non-Goals:**
- Progress tracking in trackers (remains in DownloadQueueActor's in-memory progressMap for now; progress is per-second volatile data, not worth persisting)
- Removing GetQueue/GetHistory from DownloadQueueActor (kept for backward compat during transition)

## Decisions

### D1: Single-node Cluster Sharding

**Decision:** Add `Akka.Cluster.Hosting` package and configure a single-node cluster with Cluster Sharding. The DownloadRequestTracker ShardRegion uses nzoId as entity ID.

**Why:** Architecture-redesign.md §6.1 specifies single-node sharding. This provides entity-addressability and passivation without multi-node complexity. Phase 2c will add DownloadCoordinator as a second ShardRegion using the same cluster.

**Cluster config:**
```
akka.actor.provider = cluster
akka.remote.dot-netty.tcp.hostname = localhost
akka.remote.dot-netty.tcp.port = 0
akka.cluster.seed-nodes = ["akka.tcp://funkarr@localhost:<port>"]
akka.cluster.roles = ["download"]
```

Using `Akka.Cluster.Hosting` API: `WithClustering()` + `WithShardRegion<DownloadRequestTracker>()`.

### D2: Message envelope with entity ID extraction

**Decision:** All messages to DownloadRequestTracker carry the nzoId. Entity ID extraction: `message switch { IWithNzoId m => m.NzoId }`. Shard count: 10 (low, single-node).

**Why:** Standard Akka.Cluster.Sharding pattern. `IWithNzoId` interface on all tracker messages enables clean extraction.

### D3: DownloadRequestTracker persistence

**Decision:** Each entity has `PersistenceId: "download-request-{nzoId}"` with events: `RequestCreated`, `StatusChanged`, `Completed`, `Failed`.

**Why:** Status must survive restarts for the SABnzbd API contract. Per-entity persistence keeps journals small (~5 events per download).

### D4: QueueCoordinator creates tracker entries on Enqueue

**Decision:** When QueueCoordinator enqueues a job, it also tells the DownloadRequestTracker ShardRegion to create a tracker for that nzoId via `CreateRequest` message.

**Why:** The tracker needs to exist before status queries arrive.

### D5: SabnzbdController fans out to shard for queue/history

**Decision:** For `mode=queue`, the controller asks QueueCoordinator for the ordered nzoId list, then asks each tracker entity for status via the ShardRegion proxy. For `mode=history`, the controller asks QueueCoordinator for completed job IDs, then asks each tracker for history entries.

**Why:** Each tracker holds its own status independently. Fan-out bounded by MaxConcurrent + queue depth (typically <20).

### D6: DownloadQueueActor forwards status to shard

**Decision:** DownloadQueueActor resolves the DownloadRequestTracker ShardRegion and sends status updates (DownloadStarted, DownloadCompleted, MuxingStarted, etc.) to the appropriate entity.

**Why:** DownloadQueueActor is the source of download lifecycle events until Phase 2c.

## Risks / Trade-offs

**[Risk] Cluster setup complexity** — Single-node cluster adds configuration overhead.
→ Mitigation: `Akka.Cluster.Hosting` API handles most setup. Config is straightforward for single-node.

**[Risk] Port binding** — Cluster needs a remoting port.
→ Mitigation: Use port 0 (random) for single-node — no need for stable ports without multi-node.

**[Trade-off] Fan-out for queue queries** — Asking N trackers is more messages than asking one actor.
→ Accepted: N is small (<20), and each tracker responds from in-memory state. Fan-out isolates queries from download work, which is the goal.
