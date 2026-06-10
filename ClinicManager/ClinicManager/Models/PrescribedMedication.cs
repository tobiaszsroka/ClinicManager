using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models
{
    public class PrescribedMedication
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Dawkowanie jest wymagane")]
        [MaxLength(100)]
        public string Dosage { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ilość jest wymagana")]
        [Range(1, 1000)]
        public int Quantity { get; set; }

        // Klucz obcy do leku z katalogu
        [Required]
        [ForeignKey("Medication")]
        public int MedicationId { get; set; }
        public Medication? Medication { get; set; }

        // Klucz obcy do wykonanej procedury
        [Required]
        [ForeignKey("ProcedurePerformed")]
        public int ProcedurePerformedId { get; set; }
        public ProcedurePerformed? ProcedurePerformed { get; set; }
    }
}
