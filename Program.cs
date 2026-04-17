using System.Diagnostics;
using System.Net;
using StudentPortal.Diagnostics.Middleware;
using StudentPortal.Diagnostics.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddStudentPortalServices();

var serviceSnapshot = builder.Services
    .Select(descriptor => new ServiceRegistrationInfo(
        descriptor.ServiceType.FullName ?? descriptor.ServiceType.Name,
        descriptor.Lifetime.ToString(),
        descriptor.ImplementationType?.FullName
            ?? descriptor.ImplementationInstance?.GetType().FullName
            ?? descriptor.ImplementationFactory?.Method.ReturnType.FullName
            ?? "(factory or unknown)"))
    .OrderBy(item => item.ServiceType)
    .ToList();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

app.Use(async (context, next) =>
{
    var logger = context.RequestServices
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("PipelineAudit");

    var startedAt = DateTimeOffset.Now;
    var timer = Stopwatch.StartNew();

    logger.LogInformation(
        "Request started: {Method} {Path} at {StartedAt}",
        context.Request.Method,
        context.Request.Path,
        startedAt);

    await next();

    timer.Stop();

    logger.LogInformation(
        "Request finished: {Method} {Path} with {StatusCode} in {ElapsedMs} ms",
        context.Request.Method,
        context.Request.Path,
        context.Response.StatusCode,
        timer.ElapsedMilliseconds);
});

app.UseWhen(
    context => string.Equals(context.Request.Query["trace"], "true", StringComparison.OrdinalIgnoreCase),
    branch =>
    {
        branch.Use(async (context, next) =>
        {
            var logger = context.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("TraceBranch");

            logger.LogInformation("Trace branch entered for {Path}", context.Request.Path);
            context.Response.Headers["X-Debug-Trace"] = "enabled";

            await next();

            logger.LogInformation("Trace branch returned to main pipeline for {Path}", context.Request.Path);
        });
    });

app.MapWhen(
    context => string.Equals(context.Request.Query["format"], "plain", StringComparison.OrdinalIgnoreCase),
    branch =>
    {
        branch.Run(async context =>
        {
            var clock = context.RequestServices.GetRequiredService<IDateTimeService>();

            context.Response.StatusCode = StatusCodes.Status200OK;
            context.Response.ContentType = "text/plain; charset=utf-8";

            await context.Response.WriteAsync(
                "Plain-text diagnostics branch\n" +
                $"Path: {context.Request.Path}\n" +
                $"Method: {context.Request.Method}\n" +
                $"Time: {clock.GetTime()}\n" +
                "Source: MapWhen(format=plain)\n" +
                "Note: The request was handled in a separate branch and did not continue through the main pipeline.");
        });
    });

