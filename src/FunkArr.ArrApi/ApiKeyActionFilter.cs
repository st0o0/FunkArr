using FunkArr.ArrApi.Newznab;
using FunkArr.ArrApi.Newznab.Models;
using FunkArr.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace FunkArr.ArrApi;

public abstract class ApiKeyActionFilter(IOptions<FunkArrOptions> options) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var expectedApiKey = options.Value.ApiKey;
        var apiKey = context.HttpContext.Request.Query["apikey"].FirstOrDefault();

        if (string.IsNullOrEmpty(apiKey) || !string.Equals(apiKey, expectedApiKey, StringComparison.Ordinal))
        {
            context.Result = CreateErrorResult();
            return;
        }

        await next();
    }

    protected abstract IActionResult CreateErrorResult();
}

public sealed class NewznabApiKeyFilter(IOptions<FunkArrOptions> options) : ApiKeyActionFilter(options)
{
    protected override IActionResult CreateErrorResult() =>
        NewznabXmlResult.Error(NewznabError.InvalidApiKey);
}

public sealed class SabnzbdApiKeyFilter(IOptions<FunkArrOptions> options) : ApiKeyActionFilter(options)
{
    protected override IActionResult CreateErrorResult() =>
        new JsonResult(new { status = false, error = "API Key Incorrect" }) { StatusCode = 403 };
}
