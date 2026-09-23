using System.ComponentModel.DataAnnotations;
using FunkArr.Api.Validation;

namespace FunkArr.Api.Tests;

public sealed class SafeUrlAttributeTests
{
    private readonly SafeUrlAttribute _attribute = new();

    [Fact]
    public void Null_value_is_valid()
    {
        var result = Validate(null);
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void Empty_string_is_valid()
    {
        var result = Validate("");
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void Public_ip_url_is_valid()
    {
        var result = Validate("http://8.8.8.8:8989/api");
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void Unresolvable_host_is_rejected()
    {
        var result = Validate("https://nonexistent.invalid/api");
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("Cannot resolve", result!.ErrorMessage!);
    }

    [Fact]
    public void Ftp_scheme_is_rejected()
    {
        var result = Validate("ftp://example.com/file");
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("http or https", result!.ErrorMessage!);
    }

    [Fact]
    public void Non_absolute_uri_is_rejected()
    {
        var result = Validate("not-a-url");
        Assert.NotEqual(ValidationResult.Success, result);
    }

    [Fact]
    public void Loopback_127_is_rejected()
    {
        var result = Validate("http://127.0.0.1:8080/api");
        Assert.NotEqual(ValidationResult.Success, result);
        Assert.Contains("private or loopback", result!.ErrorMessage!);
    }

    [Fact]
    public void Localhost_is_rejected()
    {
        var result = Validate("http://localhost:8989/api");
        Assert.NotEqual(ValidationResult.Success, result);
    }

    [Fact]
    public void Private_10_range_is_rejected()
    {
        var result = Validate("http://10.0.0.1:8989/api");
        Assert.NotEqual(ValidationResult.Success, result);
    }

    [Fact]
    public void Private_192_168_range_is_rejected()
    {
        var result = Validate("http://192.168.1.100:7878/api");
        Assert.NotEqual(ValidationResult.Success, result);
    }

    [Fact]
    public void Private_172_16_range_is_rejected()
    {
        var result = Validate("http://172.16.0.1:9696/api");
        Assert.NotEqual(ValidationResult.Success, result);
    }

    [Fact]
    public void Link_local_169_254_is_rejected()
    {
        var result = Validate("http://169.254.169.254/metadata");
        Assert.NotEqual(ValidationResult.Success, result);
    }

    private ValidationResult? Validate(string? url)
    {
        var context = new ValidationContext(new object()) { MemberName = "Url" };
        return _attribute.GetValidationResult(url, context);
    }
}
