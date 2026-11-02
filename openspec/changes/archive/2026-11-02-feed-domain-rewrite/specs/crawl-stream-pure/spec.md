## REMOVED Requirements

### Requirement: Single ConcatMany at the page fan-out
**Reason**: The stream pipeline is replaced entirely by actor Become phases with pool-based parallelism. Page fetching is handled by FeedPageActor via SmallestMailboxPool, not by stream SelectAsync stages.
**Migration**: Remove all Akka.Streams code from the Feed domain. Page fetching moves to FeedCrawlWorker's Harvesting phase + FeedPageActor pool. Evaluation moves to Evaluating phase + FeedEvalActor pool. Publishing moves to Publishing phase with direct Ask to FeedStoreManager.

### Requirement: CrawlInput carries all context
**Reason**: CrawlInput still carries context, but it is consumed by actor phases and pool workers, not by a stream pipeline. The requirement as written is stream-specific.
**Migration**: CrawlInput continues to exist and carry the options snapshot. FeedCrawlWorkerState holds the progressive context (window, lookup, collected items) as the worker transitions through Become phases.

### Requirement: Errors flow as data records
**Reason**: Error handling moves to actor message responses (PageResult.Error, GroupResult.Error) instead of stream error records with LoggingDecider. Each pool actor handles its own errors and returns them as data in the response message.
**Migration**: FeedPageActor and FeedEvalActor return error information in their response messages. FeedCrawlWorker collects errors in state and builds CrawlResult from them.

### Requirement: Counting in stream closures
**Reason**: Mutable closure-captured counting variables are eliminated. Counts are tracked in FeedCrawlWorkerState as the worker processes items through actor phases — collected count from Harvesting phase, matched count from the sync matching step in Evaluating phase.
**Migration**: FeedCrawlWorkerState tracks collected and matched counts as immutable state fields updated via extension methods.

### Requirement: One CrawlResult with everything
**Reason**: CrawlResult is still produced, but by the FeedCrawlWorker actor assembling it from state after all phases complete, not by a stream Aggregate stage. The worker sends it to Context.Parent and stops.
**Migration**: FeedCrawlWorker builds CrawlResult from its state (collected, matched, published, failed groups, duration, errors) in the Publishing→Complete transition.
