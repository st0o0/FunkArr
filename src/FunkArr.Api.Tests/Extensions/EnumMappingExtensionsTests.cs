using FunkArr.Api.Extensions;
using FunkArr.Api.Models.Downloads;
using FunkArr.Api.Models.RuleSets;
using FunkArr.Api.Models.Scoring;
using FunkArr.Api.Models.Shared;
using Msg = FunkArr.Messages;
using MsgDownload = FunkArr.Messages.Download;
using MsgEnrichment = FunkArr.Messages.Enrichment;
using MsgScoring = FunkArr.Messages.Scoring;

namespace FunkArr.Api.Tests.Extensions;

public sealed class EnumMappingExtensionsTests
{
    [Fact]
    public void MediaType_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<Msg.MediaType, MediaType>(v => v.ToApi(), v => v.ToMessage());

    [Fact]
    public void SearchSource_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<Msg.SearchSource, SearchSource>(v => v.ToApi(), v => v.ToMessage());

    [Fact]
    public void MediathekVariant_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<Msg.MediathekVariant, MediathekVariant>(v => v.ToApi(), v => v.ToMessage());

    [Fact]
    public void FilterOp_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<MsgScoring.FilterOp, FilterOp>(v => v.ToApi(), v => v.ToMessage());

    [Fact]
    public void FilterField_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<MsgScoring.FilterField, FilterField>(v => v.ToApi(), v => v.ToMessage());

    [Fact]
    public void IdentificationStrategy_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<MsgScoring.IdentificationStrategy, IdentificationStrategy>(v => v.ToApi(), v => v.ToMessage());

    [Fact]
    public void TitlePartType_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<MsgScoring.TitlePartType, TitlePartType>(v => v.ToApi(), v => v.ToMessage());

    [Fact]
    public void EnrichmentMethod_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<MsgEnrichment.EnrichmentMethod, EnrichmentMethod>(v => v.ToApi(), v => v.ToMessage());

    [Fact]
    public void RuntimeMode_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<MsgEnrichment.RuntimeMode, RuntimeMode>(v => v.ToApi(), v => v.ToMessage());

    [Fact]
    public void MatchMethod_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<MsgEnrichment.MatchMethod, MatchMethod>(v => v.ToApi(), v => v.ToMessage());

    [Fact]
    public void QueuePriority_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<MsgDownload.DownloadPriority, QueuePriority>(v => v.ToApi(), v => v.ToMessage());

    [Fact]
    public void UnknownApiValue_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => MediaType.Unknown.ToMessage());

    [Fact]
    public void UnknownMessageValue_MapsToApiUnknown() =>
        Assert.Equal(MediaType.Unknown, Msg.MediaType.Unknown.ToApi());

    [Fact]
    public void UndefinedApiValue_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ((QueuePriority)5).ToMessage());

        Assert.Equal("value", ex.ParamName);
        Assert.Equal((QueuePriority)5, ex.ActualValue);
    }

    [Fact]
    public void UndefinedMessageValue_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ((Msg.MediaType)7).ToApi());

        Assert.Equal("value", ex.ParamName);
        Assert.Equal((Msg.MediaType)7, ex.ActualValue);
    }

    private static void AssertRoundtrip<TMessage, TApi>(Func<TMessage, TApi> toApi, Func<TApi, TMessage> toMessage)
        where TMessage : struct, Enum
        where TApi : struct, Enum
    {
        var messageValues = Enum.GetValues<TMessage>().Where(v => v.ToString() != "Unknown").ToArray();
        var apiValues = Enum.GetValues<TApi>().Where(v => v.ToString() != "Unknown").ToArray();
        Assert.Equal(messageValues.Length, apiValues.Length);

        foreach (var message in messageValues)
        {
            var api = toApi(message);
            Assert.Equal(message.ToString(), api.ToString());
            Assert.Equal(Convert.ToInt32(message), Convert.ToInt32(api));
            Assert.Equal(message, toMessage(api));
        }
    }
}
