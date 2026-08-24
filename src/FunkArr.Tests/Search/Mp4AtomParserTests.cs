using FunkArr.Search;
using FunkArr.Search.Quality;

namespace FunkArr.Tests.Search;

public class Mp4AtomParserTests
{
    [Fact]
    public void Parse_ValidMp4WithAvc1_ExtractsCodecAndResolution()
    {
        var data = BuildMinimalMp4("avc1", 1920, 1080);
        var result = Mp4AtomParser.Parse(data);

        Assert.NotNull(result);
        Assert.Equal("h264", result.Codec);
        Assert.Equal(1920, result.Resolution.Width);
        Assert.Equal(1080, result.Resolution.Height);
    }

    [Fact]
    public void Parse_ValidMp4WithHev1_ExtractsH265()
    {
        var data = BuildMinimalMp4("hev1", 1280, 720);
        var result = Mp4AtomParser.Parse(data);

        Assert.NotNull(result);
        Assert.Equal("h265", result.Codec);
        Assert.Equal(1280, result.Resolution.Width);
        Assert.Equal(720, result.Resolution.Height);
    }

    [Fact]
    public void Parse_ValidMp4WithVp09_ExtractsVp9()
    {
        var data = BuildMinimalMp4("vp09", 1920, 1080);
        var result = Mp4AtomParser.Parse(data);

        Assert.NotNull(result);
        Assert.Equal("vp9", result.Codec);
    }

    [Fact]
    public void Parse_ValidMp4WithAv01_ExtractsAv1()
    {
        var data = BuildMinimalMp4("av01", 3840, 2160);
        var result = Mp4AtomParser.Parse(data);

        Assert.NotNull(result);
        Assert.Equal("av1", result.Codec);
        Assert.Equal(3840, result.Resolution.Width);
        Assert.Equal(2160, result.Resolution.Height);
    }

    [Fact]
    public void Parse_TooSmallData_ReturnsNull()
    {
        Assert.Null(Mp4AtomParser.Parse(new byte[4]));
    }

    [Fact]
    public void Parse_NoMoovAtom_ReturnsNull()
    {
        var data = BuildAtom("ftyp", new byte[8]);
        Assert.Null(Mp4AtomParser.Parse(data));
    }

    [Fact]
    public void Parse_UnknownCodec_ReturnsNull()
    {
        var data = BuildMinimalMp4("xxxx", 1920, 1080);
        Assert.Null(Mp4AtomParser.Parse(data));
    }

    private static byte[] BuildMinimalMp4(string codec, ushort width, ushort height)
    {
        // stsd content: 4 bytes version+flags, 4 bytes entry count,
        // then sample entry: size(4) + fourcc(4) + 6 reserved + 2 data_ref_index + 16 pre_defined + 2 width + 2 height
        var sampleEntry = new byte[8 + 6 + 2 + 16 + 4];
        WriteUInt32(sampleEntry, 0, (uint)sampleEntry.Length);
        WriteFourCc(sampleEntry, 4, codec);
        WriteUInt16(sampleEntry, 8 + 6 + 2 + 16, width);
        WriteUInt16(sampleEntry, 8 + 6 + 2 + 16 + 2, height);

        var stsdContent = new byte[8 + sampleEntry.Length];
        WriteUInt32(stsdContent, 4, 1); // entry count
        Array.Copy(sampleEntry, 0, stsdContent, 8, sampleEntry.Length);

        var stsd = BuildAtom("stsd", stsdContent);
        var vmhd = BuildAtom("vmhd", new byte[12]);
        var stbl = BuildAtom("stbl", stsd);
        var minf = BuildAtom("minf", [.. vmhd, .. stbl]);
        var mdia = BuildAtom("mdia", minf);
        var trak = BuildAtom("trak", mdia);
        var moov = BuildAtom("moov", trak);
        var ftyp = BuildAtom("ftyp", new byte[8]);

        return [.. ftyp, .. moov];
    }

    private static byte[] BuildAtom(string name, byte[] content)
    {
        var size = 8 + content.Length;
        var atom = new byte[size];
        WriteUInt32(atom, 0, (uint)size);
        WriteFourCc(atom, 4, name);
        Array.Copy(content, 0, atom, 8, content.Length);
        return atom;
    }

    private static void WriteUInt32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }

    private static void WriteUInt16(byte[] data, int offset, ushort value)
    {
        data[offset] = (byte)(value >> 8);
        data[offset + 1] = (byte)value;
    }

    private static void WriteFourCc(byte[] data, int offset, string name)
    {
        data[offset] = (byte)name[0];
        data[offset + 1] = (byte)name[1];
        data[offset + 2] = (byte)name[2];
        data[offset + 3] = (byte)name[3];
    }
}
