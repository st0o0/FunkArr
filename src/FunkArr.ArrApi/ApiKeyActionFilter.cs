using System.Security.Cryptography;
using System.Text;
using FunkArr.ArrApi.Newznab;
using FunkArr.ArrApi.Newznab.Models;
using FunkArr.ArrApi.Sabnzbd.Models;
using FunkArr.Core.Configuration;
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

        if (string.IsNullOrEmpty(apiKey) || !FixedTimeEquals(apiKey, expectedApiKey))
        {
            context.Result = CreateErrorResult();
            return;
        }

        context.HttpContext.Items["ApiKey"] = apiKey;
        await next();
    }

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(a),
            Encoding.UTF8.GetBytes(b));

    protected abstract IActionResult CreateErrorResult();
}

public sealed class NewznabApiKeyFilter(IOptions<FunkArrOptions> options) : ApiKeyActionFilter(options)
{
    protected override IActionResult CreateErrorResult()
        => NewznabXmlResult.Error(NewznabError.InvalidApiKey);
}

public sealed class SabnzbdApiKeyFilter(IOptions<FunkArrOptions> options) : ApiKeyActionFilter(options)
{
    protected override IActionResult CreateErrorResult()
        => new JsonResult(new SabnzbdErrorResponse(false, "API Key Required"));
}
