using ClinicManager.DTOs;
using ClinicManager.Models;
using ClinicManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClinicManager.Controllers.Api;

[ApiController]
[ApiExplorerSettings(GroupName = "v1")]
[Route("api/visits")]
[Authorize(Roles = "Admin,Rejestratorka,Lekarz")]
[Produces("application/json")]
public class VisitsApiController : ControllerBase
{
    private readonly VisitService _visitService;

    public VisitsApiController(VisitService visitService)
    {
        _visitService = visitService;
    }

    /// <summary>Pobiera wizyty. Lekarz otrzymuje wyłącznie wizyty przypisane do niego.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyCollection<VisitDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IReadOnlyCollection<VisitDto>>> GetAll()
    {
        var doctorId = IsDoctorOnly()
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;

        return Ok(await _visitService.GetAllAsync(doctorId));
    }

    /// <summary>Pobiera szczegóły wizyty, procedury, receptę i dostępne notatki.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(VisitDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VisitDto>> GetById(int id)
    {
        var visit = await _visitService.GetDetailsAsync(id);
        if (visit == null) return NotFound();
        if (!CanDoctorAccess(visit)) return Forbid();

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!User.IsInRole("Lekarz") || visit.AssignedDoctorId != currentUserId)
        {
            visit.ClinicalNotes = [];
        }

