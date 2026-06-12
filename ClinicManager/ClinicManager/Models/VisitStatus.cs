using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Models
{
    public enum VisitStatus
    {
        [Display(Name = "Zaplanowana")]
        Scheduled,
        
        [Display(Name = "W trakcie")]
        InProgress,
        
        [Display(Name = "Zakończona")]
        Completed,
        
        [Display(Name = "Anulowana")]
        Cancelled
    }
}