app.MapGet("/", (IWebHostEnvironment environment) =>
{
    var html = $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8" />
            <title>StudentPortal.Diagnostics</title>
            <style>
                body { font-family: Segoe UI, Arial, sans-serif; margin: 32px; background: #f5f7fb; color: #1f2937; }
                .panel { max-width: 900px; margin: 0 auto; background: white; border-radius: 16px; padding: 28px; box-shadow: 0 12px 30px rgba(15, 23, 42, 0.08); }
                h1 { margin-top: 0; }
                ul { line-height: 1.7; }
                code { background: #eef2ff; padding: 2px 6px; border-radius: 6px; }
                a { color: #1d4ed8; text-decoration: none; }
            </style>
        </head>
        <body>
            <div class="panel">
                <h1>StudentPortal.Diagnostics</h1>
                <p>Educational diagnostics service for demonstrating middleware, branching and dependency injection.</p>
                <p><strong>Environment:</strong> {{WebUtility.HtmlEncode(environment.EnvironmentName)}}</p>
                <ul>
                    <li><a href="/tools/time">/tools/time</a></li>
                    <li><a href="/tools/date">/tools/date</a></li>
                    <li><a href="/tools/info">/tools/info</a></li>
                    <li><a href="/tools/time?trace=true">/tools/time?trace=true</a></li>
                    <li><a href="/anything?format=plain">/anything?format=plain</a></li>
                    <li><a href="/secure/report">/secure/report</a></li>
                    <li><a href="/secure/report?token=study2026">/secure/report?token=study2026</a></li>
                    <li><a href="/env">/env</a></li>
                    <li><a href="/di/services">/di/services</a></li>
                </ul>
            </div>
        </body>
        </html>
        """;

    return Results.Content(html, "text/html; charset=utf-8");
});

app.Map("/tools", tools =>
{
    tools.Map("/time", timeBranch =>
    {
        timeBranch.Run(async context =>
        {
            var clock = context.RequestServices.GetRequiredService<IDateTimeService>();

            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync($"Current time: {clock.GetTime()}");
        });
    });

    tools.Map("/date", dateBranch =>
    {
        dateBranch.Run(async context =>
        {
            var clock = context.RequestServices.GetRequiredService<IDateTimeService>();

            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync($"Current date: {clock.GetDate()}");
        });
    });

    tools.Map("/info", infoBranch =>
    {
        infoBranch.Run(async context =>
        {
            var clock = context.RequestServices.GetRequiredService<IDateTimeService>();
            var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();

            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync(
                "StudentPortal.Diagnostics summary\n" +
                $"Application: {environment.ApplicationName}\n" +
                $"Environment: {environment.EnvironmentName}\n" +
                $"Generated at: {clock.GetDate()} {clock.GetTime()}\n" +
                "Active branches: Use, UseWhen(trace=true), MapWhen(format=plain), Map(/tools), Map(/secure)");
        });
    });

    tools.Run(context =>
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return Task.CompletedTask;
    });
});

app.Map("/secure", secure =>
{
    secure.UseToken("study2026");

    secure.Map("/report", reportBranch =>
    {
        reportBranch.Run(async context =>
        {
            var clock = context.RequestServices.GetRequiredService<IDateTimeService>();

            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync(
                "Secure report\n" +
                "Access granted.\n" +
                $"Generated at: {clock.GetDate()} {clock.GetTime()}");
        });
    });

    secure.Run(context =>
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return Task.CompletedTask;
    });
});

app.MapGet("/env", (IEnvironmentReportService reportService) =>
    Results.Text(reportService.BuildReport(), "text/plain; charset=utf-8"));

app.MapGet("/di/services", () =>
{
    var rows = string.Join(
        Environment.NewLine,
        serviceSnapshot.Select(item =>
        {
            var isCustom =
                item.ServiceType.StartsWith("StudentPortal.Diagnostics", StringComparison.Ordinal) ||
                item.ImplementationType.StartsWith("StudentPortal.Diagnostics", StringComparison.Ordinal);

            var rowStyle = isCustom ? " style=\"background:#dbeafe;\"" : string.Empty;

            return $"<tr{rowStyle}><td>{WebUtility.HtmlEncode(item.ServiceType)}</td><td>{WebUtility.HtmlEncode(item.Lifetime)}</td><td>{WebUtility.HtmlEncode(item.ImplementationType)}</td></tr>";
        }));

    var html = $$"""
        <!DOCTYPE html>
        <html lang="en">
        <head>
            <meta charset="utf-8" />
            <title>DI Services</title>
            <style>
                body { font-family: Segoe UI, Arial, sans-serif; margin: 32px; background: #f8fafc; color: #0f172a; }
                .panel { max-width: 1100px; margin: 0 auto; background: white; border-radius: 16px; padding: 28px; box-shadow: 0 12px 30px rgba(15, 23, 42, 0.08); }
                table { width: 100%; border-collapse: collapse; margin-top: 16px; }
                th, td { border: 1px solid #cbd5e1; padding: 10px; text-align: left; vertical-align: top; }
                th { background: #e2e8f0; }
                code { background: #eef2ff; padding: 2px 6px; border-radius: 6px; }
            </style>
        </head>
        <body>
            <div class="panel">
                <h1>IServiceCollection snapshot</h1>
                <p>Total registered services: <strong>{{serviceSnapshot.Count}}</strong></p>
                <p>Captured before <code>builder.Build()</code>. Custom StudentPortal services are highlighted in blue.</p>
                <table>
                    <thead>
                        <tr>
                            <th>ServiceType</th>
                            <th>Lifetime</th>
                            <th>ImplementationType</th>
                        </tr>
                    </thead>
                    <tbody>
                        {{rows}}
                    </tbody>
                </table>
            </div>
        </body>
        </html>
        """;

    return Results.Content(html, "text/html; charset=utf-8");
});

app.Run();

internal sealed record ServiceRegistrationInfo(string ServiceType, string Lifetime, string ImplementationType);
