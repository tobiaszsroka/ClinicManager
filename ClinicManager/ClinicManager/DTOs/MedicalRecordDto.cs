namespace ClinicManager.DTOs;

public class MedicalRecordDto
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public string? GeneralNotes { get; set; }
    public IReadOnlyCollection<MedicalDocumentDto> Documents { get; set; } = [];
}

public class MedicalDocumentDto
{
    public int Id { get; set; }
    public int MedicalRecordId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string SavedFileName { get; set; } = string.Empty;
    public DateTime UploadDate { get; set; }
}

public class PatientRecordDto
{
    public PatientDto Patient { get; set; } = new();
    public MedicalRecordDto? Record { get; set; }
}

public record MedicalDocumentDownloadDto(string FilePath, string ContentType, string OriginalFileName);
