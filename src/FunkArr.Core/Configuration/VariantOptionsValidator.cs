using Microsoft.Extensions.Options;
using Terp.Options;

namespace FunkArr.Core.Configuration;

public sealed class VariantOptionsValidator : IValidateOptions<VariantOptions>
{
    public ValidateOptionsResult Validate(string? name, VariantOptions options)
    {
        var failures = new ValidationFailures(VariantOptions.SectionName);

        failures.Require(Enum.IsDefined(options.AudioDescription), "AudioDescription must be a valid VariantPolicy");
        failures.Require(Enum.IsDefined(options.SignLanguage), "SignLanguage must be a valid VariantPolicy");
        failures.Require(Enum.IsDefined(options.PlainLanguage), "PlainLanguage must be a valid VariantPolicy");
        failures.Require(Enum.IsDefined(options.OriginalVersion), "OriginalVersion must be a valid VariantPolicy");

        return failures.ToResult();
    }
}
