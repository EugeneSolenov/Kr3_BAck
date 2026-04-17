namespace StudentPortal.Diagnostics.Middleware;

public sealed class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.HasStarted)
        {
            return;
        }

        if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
        {
            _logger.LogWarning("403 response generated for {Path}", context.Request.Path);
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync("Forbidden: provide a valid token via ?token=study2026.");
            return;
        }

        if (context.Response.StatusCode == StatusCodes.Status404NotFound)
        {
            _logger.LogWarning("404 response generated for {Path}", context.Request.Path);
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync($"Not Found: route '{context.Request.Path}' is not configured.");
        }
    }
}
