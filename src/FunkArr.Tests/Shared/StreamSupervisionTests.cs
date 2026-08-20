using Akka.Event;
using Akka.Streams.Supervision;
using FunkArr.Shared;

namespace FunkArr.Tests.Shared;

public class StreamSupervisionTests
{
    [Theory]
    [InlineData(typeof(TaskCanceledException), Directive.Resume)]
    [InlineData(typeof(OperationCanceledException), Directive.Resume)]
    [InlineData(typeof(NullReferenceException), Directive.Stop)]
    [InlineData(typeof(HttpRequestException), Directive.Stop)]
    [InlineData(typeof(InvalidOperationException), Directive.Stop)]
    [InlineData(typeof(IOException), Directive.Stop)]
    public void LoggingDecider_ClassifiesExceptions(Type exceptionType, Directive expected)
    {
        var decider = StreamSupervision.LoggingDecider(NoLogger.Instance);

        var exception = (Exception)Activator.CreateInstance(exceptionType)!;
        var result = decider(exception);

        Assert.Equal(expected, result);
    }
}
