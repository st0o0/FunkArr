using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FunkArr.Api.Validation;

internal sealed class ValidationEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var argument in context.Arguments)
        {
            if (argument is null)
            {
                continue;
            }

            var type = argument.GetType();
            if (type.IsPrimitive || type == typeof(string) || type == typeof(Guid) || type.IsEnum)
            {
                continue;
            }

            var validationResults = new List<ValidationResult>();
            var validationContext = new ValidationContext(argument);
            if (!Validator.TryValidateObject(argument, validationContext, validationResults, validateAllProperties: true))
            {
                var errors = validationResults
                    .Where(r => r.ErrorMessage is not null)
                    .GroupBy(r => r.MemberNames.FirstOrDefault() ?? "")
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(r => r.ErrorMessage!).ToArray());

                return Results.ValidationProblem(errors);
            }
        }

        return await next(context);
    }
}
