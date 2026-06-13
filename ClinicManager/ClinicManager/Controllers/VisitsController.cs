using ClinicManager.Data;
using ClinicManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ClinicManager.Controllers
{
    [Authorize(Roles = "Admin,Rejestratorka,Lekarz")]
    public class VisitsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public VisitsController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Visits
        public async Task<IActionResult> Index()
        {
            IQueryable<Visit> visitsQuery = _context.Visits
                .Include(v => v.Patient)
                .Include(v => v.Doctor);

            if (User.IsInRole("Lekarz") && !User.IsInRole("Admin") && !User.IsInRole("Rejestratorka"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId != null)
                {
                    visitsQuery = visitsQuery.Where(v => v.AssignedDoctorId == userId);
                }
            }

            return View(await visitsQuery.OrderBy(v => v.ScheduledDate).ToListAsync());
        }

        // GET: Visits/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var visit = await _context.Visits
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .Include(v => v.Procedures)
                .Include(v => v.PrescribedMedications)
                    .ThenInclude(p => p.Medication)
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (visit == null) return NotFound();

            if (User.IsInRole("Lekarz") && !User.IsInRole("Admin") && !User.IsInRole("Rejestratorka"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (visit.AssignedDoctorId != userId) return Forbid();
            }

            var medications = await _context.Medications
                .AsNoTracking()
                .OrderBy(m => m.Name)
                .Select(m => new { m.Id, Label = m.Name + " (" + m.UnitPrice.ToString("0.00") + " zł)" })
                .ToListAsync();
            ViewData["MedicationId"] = new SelectList(medications, "Id", "Label");
            ViewData["HasMedications"] = medications.Count > 0;

            return View(visit);
        }

        // GET: Visits/Create
        [Authorize(Roles = "Admin,Rejestratorka")]
        public async Task<IActionResult> Create(int? patientId)
        {
            await PopulateDropDownsAsync();
            var visit = new Visit { ScheduledDate = DateTime.Now.AddDays(1) };
            if (patientId.HasValue) visit.PatientId = patientId.Value;
            
            return View(visit);
        }

        // POST: Visits/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Rejestratorka")]
        public async Task<IActionResult> Create([Bind("ScheduledDate,Status,PatientId,AssignedDoctorId")] Visit visit)
        {
            if (visit.ScheduledDate < DateTime.Now)
            {
                ModelState.AddModelError("ScheduledDate", "Wizyta nie może zostać zaplanowana w przeszłości.");
            }
            else if (await HasTimeConflictAsync(visit.AssignedDoctorId, visit.PatientId, visit.ScheduledDate))
            {
                ModelState.AddModelError("ScheduledDate", "Kolizja terminów! Lekarz lub pacjent ma w tym czasie inną wizytę (wymagane 30 min odstępu).");
            }

            if (ModelState.IsValid)
            {
                _context.Add(visit);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            await PopulateDropDownsAsync(visit.PatientId, visit.AssignedDoctorId);
            return View(visit);
        }

        // GET: Visits/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var visit = await _context.Visits.FindAsync(id);
            if (visit == null) return NotFound();

            if (User.IsInRole("Lekarz") && !User.IsInRole("Admin") && !User.IsInRole("Rejestratorka"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (visit.AssignedDoctorId != userId) return Forbid();
            }

            await PopulateDropDownsAsync(visit.PatientId, visit.AssignedDoctorId);
            return View(visit);
        }

        // POST: Visits/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ScheduledDate,Status,PatientId,AssignedDoctorId")] Visit visit)
        {
            if (id != visit.Id) return NotFound();

            var visitInDb = await _context.Visits.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id);
            if (visitInDb == null) return NotFound();

            bool isLekarz = User.IsInRole("Lekarz") && !User.IsInRole("Admin") && !User.IsInRole("Rejestratorka");
            string? currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (isLekarz && visitInDb.AssignedDoctorId != currentUserId)
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                if (!isLekarz && await HasTimeConflictAsync(visit.AssignedDoctorId, visit.PatientId, visit.ScheduledDate, visit.Id))
                {
                    ModelState.AddModelError("ScheduledDate", "Kolizja terminów! Lekarz lub pacjent ma w tym czasie inną wizytę (wymagane 30 min odstępu).");
                    await PopulateDropDownsAsync(visit.PatientId, visit.AssignedDoctorId);
                    return View(visit);
                }

                try
                {
                    if (isLekarz)
                    {
                        // Lekarz może zmienić TYLKO status.
                        visitInDb.Status = visit.Status;
                        _context.Update(visitInDb);
                    }
                    else
                    {
                        // Admin / Rejestratorka mogą zmienić wszystko
                        _context.Update(visit);
                    }
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VisitExists(visit.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            await PopulateDropDownsAsync(visit.PatientId, visit.AssignedDoctorId);
            return View(visit);
        }

        // GET: Visits/Delete/5
        [Authorize(Roles = "Admin,Rejestratorka")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var visit = await _context.Visits
                .Include(v => v.Patient)
                .Include(v => v.Doctor)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (visit == null) return NotFound();

            return View(visit);
        }

        // POST: Visits/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Rejestratorka")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var visit = await _context.Visits.FindAsync(id);
            if (visit != null)
            {
                _context.Visits.Remove(visit);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool VisitExists(int id)
        {
            return _context.Visits.Any(e => e.Id == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Rejestratorka,Lekarz")]
        public async Task<IActionResult> AddProcedure(int visitId, string name, string description, decimal baseCost, decimal discount)
        {
            var visit = await _context.Visits.FindAsync(visitId);
            if (visit == null) return NotFound();

            if (visit.Status == VisitStatus.Completed || visit.Status == VisitStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Nie można dodać procedury do zakończonej lub anulowanej wizyty.";
                return RedirectToAction(nameof(Details), new { id = visitId });
            }

            bool isLekarz = User.IsInRole("Lekarz") && !User.IsInRole("Admin") && !User.IsInRole("Rejestratorka");
            string? currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (isLekarz && visit.AssignedDoctorId != currentUserId)
            {
                return Forbid();
            }

            var procedure = new MedicalProcedure
            {
                VisitId = visitId,
                Name = name,
                Description = description,
                BaseCost = baseCost,
                Discount = discount
            };

            _context.MedicalProcedures.Add(procedure);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Procedura została pomyślnie dodana do wizyty.";
            return RedirectToAction(nameof(Details), new { id = visitId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Rejestratorka,Lekarz")]
        public async Task<IActionResult> DeleteProcedure(int procedureId)
        {
            var procedure = await _context.MedicalProcedures.Include(p => p.Visit).FirstOrDefaultAsync(p => p.Id == procedureId);
            if (procedure == null) return NotFound();

            var visit = procedure.Visit;
            if (visit!.Status == VisitStatus.Completed || visit.Status == VisitStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Nie można usuwać procedur z zakończonej wizyty.";
                return RedirectToAction(nameof(Details), new { id = visit.Id });
            }

            bool isLekarz = User.IsInRole("Lekarz") && !User.IsInRole("Admin") && !User.IsInRole("Rejestratorka");
            string? currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (isLekarz && visit.AssignedDoctorId != currentUserId)
            {
                return Forbid();
            }

            _context.MedicalProcedures.Remove(procedure);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Procedura usunięta.";
            return RedirectToAction(nameof(Details), new { id = visit.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Lekarz")]
        public async Task<IActionResult> AddPrescription(int visitId, int medicationId, string dosage, int quantity)
        {
            var visit = await _context.Visits.FindAsync(visitId);
            if (visit == null) return NotFound();

            if (visit.Status == VisitStatus.Completed || visit.Status == VisitStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Nie można wystawić recepty do zakończonej lub anulowanej wizyty.";
                return RedirectToAction(nameof(Details), new { id = visitId });
            }

            bool isDoctorOnly = User.IsInRole("Lekarz") && !User.IsInRole("Admin");
            string? currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (isDoctorOnly && visit.AssignedDoctorId != currentUserId)
            {
                return Forbid();
            }

            var medication = await _context.Medications.FindAsync(medicationId);
            if (medication == null)
            {
                TempData["ErrorMessage"] = "Wybrany lek nie istnieje w katalogu.";
                return RedirectToAction(nameof(Details), new { id = visitId });
            }

            if (string.IsNullOrWhiteSpace(dosage) || dosage.Length > 100)
            {
                TempData["ErrorMessage"] = "Podaj dawkowanie o długości do 100 znaków.";
                return RedirectToAction(nameof(Details), new { id = visitId });
            }

            if (quantity < 1 || quantity > 1000)
            {
                TempData["ErrorMessage"] = "Ilość leku musi mieścić się w zakresie od 1 do 1000.";
                return RedirectToAction(nameof(Details), new { id = visitId });
            }

            _context.PrescribedMedications.Add(new PrescribedMedication
            {
                VisitId = visitId,
                MedicationId = medication.Id,
                Dosage = dosage.Trim(),
                Quantity = quantity,
                UnitPriceAtPrescription = medication.UnitPrice
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Lek został dodany do recepty.";
            return RedirectToAction(nameof(Details), new { id = visitId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Lekarz")]
        public async Task<IActionResult> DeletePrescription(int prescriptionId)
        {
            var prescription = await _context.PrescribedMedications
                .Include(p => p.Visit)
                .FirstOrDefaultAsync(p => p.Id == prescriptionId);

            if (prescription == null) return NotFound();

            var visit = prescription.Visit!;
            if (visit.Status == VisitStatus.Completed || visit.Status == VisitStatus.Cancelled)
            {
                TempData["ErrorMessage"] = "Nie można zmieniać recepty zakończonej lub anulowanej wizyty.";
                return RedirectToAction(nameof(Details), new { id = visit.Id });
            }

            bool isDoctorOnly = User.IsInRole("Lekarz") && !User.IsInRole("Admin");
            string? currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (isDoctorOnly && visit.AssignedDoctorId != currentUserId)
            {
                return Forbid();
            }

            _context.PrescribedMedications.Remove(prescription);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Lek został usunięty z recepty.";
            return RedirectToAction(nameof(Details), new { id = visit.Id });
        }

        private async Task PopulateDropDownsAsync(object? selectedPatient = null, object? selectedDoctor = null)
        {
            ViewData["PatientId"] = new SelectList(_context.Patients.Select(p => new {
                Id = p.Id,
                FullName = p.FirstName + " " + p.LastName + " (" + p.Pesel + ")"
            }), "Id", "FullName", selectedPatient);

            var doctors = await _userManager.GetUsersInRoleAsync("Lekarz");
            ViewData["AssignedDoctorId"] = new SelectList(doctors.Select(d => new {
                Id = d.Id,
                Email = d.Email
            }), "Id", "Email", selectedDoctor);
        }

        private async Task<bool> HasTimeConflictAsync(string doctorId, int patientId, DateTime date, int? excludeVisitId = null)
        {
            var startTime = date.AddMinutes(-29);
            var endTime = date.AddMinutes(29);

            return await _context.Visits.AnyAsync(v =>
                v.Status != VisitStatus.Cancelled &&
                v.Status != VisitStatus.Completed &&
                (!excludeVisitId.HasValue || v.Id != excludeVisitId.Value) &&
                (v.AssignedDoctorId == doctorId || v.PatientId == patientId) &&
                v.ScheduledDate >= startTime &&
                v.ScheduledDate <= endTime);
        }
    }
}
