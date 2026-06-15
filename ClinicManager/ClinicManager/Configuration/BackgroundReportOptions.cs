namespace ClinicManager.Configuration;

public class BackgroundReportOptions
{
    public const string SectionName = "BackgroundReports";

    public bool Enabled { get; set; }
    public int IntervalMinutes { get; set; } = 1440;
    public int DaysAhead { get; set; } = 7;
    public string FileName { get; set; } = "raport-nadchodzace-wizyty.pdf";
}
