using ClinicManager.DTOs;
using ClinicManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicManager.Controllers.Api;

[ApiController]
[ApiExplorerSettings(GroupName = "v1")]
[Route("api/patients")]
[Authorize(Roles = "Admin,Rejestratorka,Lekarz")]
[Produces("application/json")]
public class PatientsApiController : ControllerBase
{
    private readonly PatientService _patientService;

    public PatientsApiController(PatientService patientService)
    {
        _patientService = patientService;
    }

    /// <summary>Pobiera listę pacjentów z opcjonalnym filtrem nazwiska lub numeru PESEL.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<PatientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<PatientDto>>> GetAll([FromQuery] string? search)
    {
        return Ok(await _patientService.GetAllAsync(search));
    }

    /// <summary>Pobiera pacjenta wraz z historią wizyt.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PatientDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientDetailsDto>> GetById(int id)
    {
        var patient = await _patientService.GetDetailsAsync(id);
        if (patient == null) return NotFound();

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        foreach (var visit in patient.Visits)
        {
            var canReadNotes = User.IsInRole("Lekarz") &&
                visit.AssignedDoctorId == currentUserId;
            if (!canReadNotes)
            {
                visit.ClinicalNotes = [];
            }
        }

        return Ok(patient);
    }

    /// <summary>Dodaje pacjenta.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Rejestratorka")]
    [ProducesResponseType(typeof(PatientDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PatientDto>> Create([FromBody] PatientDto patient)
    {
        if (await _patientService.PeselExistsAsync(patient.Pesel))
        {
            ModelState.AddModelError(nameof(patient.Pesel), "Pacjent z takim numerem PESEL już istnieje.");
            return ValidationProblem(ModelState);
        }

        patient.Id = await _patientService.CreateAsync(patient);
        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, patient);
    }

    /// <summary>Aktualizuje dane pacjenta.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Rejestratorka")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] PatientDto patient)
    {
        if (id != patient.Id) return BadRequest();

        if (await _patientService.PeselExistsAsync(patient.Pesel, patient.Id))
        {
            ModelState.AddModelError(nameof(patient.Pesel), "Pacjent z takim numerem PESEL już istnieje.");
            return ValidationProblem(ModelState);
        }

        return await _patientService.UpdateAsync(patient) ? NoContent() : NotFound();
    }

    /// <summary>Usuwa pacjenta.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Rejestratorka")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        if (await _patientService.GetByIdAsync(id) == null) return NotFound();

        await _patientService.DeleteAsync(id);
        return NoContent();
    }
}
