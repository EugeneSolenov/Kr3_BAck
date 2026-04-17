namespace StudentPortal.Diagnostics.Middleware;

public static class TokenExtensions
{
    public static IApplicationBuilder UseToken(this IApplicationBuilder app, string pattern)
    {
        return app.UseMiddleware<TokenMiddleware>(pattern);
    }
}
