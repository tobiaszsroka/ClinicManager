using ClinicManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Controllers;

[Authorize(Roles = "Admin,Rejestratorka")]
public class ReportsController : Controller
{
    private readonly PdfReportService _pdfReportService;
    private readonly ReportService _reportService;

    public ReportsController(
        PdfReportService pdfReportService,
        ReportService reportService)
    {
        _pdfReportService = pdfReportService;
        _reportService = reportService;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DownloadPdf(int year, int month)
    {
        if (year < 2000 || year > 2100 || month < 1 || month > 12)
        {
            TempData["ErrorMessage"] = "Nieprawidłowy rok lub miesiąc.";
            return RedirectToAction(nameof(Index));
        }

        var startDate = new DateTime(year, month, 1);
        var endDate = startDate.AddMonths(1).AddTicks(-1);
        var visits = await _reportService.GetCompletedVisitsAsync(startDate, endDate);
        var pdfBytes = _pdfReportService.GenerateCostReport(startDate, visits);

        return File(pdfBytes, "application/pdf", $"Raport_Kosztow_{year}_{month:D2}.pdf");
    }
}
