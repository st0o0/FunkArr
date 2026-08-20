using FunkArr.Shared.Models;

namespace FunkArr.Tests.Shared;

public class QualityInfoTests
{
    [Theory]
    [InlineData(1080, QualityTier.HD1080)]
    [InlineData(1920, QualityTier.HD1080)]
    [InlineData(2160, QualityTier.HD1080)]
    [InlineData(720, QualityTier.HD720)]
    [InlineData(900, QualityTier.HD720)]
    [InlineData(1079, QualityTier.HD720)]
    [InlineData(480, QualityTier.SD)]
    [InlineData(360, QualityTier.SD)]
    [InlineData(0, QualityTier.SD)]
    public void DeriveQualityTier_ReturnsCorrectTier(int height, QualityTier expected)
    {
        Assert.Equal(expected, QualityInfo.DeriveQualityTier(height));
    }

    [Fact]
    public void QualityTier_DerivedFromResolution()
    {
        var info = new QualityInfo
        {
            Resolution = new Resolution(1920, 1080),
            FileSize = 1_000_000,
            ProbeSource = ProbeSource.ContainerHeader,
            Codec = "h265",
        };

        Assert.Equal(QualityTier.HD1080, info.QualityTier);
    }

    [Fact]
    public void Estimated_CreatesCorrectDefaults()
    {
        var info = QualityInfo.Estimated(QualityTier.HD720, 500_000_000);

        Assert.Equal(720, info.Resolution.Height);
        Assert.Equal(1280, info.Resolution.Width);
        Assert.Equal(500_000_000, info.FileSize);
        Assert.Equal(ProbeSource.Estimated, info.ProbeSource);
        Assert.Equal("h264", info.Codec);
        Assert.Equal("mp4", info.Container);
        Assert.Equal(QualityTier.HD720, info.QualityTier);
    }
}
