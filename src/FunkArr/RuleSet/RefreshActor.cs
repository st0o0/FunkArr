using Akka.Actor;
using Akka.Event;

namespace FunkArr.RuleSet;

internal sealed class RefreshActor : ReceiveActor
{
    private readonly GitHubReleaseClient _gitHubReleaseClient;
    private readonly ILoggingAdapter _log = Context.GetLogger();

    public sealed record DoRefresh(string CommunityPath);
    public sealed record RefreshComplete(bool Updated);

    private sealed record DoWork;

    public RefreshActor(GitHubReleaseClient gitHubReleaseClient)
    {
        _gitHubReleaseClient = gitHubReleaseClient;

        ReceiveAsync<DoRefresh>(HandleRefreshAsync);
    }

    private async Task HandleRefreshAsync(DoRefresh cmd)
    {
        try
        {
            var updated = await _gitHubReleaseClient.RefreshAsync(cmd.CommunityPath);
            Context.Parent.Tell(new RefreshComplete(updated));

            if (updated)
            {
                _log.Info("Community rulesets refreshed successfully");
            }
            else
            {
                _log.Debug("Community rulesets unchanged");
            }
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to refresh community rulesets");
            Context.Parent.Tell(new RefreshComplete(false));
        }
    }
}
