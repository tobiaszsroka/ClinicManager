using ClinicManager.DTOs;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClinicManager.Services;

public class PdfReportService
{
    public byte[] GenerateCostReport(
        DateTime reportDate,
        IReadOnlyCollection<ReportVisitDto> visits)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page);
                page.Header().Element(header =>
                {
                    header.Column(column =>
                    {
                        column.Item()
                            .Text($"Raport kosztów: {reportDate:MMMM yyyy}")
                            .FontSize(20)
                            .SemiBold()
                            .FontColor(Colors.Blue.Darken2);
                        column.Item().Text($"Wygenerowano: {DateTime.Now:dd.MM.yyyy HH:mm}");
                    });
                });
                page.Content().Element(content => ComposeCostReport(content, visits));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    public byte[] GenerateUpcomingVisitsReport(
        DateTime generatedAt,
        DateTime endDate,
        IReadOnlyCollection<ReportVisitDto> visits)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                ConfigurePage(page);
                page.Header().Column(column =>
                {
                    column.Item()
                        .Text("Raport nadchodzących wizyt")
                        .FontSize(20)
                        .SemiBold()
                        .FontColor(Colors.Blue.Darken2);
                    column.Item().Text(
                        $"Okres: {generatedAt:dd.MM.yyyy HH:mm} - {endDate:dd.MM.yyyy HH:mm}");
                });
                page.Content().Element(content => ComposeUpcomingVisits(content, visits));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static void ConfigurePage(PageDescriptor page)
    {
        page.Size(PageSizes.A4);
        page.Margin(2, Unit.Centimetre);
        page.PageColor(Colors.White);
        page.DefaultTextStyle(style => style.FontSize(11));
    }

    private static void ComposeCostReport(
        IContainer container,
        IReadOnlyCollection<ReportVisitDto> visits)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(80);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.ConstantColumn(70);
                    columns.ConstantColumn(70);
                    columns.ConstantColumn(80);
                });

                table.Header(header =>
                {
                    header.Cell().Text("Data").SemiBold();
                    header.Cell().Text("Pacjent").SemiBold();
                    header.Cell().Text("Lekarz").SemiBold();
                    header.Cell().AlignRight().Text("Zabiegi").SemiBold();
                    header.Cell().AlignRight().Text("Leki").SemiBold();
                    header.Cell().AlignRight().Text("Suma").SemiBold();
                    header.Cell().ColumnSpan(6).PaddingVertical(5).BorderBottom(1);
                });

                foreach (var visit in visits)
                {
                    table.Cell().Text(visit.ScheduledDate.ToString("dd.MM.yyyy"));
                    table.Cell().Text($"{visit.PatientFirstName} {visit.PatientLastName}");
                    table.Cell().Text(ShortDoctorName(visit.DoctorEmail));
                    table.Cell().AlignRight().Text(visit.ProceduresCost.ToString("c"));
                    table.Cell().AlignRight().Text(visit.MedicationsCost.ToString("c"));
                    table.Cell().AlignRight().Text(visit.TotalCost.ToString("c")).SemiBold();
                }

                table.Cell().ColumnSpan(5).AlignRight().PaddingTop(10)
                    .Text("Łączny koszt wizyt:").SemiBold().FontSize(14);
                table.Cell().AlignRight().PaddingTop(10)
                    .Text(visits.Sum(v => v.TotalCost).ToString("c"))
                    .SemiBold()
                    .FontSize(14)
                    .FontColor(Colors.Green.Darken2);
            });
        });
    }

    private static void ComposeUpcomingVisits(
        IContainer container,
        IReadOnlyCollection<ReportVisitDto> visits)
    {
        container.PaddingVertical(1, Unit.Centimetre).Column(column =>
        {
            if (visits.Count == 0)
            {
                column.Item().Text("Brak zaplanowanych wizyt w wybranym okresie.");
                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(105);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.ConstantColumn(80);
                });

                table.Header(header =>
                {
                    header.Cell().Text("Termin").SemiBold();
                    header.Cell().Text("Pacjent").SemiBold();
                    header.Cell().Text("Lekarz").SemiBold();
                    header.Cell().Text("Status").SemiBold();
                    header.Cell().ColumnSpan(4).PaddingVertical(5).BorderBottom(1);
                });

                foreach (var visit in visits)
                {
                    table.Cell().Text(visit.ScheduledDate.ToString("dd.MM.yyyy HH:mm"));
                    table.Cell().Text($"{visit.PatientFirstName} {visit.PatientLastName}");
                    table.Cell().Text(ShortDoctorName(visit.DoctorEmail));
                    table.Cell().Text("Zaplanowana");
                }
            });
        });
    }

    private static string ShortDoctorName(string? email)
    {
        return email?.Split('@')[0] ?? "Nieznany";
    }

    private static void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Text(text =>
        {
            text.Span("Strona ");
            text.CurrentPageNumber();
            text.Span(" z ");
            text.TotalPages();
        });
    }
}
