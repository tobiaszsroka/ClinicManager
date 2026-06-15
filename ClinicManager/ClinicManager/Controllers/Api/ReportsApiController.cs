using ClinicManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Controllers.Api;

[ApiController]
[ApiExplorerSettings(GroupName = "v1")]
[Route("api/reports")]
[Authorize(Roles = "Admin,Rejestratorka")]
public class ReportsApiController : ControllerBase
{
    private readonly PdfReportService _pdfReportService;
    private readonly ReportService _reportService;

    public ReportsApiController(PdfReportService pdfReportService, ReportService reportService)
    {
        _pdfReportService = pdfReportService;
        _reportService = reportService;
    }

    /// <summary>Generuje raport PDF kosztów zakończonych wizyt z wybranego miesiąca.</summary>
    [HttpGet("costs/{year:int}/{month:int}")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DownloadCostReport(int year, int month)
    {
        if (year is < 2000 or > 2100 || month is < 1 or > 12)
        {
            ModelState.AddModelError(nameof(month), "Podaj rok 2000-2100 i miesiąc 1-12.");
            return ValidationProblem(ModelState);
        }

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddTicks(-1);
        var visits = await _reportService.GetCompletedVisitsAsync(startDate, endDate);
        var pdf = _pdfReportService.GenerateCostReport(startDate, visits);

        return File(pdf, "application/pdf", $"Raport_Kosztow_{year}_{month:D2}.pdf");
    }
}
