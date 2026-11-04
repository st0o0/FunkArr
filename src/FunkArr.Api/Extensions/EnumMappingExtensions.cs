using FunkArr.Api.Models.Downloads;
using FunkArr.Api.Models.RuleSets;
using FunkArr.Api.Models.Scoring;
using FunkArr.Api.Models.Shared;
using Msg = FunkArr.Messages;
using MsgDownload = FunkArr.Messages.Download;
using MsgEnrichment = FunkArr.Messages.Enrichment;
using MsgScoring = FunkArr.Messages.Scoring;

namespace FunkArr.Api.Extensions;

internal static class EnumMappingExtensions
{
    public static MediaType ToApi(this Msg.MediaType value) =>
        value switch
        {
            Msg.MediaType.Show => MediaType.Show,
            Msg.MediaType.Movie => MediaType.Movie,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static Msg.MediaType ToMessage(this MediaType value) =>
        value switch
        {
            MediaType.Show => Msg.MediaType.Show,
            MediaType.Movie => Msg.MediaType.Movie,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static SearchSource ToApi(this Msg.SearchSource value) =>
        value switch
        {
            Msg.SearchSource.Sonarr => SearchSource.Sonarr,
            Msg.SearchSource.Radarr => SearchSource.Radarr,
            Msg.SearchSource.Prowlarr => SearchSource.Prowlarr,
            Msg.SearchSource.Feed => SearchSource.Feed,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static Msg.SearchSource ToMessage(this SearchSource value) =>
        value switch
        {
            SearchSource.Sonarr => Msg.SearchSource.Sonarr,
            SearchSource.Radarr => Msg.SearchSource.Radarr,
            SearchSource.Prowlarr => Msg.SearchSource.Prowlarr,
            SearchSource.Feed => Msg.SearchSource.Feed,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static MediathekVariant ToApi(this Msg.MediathekVariant value) =>
        value switch
        {
            Msg.MediathekVariant.Standard => MediathekVariant.Standard,
            Msg.MediathekVariant.AudioDescription => MediathekVariant.AudioDescription,
            Msg.MediathekVariant.SignLanguage => MediathekVariant.SignLanguage,
            Msg.MediathekVariant.PlainLanguage => MediathekVariant.PlainLanguage,
            Msg.MediathekVariant.OriginalVersion => MediathekVariant.OriginalVersion,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static Msg.MediathekVariant ToMessage(this MediathekVariant value) =>
        value switch
        {
            MediathekVariant.Standard => Msg.MediathekVariant.Standard,
            MediathekVariant.AudioDescription => Msg.MediathekVariant.AudioDescription,
            MediathekVariant.SignLanguage => Msg.MediathekVariant.SignLanguage,
            MediathekVariant.PlainLanguage => Msg.MediathekVariant.PlainLanguage,
            MediathekVariant.OriginalVersion => Msg.MediathekVariant.OriginalVersion,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static FilterOp ToApi(this MsgScoring.FilterOp value) =>
        value switch
        {
            MsgScoring.FilterOp.Eq => FilterOp.Eq,
            MsgScoring.FilterOp.Contains => FilterOp.Contains,
            MsgScoring.FilterOp.NotContains => FilterOp.NotContains,
            MsgScoring.FilterOp.GreaterThan => FilterOp.GreaterThan,
            MsgScoring.FilterOp.LessThan => FilterOp.LessThan,
            MsgScoring.FilterOp.Regex => FilterOp.Regex,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static MsgScoring.FilterOp ToMessage(this FilterOp value) =>
        value switch
        {
            FilterOp.Eq => MsgScoring.FilterOp.Eq,
            FilterOp.Contains => MsgScoring.FilterOp.Contains,
            FilterOp.NotContains => MsgScoring.FilterOp.NotContains,
            FilterOp.GreaterThan => MsgScoring.FilterOp.GreaterThan,
            FilterOp.LessThan => MsgScoring.FilterOp.LessThan,
            FilterOp.Regex => MsgScoring.FilterOp.Regex,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static FilterField ToApi(this MsgScoring.FilterField value) =>
        value switch
        {
            MsgScoring.FilterField.Title => FilterField.Title,
            MsgScoring.FilterField.Topic => FilterField.Topic,
            MsgScoring.FilterField.Channel => FilterField.Channel,
            MsgScoring.FilterField.Description => FilterField.Description,
            MsgScoring.FilterField.Duration => FilterField.Duration,
            MsgScoring.FilterField.Timestamp => FilterField.Timestamp,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static MsgScoring.FilterField ToMessage(this FilterField value) =>
        value switch
        {
            FilterField.Title => MsgScoring.FilterField.Title,
            FilterField.Topic => MsgScoring.FilterField.Topic,
            FilterField.Channel => MsgScoring.FilterField.Channel,
            FilterField.Description => MsgScoring.FilterField.Description,
            FilterField.Duration => MsgScoring.FilterField.Duration,
            FilterField.Timestamp => MsgScoring.FilterField.Timestamp,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static IdentificationStrategy ToApi(this MsgScoring.IdentificationStrategy value) =>
        value switch
        {
            MsgScoring.IdentificationStrategy.SeasonAndEpisodeNumber => IdentificationStrategy.SeasonAndEpisodeNumber,
            MsgScoring.IdentificationStrategy.AbsoluteEpisodeNumber => IdentificationStrategy.AbsoluteEpisodeNumber,
            MsgScoring.IdentificationStrategy.TitleExact => IdentificationStrategy.TitleExact,
            MsgScoring.IdentificationStrategy.TitleIncludes => IdentificationStrategy.TitleIncludes,
            MsgScoring.IdentificationStrategy.AirdateExtraction => IdentificationStrategy.AirdateExtraction,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static MsgScoring.IdentificationStrategy ToMessage(this IdentificationStrategy value) =>
        value switch
        {
            IdentificationStrategy.SeasonAndEpisodeNumber => MsgScoring.IdentificationStrategy.SeasonAndEpisodeNumber,
            IdentificationStrategy.AbsoluteEpisodeNumber => MsgScoring.IdentificationStrategy.AbsoluteEpisodeNumber,
            IdentificationStrategy.TitleExact => MsgScoring.IdentificationStrategy.TitleExact,
            IdentificationStrategy.TitleIncludes => MsgScoring.IdentificationStrategy.TitleIncludes,
            IdentificationStrategy.AirdateExtraction => MsgScoring.IdentificationStrategy.AirdateExtraction,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static TitlePartType ToApi(this MsgScoring.TitlePartType value) =>
        value switch
        {
            MsgScoring.TitlePartType.Static => TitlePartType.Static,
            MsgScoring.TitlePartType.Regex => TitlePartType.Regex,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static MsgScoring.TitlePartType ToMessage(this TitlePartType value) =>
        value switch
        {
            TitlePartType.Static => MsgScoring.TitlePartType.Static,
            TitlePartType.Regex => MsgScoring.TitlePartType.Regex,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static EnrichmentMethod ToApi(this MsgEnrichment.EnrichmentMethod value) =>
        value switch
        {
            MsgEnrichment.EnrichmentMethod.Title => EnrichmentMethod.Title,
            MsgEnrichment.EnrichmentMethod.Airdate => EnrichmentMethod.Airdate,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static MsgEnrichment.EnrichmentMethod ToMessage(this EnrichmentMethod value) =>
        value switch
        {
            EnrichmentMethod.Title => MsgEnrichment.EnrichmentMethod.Title,
            EnrichmentMethod.Airdate => MsgEnrichment.EnrichmentMethod.Airdate,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static RuntimeMode ToApi(this MsgEnrichment.RuntimeMode value) =>
        value switch
        {
            MsgEnrichment.RuntimeMode.Tiebreaker => RuntimeMode.Tiebreaker,
            MsgEnrichment.RuntimeMode.Filter => RuntimeMode.Filter,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static MsgEnrichment.RuntimeMode ToMessage(this RuntimeMode value) =>
        value switch
        {
            RuntimeMode.Tiebreaker => MsgEnrichment.RuntimeMode.Tiebreaker,
            RuntimeMode.Filter => MsgEnrichment.RuntimeMode.Filter,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static MatchMethod ToApi(this MsgEnrichment.MatchMethod value) =>
        value switch
        {
            MsgEnrichment.MatchMethod.RegexExtracted => MatchMethod.RegexExtracted,
            MsgEnrichment.MatchMethod.TitleMatch => MatchMethod.TitleMatch,
            MsgEnrichment.MatchMethod.AirdateMatch => MatchMethod.AirdateMatch,
            MsgEnrichment.MatchMethod.YearMatch => MatchMethod.YearMatch,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static MsgEnrichment.MatchMethod ToMessage(this MatchMethod value) =>
        value switch
        {
            MatchMethod.RegexExtracted => MsgEnrichment.MatchMethod.RegexExtracted,
            MatchMethod.TitleMatch => MsgEnrichment.MatchMethod.TitleMatch,
            MatchMethod.AirdateMatch => MsgEnrichment.MatchMethod.AirdateMatch,
            MatchMethod.YearMatch => MsgEnrichment.MatchMethod.YearMatch,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static QueuePriority ToApi(this MsgDownload.DownloadPriority value) =>
        value switch
        {
            MsgDownload.DownloadPriority.Low => QueuePriority.Low,
            MsgDownload.DownloadPriority.Normal => QueuePriority.Normal,
            MsgDownload.DownloadPriority.High => QueuePriority.High,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static MsgDownload.DownloadPriority ToMessage(this QueuePriority value) =>
        value switch
        {
            QueuePriority.Low => MsgDownload.DownloadPriority.Low,
            QueuePriority.Normal => MsgDownload.DownloadPriority.Normal,
            QueuePriority.High => MsgDownload.DownloadPriority.High,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
}
