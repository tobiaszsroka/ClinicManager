using ClinicManager.Data;
using ClinicManager.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace ClinicManager.Controllers
{
    [Authorize(Roles = "Admin,Rejestratorka,Lekarz")]
    public class MedicalRecordsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public MedicalRecordsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: MedicalRecords/Details/5 (patient ID)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var patient = await _context.Patients
                .Include(p => p.MedicalRecord)
                    .ThenInclude(m => m.Documents)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (patient == null) return NotFound();

            if (patient.MedicalRecord == null)
            {
                return RedirectToAction(nameof(Create), new { patientId = patient.Id });
            }

            ViewData["PatientName"] = $"{patient.FirstName} {patient.LastName}";
            ViewData["PatientPesel"] = patient.Pesel;
            ViewData["PatientInsurance"] = string.IsNullOrEmpty(patient.InsuranceNumber) ? "Brak wpisu" : patient.InsuranceNumber;
            
            return View(patient.MedicalRecord);
        }

        // GET: MedicalRecords/Create
        public async Task<IActionResult> Create(int patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null) return NotFound();

            ViewData["PatientName"] = $"{patient.FirstName} {patient.LastName}";
            var record = new MedicalRecord { PatientId = patientId };
            return View(record);
        }

        // POST: MedicalRecords/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PatientId,GeneralNotes")] MedicalRecord medicalRecord, List<IFormFile> scanFiles)
        {
            if (ModelState.IsValid)
            {
                medicalRecord.Documents = new List<MedicalDocument>();

                if (scanFiles != null && scanFiles.Count > 0)
                {
                    var uploadsDir = Path.Combine(_env.ContentRootPath, "App_Data", "Uploads");
                    if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                    foreach (var file in scanFiles)
                    {
                        if (file.Length > 0)
                        {
                            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                            var filePath = Path.Combine(uploadsDir, uniqueFileName);

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                await file.CopyToAsync(stream);
                            }

                            medicalRecord.Documents.Add(new MedicalDocument
                            {
                                OriginalFileName = Path.GetFileName(file.FileName),
                                SavedFileName = uniqueFileName,
                                UploadDate = DateTime.Now
                            });
                        }
                    }
                }

                _context.Add(medicalRecord);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = medicalRecord.PatientId });
            }
            return View(medicalRecord);
        }

        // GET: MedicalRecords/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var record = await _context.MedicalRecords
                .Include(m => m.Patient)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (record == null) return NotFound();

            ViewData["PatientName"] = $"{record.Patient?.FirstName} {record.Patient?.LastName}";
            return View(record);
        }

        // POST: MedicalRecords/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PatientId,GeneralNotes")] MedicalRecord medicalRecord)
        {
            if (id != medicalRecord.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var recordInDb = await _context.MedicalRecords.FindAsync(id);
                    if (recordInDb != null)
                    {
                        recordInDb.GeneralNotes = medicalRecord.GeneralNotes;
                        await _context.SaveChangesAsync();
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.MedicalRecords.Any(e => e.Id == medicalRecord.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Details), new { id = medicalRecord.PatientId }); // Wracamy do szczegółów po PatientId
            }
            return View(medicalRecord);
        }

        // POST: MedicalRecords/UploadDocument
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(int medicalRecordId, List<IFormFile> scanFiles)
        {
            var record = await _context.MedicalRecords.FindAsync(medicalRecordId);
            if (record == null) return NotFound();

            if (scanFiles != null && scanFiles.Count > 0)
            {
                var uploadsDir = Path.Combine(_env.ContentRootPath, "App_Data", "Uploads");
                if (!Directory.Exists(uploadsDir)) Directory.CreateDirectory(uploadsDir);

                foreach (var file in scanFiles)
                {
                    if (file.Length > 0)
                    {
                        var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                        var filePath = Path.Combine(uploadsDir, uniqueFileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        _context.MedicalDocuments.Add(new MedicalDocument
                        {
                            OriginalFileName = Path.GetFileName(file.FileName),
                            SavedFileName = uniqueFileName,
                            UploadDate = DateTime.Now,
                            MedicalRecordId = medicalRecordId
                        });
                    }
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = record.PatientId });
        }

        // POST: MedicalRecords/DeleteDocument/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var document = await _context.MedicalDocuments.Include(d => d.MedicalRecord).FirstOrDefaultAsync(d => d.Id == id);
            if (document == null) return NotFound();

            var patientId = document.MedicalRecord!.PatientId;

            // Usunięcie pliku fizycznego
            var uploadsDir = Path.Combine(_env.ContentRootPath, "App_Data", "Uploads");
            var filePath = Path.Combine(uploadsDir, document.SavedFileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            _context.MedicalDocuments.Remove(document);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = patientId });
        }

        // GET: MedicalRecords/DownloadScan/5 (document ID)
        public async Task<IActionResult> DownloadScan(int id)
        {
            var doc = await _context.MedicalDocuments.FindAsync(id);
            if (doc == null || string.IsNullOrEmpty(doc.SavedFileName)) return NotFound();

            var uploadsDir = Path.Combine(_env.ContentRootPath, "App_Data", "Uploads");
            var filePath = Path.Combine(uploadsDir, doc.SavedFileName);

            if (!System.IO.File.Exists(filePath)) return NotFound();

            var contentType = "application/octet-stream";
            if (doc.SavedFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) contentType = "application/pdf";
            else if (doc.SavedFileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || doc.SavedFileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)) contentType = "image/jpeg";
            else if (doc.SavedFileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) contentType = "image/png";

            return PhysicalFile(filePath, contentType, doc.OriginalFileName);
        }
    }
}
