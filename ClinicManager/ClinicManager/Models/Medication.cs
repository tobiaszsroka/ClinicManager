using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Models
{
    public class Medication
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Nazwa leku jest wymagana")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Cena jest wymagana")]
        [Range(0.01, 10000.00, ErrorMessage = "Cena musi być większa niż 0")]
        public decimal UnitPrice { get; set; }

        public ICollection<PrescribedMedication> Prescriptions { get; set; } = new List<PrescribedMedication>();
    }
}
