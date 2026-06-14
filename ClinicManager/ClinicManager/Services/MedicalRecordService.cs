using ClinicManager.Data;
using ClinicManager.DTOs;
using ClinicManager.Mappers;
using ClinicManager.Models;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Services;

public class MedicalRecordService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _environment;

    public MedicalRecordService(ApplicationDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    public async Task<PatientRecordDto?> GetByPatientIdAsync(int patientId)
    {
        var patient = await _context.Patients
            .AsNoTracking()
            .Include(p => p.MedicalRecord)
                .ThenInclude(m => m!.Documents)
            .FirstOrDefaultAsync(p => p.Id == patientId);

        if (patient == null) return null;

        return new PatientRecordDto
        {
            Patient = PatientMapper.ToDto(patient),
            Record = patient.MedicalRecord == null
                ? null
                : MedicalRecordMapper.ToDto(patient.MedicalRecord)
        };
    }

    public async Task<PatientRecordDto?> GetByRecordIdAsync(int id)
    {
        var record = await _context.MedicalRecords
            .AsNoTracking()
            .Include(m => m.Patient)
            .Include(m => m.Documents)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (record?.Patient == null) return null;

        return new PatientRecordDto
        {
            Patient = PatientMapper.ToDto(record.Patient),
            Record = MedicalRecordMapper.ToDto(record)
        };
    }

    public async Task<int> CreateAsync(MedicalRecordDto record, IReadOnlyCollection<IFormFile> files)
    {
        var entity = MedicalRecordMapper.ToEntity(record);
        entity.Documents = await SaveFilesAsync(files);

        _context.MedicalRecords.Add(entity);
        await _context.SaveChangesAsync();
        return entity.PatientId;
    }

    public async Task<bool> UpdateAsync(MedicalRecordDto record)
    {
        var entity = await _context.MedicalRecords.FindAsync(record.Id);
        if (entity == null) return false;

        MedicalRecordMapper.UpdateEntity(record, entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int?> UploadDocumentsAsync(int medicalRecordId, IReadOnlyCollection<IFormFile> files)
    {
        var record = await _context.MedicalRecords.FindAsync(medicalRecordId);
        if (record == null) return null;

        var documents = await SaveFilesAsync(files);
        foreach (var document in documents)
        {
            document.MedicalRecordId = medicalRecordId;
            _context.MedicalDocuments.Add(document);
        }

        await _context.SaveChangesAsync();
        return record.PatientId;
    }

    public async Task<int?> DeleteDocumentAsync(int id)
    {
        var document = await _context.MedicalDocuments
            .Include(d => d.MedicalRecord)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document?.MedicalRecord == null) return null;

        var filePath = Path.Combine(GetUploadsDirectory(), document.SavedFileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        var patientId = document.MedicalRecord.PatientId;
        _context.MedicalDocuments.Remove(document);
        await _context.SaveChangesAsync();
        return patientId;
    }

    public async Task<MedicalDocumentDownloadDto?> GetDownloadAsync(int id)
    {
        var document = await _context.MedicalDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id);
        if (document == null || string.IsNullOrEmpty(document.SavedFileName)) return null;

        var filePath = Path.Combine(GetUploadsDirectory(), document.SavedFileName);
        if (!File.Exists(filePath)) return null;

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(document.OriginalFileName, out var contentType))
        {
            contentType = "application/octet-stream";
        }

        return new MedicalDocumentDownloadDto(filePath, contentType, document.OriginalFileName);
    }

    private async Task<List<MedicalDocument>> SaveFilesAsync(IReadOnlyCollection<IFormFile> files)
    {
        var documents = new List<MedicalDocument>();
        if (files.Count == 0) return documents;

        var uploadsDirectory = GetUploadsDirectory();
        Directory.CreateDirectory(uploadsDirectory);

        foreach (var file in files.Where(f => f.Length > 0))
        {
            var originalFileName = Path.GetFileName(file.FileName);
            var savedFileName = Guid.NewGuid() + "_" + originalFileName;
            var filePath = Path.Combine(uploadsDirectory, savedFileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            documents.Add(new MedicalDocument
            {
                OriginalFileName = originalFileName,
                SavedFileName = savedFileName,
                UploadDate = DateTime.Now
            });
        }

        return documents;
    }

    private string GetUploadsDirectory()
    {
        return Path.Combine(_environment.ContentRootPath, "App_Data", "Uploads");
    }
}
