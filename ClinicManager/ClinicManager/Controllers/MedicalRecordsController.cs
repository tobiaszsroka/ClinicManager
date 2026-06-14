using ClinicManager.DTOs;
using ClinicManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Controllers;

[Authorize(Roles = "Admin,Rejestratorka,Lekarz")]
public class MedicalRecordsController : Controller
{
    private readonly MedicalRecordService _medicalRecordService;

    public MedicalRecordsController(MedicalRecordService medicalRecordService)
    {
        _medicalRecordService = medicalRecordService;
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var result = await _medicalRecordService.GetByPatientIdAsync(id.Value);
        if (result == null) return NotFound();

        if (result.Record == null)
        {
            return RedirectToAction(nameof(Create), new { patientId = result.Patient.Id });
        }

        SetPatientViewData(result.Patient);
        return View(result.Record);
    }

    public async Task<IActionResult> Create(int patientId)
    {
        var result = await _medicalRecordService.GetByPatientIdAsync(patientId);
        if (result == null) return NotFound();

        ViewData["PatientName"] = $"{result.Patient.FirstName} {result.Patient.LastName}";
        return View(new MedicalRecordDto { PatientId = patientId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MedicalRecordDto medicalRecord, List<IFormFile> scanFiles)
    {
        if (!ModelState.IsValid)
        {
            var result = await _medicalRecordService.GetByPatientIdAsync(medicalRecord.PatientId);
            if (result != null)
            {
                ViewData["PatientName"] = $"{result.Patient.FirstName} {result.Patient.LastName}";
            }

            return View(medicalRecord);
        }

        var patientId = await _medicalRecordService.CreateAsync(medicalRecord, scanFiles);
        return RedirectToAction(nameof(Details), new { id = patientId });
    }

    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var result = await _medicalRecordService.GetByRecordIdAsync(id.Value);
        if (result?.Record == null) return NotFound();

        ViewData["PatientName"] = $"{result.Patient.FirstName} {result.Patient.LastName}";
        return View(result.Record);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MedicalRecordDto medicalRecord)
    {
        if (id != medicalRecord.Id) return NotFound();
        if (!ModelState.IsValid) return View(medicalRecord);

        return await _medicalRecordService.UpdateAsync(medicalRecord)
            ? RedirectToAction(nameof(Details), new { id = medicalRecord.PatientId })
            : NotFound();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadDocument(int medicalRecordId, List<IFormFile> scanFiles)
    {
        var patientId = await _medicalRecordService.UploadDocumentsAsync(medicalRecordId, scanFiles);
        return patientId == null
            ? NotFound()
            : RedirectToAction(nameof(Details), new { id = patientId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDocument(int id)
    {
        var patientId = await _medicalRecordService.DeleteDocumentAsync(id);
        return patientId == null
            ? NotFound()
            : RedirectToAction(nameof(Details), new { id = patientId });
    }

    public async Task<IActionResult> DownloadScan(int id)
    {
        var document = await _medicalRecordService.GetDownloadAsync(id);
        return document == null
            ? NotFound()
            : PhysicalFile(document.FilePath, document.ContentType, document.OriginalFileName);
    }

    private void SetPatientViewData(PatientDto patient)
    {
        ViewData["PatientName"] = $"{patient.FirstName} {patient.LastName}";
        ViewData["PatientPesel"] = patient.Pesel;
        ViewData["PatientInsurance"] = string.IsNullOrEmpty(patient.InsuranceNumber)
            ? "Brak wpisu"
            : patient.InsuranceNumber;
    }
}
