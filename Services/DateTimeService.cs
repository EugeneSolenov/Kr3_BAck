namespace StudentPortal.Diagnostics.Services;

public sealed class DateTimeService : IDateTimeService
{
    public string GetDate() => DateTime.Now.ToString("yyyy-MM-dd");

    public string GetTime() => DateTime.Now.ToString("HH:mm:ss");
}
