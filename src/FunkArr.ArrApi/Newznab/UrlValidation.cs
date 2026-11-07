namespace FunkArr.ArrApi.Newznab;

internal static class UrlValidation
{
    internal static bool HasAllowedScheme(string? url) =>
        !string.IsNullOrEmpty(url)
        && Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && uri.Scheme is "http" or "https";
}
