using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models
{
    public class Visit
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Data wizyty jest wymagana")]
        public DateTime ScheduledDate { get; set; }

        public VisitStatus Status { get; set; } = VisitStatus.Scheduled;

        // Klucz obcy pacjenta
        [Required]
        [ForeignKey("Patient")]
        public int PatientId { get; set; }
        public Patient? Patient { get; set; }

        // TODO: Dodanie relacji do Lekarza (po dodaniu ASP.NET Identity)
        // public string? AssignedDoctorId { get; set; }

        // Zostawiam zkomentowane do momentu dodania kolejnych modeli
        // public ICollection<ProcedurePerformed> Procedures { get; set; } = new List<ProcedurePerformed>();
        // public ICollection<ClinicalNote> ClinicalNotes { get; set; } = new List<ClinicalNote>();
    }
}
