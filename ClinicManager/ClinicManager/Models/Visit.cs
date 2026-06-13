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

        public MedicalRecord? MedicalRecord { get; set; }

        public VisitStatus Status { get; set; } = VisitStatus.Scheduled;

        // Klucz obcy pacjenta
        [Required]
        [ForeignKey("Patient")]
        public int PatientId { get; set; }
        public Patient? Patient { get; set; }

        // Relacja do Lekarza (ASP.NET Identity)
        [Required(ErrorMessage = "Wymagane jest przypisanie lekarza")]
        public string AssignedDoctorId { get; set; } = string.Empty;
        
        [ForeignKey("AssignedDoctorId")]
        public Microsoft.AspNetCore.Identity.IdentityUser? Doctor { get; set; }

        public ICollection<MedicalProcedure> Procedures { get; set; } = new List<MedicalProcedure>();
        public ICollection<ClinicalNote> ClinicalNotes { get; set; } = new List<ClinicalNote>();
    }
}
