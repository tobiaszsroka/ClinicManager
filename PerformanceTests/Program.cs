using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using NBomber.Contracts;
using NBomber.Contracts.Stats;
using NBomber.CSharp;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var options = LoadTestOptions.FromArgs(args);
using var httpClient = CreateHttpClient(options.BaseUrl);
var targetUrl = new Uri(options.BaseUrl, "/api/visits");

Console.WriteLine("ClinicManager NBomber load test");
Console.WriteLine($"Target: {targetUrl}");
Console.WriteLine($"Users: {options.Users}, Duration: {options.DurationSeconds}s");

await LoginAsync(httpClient, options);
await EnsureEndpointIsReadyAsync(httpClient);

var stopwatch = Stopwatch.StartNew();
var scenario = Scenario.Create("get_active_visits_with_joins", async context =>
{
    return await Step.Run("GET /api/visits", context, async () =>
    {
        using var response = await httpClient.GetAsync("/api/visits");
        var body = await response.Content.ReadAsStringAsync();

        return response.IsSuccessStatusCode
            ? Response.Ok(sizeBytes: body.Length, statusCode: ((int)response.StatusCode).ToString())
            : Response.Fail(statusCode: ((int)response.StatusCode).ToString(), message: body);
    });
})
.WithoutWarmUp()
.WithLoadSimulations(
    Simulation.KeepConstant(copies: options.Users, during: TimeSpan.FromSeconds(options.DurationSeconds)));

var stats = NBomberRunner
    .RegisterScenarios(scenario)
    .WithReportFolder(options.ReportsDirectory)
    .WithReportFormats(ReportFormat.Html, ReportFormat.Csv, ReportFormat.Txt)
    .Run();
stopwatch.Stop();

var pdfPath = Path.Combine(options.OutputDirectory, "nbomber-report.pdf");
Directory.CreateDirectory(options.OutputDirectory);
GeneratePdfReport(stats, options, stopwatch.Elapsed, pdfPath);

Console.WriteLine($"PDF report: {pdfPath}");

static HttpClient CreateHttpClient(Uri baseUrl)
{
    var handler = new HttpClientHandler
    {
        CookieContainer = new CookieContainer(),
        AllowAutoRedirect = true,
        ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };

    return new HttpClient(handler)
    {
        BaseAddress = baseUrl,
        Timeout = TimeSpan.FromSeconds(30)
    };
}

static async Task LoginAsync(HttpClient httpClient, LoadTestOptions options)
{
    var loginPage = await httpClient.GetStringAsync("/Identity/Account/Login");
    var token = ExtractRequestVerificationToken(loginPage);
    var form = new Dictionary<string, string>
    {
        ["Input.Email"] = options.Email,
        ["Input.Password"] = options.Password,
        ["Input.RememberMe"] = "false",
        ["__RequestVerificationToken"] = token
    };

    using var response = await httpClient.PostAsync("/Identity/Account/Login", new FormUrlEncodedContent(form));
    if (!response.IsSuccessStatusCode)
    {
        throw new InvalidOperationException($"Login failed with HTTP {(int)response.StatusCode}.");
    }

    using var probe = await httpClient.GetAsync("/api/visits");
    if (probe.StatusCode == HttpStatusCode.Unauthorized || probe.StatusCode == HttpStatusCode.Forbidden)
    {
        throw new InvalidOperationException("Login succeeded, but the API rejected the authenticated user.");
    }
}

static async Task EnsureEndpointIsReadyAsync(HttpClient httpClient)
{
    using var response = await httpClient.GetAsync("/api/visits");
    if (!response.IsSuccessStatusCode)
    {
        throw new InvalidOperationException($"GET /api/visits returned HTTP {(int)response.StatusCode} before the load test.");
    }
}

static string ExtractRequestVerificationToken(string html)
{
    string[] patterns =
    [
        "<input[^>]*name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"[^>]*>",
        "<input[^>]*value=\"(?<token>[^\"]+)\"[^>]*name=\"__RequestVerificationToken\"[^>]*>"
    ];

    foreach (var pattern in patterns)
    {
        var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
        if (match.Success)
        {
            return WebUtility.HtmlDecode(match.Groups["token"].Value);
        }
    }

    throw new InvalidOperationException("Request verification token was not found on the login page.");
}

