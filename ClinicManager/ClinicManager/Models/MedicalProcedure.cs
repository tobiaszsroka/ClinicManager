using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClinicManager.Models
{
    public class MedicalProcedure
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Nazwa procedury jest wymagana.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 100000, ErrorMessage = "Koszt musi być wartością dodatnią.")]
        public decimal BaseCost { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, 100000, ErrorMessage = "Zniżka musi być wartością dodatnią.")]
        public decimal Discount { get; set; } = 0;

        [NotMapped]
        public decimal FinalCost => BaseCost - Discount < 0 ? 0 : BaseCost - Discount;

        public int VisitId { get; set; }
        
        [ForeignKey("VisitId")]
        public Visit? Visit { get; set; }
    }
}
