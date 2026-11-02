using Akka.Actor;
using Akka.Event;
using FunkArr.Core;
using FunkArr.Messages.Feed;
using Microsoft.Extensions.Options;
using Terp.Akka;

namespace FunkArr.Feed;

public sealed class FeedScheduler : ReceiveActor, IWithTimers
{
    private const string _timerKey = "crawl";

    private static readonly TimeSpan _startupDelay = TimeSpan.FromSeconds(30);

    private sealed record Tick;
    private sealed record OptionsChanged;

    private readonly ILoggingAdapter _log = Context.GetLogger();
    private readonly IActorRef _crawlManager = Context.GetActor<IFeedCrawlManager>();
    private readonly IOptionsMonitor<FeedOptions> _optionsMonitor;
    private FeedSchedulerState _state = FeedSchedulerState.Empty;

    public ITimerScheduler Timers { get; set; } = null!;

    public FeedScheduler(IOptionsMonitor<FeedOptions> options)
    {
        _optionsMonitor = options;
        _state = _state.Apply(options.CurrentValue);

        Receive<Tick>(_ => HandleTick());
        Receive<OptionsChanged>(_ => HandleOptionsChanged());

        var self = Self;
        options.OnChange((_, _) => self.Tell(new OptionsChanged()));

        ScheduleNext(_startupDelay);
    }

    private void HandleTick()
    {
        if (_state.Enabled)
        {
            _crawlManager.Tell(new StartFeedCrawl());
        }

        ScheduleNext(_state.Interval);
    }

    private void HandleOptionsChanged()
    {
        var previous = _state;
        _state = _state.Apply(_optionsMonitor.CurrentValue);

        if (_state.Interval != previous.Interval)
        {
            _log.Info("Feed crawl interval changed from {Old} to {New}", previous.Interval, _state.Interval);
            ScheduleNext(_state.Interval);
        }
    }

    private void ScheduleNext(TimeSpan delay) =>
        Timers.StartSingleTimer(_timerKey, new Tick(), delay);
}
