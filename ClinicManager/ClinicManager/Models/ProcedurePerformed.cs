using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models
{
    public class ProcedurePerformed
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Opis procedury jest wymagany")]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0, 50000.00)]
        public decimal ServiceCost { get; set; }

        // Klucz obcy do wizyty
        [Required]
        [ForeignKey("Visit")]
        public int VisitId { get; set; }
        public Visit? Visit { get; set; }

        // Zostawiam zakomentowane dopóki nie utworzymy PrescribedMedication
        // public ICollection<PrescribedMedication> PrescribedMedications { get; set; } = new List<PrescribedMedication>();
    }
}
