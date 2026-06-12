using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models
{
    public class MedicalDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string OriginalFileName { get; set; } = string.Empty;

        [Required]
        public string SavedFileName { get; set; } = string.Empty;

        public DateTime UploadDate { get; set; } = DateTime.Now;

        [Required]
        [ForeignKey("MedicalRecord")]
        public int MedicalRecordId { get; set; }

        public MedicalRecord? MedicalRecord { get; set; }
    }
}
