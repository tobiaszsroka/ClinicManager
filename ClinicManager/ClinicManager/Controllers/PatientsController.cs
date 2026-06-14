using ClinicManager.DTOs;
using ClinicManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Controllers;

[Authorize(Roles = "Admin,Rejestratorka,Lekarz")]
public class PatientsController : Controller
{
    private readonly PatientService _patientService;

    public PatientsController(PatientService patientService)
    {
        _patientService = patientService;
    }

    public async Task<IActionResult> Index(string? searchString)
    {
        ViewData["CurrentFilter"] = searchString;
        return View(await _patientService.GetAllAsync(searchString));
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var patient = await _patientService.GetDetailsAsync(id.Value);
        return patient == null ? NotFound() : View(patient);
    }

    public IActionResult Create()
    {
        return View(new PatientDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PatientDto patient)
    {
        if (await _patientService.PeselExistsAsync(patient.Pesel))
        {
            ModelState.AddModelError(nameof(patient.Pesel), "Pacjent z takim numerem PESEL już istnieje w systemie.");
        }

        if (!ModelState.IsValid) return View(patient);

        await _patientService.CreateAsync(patient);
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var patient = await _patientService.GetByIdAsync(id.Value);
        return patient == null ? NotFound() : View(patient);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PatientDto patient)
    {
        if (id != patient.Id) return NotFound();

        if (await _patientService.PeselExistsAsync(patient.Pesel, patient.Id))
        {
            ModelState.AddModelError(nameof(patient.Pesel), "Pacjent z takim numerem PESEL już istnieje w systemie.");
        }

        if (!ModelState.IsValid) return View(patient);

        return await _patientService.UpdateAsync(patient)
            ? RedirectToAction(nameof(Index))
            : NotFound();
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var patient = await _patientService.GetByIdAsync(id.Value);
        return patient == null ? NotFound() : View(patient);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _patientService.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
