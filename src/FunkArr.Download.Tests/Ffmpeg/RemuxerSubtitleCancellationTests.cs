using FunkArr.Core;
using FunkArr.Download.Ffmpeg;
using FunkArr.Download.Subtitles;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FunkArr.Download.Tests.Ffmpeg;

public sealed class RemuxerSubtitleCancellationTests
{
    [Fact]
    public async Task Subtitle_cancellation_returns_cancelled_result()
    {
        var routeResolver = Substitute.For<IRouteResolver>();
        routeResolver.Resolve(Arg.Any<string>()).Returns(new ResolvedRoute("Direct", null));

        var subtitleDownloader = Substitute.For<ISubtitleDownloader>();
        subtitleDownloader
            .DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync<OperationCanceledException>();

        var ffmpegProcess = Substitute.For<IFfmpegProcess>();

        var remuxer = new Remuxer(routeResolver, subtitleDownloader, ffmpegProcess,
            NullLogger<Remuxer>.Instance);

        var options = RemuxOptions.Create("https://example.com/video.mp4", "/tmp/out.mkv")
            .WithSubtitle("https://example.com/sub.srt")
            .WithChannel("ARD");

        var result = await remuxer.RunAsync(options, _ => { }, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Cancelled", result.Error);
        await ffmpegProcess.DidNotReceive().ExecuteAsync(
            Arg.Any<FfmpegInput>(), Arg.Any<Action<ProgressUpdate>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Subtitle_failure_continues_to_ffmpeg_without_subtitle()
    {
        var routeResolver = Substitute.For<IRouteResolver>();
        routeResolver.Resolve(Arg.Any<string>()).Returns(new ResolvedRoute("Direct", null));

        var subtitleDownloader = Substitute.For<ISubtitleDownloader>();
        subtitleDownloader
            .DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new SubtitleResult.Failed(SubtitleFailureReason.DownloadFailed, "HTTP 404"));

        var ffmpegProcess = Substitute.For<IFfmpegProcess>();
        ffmpegProcess
            .ExecuteAsync(Arg.Any<FfmpegInput>(), Arg.Any<Action<ProgressUpdate>>(), Arg.Any<CancellationToken>())
            .Returns(new FfmpegResult(true, 0, null, 120));

        var remuxer = new Remuxer(routeResolver, subtitleDownloader, ffmpegProcess,
            NullLogger<Remuxer>.Instance);

        var options = RemuxOptions.Create("https://example.com/video.mp4", "/tmp/out.mkv")
            .WithSubtitle("https://example.com/sub.srt")
            .WithChannel("ARD");

        var result = await remuxer.RunAsync(options, _ => { }, CancellationToken.None);

        Assert.True(result.Success);
        Assert.False(result.SubtitleMuxed);
        await ffmpegProcess.Received(1).ExecuteAsync(
            Arg.Is<FfmpegInput>(i => i.SubtitlePath == null),
            Arg.Any<Action<ProgressUpdate>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Subtitle_success_muxes_subtitle()
    {
        var routeResolver = Substitute.For<IRouteResolver>();
        routeResolver.Resolve(Arg.Any<string>()).Returns(new ResolvedRoute("Direct", null));

        var subtitleDownloader = Substitute.For<ISubtitleDownloader>();
        subtitleDownloader
            .DownloadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new SubtitleResult.Succeeded(new SubtitleTrack("srt", "deu", []), "/tmp/sub.srt"));

        var ffmpegProcess = Substitute.For<IFfmpegProcess>();
        ffmpegProcess
            .ExecuteAsync(Arg.Any<FfmpegInput>(), Arg.Any<Action<ProgressUpdate>>(), Arg.Any<CancellationToken>())
            .Returns(new FfmpegResult(true, 0, null, 120));

        var remuxer = new Remuxer(routeResolver, subtitleDownloader, ffmpegProcess,
            NullLogger<Remuxer>.Instance);

        var options = RemuxOptions.Create("https://example.com/video.mp4", "/tmp/out.mkv")
            .WithSubtitle("https://example.com/sub.srt")
            .WithChannel("ARD");

        var result = await remuxer.RunAsync(options, _ => { }, CancellationToken.None);

        Assert.True(result.Success);
        Assert.True(result.SubtitleMuxed);
    }
}
