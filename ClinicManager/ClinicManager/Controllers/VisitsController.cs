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
                .FirstOrDefaultAsync(m => m.Id == id);
            
            if (visit == null) return NotFound();

            if (User.IsInRole("Lekarz") && !User.IsInRole("Admin") && !User.IsInRole("Rejestratorka"))
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (visit.AssignedDoctorId != userId) return Forbid();
            }

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
