using ClinicManager.DTOs;
using ClinicManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Controllers;

[Authorize(Roles = "Admin,Rejestratorka")]
public class MedicationsController : Controller
{
    private readonly MedicationService _medicationService;

    public MedicationsController(MedicationService medicationService)
    {
        _medicationService = medicationService;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _medicationService.GetAllAsync());
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var medication = await _medicationService.GetByIdAsync(id.Value);
        return medication == null ? NotFound() : View(medication);
    }

    public IActionResult Create()
    {
        return View(new MedicationDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MedicationDto medication)
    {
        medication.Name = medication.Name.Trim();

        if (await _medicationService.NameExistsAsync(medication.Name))
        {
            ModelState.AddModelError(nameof(medication.Name), "Lek o tej nazwie już istnieje w katalogu.");
        }

        if (!ModelState.IsValid) return View(medication);

        await _medicationService.CreateAsync(medication);
        TempData["SuccessMessage"] = "Lek został dodany do katalogu.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var medication = await _medicationService.GetByIdAsync(id.Value);
        return medication == null ? NotFound() : View(medication);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MedicationDto medication)
    {
        if (id != medication.Id) return NotFound();

        medication.Name = medication.Name.Trim();
        if (await _medicationService.NameExistsAsync(medication.Name, medication.Id))
        {
            ModelState.AddModelError(nameof(medication.Name), "Lek o tej nazwie już istnieje w katalogu.");
        }

        if (!ModelState.IsValid) return View(medication);
        if (!await _medicationService.UpdateAsync(medication)) return NotFound();

        TempData["SuccessMessage"] = "Dane leku zostały zaktualizowane.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var medication = await _medicationService.GetByIdAsync(id.Value);
        return medication == null ? NotFound() : View(medication);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (await _medicationService.IsUsedAsync(id))
        {
            TempData["ErrorMessage"] = "Nie można usunąć leku, który występuje na zapisanej recepcie.";
            return RedirectToAction(nameof(Index));
        }

        await _medicationService.DeleteAsync(id);
        TempData["SuccessMessage"] = "Lek został usunięty z katalogu.";
        return RedirectToAction(nameof(Index));
    }
}
