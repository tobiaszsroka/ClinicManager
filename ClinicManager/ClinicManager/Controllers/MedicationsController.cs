using ClinicManager.Data;
using ClinicManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Controllers
{
    [Authorize(Roles = "Admin,Rejestratorka")]
    public class MedicationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MedicationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Medications
                .AsNoTracking()
                .OrderBy(m => m.Name)
                .ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var medication = await _context.Medications
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            return medication == null ? NotFound() : View(medication);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,UnitPrice")] Medication medication)
        {
            medication.Name = medication.Name.Trim();

            if (await MedicationNameExistsAsync(medication.Name))
            {
                ModelState.AddModelError("Name", "Lek o tej nazwie już istnieje w katalogu.");
            }

            if (!ModelState.IsValid) return View(medication);

            _context.Add(medication);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Lek został dodany do katalogu.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var medication = await _context.Medications.FindAsync(id);
            return medication == null ? NotFound() : View(medication);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,UnitPrice")] Medication medication)
        {
            if (id != medication.Id) return NotFound();

            medication.Name = medication.Name.Trim();

            if (await MedicationNameExistsAsync(medication.Name, medication.Id))
            {
                ModelState.AddModelError("Name", "Lek o tej nazwie już istnieje w katalogu.");
            }

            if (!ModelState.IsValid) return View(medication);

            try
            {
                _context.Update(medication);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Medications.AnyAsync(m => m.Id == medication.Id))
                {
                    return NotFound();
                }

                throw;
            }

            TempData["SuccessMessage"] = "Dane leku zostały zaktualizowane.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var medication = await _context.Medications
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            return medication == null ? NotFound() : View(medication);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var medication = await _context.Medications.FindAsync(id);
            if (medication == null) return RedirectToAction(nameof(Index));

            if (await _context.PrescribedMedications.AnyAsync(p => p.MedicationId == id))
            {
                TempData["ErrorMessage"] = "Nie można usunąć leku, który występuje na zapisanej recepcie.";
                return RedirectToAction(nameof(Index));
            }

            _context.Medications.Remove(medication);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Lek został usunięty z katalogu.";
            return RedirectToAction(nameof(Index));
        }

        private Task<bool> MedicationNameExistsAsync(string name, int? excludedId = null)
        {
            return _context.Medications.AnyAsync(m =>
                m.Name == name && (!excludedId.HasValue || m.Id != excludedId.Value));
        }
    }
}
