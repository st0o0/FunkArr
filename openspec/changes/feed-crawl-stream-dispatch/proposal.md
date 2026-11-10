## Why

FeedCrawlPageActor uses SmallestMailboxPool + IWithUnboundedStash, which is broken by design: Stash hides mailbox depth from the pool router, defeating its routing heuristic. The Manager manually tracks PendingPages/PagesFired/NextOffset — reimplementing what Akka.Streams SelectAsync gives for free via backpressure. The Manager has 5 Become-states and ~20 methods for a linear flow that could be expressed more simply. The PageActor also violates project conventions (mutable state on actor, _manager.Tell instead of Sender.Tell, wrong casing on static readonly fields).

## What Changes

- Replace the manual page dispatch loop (Tell to pool + PendingPages tracking) in FeedCrawlManager with an Akka.Streams pipeline: `Source → SelectAsync(n, Ask) → TakeWhile → Sink.Seq → PipeTo`
- Collapse Manager's 5 Become-states into 3 by merging Probing+LoadingRuleSets and Crawling+Publishing
- Inline small handler methods as lambdas instead of separate HandleX methods
- Clean up FeedCrawlPageActor: State object, remove Stash, reply to Sender (captured as _replyTo), PascalCase static readonly fields
- Slim down FeedCrawlRunState by removing dispatch-only fields and methods (PendingPages, NextOffset, PagesFired, HasMorePages, StartCrawling, NextPageRequest)

## Capabilities

### New Capabilities

- `crawl-stream-dispatch`: Akka.Streams-based page dispatch replacing manual pool Tell-loop with SelectAsync + Ask + Sink.Seq + PipeTo pattern

### Modified Capabilities

- `feed-crawl-page-actor`: PageActor removes Stash, adds State object, replies to captured Sender instead of hardcoded _manager reference — required for Ask-based dispatch to work
- `feed-actor-topology`: Pool creation stays but Stash is removed; Manager states collapse from 5 to 3

## Impact

- `src/FunkArr.Feed/Crawl/FeedCrawlManager.cs` — stream dispatch, state collapse, inline handlers
- `src/FunkArr.Feed/Crawl/FeedCrawlPageActor.cs` — convention cleanup, Stash removal, Sender reply
- `src/FunkArr.Feed/Crawl/FeedCrawlPageActorState.cs` — new file, State object
- `src/FunkArr.Feed/Crawl/FeedCrawlRunState.cs` — remove dispatch fields/methods
- `src/FunkArr.Feed/Crawl/FeedCrawlPageMessages.cs` — unchanged (ProcessPage/PageProcessed stay)
- `src/FunkArr.Feed.Tests/` — tests adapt to new Manager behavior and PageActor reply pattern
- No API changes, no persistence changes, no message shape changes
