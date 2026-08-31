using System.Text;
using Xunit;

namespace FunkArr.ArrApi.Tests.Newznab;

public sealed class NzbGetTests
{
    [Fact]
    public void EncodeGuid_produces_valid_base64()
    {
        var title = "Tatort S01E05";
        var url = "https://example.com/video.mp4";
        var guid = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{title}|{url}"));

        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(guid));

        Assert.Contains("|", decoded);

        var pipeIndex = decoded.IndexOf('|');
        Assert.Equal(title, decoded[..pipeIndex]);
        Assert.Equal(url, decoded[(pipeIndex + 1)..]);
    }

    [Fact]
    public void Invalid_base64_throws_FormatException()
    {
        Assert.Throws<FormatException>(() => Convert.FromBase64String("not-valid!!!"));
    }

    [Fact]
    public void Guid_without_pipe_is_invalid()
    {
        var guid = Convert.ToBase64String(Encoding.UTF8.GetBytes("no-pipe-here"));
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(guid));

        Assert.DoesNotContain("|", decoded);
    }
}
