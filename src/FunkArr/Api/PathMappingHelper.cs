namespace FunkArr.Api;

public static class PathMappingHelper
{
    public static (string From, string To)? ParsePathMapping(string? mapping)
    {
        if (string.IsNullOrEmpty(mapping))
        {
            return null;
        }

        var parts = mapping.Split(':');
        return parts.Length == 2 ? (parts[0], parts[1]) : null;
    }

    public static string MapPath(string path, (string From, string To)? mapping)
    {
        if (mapping is null || string.IsNullOrEmpty(path))
        {
            return path;
        }

        return path.StartsWith(mapping.Value.From, StringComparison.OrdinalIgnoreCase)
            ? mapping.Value.To + path[mapping.Value.From.Length..]
            : path;
    }
}
