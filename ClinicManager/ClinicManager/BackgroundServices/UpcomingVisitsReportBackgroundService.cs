using ClinicManager.Configuration;
using ClinicManager.Services;
using Microsoft.Extensions.Options;

namespace ClinicManager.BackgroundServices;

public class UpcomingVisitsReportBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UpcomingVisitsReportBackgroundService> _logger;
    private readonly BackgroundReportOptions _options;

    public UpcomingVisitsReportBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<BackgroundReportOptions> options,
        ILogger<UpcomingVisitsReportBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(1, _options.IntervalMinutes));

        while (!stoppingToken.IsCancellationRequested)
        {
            await GenerateAndSendReportAsync(stoppingToken);

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task GenerateAndSendReportAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var reportService = scope.ServiceProvider.GetRequiredService<ReportService>();
            var pdfReportService = scope.ServiceProvider.GetRequiredService<PdfReportService>();
            var emailService = scope.ServiceProvider.GetRequiredService<SmtpEmailService>();

            var generatedAt = DateTime.Now;
            var endDate = generatedAt.AddDays(Math.Max(1, _options.DaysAhead));
            var visits = await reportService.GetUpcomingVisitsAsync(
                generatedAt,
                endDate,
                cancellationToken);

            var pdfBytes = pdfReportService.GenerateUpcomingVisitsReport(
                generatedAt,
                endDate,
                visits);

            await emailService.SendReportAsync(
                pdfBytes,
                _options.FileName,
                visits.Count,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Nie udało się wygenerować lub wysłać raportu nadchodzących wizyt.");
        }
    }
}
