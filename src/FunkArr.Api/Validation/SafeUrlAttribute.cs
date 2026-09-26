using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Sockets;

namespace FunkArr.Api.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class SafeUrlAttribute : ValidationAttribute
{
    public SafeUrlAttribute() : base("URL must not target a private or loopback address") { }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string url || string.IsNullOrWhiteSpace(url))
        {
            return ValidationResult.Success;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return new ValidationResult("URL must be a valid absolute URI");
        }

        if (uri.Scheme is not ("http" or "https"))
        {
            return new ValidationResult("URL must use http or https scheme");
        }

        IPAddress[] addresses;
        try
        {
            addresses = Dns.GetHostAddresses(uri.Host);
        }
        catch (SocketException)
        {
            return new ValidationResult($"Cannot resolve host '{uri.Host}'");
        }

        foreach (var address in addresses)
        {
            if (IPAddress.IsLoopback(address))
            {
                return new ValidationResult(ErrorMessage);
            }

            if (address.IsInPrivateRange())
            {
                return new ValidationResult(ErrorMessage);
            }
        }

        return ValidationResult.Success;
    }
}

internal static class IpAddressExtensions
{
    internal static bool IsInPrivateRange(this IPAddress address)
    {
        if (address.AddressFamily == AddressFamily.InterNetworkV6 && address.IsIPv6LinkLocal)
        {
            return true;
        }

        var bytes = address.GetAddressBytes();
        if (bytes.Length != 4)
        {
            return false;
        }

        return bytes[0] switch
        {
            10 => true,
            172 => bytes[1] >= 16 && bytes[1] <= 31,
            192 => bytes[1] == 168,
            169 => bytes[1] == 254,
            _ => false,
        };
    }
}
