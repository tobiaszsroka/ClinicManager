using ClinicManager.Models;
using System.ComponentModel.DataAnnotations;

namespace ClinicManager.DTOs;

/// <summary>Dane służące do zmiany statusu wizyty.</summary>
public class VisitStatusUpdateDto
{
    /// <summary>Nowy status wizyty.</summary>
    [Required]
    public VisitStatus Status { get; set; }
}

/// <summary>Dane formularza przesyłania dokumentów kartoteki.</summary>
public class MedicalDocumentUploadDto
{
    /// <summary>Pliki, które zostaną dołączone do kartoteki.</summary>
    [Required]
    public List<IFormFile> Files { get; set; } = [];
}

/// <summary>Dane nowej kartoteki medycznej wraz z opcjonalnymi dokumentami.</summary>
public class MedicalRecordCreateDto
{
    /// <summary>Identyfikator pacjenta.</summary>
    [Required]
    public int PatientId { get; set; }

    /// <summary>Ogólne informacje medyczne o pacjencie.</summary>
    public string? GeneralNotes { get; set; }

    /// <summary>Początkowe dokumenty kartoteki.</summary>
    public List<IFormFile> Files { get; set; } = [];
}
