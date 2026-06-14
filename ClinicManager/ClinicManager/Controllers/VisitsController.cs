using ClinicManager.DTOs;
using ClinicManager.Models;
using ClinicManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace ClinicManager.Controllers;

[Authorize(Roles = "Admin,Rejestratorka,Lekarz")]
public class VisitsController : Controller
{
    private readonly VisitService _visitService;

    public VisitsController(VisitService visitService)
    {
        _visitService = visitService;
    }

    public async Task<IActionResult> Index()
    {
        var doctorId = IsDoctorOnly()
            ? User.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;

        return View(await _visitService.GetAllAsync(doctorId));
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var visit = await _visitService.GetDetailsAsync(id.Value);
        if (visit == null) return NotFound();
        if (!CanDoctorAccess(visit)) return Forbid();

        await PopulateMedicationOptionsAsync();
        return View(visit);
    }

    [Authorize(Roles = "Admin,Rejestratorka")]
    public async Task<IActionResult> Create(int? patientId)
    {
        await PopulateVisitOptionsAsync(patientId);
        return View(new VisitDto
        {
            ScheduledDate = DateTime.Now.AddDays(1),
            PatientId = patientId ?? 0
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Rejestratorka")]
    public async Task<IActionResult> Create(VisitDto visit)
    {
        if (visit.ScheduledDate < DateTime.Now)
        {
            ModelState.AddModelError(nameof(visit.ScheduledDate), "Wizyta nie może zostać zaplanowana w przeszłości.");
        }
        else if (await _visitService.HasTimeConflictAsync(
            visit.AssignedDoctorId,
            visit.PatientId,
            visit.ScheduledDate))
        {
            ModelState.AddModelError(
                nameof(visit.ScheduledDate),
                "Kolizja terminów! Lekarz lub pacjent ma w tym czasie inną wizytę (wymagane 30 min odstępu).");
        }

        if (!ModelState.IsValid)
        {
            await PopulateVisitOptionsAsync(visit.PatientId, visit.AssignedDoctorId);
            return View(visit);
        }

        await _visitService.CreateAsync(visit);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var visit = await _visitService.GetByIdAsync(id.Value);
        if (visit == null) return NotFound();
        if (!CanDoctorAccess(visit)) return Forbid();

        await PopulateVisitOptionsAsync(visit.PatientId, visit.AssignedDoctorId);
        return View(visit);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VisitDto visit)
    {
        if (id != visit.Id) return NotFound();

        var currentVisit = await _visitService.GetByIdAsync(id);
        if (currentVisit == null) return NotFound();
        if (!CanDoctorAccess(currentVisit)) return Forbid();

        if (IsDoctorOnly())
        {
            return await _visitService.UpdateStatusAsync(id, visit.Status)
                ? RedirectToAction(nameof(Index))
                : NotFound();
        }

        if (await _visitService.HasTimeConflictAsync(
            visit.AssignedDoctorId,
            visit.PatientId,
            visit.ScheduledDate,
            visit.Id))
        {
            ModelState.AddModelError(
                nameof(visit.ScheduledDate),
                "Kolizja terminów! Lekarz lub pacjent ma w tym czasie inną wizytę (wymagane 30 min odstępu).");
        }

        if (!ModelState.IsValid)
        {
            await PopulateVisitOptionsAsync(visit.PatientId, visit.AssignedDoctorId);
            return View(visit);
        }

        return await _visitService.UpdateAsync(visit)
            ? RedirectToAction(nameof(Index))
            : NotFound();
    }

    [Authorize(Roles = "Admin,Rejestratorka")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var visit = await _visitService.GetForDeleteAsync(id.Value);
        return visit == null ? NotFound() : View(visit);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Rejestratorka")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _visitService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProcedure(MedicalProcedureDto procedure)
    {
        var visit = await _visitService.GetByIdAsync(procedure.VisitId);
        if (visit == null) return NotFound();

        if (visit.Status is VisitStatus.Completed or VisitStatus.Cancelled)
        {
            TempData["ErrorMessage"] = "Nie można dodać procedury do zakończonej lub anulowanej wizyty.";
            return RedirectToAction(nameof(Details), new { id = procedure.VisitId });
        }

        if (!CanDoctorAccess(visit)) return Forbid();
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Sprawdź dane procedury.";
            return RedirectToAction(nameof(Details), new { id = procedure.VisitId });
        }

        await _visitService.AddProcedureAsync(procedure);
        TempData["SuccessMessage"] = "Procedura została pomyślnie dodana do wizyty.";
        return RedirectToAction(nameof(Details), new { id = procedure.VisitId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProcedure(int procedureId)
    {
        var visit = await _visitService.GetByProcedureIdAsync(procedureId);
        if (visit == null) return NotFound();

        if (visit.Status is VisitStatus.Completed or VisitStatus.Cancelled)
        {
            TempData["ErrorMessage"] = "Nie można usuwać procedur z zakończonej wizyty.";
            return RedirectToAction(nameof(Details), new { id = visit.Id });
        }

        if (!CanDoctorAccess(visit)) return Forbid();

        await _visitService.DeleteProcedureAsync(procedureId);
        TempData["SuccessMessage"] = "Procedura usunięta.";
        return RedirectToAction(nameof(Details), new { id = visit.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Lekarz")]
    public async Task<IActionResult> AddPrescription(PrescribedMedicationDto prescription)
    {
        var visit = await _visitService.GetByIdAsync(prescription.VisitId);
        if (visit == null) return NotFound();

        if (visit.Status is VisitStatus.Completed or VisitStatus.Cancelled)
        {
            TempData["ErrorMessage"] = "Nie można wystawić recepty do zakończonej lub anulowanej wizyty.";
            return RedirectToAction(nameof(Details), new { id = prescription.VisitId });
        }

        if (!CanDoctorAccess(visit)) return Forbid();

        prescription.Dosage = prescription.Dosage.Trim();
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Sprawdź dawkowanie i ilość leku.";
            return RedirectToAction(nameof(Details), new { id = prescription.VisitId });
        }

        if (!await _visitService.AddPrescriptionAsync(prescription))
        {
            TempData["ErrorMessage"] = "Wybrany lek nie istnieje w katalogu.";
            return RedirectToAction(nameof(Details), new { id = prescription.VisitId });
        }

        TempData["SuccessMessage"] = "Lek został dodany do recepty.";
        return RedirectToAction(nameof(Details), new { id = prescription.VisitId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Lekarz")]
    public async Task<IActionResult> DeletePrescription(int prescriptionId)
    {
        var visit = await _visitService.GetByPrescriptionIdAsync(prescriptionId);
        if (visit == null) return NotFound();

        if (visit.Status is VisitStatus.Completed or VisitStatus.Cancelled)
        {
            TempData["ErrorMessage"] = "Nie można zmieniać recepty zakończonej lub anulowanej wizyty.";
            return RedirectToAction(nameof(Details), new { id = visit.Id });
        }

        if (!CanDoctorAccess(visit)) return Forbid();

        await _visitService.DeletePrescriptionAsync(prescriptionId);
        TempData["SuccessMessage"] = "Lek został usunięty z recepty.";
        return RedirectToAction(nameof(Details), new { id = visit.Id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Lekarz")]
    public async Task<IActionResult> AddNote(ClinicalNoteDto note)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == null) return Forbid();

        var visit = await _visitService.GetByIdAsync(note.VisitId);
        if (visit == null) return NotFound();
        if (visit.AssignedDoctorId != currentUserId) return Forbid();

        if (visit.Status != VisitStatus.InProgress)
        {
            TempData["ErrorMessage"] = "Notatkę kliniczną można dodać tylko do wizyty w trakcie.";
            return RedirectToAction(nameof(Details), new { id = note.VisitId });
        }

        note.Content = note.Content.Trim();
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Treść notatki jest wymagana i może mieć maksymalnie 4000 znaków.";
            return RedirectToAction(nameof(Details), new { id = note.VisitId });
        }

        await _visitService.AddNoteAsync(note, currentUserId);
        TempData["SuccessMessage"] = "Notatka kliniczna została dodana.";
        return RedirectToAction(nameof(Details), new { id = note.VisitId });
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

    private async Task PopulateVisitOptionsAsync(object? selectedPatient = null, object? selectedDoctor = null)
    {
        var options = await _visitService.GetFormOptionsAsync();
        ViewData["PatientId"] = new SelectList(options.Patients, "Value", "Text", selectedPatient?.ToString());
        ViewData["AssignedDoctorId"] = new SelectList(options.Doctors, "Value", "Text", selectedDoctor?.ToString());
    }

    private async Task PopulateMedicationOptionsAsync()
    {
        var options = await _visitService.GetFormOptionsAsync();
        ViewData["MedicationId"] = new SelectList(options.Medications, "Value", "Text");
        ViewData["HasMedications"] = options.Medications.Count > 0;
    }
}
