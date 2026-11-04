using FunkArr.Api.Extensions;
using ApiModels = FunkArr.Api.Models;
using Msg = FunkArr.Messages;
using MsgDownload = FunkArr.Messages.Download;
using MsgEnrichment = FunkArr.Messages.Enrichment;
using MsgScoring = FunkArr.Messages.Scoring;

namespace FunkArr.Api.Tests.Extensions;

public sealed class EnumMappingExtensionsTests
{
    [Fact]
    public void MediaType_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<Msg.MediaType, ApiModels.MediaType>(v => v.ToApi(), v => v.ToMessage());

    [Fact]
    public void SearchSource_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<Msg.SearchSource, ApiModels.SearchSource>(v => v.ToApi(), v => v.ToMessage());

    [Fact]
    public void MediathekVariant_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<Msg.MediathekVariant, ApiModels.MediathekVariant>(v => v.ToApi(), v => v.ToMessage());

    [Fact]
    public void FilterOp_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<MsgScoring.FilterOp, ApiModels.FilterOp>(v => v.ToApi(), v => v.ToMessage());

    [Fact]
    public void FilterField_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<MsgScoring.FilterField, ApiModels.FilterField>(v => v.ToApi(), v => v.ToMessage());

    [Fact]
    public void IdentificationStrategy_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<MsgScoring.IdentificationStrategy, ApiModels.IdentificationStrategy>(v => v.ToApi(), v => v.ToMessage());

    [Fact]
    public void TitlePartType_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<MsgScoring.TitlePartType, ApiModels.TitlePartType>(v => v.ToApi(), v => v.ToMessage());

    [Fact]
    public void EnrichmentMethod_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<MsgEnrichment.EnrichmentMethod, ApiModels.EnrichmentMethod>(v => v.ToApi(), v => v.ToMessage());

    [Fact]
    public void RuntimeMode_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<MsgEnrichment.RuntimeMode, ApiModels.RuntimeMode>(v => v.ToApi(), v => v.ToMessage());

    [Fact]
    public void MatchMethod_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<MsgEnrichment.MatchMethod, ApiModels.MatchMethod>(v => v.ToApi(), v => v.ToMessage());

    [Fact]
    public void QueuePriority_RoundtripsEveryMemberByName() =>
        AssertRoundtrip<MsgDownload.DownloadPriority, ApiModels.QueuePriority>(v => v.ToApi(), v => v.ToMessage());

    [Fact]
    public void UndefinedApiValue_Throws()
    {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => ((ApiModels.QueuePriority)5).ToMessage());

        Assert.Equal("value", ex.ParamName);
        Assert.Equal((ApiModels.QueuePriority)5, ex.ActualValue);
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
        var messageValues = Enum.GetValues<TMessage>();
        Assert.Equal(messageValues.Length, Enum.GetValues<TApi>().Length);

        foreach (var message in messageValues)
        {
            var api = toApi(message);
            Assert.Equal(message.ToString(), api.ToString());
            Assert.Equal(Convert.ToInt32(message), Convert.ToInt32(api));
            Assert.Equal(message, toMessage(api));
        }
    }
}
