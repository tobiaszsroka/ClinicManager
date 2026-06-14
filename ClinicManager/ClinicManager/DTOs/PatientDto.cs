using System.ComponentModel.DataAnnotations;

namespace ClinicManager.DTOs;

public class PatientDto
{
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

    [RegularExpression(@"^\d{9}$", ErrorMessage = "Numer telefonu musi składać się z dokładnie 9 cyfr bez spacji")]
    public string? PhoneNumber { get; set; }

    [EmailAddress(ErrorMessage = "Niepoprawny format adresu e-mail")]
    public string? Email { get; set; }
}

public class PatientDetailsDto
{
    public PatientDto Patient { get; set; } = new();
    public IReadOnlyCollection<VisitDto> Visits { get; set; } = [];
}
