using Akka.Actor;
using Akka.Event;
using FunkArr.Shared.Models;

namespace FunkArr.Search;

internal sealed class QualityProbeWorker : ReceiveActor
{
    private readonly QualityProbeService _qualityProbeService;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public QualityProbeWorker(QualityProbeService qualityProbeService)
    {
        _qualityProbeService = qualityProbeService;

        ReceiveAsync<ProbeUrls>(HandleAsync);
    }

    private async Task HandleAsync(ProbeUrls message)
    {
        var results = new List<SearchResult>(message.Results.Count);
        var probed = 0;

        foreach (var result in message.Results)
        {
            if (probed < message.ProbeLimit)
            {
                try
                {
                    var qualityInfo = await _qualityProbeService.ProbeAsync(
                        result.Url, result.Quality, result.DurationSeconds);

                    results.Add(result with
                    {
                        SizeBytes = qualityInfo.FileSize,
                        Quality = qualityInfo.QualityTier,
                        QualityInfo = qualityInfo,
                    });
                    probed++;
                }
                catch (Exception ex)
                {
                    _log.Warning(ex, "Quality probe failed for '{Url}'", result.Url);
                    results.Add(result);
                }
            }
            else
            {
                results.Add(result);
            }
        }

        _log.Debug("Probed {Probed}/{Total} URLs", probed, message.Results.Count);
        Sender.Tell(new UrlsProbed(results));
    }
}
