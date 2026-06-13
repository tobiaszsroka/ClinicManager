using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models
{
    public class ClinicalNote
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Author { get; set; } = string.Empty;

        [Required(ErrorMessage = "Treść notatki jest wymagana")]
        public string Content { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.Now;

        [Required]
        [ForeignKey("Visit")]
        public int VisitId { get; set; }
        public Visit? Visit { get; set; }
    }
}
