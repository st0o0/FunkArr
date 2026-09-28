using System.Net;
using Polly;
using Polly.Retry;

namespace FunkArr.Configuration;

internal static class RetryAfterDefaults
{
    private static readonly TimeSpan _maxDelay = TimeSpan.FromSeconds(60);

    internal static ValueTask<TimeSpan?> DelayGenerator(RetryDelayGeneratorArguments<HttpResponseMessage> args)
    {
        if (args.Outcome.Result is { StatusCode: HttpStatusCode.TooManyRequests, Headers.RetryAfter.Delta: { } delta })
        {
            var capped = delta > _maxDelay ? _maxDelay : delta;
            return new ValueTask<TimeSpan?>(capped);
        }

        return new ValueTask<TimeSpan?>((TimeSpan?)null);
    }
}
