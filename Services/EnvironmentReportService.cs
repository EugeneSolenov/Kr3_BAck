namespace StudentPortal.Diagnostics.Services;

public sealed class EnvironmentReportService : IEnvironmentReportService
{
    private readonly IWebHostEnvironment _environment;

    public EnvironmentReportService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public string BuildReport()
    {
        var mode = _environment.IsDevelopment() ? "Detailed development diagnostics" : "Production-safe diagnostics";

        return string.Join(
            Environment.NewLine,
            new[]
            {
                "Environment report",
                $"EnvironmentName: {_environment.EnvironmentName}",
                $"ApplicationName: {_environment.ApplicationName}",
                $"ContentRootPath: {_environment.ContentRootPath}",
                $"WebRootPath: {_environment.WebRootPath ?? "(not configured)"}",
                $"Mode: {mode}"
            });
    }
}
