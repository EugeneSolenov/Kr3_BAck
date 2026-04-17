namespace StudentPortal.Diagnostics.Middleware;

public sealed class TokenMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenMiddleware> _logger;
    private readonly string _pattern;

    public TokenMiddleware(RequestDelegate next, ILogger<TokenMiddleware> logger, string pattern)
    {
        _next = next;
        _logger = logger;
        _pattern = pattern;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var token = context.Request.Query["token"].ToString();

        if (!string.Equals(token, _pattern, StringComparison.Ordinal))
        {
            _logger.LogWarning("Token validation failed for {Path}", context.Request.Path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        _logger.LogInformation("Token validation passed for {Path}", context.Request.Path);
        await _next(context);
    }
}
