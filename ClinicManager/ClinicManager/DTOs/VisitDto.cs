using ClinicManager.Models;
using System.ComponentModel.DataAnnotations;

namespace ClinicManager.DTOs;

public class VisitDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Data wizyty jest wymagana")]
    public DateTime ScheduledDate { get; set; }

    public VisitStatus Status { get; set; } = VisitStatus.Scheduled;

    [Required]
    public int PatientId { get; set; }

    [Required(ErrorMessage = "Wymagane jest przypisanie lekarza")]
    public string AssignedDoctorId { get; set; } = string.Empty;

    public string? PatientFirstName { get; set; }
    public string? PatientLastName { get; set; }
    public string? DoctorEmail { get; set; }
    public IReadOnlyCollection<MedicalProcedureDto> Procedures { get; set; } = [];
    public IReadOnlyCollection<ClinicalNoteDto> ClinicalNotes { get; set; } = [];
    public IReadOnlyCollection<PrescribedMedicationDto> PrescribedMedications { get; set; } = [];
}

public class MedicalProcedureDto
{
    public int Id { get; set; }
    public int VisitId { get; set; }

    [Required(ErrorMessage = "Nazwa procedury jest wymagana.")]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(0, 100000, ErrorMessage = "Koszt musi być wartością dodatnią.")]
    public decimal BaseCost { get; set; }

    [Range(0, 100000, ErrorMessage = "Zniżka musi być wartością dodatnią.")]
    public decimal Discount { get; set; }

    public decimal FinalCost { get; set; }
}

public class PrescribedMedicationDto
{
    public int Id { get; set; }
    public int VisitId { get; set; }
    public int MedicationId { get; set; }

    [Required(ErrorMessage = "Dawkowanie jest wymagane")]
    [MaxLength(100)]
    public string Dosage { get; set; } = string.Empty;

    [Range(1, 1000)]
    public int Quantity { get; set; }

    public decimal UnitPriceAtPrescription { get; set; }
    public decimal TotalCost { get; set; }
    public string? MedicationName { get; set; }
}

public class ClinicalNoteDto
{
    public int Id { get; set; }
    public int VisitId { get; set; }

    [Required(ErrorMessage = "Treść notatki jest wymagana")]
    [MaxLength(4000)]
    public string Content { get; set; } = string.Empty;

    public string Author { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public record SelectOptionDto(string Value, string Text);

public class VisitFormOptionsDto
{
    public IReadOnlyCollection<SelectOptionDto> Patients { get; set; } = [];
    public IReadOnlyCollection<SelectOptionDto> Doctors { get; set; } = [];
    public IReadOnlyCollection<SelectOptionDto> Medications { get; set; } = [];
}
