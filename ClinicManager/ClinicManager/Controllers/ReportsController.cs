using ClinicManager.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClinicManager.Controllers
{
    [Authorize(Roles = "Admin,Rejestratorka")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
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
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var visits = await _context.Visits
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .Include(v => v.Procedures)
                .Include(v => v.PrescribedMedications)
                    .ThenInclude(pm => pm.Medication)
                .Where(v => v.Status == Models.VisitStatus.Completed && 
                            v.ScheduledDate >= startDate && 
                            v.ScheduledDate <= endDate)
                .OrderBy(v => v.ScheduledDate)
                .ToListAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Element(compose => ComposeHeader(compose, startDate));
                    page.Content().Element(compose => ComposeContent(compose, visits));
                    page.Footer().Element(ComposeFooter);
                });
            });

            byte[] pdfBytes = document.GeneratePdf();

            return File(pdfBytes, "application/pdf", $"Raport_Kosztow_{year}_{month:D2}.pdf");
        }

        private void ComposeHeader(IContainer container, DateTime reportDate)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text($"Raport Kosztów: {reportDate:MMMM yyyy}").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                    column.Item().Text($"Wygenerowano: {DateTime.Now:dd.MM.yyyy HH:mm}");
                });
            });
        }

        private void ComposeContent(IContainer container, List<Models.Visit> visits)
        {
            container.PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(80); // Data
                        columns.RelativeColumn();   // Pacjent
                        columns.RelativeColumn();   // Lekarz
                        columns.ConstantColumn(70); // Procedury
                        columns.ConstantColumn(70); // Leki
                        columns.ConstantColumn(80); // Suma
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Data").SemiBold();
                        header.Cell().Text("Pacjent").SemiBold();
                        header.Cell().Text("Lekarz").SemiBold();
                        header.Cell().AlignRight().Text("Zabiegi").SemiBold();
                        header.Cell().AlignRight().Text("Leki").SemiBold();
                        header.Cell().AlignRight().Text("Suma").SemiBold();

                        header.Cell().ColumnSpan(6).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                    });

                    decimal totalSum = 0;

                    foreach (var visit in visits)
                    {
                        var proceduresCost = visit.Procedures?.Sum(p => p.FinalCost) ?? 0;
                        var medsCost = visit.PrescribedMedications?.Sum(m => m.UnitPriceAtPrescription * m.Quantity) ?? 0;
                        var visitTotal = proceduresCost + medsCost;
                        totalSum += visitTotal;

                        table.Cell().Text(visit.ScheduledDate.ToString("dd.MM.yyyy"));
                        table.Cell().Text($"{visit.Patient?.FirstName} {visit.Patient?.LastName}");
                        table.Cell().Text(visit.Doctor?.Email?.Split('@')[0] ?? "Nieznany");
                        table.Cell().AlignRight().Text(proceduresCost.ToString("c"));
                        table.Cell().AlignRight().Text(medsCost.ToString("c"));
                        table.Cell().AlignRight().Text(visitTotal.ToString("c")).SemiBold();
                    }

                    table.Cell().ColumnSpan(6).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);

                    table.Cell().ColumnSpan(5).AlignRight().PaddingTop(10).Text("Łączny koszt wizyt (Zakończone):").SemiBold().FontSize(14);
                    table.Cell().AlignRight().PaddingTop(10).Text(totalSum.ToString("c")).SemiBold().FontSize(14).FontColor(Colors.Green.Darken2);
                });
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.AlignCenter().Text(x =>
            {
                x.Span("Strona ");
                x.CurrentPageNumber();
                x.Span(" z ");
                x.TotalPages();
            });
        }
    }
}
