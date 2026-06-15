using ClinicManager.DTOs;
using ClinicManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicManager.Controllers.Api;

[ApiController]
[ApiExplorerSettings(GroupName = "v1")]
[Route("api/medical-records")]
[Authorize(Roles = "Admin,Rejestratorka,Lekarz")]
[Produces("application/json")]
public class MedicalRecordsApiController : ControllerBase
{
    private readonly MedicalRecordService _medicalRecordService;

    public MedicalRecordsApiController(MedicalRecordService medicalRecordService)
    {
        _medicalRecordService = medicalRecordService;
    }

    /// <summary>Pobiera kartotekę wskazanego pacjenta.</summary>
    [HttpGet("patient/{patientId:int}")]
    [ProducesResponseType(typeof(PatientRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientRecordDto>> GetByPatientId(int patientId)
    {
        var record = await _medicalRecordService.GetByPatientIdAsync(patientId);
        return record == null ? NotFound() : Ok(record);
    }

    /// <summary>Tworzy kartotekę pacjenta i zapisuje opcjonalne dokumenty.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(PatientRecordDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PatientRecordDto>> Create([FromForm] MedicalRecordCreateDto request)
    {
        var record = new MedicalRecordDto
        {
            PatientId = request.PatientId,
            GeneralNotes = request.GeneralNotes
        };
        await _medicalRecordService.CreateAsync(record, request.Files);

        var created = await _medicalRecordService.GetByPatientIdAsync(request.PatientId);
        return CreatedAtAction(
            nameof(GetByPatientId),
            new { patientId = request.PatientId },
            created);
    }

    /// <summary>Aktualizuje ogólne informacje w kartotece.</summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] MedicalRecordDto record)
    {
        if (id != record.Id) return BadRequest();
        return await _medicalRecordService.UpdateAsync(record) ? NoContent() : NotFound();
    }

    /// <summary>Dodaje dokumenty do istniejącej kartoteki.</summary>
    [HttpPost("{id:int}/documents")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadDocuments(int id, [FromForm] MedicalDocumentUploadDto request)
    {
        return await _medicalRecordService.UploadDocumentsAsync(id, request.Files) == null
            ? NotFound()
            : NoContent();
    }

    /// <summary>Pobiera dokument zapisany w kartotece.</summary>
    [HttpGet("documents/{documentId:int}")]
    [Produces("application/octet-stream")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadDocument(int documentId)
    {
        var document = await _medicalRecordService.GetDownloadAsync(documentId);
        return document == null
            ? NotFound()
            : PhysicalFile(document.FilePath, document.ContentType, document.OriginalFileName);
    }

    /// <summary>Usuwa dokument z kartoteki.</summary>
    [HttpDelete("documents/{documentId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDocument(int documentId)
    {
        return await _medicalRecordService.DeleteDocumentAsync(documentId) == null
            ? NotFound()
            : NoContent();
    }
}
