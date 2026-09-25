using Microsoft.Extensions.Options;

namespace FunkArr.Core;

public sealed class RoutingOptionsValidator : IValidateOptions<RoutingOptions>
{
    public ValidateOptionsResult Validate(string? name, RoutingOptions options)
    {
        var failures = new List<string>();
        var definedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var def in options.Definitions)
        {
            if (string.IsNullOrWhiteSpace(def.Name))
            {
                failures.Add("Route definition has an empty Name.");
                continue;
            }

            if (!definedNames.Add(def.Name))
            {
                failures.Add($"Duplicate route definition name: '{def.Name}'.");
            }

            if (string.IsNullOrWhiteSpace(def.Proxy))
            {
                continue;
            }

            if (!Uri.TryCreate(def.Proxy, UriKind.Absolute, out var uri))
            {
                failures.Add($"Route '{def.Name}' has an invalid Proxy URI: '{def.Proxy}'.");
            }
            else if (uri.Scheme is not ("http" or "https"))
            {
                failures.Add($"Route '{def.Name}' Proxy must use http or https scheme: '{def.Proxy}'.");
            }
        }

        if (definedNames.Count > 0 && !definedNames.Contains(options.Default))
        {
            failures.Add($"Default route '{options.Default}' does not reference a defined route.");
        }

        for (var i = 0; i < options.ChannelRoutes.Count; i++)
        {
            var cr = options.ChannelRoutes[i];

            if (string.IsNullOrWhiteSpace(cr.Pattern))
            {
                failures.Add($"ChannelRoutes[{i}] has an empty Pattern.");
            }

            if (!definedNames.Contains(cr.Route))
            {
                failures.Add($"ChannelRoutes[{i}] references undefined route '{cr.Route}'.");
            }
        }

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}