static void GeneratePdfReport(NodeStats stats, LoadTestOptions options, TimeSpan elapsed, string outputPath)
{
    var scenario = stats.ScenarioStats.First();
    var step = scenario.StepStats.FirstOrDefault();
    var latency = step?.Ok.Latency ?? scenario.Ok.Latency;
    var requests = step?.Ok.Request ?? scenario.Ok.Request;
    var totalRequestCount = step?.Ok.Request.Count ?? scenario.Ok.Request.Count;
    var failedRequestCount = step?.Fail.Request.Count ?? scenario.Fail.Request.Count;
    var okRequestCount = totalRequestCount - failedRequestCount;
    var targetUrl = new Uri(options.BaseUrl, "/api/visits");

    Document.Create(container =>
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(style => style.FontSize(10));

            page.Header().Column(column =>
            {
                column.Item().Text("US19 - NBomber test wydajnosci").FontSize(20).SemiBold().FontColor(Colors.Blue.Darken2);
                column.Item().Text($"Wygenerowano: {DateTime.Now:dd.MM.yyyy HH:mm}");
            });

            page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
            {
                column.Item().Text("Scenariusz").FontSize(14).SemiBold();
                column.Item().Text($"Endpoint: GET {targetUrl}");
                column.Item().Text($"Uzytkownicy wirtualni: {options.Users}");
                column.Item().Text($"Czas testu: {options.DurationSeconds}s");
                column.Item().Text($"Rzeczywisty czas wykonania: {elapsed.TotalSeconds:0.0}s");
                column.Item().PaddingTop(8).Text("Wynik").FontSize(14).SemiBold();

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    AddRow(table, "Laczna liczba zadan", totalRequestCount.ToString());
                    AddRow(table, "Poprawne zadania", okRequestCount.ToString());
                    AddRow(table, "Bledne zadania", failedRequestCount.ToString());
                    AddRow(table, "RPS", requests.RPS.ToString("0.00"));
                    AddRow(table, "Srednia latencja", $"{latency.MeanMs:0.00} ms");
                    AddRow(table, "P95", $"{latency.Percent95:0.00} ms");
                    AddRow(table, "P99", $"{latency.Percent99:0.00} ms");
                    AddRow(table, "Min / Max", $"{latency.MinMs:0.00} ms / {latency.MaxMs:0.00} ms");
                });

                column.Item().PaddingTop(8).Text("Kryterium powodzenia").FontSize(14).SemiBold();
                column.Item().Text("Scenariusz jest uznany za udany, gdy zadania GET /api/visits zwracaja odpowiedz 2xx.");
            });

            page.Footer().AlignCenter().Text(text =>
            {
                text.Span("ClinicManager - raport NBomber");
            });
        });
    }).GeneratePdf(outputPath);
}

static void AddRow(TableDescriptor table, string label, string value)
{
    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(label).SemiBold();
    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(value);
}

internal sealed record LoadTestOptions(
    Uri BaseUrl,
    string Email,
    string Password,
    int Users,
    int DurationSeconds,
    string ReportsDirectory,
    string OutputDirectory)
{
    public static LoadTestOptions FromArgs(string[] args)
    {
        string Get(string name, string fallback)
        {
            var prefix = $"--{name}=";
            return args.FirstOrDefault(arg => arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))?[prefix.Length..]
                ?? Environment.GetEnvironmentVariable($"CLINICMANAGER_LOAD_{name.Replace("-", "_").ToUpperInvariant()}")
                ?? fallback;
        }

        return new LoadTestOptions(
            BaseUrl: new Uri(Get("base-url", "https://localhost:7286")),
            Email: Get("email", "admin@clinic.com"),
            Password: Get("password", "Admin123!"),
            Users: int.Parse(Get("users", "50")),
            DurationSeconds: int.Parse(Get("duration", "20")),
            ReportsDirectory: Get("reports-dir", Path.Combine(Environment.CurrentDirectory, "nbomber-results")),
            OutputDirectory: Get("output-dir", FindRepositoryRoot(Environment.CurrentDirectory)));
    }

    private static string FindRepositoryRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory != null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, ".git")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return startDirectory;
    }
}
