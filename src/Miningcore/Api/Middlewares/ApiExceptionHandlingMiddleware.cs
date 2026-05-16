using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using NLog;

namespace Miningcore.Api.Middlewares;

public class ApiExceptionHandlingMiddleware
{
    private readonly RequestDelegate next;
    private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

    public ApiExceptionHandlingMiddleware(RequestDelegate next)
    {
        this.next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }

        catch(ApiException ex)
        {
            await HandleResponseOverrideExceptionAsync(context, ex);
        }

        catch(Exception ex)
        {
            logger.Error(ex, "Unhandled API exception");

            await HandleResponseOverrideExceptionAsync(context, new ApiException(ex.Message, System.Net.HttpStatusCode.InternalServerError));
        }
    }

    private static async Task HandleResponseOverrideExceptionAsync(HttpContext context, ApiException ex)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        if(ex.ResponseStatusCode.HasValue)
            response.StatusCode = ex.ResponseStatusCode.Value;
        else
            response.StatusCode = StatusCodes.Status500InternalServerError;

        await response.WriteAsync(JsonConvert.SerializeObject(new
        {
            error = ex.Message
        })).ConfigureAwait(false);
    }
}
