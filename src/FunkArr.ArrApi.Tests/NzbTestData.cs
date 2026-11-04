using System.Text;
using FunkArr.ArrApi.Newznab.Services;

namespace FunkArr.ArrApi.Tests;

internal static class NzbTestData
{
    internal static string Id(
        string title, string url, string? subtitleUrl, string channel, int duration, long size, string category) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(
            string.Join('\t', title, url, subtitleUrl ?? "", channel, duration, size, category)));

    internal static byte[] Generate(
        string title, string url, string? subtitleUrl, string channel, int duration, long size, string category)
    {
        var result = NzbService.GetNzb(Id(title, url, subtitleUrl, channel, duration, size, category), TimeProvider.System);
        return Assert.IsType<NzbGetResult.Success>(result).Content;
    }
}
