using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Controllers;

[Authorize(Roles = "Admin")]
public class DiagnosticsController : Controller
{
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<DiagnosticsController> _logger;

    public DiagnosticsController(
        IWebHostEnvironment environment,
        ILogger<DiagnosticsController> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult TestErrorLog()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        try
        {
            throw new InvalidOperationException("Testowy wyjątek NLog dla US13.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Test zapisu błędu do pliku NLog został wykonany.");
        }

        return Ok(new
        {
            message = "Testowy błąd został zapisany.",
            logFile = "logs/errors.log"
        });
    }

    [HttpGet]
    public IActionResult TestUnhandledError()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        throw new InvalidOperationException("Nieobsłużony testowy wyjątek NLog dla US13.");
    }
}
