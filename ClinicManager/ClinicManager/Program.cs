using ClinicManager.Data;
using ClinicManager.Middleware;
using ClinicManager.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);
var logDirectory = Path.Combine(builder.Environment.ContentRootPath, "logs");
Environment.SetEnvironmentVariable("CLINICMANAGER_LOG_DIR", logDirectory);

var bootstrapLogger = LogManager
    .Setup()
    .LoadConfigurationFromFile("nlog.config")
    .GetCurrentClassLogger();

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddScoped<PatientService>();
builder.Services.AddScoped<MedicationService>();
builder.Services.AddScoped<VisitService>();
builder.Services.AddScoped<MedicalRecordService>();
builder.Services.AddScoped<ReportService>();

builder.Services.AddRazorPages();

builder.Logging.ClearProviders();
builder.Host.UseNLog();

try
{
    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        await DataSeeder.SeedRolesAndAdminAsync(services);
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseMiddleware<ExceptionLoggingMiddleware>();

    app.UseHttpsRedirection();
    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapStaticAssets();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();
    app.MapRazorPages();

    app.Run();
}
catch (Exception exception)
{
    bootstrapLogger.Error(exception, "Aplikacja została zatrzymana z powodu nieobsłużonego wyjątku.");
    throw;
}
finally
{
    LogManager.Shutdown();
}
