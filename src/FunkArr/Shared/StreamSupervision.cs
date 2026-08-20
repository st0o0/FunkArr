using Akka.Event;
using Akka.Streams;
using Akka.Streams.Supervision;

namespace FunkArr.Shared;

public static class StreamSupervision
{
    public static Decider LoggingDecider(ILoggingAdapter log) =>
        ex =>
        {
            var directive = Classify(ex);
            log.Warning(ex, "Stream supervision: {Directive} for {ExceptionType}", directive, ex.GetType().Name);
            return directive;
        };

    private static Directive Classify(Exception ex) => ex switch
    {
        TaskCanceledException => Directive.Resume,
        OperationCanceledException => Directive.Resume,
        _ => Directive.Stop,
    };
}
