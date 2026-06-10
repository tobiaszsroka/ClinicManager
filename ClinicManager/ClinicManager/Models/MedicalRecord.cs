using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models
{
    public class MedicalRecord
    {
        [Key]
        public int Id { get; set; }

        public string? DocumentScanUrl { get; set; }
        
        public string? GeneralNotes { get; set; }

        // Klucz obcy i relacja 1:1 z pacjentem
        [Required]
        [ForeignKey("Patient")]
        public int PatientId { get; set; }
        
        public Patient? Patient { get; set; }
    }
}
