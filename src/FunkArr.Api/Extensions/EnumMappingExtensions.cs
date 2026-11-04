using ApiModels = FunkArr.Api.Models;
using Msg = FunkArr.Messages;
using MsgDownload = FunkArr.Messages.Download;
using MsgEnrichment = FunkArr.Messages.Enrichment;
using MsgScoring = FunkArr.Messages.Scoring;

namespace FunkArr.Api.Extensions;

internal static class EnumMappingExtensions
{
    public static ApiModels.MediaType ToApi(this Msg.MediaType value) =>
        value switch
        {
            Msg.MediaType.Show => ApiModels.MediaType.Show,
            Msg.MediaType.Movie => ApiModels.MediaType.Movie,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static Msg.MediaType ToMessage(this ApiModels.MediaType value) =>
        value switch
        {
            ApiModels.MediaType.Show => Msg.MediaType.Show,
            ApiModels.MediaType.Movie => Msg.MediaType.Movie,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static ApiModels.SearchSource ToApi(this Msg.SearchSource value) =>
        value switch
        {
            Msg.SearchSource.Sonarr => ApiModels.SearchSource.Sonarr,
            Msg.SearchSource.Radarr => ApiModels.SearchSource.Radarr,
            Msg.SearchSource.Prowlarr => ApiModels.SearchSource.Prowlarr,
            Msg.SearchSource.Feed => ApiModels.SearchSource.Feed,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static Msg.SearchSource ToMessage(this ApiModels.SearchSource value) =>
        value switch
        {
            ApiModels.SearchSource.Sonarr => Msg.SearchSource.Sonarr,
            ApiModels.SearchSource.Radarr => Msg.SearchSource.Radarr,
            ApiModels.SearchSource.Prowlarr => Msg.SearchSource.Prowlarr,
            ApiModels.SearchSource.Feed => Msg.SearchSource.Feed,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static ApiModels.MediathekVariant ToApi(this Msg.MediathekVariant value) =>
        value switch
        {
            Msg.MediathekVariant.Standard => ApiModels.MediathekVariant.Standard,
            Msg.MediathekVariant.AudioDescription => ApiModels.MediathekVariant.AudioDescription,
            Msg.MediathekVariant.SignLanguage => ApiModels.MediathekVariant.SignLanguage,
            Msg.MediathekVariant.PlainLanguage => ApiModels.MediathekVariant.PlainLanguage,
            Msg.MediathekVariant.OriginalVersion => ApiModels.MediathekVariant.OriginalVersion,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static Msg.MediathekVariant ToMessage(this ApiModels.MediathekVariant value) =>
        value switch
        {
            ApiModels.MediathekVariant.Standard => Msg.MediathekVariant.Standard,
            ApiModels.MediathekVariant.AudioDescription => Msg.MediathekVariant.AudioDescription,
            ApiModels.MediathekVariant.SignLanguage => Msg.MediathekVariant.SignLanguage,
            ApiModels.MediathekVariant.PlainLanguage => Msg.MediathekVariant.PlainLanguage,
            ApiModels.MediathekVariant.OriginalVersion => Msg.MediathekVariant.OriginalVersion,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static ApiModels.FilterOp ToApi(this MsgScoring.FilterOp value) =>
        value switch
        {
            MsgScoring.FilterOp.Eq => ApiModels.FilterOp.Eq,
            MsgScoring.FilterOp.Contains => ApiModels.FilterOp.Contains,
            MsgScoring.FilterOp.NotContains => ApiModels.FilterOp.NotContains,
            MsgScoring.FilterOp.GreaterThan => ApiModels.FilterOp.GreaterThan,
            MsgScoring.FilterOp.LessThan => ApiModels.FilterOp.LessThan,
            MsgScoring.FilterOp.Regex => ApiModels.FilterOp.Regex,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static MsgScoring.FilterOp ToMessage(this ApiModels.FilterOp value) =>
        value switch
        {
            ApiModels.FilterOp.Eq => MsgScoring.FilterOp.Eq,
            ApiModels.FilterOp.Contains => MsgScoring.FilterOp.Contains,
            ApiModels.FilterOp.NotContains => MsgScoring.FilterOp.NotContains,
            ApiModels.FilterOp.GreaterThan => MsgScoring.FilterOp.GreaterThan,
            ApiModels.FilterOp.LessThan => MsgScoring.FilterOp.LessThan,
            ApiModels.FilterOp.Regex => MsgScoring.FilterOp.Regex,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static ApiModels.FilterField ToApi(this MsgScoring.FilterField value) =>
        value switch
        {
            MsgScoring.FilterField.Title => ApiModels.FilterField.Title,
            MsgScoring.FilterField.Topic => ApiModels.FilterField.Topic,
            MsgScoring.FilterField.Channel => ApiModels.FilterField.Channel,
            MsgScoring.FilterField.Description => ApiModels.FilterField.Description,
            MsgScoring.FilterField.Duration => ApiModels.FilterField.Duration,
            MsgScoring.FilterField.Timestamp => ApiModels.FilterField.Timestamp,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static MsgScoring.FilterField ToMessage(this ApiModels.FilterField value) =>
        value switch
        {
            ApiModels.FilterField.Title => MsgScoring.FilterField.Title,
            ApiModels.FilterField.Topic => MsgScoring.FilterField.Topic,
            ApiModels.FilterField.Channel => MsgScoring.FilterField.Channel,
            ApiModels.FilterField.Description => MsgScoring.FilterField.Description,
            ApiModels.FilterField.Duration => MsgScoring.FilterField.Duration,
            ApiModels.FilterField.Timestamp => MsgScoring.FilterField.Timestamp,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static ApiModels.IdentificationStrategy ToApi(this MsgScoring.IdentificationStrategy value) =>
        value switch
        {
            MsgScoring.IdentificationStrategy.SeasonAndEpisodeNumber => ApiModels.IdentificationStrategy.SeasonAndEpisodeNumber,
            MsgScoring.IdentificationStrategy.AbsoluteEpisodeNumber => ApiModels.IdentificationStrategy.AbsoluteEpisodeNumber,
            MsgScoring.IdentificationStrategy.TitleExact => ApiModels.IdentificationStrategy.TitleExact,
            MsgScoring.IdentificationStrategy.TitleIncludes => ApiModels.IdentificationStrategy.TitleIncludes,
            MsgScoring.IdentificationStrategy.AirdateExtraction => ApiModels.IdentificationStrategy.AirdateExtraction,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static MsgScoring.IdentificationStrategy ToMessage(this ApiModels.IdentificationStrategy value) =>
        value switch
        {
            ApiModels.IdentificationStrategy.SeasonAndEpisodeNumber => MsgScoring.IdentificationStrategy.SeasonAndEpisodeNumber,
            ApiModels.IdentificationStrategy.AbsoluteEpisodeNumber => MsgScoring.IdentificationStrategy.AbsoluteEpisodeNumber,
            ApiModels.IdentificationStrategy.TitleExact => MsgScoring.IdentificationStrategy.TitleExact,
            ApiModels.IdentificationStrategy.TitleIncludes => MsgScoring.IdentificationStrategy.TitleIncludes,
            ApiModels.IdentificationStrategy.AirdateExtraction => MsgScoring.IdentificationStrategy.AirdateExtraction,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static ApiModels.TitlePartType ToApi(this MsgScoring.TitlePartType value) =>
        value switch
        {
            MsgScoring.TitlePartType.Static => ApiModels.TitlePartType.Static,
            MsgScoring.TitlePartType.Regex => ApiModels.TitlePartType.Regex,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static MsgScoring.TitlePartType ToMessage(this ApiModels.TitlePartType value) =>
        value switch
        {
            ApiModels.TitlePartType.Static => MsgScoring.TitlePartType.Static,
            ApiModels.TitlePartType.Regex => MsgScoring.TitlePartType.Regex,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static ApiModels.EnrichmentMethod ToApi(this MsgEnrichment.EnrichmentMethod value) =>
        value switch
        {
            MsgEnrichment.EnrichmentMethod.Title => ApiModels.EnrichmentMethod.Title,
            MsgEnrichment.EnrichmentMethod.Airdate => ApiModels.EnrichmentMethod.Airdate,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static MsgEnrichment.EnrichmentMethod ToMessage(this ApiModels.EnrichmentMethod value) =>
        value switch
        {
            ApiModels.EnrichmentMethod.Title => MsgEnrichment.EnrichmentMethod.Title,
            ApiModels.EnrichmentMethod.Airdate => MsgEnrichment.EnrichmentMethod.Airdate,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static ApiModels.RuntimeMode ToApi(this MsgEnrichment.RuntimeMode value) =>
        value switch
        {
            MsgEnrichment.RuntimeMode.Tiebreaker => ApiModels.RuntimeMode.Tiebreaker,
            MsgEnrichment.RuntimeMode.Filter => ApiModels.RuntimeMode.Filter,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static MsgEnrichment.RuntimeMode ToMessage(this ApiModels.RuntimeMode value) =>
        value switch
        {
            ApiModels.RuntimeMode.Tiebreaker => MsgEnrichment.RuntimeMode.Tiebreaker,
            ApiModels.RuntimeMode.Filter => MsgEnrichment.RuntimeMode.Filter,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static ApiModels.MatchMethod ToApi(this MsgEnrichment.MatchMethod value) =>
        value switch
        {
            MsgEnrichment.MatchMethod.RegexExtracted => ApiModels.MatchMethod.RegexExtracted,
            MsgEnrichment.MatchMethod.TitleMatch => ApiModels.MatchMethod.TitleMatch,
            MsgEnrichment.MatchMethod.AirdateMatch => ApiModels.MatchMethod.AirdateMatch,
            MsgEnrichment.MatchMethod.YearMatch => ApiModels.MatchMethod.YearMatch,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static MsgEnrichment.MatchMethod ToMessage(this ApiModels.MatchMethod value) =>
        value switch
        {
            ApiModels.MatchMethod.RegexExtracted => MsgEnrichment.MatchMethod.RegexExtracted,
            ApiModels.MatchMethod.TitleMatch => MsgEnrichment.MatchMethod.TitleMatch,
            ApiModels.MatchMethod.AirdateMatch => MsgEnrichment.MatchMethod.AirdateMatch,
            ApiModels.MatchMethod.YearMatch => MsgEnrichment.MatchMethod.YearMatch,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static ApiModels.QueuePriority ToApi(this MsgDownload.DownloadPriority value) =>
        value switch
        {
            MsgDownload.DownloadPriority.Low => ApiModels.QueuePriority.Low,
            MsgDownload.DownloadPriority.Normal => ApiModels.QueuePriority.Normal,
            MsgDownload.DownloadPriority.High => ApiModels.QueuePriority.High,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };

    public static MsgDownload.DownloadPriority ToMessage(this ApiModels.QueuePriority value) =>
        value switch
        {
            ApiModels.QueuePriority.Low => MsgDownload.DownloadPriority.Low,
            ApiModels.QueuePriority.Normal => MsgDownload.DownloadPriority.Normal,
            ApiModels.QueuePriority.High => MsgDownload.DownloadPriority.High,
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
}
