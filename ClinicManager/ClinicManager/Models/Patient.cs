using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Models
{
    public class Patient
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Imię jest wymagane")]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nazwisko jest wymagane")]
        [MaxLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "PESEL jest wymagany")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "PESEL musi mieć dokładnie 11 znaków")]
        public string Pesel { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? InsuranceNumber { get; set; }

        [Phone(ErrorMessage = "Niepoprawny format numeru telefonu")]
        public string? PhoneNumber { get; set; }

        [EmailAddress(ErrorMessage = "Niepoprawny format adresu e-mail")]
        public string? Email { get; set; }

        // Relacja 1:1 z MedicalRecord
        public MedicalRecord? MedicalRecord { get; set; }
        
        // Relacja 1:N z Visit
        public ICollection<Visit> Visits { get; set; } = new List<Visit>();
    }
}