        return Ok(visit);
    }

    /// <summary>Planuje nową wizytę.</summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Rejestratorka")]
    [ProducesResponseType(typeof(VisitDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<VisitDto>> Create([FromBody] VisitDto visit)
    {
        if (visit.ScheduledDate < DateTime.Now)
        {
            ModelState.AddModelError(nameof(visit.ScheduledDate), "Wizyta nie może być zaplanowana w przeszłości.");
        }
        else if (await _visitService.HasTimeConflictAsync(
            visit.AssignedDoctorId,
            visit.PatientId,
            visit.ScheduledDate))
        {
            ModelState.AddModelError(nameof(visit.ScheduledDate), "Lekarz lub pacjent ma w tym czasie inną wizytę.");
        }

        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        visit.Id = await _visitService.CreateAsync(visit);
        return CreatedAtAction(nameof(GetById), new { id = visit.Id }, visit);
    }

    /// <summary>Aktualizuje termin, pacjenta, lekarza i status wizyty.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Rejestratorka")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] VisitDto visit)
    {
        if (id != visit.Id) return BadRequest();

        if (await _visitService.HasTimeConflictAsync(
            visit.AssignedDoctorId,
            visit.PatientId,
            visit.ScheduledDate,
            visit.Id))
        {
            ModelState.AddModelError(nameof(visit.ScheduledDate), "Lekarz lub pacjent ma w tym czasie inną wizytę.");
            return ValidationProblem(ModelState);
        }

        return await _visitService.UpdateAsync(visit) ? NoContent() : NotFound();
    }

    /// <summary>Zmienia status przypisanej wizyty.</summary>
    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin,Rejestratorka,Lekarz")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] VisitStatusUpdateDto request)
    {
        var visit = await _visitService.GetByIdAsync(id);
        if (visit == null) return NotFound();
        if (!CanDoctorAccess(visit)) return Forbid();

        return await _visitService.UpdateStatusAsync(id, request.Status) ? NoContent() : NotFound();
    }

    /// <summary>Usuwa wizytę.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,Rejestratorka")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        if (await _visitService.GetByIdAsync(id) == null) return NotFound();

        await _visitService.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>Dodaje procedurę medyczną do aktywnej wizyty.</summary>
    [HttpPost("{id:int}/procedures")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddProcedure(int id, [FromBody] MedicalProcedureDto procedure)
    {
        var visit = await _visitService.GetByIdAsync(id);
        if (visit == null) return NotFound();
        if (!CanDoctorAccess(visit)) return Forbid();
        if (visit.Status is VisitStatus.Completed or VisitStatus.Cancelled)
        {
            return Conflict(CreateConflict("Nie można dodać procedury do zakończonej lub anulowanej wizyty."));
        }

        procedure.VisitId = id;
        await _visitService.AddProcedureAsync(procedure);
        return NoContent();
    }

    /// <summary>Usuwa procedurę medyczną z aktywnej wizyty.</summary>
    [HttpDelete("{id:int}/procedures/{procedureId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProcedure(int id, int procedureId)
    {
        var visit = await _visitService.GetByProcedureIdAsync(procedureId);
        if (visit == null || visit.Id != id) return NotFound();
        if (!CanDoctorAccess(visit)) return Forbid();
        if (visit.Status is VisitStatus.Completed or VisitStatus.Cancelled)
        {
            return Conflict(CreateConflict("Nie można usunąć procedury z zakończonej lub anulowanej wizyty."));
        }

        await _visitService.DeleteProcedureAsync(procedureId);
        return NoContent();
    }

    /// <summary>Dodaje lek z katalogu do recepty dla aktywnej wizyty.</summary>
    [HttpPost("{id:int}/prescriptions")]
    [Authorize(Roles = "Admin,Lekarz")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddPrescription(int id, [FromBody] PrescribedMedicationDto prescription)
    {
        var visit = await _visitService.GetByIdAsync(id);
        if (visit == null) return NotFound();
        if (!CanDoctorAccess(visit)) return Forbid();
        if (visit.Status is VisitStatus.Completed or VisitStatus.Cancelled)
        {
            return Conflict(CreateConflict("Nie można zmienić recepty zakończonej lub anulowanej wizyty."));
        }

        prescription.VisitId = id;
        prescription.Dosage = prescription.Dosage.Trim();
        return await _visitService.AddPrescriptionAsync(prescription)
            ? NoContent()
            : NotFound(new ProblemDetails { Title = "Wybrany lek nie istnieje." });
    }

    /// <summary>Usuwa lek z recepty dla aktywnej wizyty.</summary>
    [HttpDelete("{id:int}/prescriptions/{prescriptionId:int}")]
    [Authorize(Roles = "Admin,Lekarz")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePrescription(int id, int prescriptionId)
    {
        var visit = await _visitService.GetByPrescriptionIdAsync(prescriptionId);
        if (visit == null || visit.Id != id) return NotFound();
        if (!CanDoctorAccess(visit)) return Forbid();
        if (visit.Status is VisitStatus.Completed or VisitStatus.Cancelled)
        {
            return Conflict(CreateConflict("Nie można zmienić recepty zakończonej lub anulowanej wizyty."));
        }

        await _visitService.DeletePrescriptionAsync(prescriptionId);
        return NoContent();
    }

    /// <summary>Dodaje poufną notatkę kliniczną do wizyty przypisanej lekarzowi.</summary>
    [HttpPost("{id:int}/notes")]
    [Authorize(Roles = "Lekarz")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddNote(int id, [FromBody] ClinicalNoteDto note)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return Forbid();

        var visit = await _visitService.GetByIdAsync(id);
        if (visit == null) return NotFound();
        if (visit.AssignedDoctorId != currentUserId) return Forbid();
        if (visit.Status != VisitStatus.InProgress)
        {
            return Conflict(CreateConflict("Notatkę można dodać tylko do wizyty w trakcie."));
        }

        note.VisitId = id;
        note.Content = note.Content.Trim();
        await _visitService.AddNoteAsync(note, currentUserId);
        return NoContent();
    }

    private bool IsDoctorOnly()
    {
        return User.IsInRole("Lekarz") &&
            !User.IsInRole("Admin") &&
            !User.IsInRole("Rejestratorka");
    }

    private bool CanDoctorAccess(VisitDto visit)
    {
        return !IsDoctorOnly() ||
            visit.AssignedDoctorId == User.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    private static ProblemDetails CreateConflict(string detail)
    {
        return new ProblemDetails
        {
            Title = "Operacja jest niedozwolona.",
            Detail = detail,
            Status = StatusCodes.Status409Conflict
        };
    }
}
