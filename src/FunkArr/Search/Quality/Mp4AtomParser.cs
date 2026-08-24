using FunkArr.Shared.Models;

namespace FunkArr.Search.Quality;

public static class Mp4AtomParser
{
    private static readonly Dictionary<uint, string> CodecMap = new()
    {
        [FourCc("avc1")] = "h264",
        [FourCc("avc3")] = "h264",
        [FourCc("hev1")] = "h265",
        [FourCc("hvc1")] = "h265",
        [FourCc("vp09")] = "vp9",
        [FourCc("av01")] = "av1",
    };

    public static Mp4ProbeResult? Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length < 8)
        {
            return null;
        }

        var moovData = FindAtom(data, "moov");
        if (moovData.IsEmpty)
        {
            return null;
        }

        var trakData = FindAtom(moovData, "trak");
        while (!trakData.IsEmpty)
        {
            var result = TryParseVideoTrack(trakData);
            if (result is not null)
            {
                return result;
            }

            var trakEnd = GetAtomEnd(moovData, "trak");
            if (trakEnd <= 0 || trakEnd >= moovData.Length)
            {
                break;
            }

            moovData = moovData[trakEnd..];
            trakData = FindAtom(moovData, "trak");
        }

        return null;
    }

    private static Mp4ProbeResult? TryParseVideoTrack(ReadOnlySpan<byte> trakData)
    {
        var mdiaData = FindAtom(trakData, "mdia");
        if (mdiaData.IsEmpty)
        {
            return null;
        }

        var minfData = FindAtom(mdiaData, "minf");
        if (minfData.IsEmpty)
        {
            return null;
        }

        var vmhdData = FindAtom(minfData, "vmhd");
        if (vmhdData.IsEmpty)
        {
            return null;
        }

        var stblData = FindAtom(minfData, "stbl");
        if (stblData.IsEmpty)
        {
            return null;
        }

        var stsdData = FindAtom(stblData, "stsd");
        if (stsdData.IsEmpty)
        {
            return null;
        }

        return ParseStsd(stsdData);
    }

    private static Mp4ProbeResult? ParseStsd(ReadOnlySpan<byte> stsdData)
    {
        if (stsdData.Length < 8)
        {
            return null;
        }

        var offset = 4; // skip version + flags
        if (offset + 4 > stsdData.Length)
        {
            return null;
        }

        var entryCount = ReadUInt32(stsdData[offset..]);
        offset += 4;

        if (entryCount == 0 || offset + 8 > stsdData.Length)
        {
            return null;
        }

        var entrySize = (int)ReadUInt32(stsdData[offset..]);
        var entryType = ReadUInt32(stsdData[(offset + 4)..]);

        if (entrySize <= 0 || offset + entrySize > stsdData.Length)
        {
            return null;
        }

        if (!CodecMap.TryGetValue(entryType, out var codec))
        {
            return null;
        }

        // visual sample entry: 6 reserved + 2 data_ref_index + 16 pre_defined/reserved + 2 width + 2 height
        var sampleOffset = offset + 8 + 6 + 2 + 16;
        if (sampleOffset + 4 > stsdData.Length)
        {
            return null;
        }

        var width = ReadUInt16(stsdData[sampleOffset..]);
        var height = ReadUInt16(stsdData[(sampleOffset + 2)..]);

        if (width == 0 || height == 0)
        {
            return null;
        }

        return new Mp4ProbeResult
        {
            Resolution = new Resolution(width, height),
            Codec = codec,
        };
    }

    private static ReadOnlySpan<byte> FindAtom(ReadOnlySpan<byte> data, string name)
    {
        var target = FourCc(name);
        var offset = 0;

        while (offset + 8 <= data.Length)
        {
            var size = (int)ReadUInt32(data[offset..]);
            var type = ReadUInt32(data[(offset + 4)..]);

            if (size < 8)
            {
                break;
            }

            if (type == target)
            {
                var contentStart = offset + 8;
                var contentEnd = Math.Min(offset + size, data.Length);
                if (contentStart >= contentEnd)
                {
                    return [];
                }

                return data[contentStart..contentEnd];
            }

            offset += size;
        }

        return [];
    }

    private static int GetAtomEnd(ReadOnlySpan<byte> data, string name)
    {
        var target = FourCc(name);
        var offset = 0;

        while (offset + 8 <= data.Length)
        {
            var size = (int)ReadUInt32(data[offset..]);
            var type = ReadUInt32(data[(offset + 4)..]);

            if (size < 8)
            {
                break;
            }

            if (type == target)
            {
                return offset + size;
            }

            offset += size;
        }

        return -1;
    }

    private static uint FourCc(string s) =>
        ((uint)s[0] << 24) | ((uint)s[1] << 16) | ((uint)s[2] << 8) | s[3];

    private static uint ReadUInt32(ReadOnlySpan<byte> data) =>
        ((uint)data[0] << 24) | ((uint)data[1] << 16) | ((uint)data[2] << 8) | data[3];

    private static ushort ReadUInt16(ReadOnlySpan<byte> data) =>
        (ushort)((data[0] << 8) | data[1]);
}

public sealed record Mp4ProbeResult
{
    public required Resolution Resolution { get; init; }
    public required string Codec { get; init; }
}
