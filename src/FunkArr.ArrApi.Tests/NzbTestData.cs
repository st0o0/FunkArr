using FunkArr.ArrApi.Newznab.Models;
using FunkArr.ArrApi.Newznab.Services;
using FunkArr.Messages;

namespace FunkArr.ArrApi.Tests;

internal static class NzbTestData
{
    internal static string Id(
        string title, string url, string? subtitleUrl, string channel, int duration, long size, MediaType category) =>
        new NzbPayload(title, url, subtitleUrl, channel, duration, size, category).Encode();

    internal static byte[] Generate(
        string title, string url, string? subtitleUrl, string channel, int duration, long size, MediaType category)
    {
        var result = NzbService.GetNzb(Id(title, url, subtitleUrl, channel, duration, size, category), TimeProvider.System);
        return Assert.IsType<NzbGetResult.Success>(result).Content;
    }
}
