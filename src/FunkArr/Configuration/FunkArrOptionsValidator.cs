using Microsoft.Extensions.Options;

namespace FunkArr.Configuration;

public sealed class FunkArrOptionsValidator : IValidateOptions<FunkArrOptions>
{
    public ValidateOptionsResult Validate(string? name, FunkArrOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return ValidateOptionsResult.Fail("FunkArr:ApiKey must be configured.");
        }

        if (options.Postgres.IsConfigured)
        {
            var pg = options.Postgres;
            if (string.IsNullOrWhiteSpace(pg.User))
            {
                return ValidateOptionsResult.Fail("FunkArr:Postgres:User must be configured when Postgres:Host is set.");
            }

            if (string.IsNullOrWhiteSpace(pg.Password))
            {
                return ValidateOptionsResult.Fail("FunkArr:Postgres:Password must be configured when Postgres:Host is set.");
            }
        }

        return ValidateOptionsResult.Success;
    }
}
